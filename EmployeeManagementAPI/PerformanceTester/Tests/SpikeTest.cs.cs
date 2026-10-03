using PerformanceTester.Models;
using PerformanceTester.Services;
using System.Diagnostics;

namespace PerformanceTester.Tests;

public static class SpikeTest
{
    public static async Task RunAsync(
        PerformanceTestService service,
        int totalRequests)
    {
        Console.WriteLine(
            "Starting spike test...");

        await service.RunSingleRequestAsync();

        var stopwatch =
            Stopwatch.StartNew();

        var tasks =
            Enumerable.Range(0, totalRequests)
                .Select(_ =>
                    service.RunSingleRequestAsync());

        var results =
            await Task.WhenAll(tasks);

        stopwatch.Stop();

        Console.WriteLine(
            $"Total Requests: {results.Length}");

        Console.WriteLine(
            $"Duration: {stopwatch.ElapsedMilliseconds} ms");

        Console.WriteLine(
            $"Average: {results.Average(x => x.ResponseTimeMs):F2} ms");
    }
}