using PerformanceTester.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PerformanceTester.Services;

public class PerformanceTestService
{
    private readonly HttpClient _httpClient;

    public PerformanceTestService(string baseAddress)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseAddress),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<TestResult> RunSingleRequestAsync(
        string requestPath,
        string? accessToken = null)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                requestPath);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);
        }

        var stopwatch =
            Stopwatch.StartNew();

        try
        {
            using var response =
                await _httpClient.SendAsync(request);

            stopwatch.Stop();

            return new TestResult
            {
                ResponseTimeMs =
                    stopwatch.Elapsed.TotalMilliseconds,

                IsSuccess =
                    response.IsSuccessStatusCode,

                StatusCode =
                    (int)response.StatusCode
            };
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            return new TestResult
            {
                ResponseTimeMs =
                    stopwatch.Elapsed.TotalMilliseconds,

                IsSuccess = false,

                StatusCode = 0,

                ErrorMessage =
                    exception.Message
            };
        }
    }

    public async Task<string> LoginAsync(
        string username,
        string password)
    {
        var loginRequest = new
        {
            Username = username,
            Password = password
        };

        using var response =
            await _httpClient.PostAsJsonAsync(
                "/api/Auth/login",
                loginRequest);

        var responseContent =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Login failed. " +
                $"Status: {(int)response.StatusCode}. " +
                $"Response: {responseContent}");
        }

        using var document =
            JsonDocument.Parse(responseContent);

        var token =
            FindToken(document.RootElement);

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Login succeeded, but no token property was found. " +
                $"Response: {responseContent}");
        }

        return token;
    }

    private static string? FindToken(
        JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Value.ValueKind ==
                    JsonValueKind.String)
                {
                    var propertyName =
                        property.Name;

                    if (propertyName.Equals(
                            "token",
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        propertyName.Equals(
                            "accessToken",
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        propertyName.Equals(
                            "jwtToken",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return property.Value.GetString();
                    }
                }

                var nestedToken =
                    FindToken(property.Value);

                if (!string.IsNullOrWhiteSpace(nestedToken))
                {
                    return nestedToken;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nestedToken =
                    FindToken(item);

                if (!string.IsNullOrWhiteSpace(nestedToken))
                {
                    return nestedToken;
                }
            }
        }

        return null;
    }
}