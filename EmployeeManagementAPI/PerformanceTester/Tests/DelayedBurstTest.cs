using PerformanceTester.Models;
using PerformanceTester.Services;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PerformanceTester.Tests;

public static class DelayedBurstTest
{
    public static async Task RunAsync(
        PerformanceTestService service,
        int burstSize,
        int delayMs,
        int totalRequests)
    {
        Console.WriteLine(
            "Starting delayed burst test...");

        Console.WriteLine(
            $"Burst Size: {burstSize}");

        Console.WriteLine(
            $"Delay: {delayMs} ms");

        Console.WriteLine(
            $"Total Requests: {totalRequests}");

        Console.WriteLine(
            "Sending warm-up request...");

        await service.RunSingleRequestAsync();

        var results =
            new ConcurrentBag<TestResult>();

        var stopwatch =
            Stopwatch.StartNew();

        var requestsSent = 0;

        while (requestsSent < totalRequests)
        {
            var currentBurstSize =
                Math.Min(
                    burstSize,
                    totalRequests - requestsSent);

            var burstTasks =
                Enumerable.Range(0, currentBurstSize)
                    .Select(async _ =>
                    {
                        var result =
                            await service.RunSingleRequestAsync();

                        results.Add(result);
                    });

            await Task.WhenAll(burstTasks);

            requestsSent += currentBurstSize;

            await Task.Delay(delayMs);
        }

        stopwatch.Stop();

        PrintResults(
            results.ToArray(),
            stopwatch.ElapsedMilliseconds);
    }

    private static void PrintResults(
        TestResult[] results,
        long batchDuration)
    {
        var responseTimes =
            results
                .Select(x => x.ResponseTimeMs)
                .OrderBy(x => x)
                .ToArray();

        var successful =
            results.Count(x => x.IsSuccess);

        var failed =
            results.Length - successful;

        var throughput =
            results.Length /
            (batchDuration / 1000.0);

        Console.WriteLine();
        Console.WriteLine(
            "Delayed Burst Test Results");
        Console.WriteLine(
            "--------------------------");

        Console.WriteLine(
            $"Requests: {results.Length}");

        Console.WriteLine(
            $"Successful: {successful}");

        Console.WriteLine(
            $"Failed: {failed}");

        Console.WriteLine(
            $"Batch Duration: {batchDuration} ms");

        Console.WriteLine(
            $"Throughput: {throughput:F2} req/sec");

        Console.WriteLine(
            $"Average: {responseTimes.Average():F2} ms");

        Console.WriteLine(
            $"Minimum: {responseTimes.Min()} ms");

        Console.WriteLine(
            $"Maximum: {responseTimes.Max()} ms");
    }
}
