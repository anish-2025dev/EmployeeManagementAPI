using PerformanceTester.Services;

namespace PerformanceTester.Tests;

public static class SingleRequestTest
{
    public static async Task RunAsync(
        PerformanceTestService service,
        int count)
    {
        Console.WriteLine(
            "Starting single request test...");

        for (int i = 1; i <= count; i++)
        {
            var result =
                await service.RunSingleRequestAsync();

            Console.WriteLine(
                $"Request {i}: {result.ResponseTimeMs} ms");
        }
    }
}