using PerformanceTester.Services;

namespace PerformanceTester.Tests;

public static class RateLimiterTest
{
    public static async Task RunAsync(
        PerformanceTestService service,
        int requestCount)
    {
        Console.WriteLine(
            "Starting rate limit test...");

        var tasks =
            Enumerable.Range(0, requestCount)
                .Select(_ =>
                    service.RunSingleRequestAsync());

        var results =
            await Task.WhenAll(tasks);

        Console.WriteLine(
            $"429 Responses: {results.Count(x => x.StatusCode == 429)}");

        Console.WriteLine(
            $"Success Responses: {results.Count(x => x.IsSuccess)}");
    }
}