using System.Net;
using Xunit;
using System.Net.Http.Json;
using EmployeeManagementApp.DTOs;
namespace EmployeeManagementApp.Tests.Integration
{
    public class AuthApiTests
    : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthApiTests(
            CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }



        [Fact]
        public async Task RegisterUser_WhenValidData_ReturnsSuccess()
        {
            var registerDto = new RegisterUserDTO
            {
                UserName = "integration_user",
                Password = "Password@123"
            };

            var response =
                await _client.PostAsJsonAsync(
                    "/api/Auth/register",
                    registerDto);

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task LoginUser_WhenCredentialsAreValid_ReturnsToken()
        {

            var registerDto = new RegisterUserDTO
            {
                UserName = "login_test_user",
                Password = "Password@123"
            };

            await _client.PostAsJsonAsync(
                "/api/Auth/register",
                registerDto);

            var loginDto = new LoginUserDTO
            {
                UserName = "login_test_user",
                Password = "Password@123"
            };

            var response =
                await _client.PostAsJsonAsync(
                    "/api/Auth/login",
                    loginDto);

            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            var result =
                await response.Content
                    .ReadFromJsonAsync<LoginResponseDTO>();

            Assert.NotNull(result);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.Token));

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.RefreshToken));
        }

        [Fact]
        public async Task LoginUser_WhenPasswordIsInvalid_ReturnsUnauthorized()
        {

            var registerDto = new RegisterUserDTO
            {
                UserName = "invalid_login_user",
                Password = "Password@123"
            };

            await _client.PostAsJsonAsync(
                "/api/Auth/register",
                registerDto);

            var loginDto = new LoginUserDTO
            {
                UserName = "invalid_login_user",
                Password = "WrongPassword"
            };

            var response =
                await _client.PostAsJsonAsync(
                    "/api/Auth/login",
                    loginDto);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        [Fact]
        public async Task RegisterUser_WhenUsernameAlreadyExists_ReturnsError()
        {
            // Arrange
            var registerDto = new RegisterUserDTO
            {
                UserName = "duplicate_user",
                Password = "Password@123"
            };

            await _client.PostAsJsonAsync(
                "/api/Auth/register",
                registerDto);

            // Act
            var response =
                await _client.PostAsJsonAsync(
                    "/api/Auth/register",
                    registerDto);

            // Assert
            Assert.False(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task RefreshToken_WhenTokenIsValid_ReturnsNewTokens()
        {
            // Arrange

            var registerDto = new RegisterUserDTO
            {
                UserName = "refresh_user",
                Password = "Password@123"
            };

            await _client.PostAsJsonAsync(
                "/api/Auth/register",
                registerDto);

            var loginResponse =
                await _client.PostAsJsonAsync(
                    "/api/Auth/login",
                    new LoginUserDTO
                    {
                        UserName = "refresh_user",
                        Password = "Password@123"
                    });

            var loginResult =
                await loginResponse.Content
                    .ReadFromJsonAsync<LoginResponseDTO>();

            // Act

            var refreshResponse =
                await _client.PostAsJsonAsync(
                    "/api/Auth/refresh-token",
                    new RefreshTokenRequestDTO
                    {
                        RefreshToken =
                            loginResult!.RefreshToken
                    });

            // Assert

            Assert.Equal(
                HttpStatusCode.OK,
                refreshResponse.StatusCode);

            var result =
                await refreshResponse.Content
                    .ReadFromJsonAsync<LoginResponseDTO>();

            Assert.NotNull(result);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.Token));

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.RefreshToken));
        }

        [Fact]
        public async Task RefreshToken_WhenTokenIsInvalid_ReturnsUnauthorized()
        {
            // Arrange

            var request =
                new RefreshTokenRequestDTO
                {
                    RefreshToken =
                        "invalid-refresh-token"
                };

            // Act

            var response =
                await _client.PostAsJsonAsync(
                    "/api/Auth/refresh-token",
                    request);

            // Assert

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }

        [Fact]
        public async Task LoginUser_AfterFiveFailedAttempts_IsLockedOut()
        {
            // Arrange

            var registerDto = new RegisterUserDTO
            {
                UserName = "lockout_user",
                Password = "Password@123"
            };

            await _client.PostAsJsonAsync(
                "/api/Auth/register",
                registerDto);

            // Act

            for (int i = 0; i < 5; i++)
            {
                await _client.PostAsJsonAsync(
                    "/api/Auth/login",
                    new LoginUserDTO
                    {
                        UserName = "lockout_user",
                        Password = "WrongPassword"
                    });
            }

            var response =
                await _client.PostAsJsonAsync(
                    "/api/Auth/login",
                    new LoginUserDTO
                    {
                        UserName = "lockout_user",
                        Password = "Password@123"
                    });

            // Assert

            Assert.False(
                response.IsSuccessStatusCode);
        }
    }
}