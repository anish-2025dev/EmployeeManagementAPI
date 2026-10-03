using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace EmployeeManagementApp.Tests.Integration
{
    public class SecurityTests
        : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        /*
         * ============================================================
         * CHANGE ONLY THIS CONFIGURATION SECTION IF YOUR VALUES DIFFER
         * ============================================================
         */

        private const string LoginEndpoint = "/api/Auth/login";
        private const string RefreshEndpoint = "/api/Auth/refresh-token";

        // Use any endpoint protected with [Authorize].
        private const string ProtectedEmployeeEndpoint = "/api/Employee";

        // Use an endpoint protected with [Authorize(Roles = "Admin")].
        // Replace 1 with an employee ID if required.
        private const string AdminOnlyEndpoint = "/api/Employee/1";

        // Replace these values with users available in your test database.
        private const string AdminUserName = "anand";
        private const string AdminPassword = "ananda";

        private const string NormalUserName = "akhilesh";
        private const string NormalUserPassword = "123456";

        /*
         * Your LoginResponseDTO was previously shown with Token,
         * UserName and Role. RefreshToken was added later.
         *
         * JSON property matching below is case-insensitive, so it works
         * with both:
         *
         * Token / token
         * RefreshToken / refreshToken
         */

        public SecurityTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        // ============================================================
        // TEST 1: TAMPERED JWT
        // ============================================================

        [Fact]
        public async Task ProtectedEndpoint_WithTamperedJwt_ReturnsUnauthorized()
        {
            // Arrange
            using HttpClient client = _factory.CreateClient();

            AuthTokens tokens = await LoginAsync(
                client,
                AdminUserName,
                AdminPassword);

            string tamperedToken = TamperJwtPayload(tokens.AccessToken);

            SetBearerToken(client, tamperedToken);

            // Act
            HttpResponseMessage response =
                await client.GetAsync(ProtectedEmployeeEndpoint);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // TEST 2: EXPIRED JWT
        // ============================================================

        [Fact]
        public async Task ProtectedEndpoint_WithExpiredJwt_ReturnsUnauthorized()
        {
            // Arrange
            using HttpClient client = _factory.CreateClient();

            string expiredToken = CreateExpiredJwt();

            SetBearerToken(client, expiredToken);

            // Act
            HttpResponseMessage response =
                await client.GetAsync(ProtectedEmployeeEndpoint);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ============================================================
        // TEST 3: INVALID REFRESH TOKEN
        // ============================================================

        [Fact]
        public async Task Refresh_WithInvalidRefreshToken_ReturnsFailureStatus()
        {
            // Arrange
            using HttpClient client = _factory.CreateClient();

            var request = new
            {
                Token = "this-is-not-a-valid-access-token",
                RefreshToken = "this-is-not-a-valid-refresh-token"
            };

            // Act
            HttpResponseMessage response =
                await client.PostAsJsonAsync(RefreshEndpoint, request);

            // Assert
            Assert.Contains(
                response.StatusCode,
                new[]
                {
                    HttpStatusCode.BadRequest,
                    HttpStatusCode.Unauthorized
                });
        }

        // ============================================================
        // TEST 4: REFRESH TOKEN REUSE
        // ============================================================

        [Fact]
        public async Task Refresh_WhenOldRefreshTokenIsReused_ReturnsFailureStatus()
        {
            // Arrange
            using HttpClient client = _factory.CreateClient();

            AuthTokens originalTokens = await LoginAsync(
                client,
                AdminUserName,
                AdminPassword);

            var firstRefreshRequest = new
            {
                Token = originalTokens.AccessToken,
                RefreshToken = originalTokens.RefreshToken
            };

            // Act 1:
            // Use RefreshToken_A for the first time.
            HttpResponseMessage firstRefreshResponse =
                await client.PostAsJsonAsync(
                    RefreshEndpoint,
                    firstRefreshRequest);

            // The first refresh must succeed.
            Assert.Equal(
                HttpStatusCode.OK,
                firstRefreshResponse.StatusCode);

            AuthTokens rotatedTokens =
                await ReadTokensAsync(firstRefreshResponse);

            Assert.False(
                string.IsNullOrWhiteSpace(rotatedTokens.AccessToken),
                "The first refresh did not return a new access token.");

            Assert.False(
                string.IsNullOrWhiteSpace(rotatedTokens.RefreshToken),
                "The first refresh did not return a new refresh token.");

            Assert.NotEqual(
                originalTokens.RefreshToken,
                rotatedTokens.RefreshToken);

            // Act 2:
            // Reuse the old RefreshToken_A.
            HttpResponseMessage reuseResponse =
                await client.PostAsJsonAsync(
                    RefreshEndpoint,
                    firstRefreshRequest);

            // Assert:
            // Old refresh token should no longer be accepted.
            Assert.Contains(
                reuseResponse.StatusCode,
                new[]
                {
                    HttpStatusCode.BadRequest,
                    HttpStatusCode.Unauthorized
                });
        }

        // ============================================================
        // HELPER: LOGIN
        // ============================================================

        private static async Task<AuthTokens> LoginAsync(
            HttpClient client,
            string userName,
            string password)
        {
            /*
             * If your LoginDTO uses Email instead of UserName,
             * change UserName below to Email.
             */

            var loginRequest = new
            {
                UserName = userName,
                Password = password
            };

            HttpResponseMessage response =
                await client.PostAsJsonAsync(
                    LoginEndpoint,
                    loginRequest);

            string responseBody =
                await response.Content.ReadAsStringAsync();

            Assert.True(
                response.IsSuccessStatusCode,
                $"""
                Login failed.

                Status code: {(int)response.StatusCode}
                Response body: {responseBody}

                Check:
                1. LoginEndpoint
                2. Login request property names
                3. Test username
                4. Test password
                5. Test database seeding
                """);

            return ReadTokens(responseBody);
        }

        // ============================================================
        // HELPER: READ TOKENS FROM HTTP RESPONSE
        // ============================================================

        private static async Task<AuthTokens> ReadTokensAsync(
            HttpResponseMessage response)
        {
            string responseBody =
                await response.Content.ReadAsStringAsync();

            return ReadTokens(responseBody);
        }

        // ============================================================
        // HELPER: READ TOKEN JSON
        // ============================================================

        private static AuthTokens ReadTokens(string responseBody)
        {
            using JsonDocument document =
                JsonDocument.Parse(responseBody);

            JsonElement root = document.RootElement;

            string accessToken =
                GetRequiredStringProperty(
                    root,
                    "token",
                    "accessToken");

            string refreshToken =
                GetRequiredStringProperty(
                    root,
                    "refreshToken");

            return new AuthTokens(
                accessToken,
                refreshToken);
        }


        //[Fact]
        //public async Task AdminEndpoint_WithNormalUserToken_ReturnsForbidden()
        //{
        //    // Arrange
        //    using HttpClient client = _factory.CreateClient();

        //    AuthTokens normalUserTokens = await LoginAsync(
        //        client,
        //        NormalUserName,
        //        NormalUserPassword);

        //    Assert.False(
        //        string.IsNullOrWhiteSpace(normalUserTokens.AccessToken),
        //        "Login succeeded but no access token was returned.");

        //    JwtSecurityTokenHandler tokenHandler =
        //        new JwtSecurityTokenHandler();

        //    Assert.True(
        //        tokenHandler.CanReadToken(normalUserTokens.AccessToken),
        //        "The returned access token is not a readable JWT.");

        //    JwtSecurityToken jwt =
        //        tokenHandler.ReadJwtToken(normalUserTokens.AccessToken);

        //    string allClaims = string.Join(
        //        Environment.NewLine,
        //        jwt.Claims.Select(
        //            claim => $"{claim.Type} = {claim.Value}"));

        //    Console.WriteLine("Normal-user JWT claims:");
        //    Console.WriteLine(allClaims);

        //    SetBearerToken(
        //        client,
        //        normalUserTokens.AccessToken);

        //    // First verify that the same token can access a normal
        //    // authenticated endpoint.
        //    HttpResponseMessage authenticatedResponse =
        //        await client.GetAsync(ProtectedEmployeeEndpoint);

        //    string authenticatedBody =
        //        await authenticatedResponse.Content.ReadAsStringAsync();

        //    Assert.True(
        //        authenticatedResponse.IsSuccessStatusCode,
        //        $"""
        //The normal user's JWT was not accepted by a regular
        //authenticated endpoint.

        //Endpoint: {ProtectedEmployeeEndpoint}
        //Expected: Successful response
        //Actual: {(int)authenticatedResponse.StatusCode}
        //        {authenticatedResponse.StatusCode}
        //Response: {authenticatedBody}

        //JWT claims:
        //{allClaims}
        //""");

        //    // Now use the same valid token on the Admin-only endpoint.
        //    HttpResponseMessage adminResponse =
        //        await client.DeleteAsync(AdminOnlyEndpoint);

        //    string adminResponseBody =
        //        await adminResponse.Content.ReadAsStringAsync();

        //    Assert.True(
        //        adminResponse.StatusCode == HttpStatusCode.Forbidden,
        //        $"""
        //The normal user was not handled as an authenticated,
        //non-Admin user.

        //Endpoint: {AdminOnlyEndpoint}
        //Expected: 403 Forbidden
        //Actual: {(int)adminResponse.StatusCode}
        //        {adminResponse.StatusCode}
        //Response: {adminResponseBody}

        //JWT claims:
        //{allClaims}
        //""");
        //}

        // ============================================================
        // HELPER: CASE-INSENSITIVE JSON PROPERTY SEARCH
        // ============================================================

        private static string GetRequiredStringProperty(
            JsonElement root,
            params string[] possiblePropertyNames)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                bool propertyMatches =
                    possiblePropertyNames.Any(
                        expectedName =>
                            string.Equals(
                                property.Name,
                                expectedName,
                                StringComparison.OrdinalIgnoreCase));

                if (propertyMatches)
                {
                    string? value = property.Value.GetString();

                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }

            throw new Xunit.Sdk.XunitException(
                $"""
                Required JSON property was not found.

                Expected one of:
                {string.Join(", ", possiblePropertyNames)}

                Actual response:
                {root.GetRawText()}
                """);
        }

        // ============================================================
        // HELPER: ADD BEARER TOKEN
        // ============================================================

        private static void SetBearerToken(
            HttpClient client,
            string token)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }

        // ============================================================
        // HELPER: TAMPER JWT
        // ============================================================

        private static string TamperJwtPayload(string originalToken)
        {
            string[] tokenParts = originalToken.Split('.');

            Assert.Equal(3, tokenParts.Length);

            string payload = tokenParts[1];

            Assert.False(
                string.IsNullOrWhiteSpace(payload),
                "JWT payload was empty.");

            char finalCharacter = payload[^1];

            char replacementCharacter =
                finalCharacter == 'A'
                    ? 'B'
                    : 'A';

            string tamperedPayload =
                payload[..^1] + replacementCharacter;

            /*
             * We alter the payload but keep the original signature.
             *
             * Original:
             * header.originalPayload.originalSignature
             *
             * Tampered:
             * header.changedPayload.originalSignature
             *
             * Because the signature does not match the changed payload,
             * JWT validation should reject it.
             */

            return
                $"{tokenParts[0]}." +
                $"{tamperedPayload}." +
                $"{tokenParts[2]}";
        }

        // ============================================================
        // HELPER: CREATE GENUINELY EXPIRED JWT
        // ============================================================

        private string CreateExpiredJwt()
        {
            IConfiguration configuration =
                _factory.Services.GetRequiredService<IConfiguration>();

            string jwtKey = GetRequiredConfigurationValue(
                configuration,
                "Jwt:Key",
                "JwtSettings:Key",
                "Jwt:SecretKey",
                "JwtSettings:SecretKey");

            string issuer = GetRequiredConfigurationValue(
                configuration,
                "Jwt:Issuer",
                "JwtSettings:Issuer");

            string audience = GetRequiredConfigurationValue(
                configuration,
                "Jwt:Audience",
                "JwtSettings:Audience");

            SymmetricSecurityKey securityKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey));

            SigningCredentials signingCredentials =
                new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms.HmacSha256);

            Claim[] claims =
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    "expired-security-test-user"),

                new Claim(
                    ClaimTypes.Name,
                    "expired-security-test-user"),

                new Claim(
                    ClaimTypes.Role,
                    "Admin"),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

            DateTime now = DateTime.UtcNow;

            JwtSecurityToken expiredJwt =
                new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,

                    // Token became valid 10 minutes ago.
                    notBefore: now.AddMinutes(-10),

                    // Token expired 5 minutes ago.
                    expires: now.AddMinutes(-5),

                    signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(expiredJwt);
        }

        // ============================================================
        // HELPER: READ JWT CONFIGURATION
        // ============================================================

        private static string GetRequiredConfigurationValue(
            IConfiguration configuration,
            params string[] possibleKeys)
        {
            foreach (string key in possibleKeys)
            {
                string? value = configuration[key];

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            throw new Xunit.Sdk.XunitException(
                $"""
                JWT configuration was not found.

                Checked these keys:
                {string.Join(", ", possibleKeys)}

                Update GetRequiredConfigurationValue calls so they match
                the JWT section in your appsettings.json.
                """);
        }

        // ============================================================
        // INTERNAL TEST MODEL
        // ============================================================

        private sealed record AuthTokens(
            string AccessToken,
            string RefreshToken);
    }
}