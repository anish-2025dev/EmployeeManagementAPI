using PerformanceTester.Models;
using PerformanceTester.Services;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PerformanceTester.Tests;

public static class ControlledLoadTest
{
    private const int ConcurrentWorkers = 100;
    private const int RequestsPerWorker = 10;

    public static async Task RunAsync(PerformanceTestService service)
    {
        Console.WriteLine(
            "Starting controlled performance test...");

        Console.WriteLine(
            $"Workers: {ConcurrentWorkers}");

        Console.WriteLine(
            $"Requests per worker: {RequestsPerWorker}");

        Console.WriteLine(
            $"Total requests: {ConcurrentWorkers * RequestsPerWorker}");

        Console.WriteLine(
            "Sending warm-up request...");

        await service.RunSingleRequestAsync();

        var results =
            new ConcurrentBag<TestResult>();

        var overallStopwatch =
            Stopwatch.StartNew();

        var workers =
            Enumerable.Range(0, ConcurrentWorkers)
                .Select(async _ =>
                {
                    for (var request = 0;
                         request < RequestsPerWorker;
                         request++)
                    {
                        var result =
                            await service.RunSingleRequestAsync();

                        results.Add(result);
                    }
                });

        await Task.WhenAll(workers);

        overallStopwatch.Stop();

        var resultArray =
            results.ToArray();

        var responseTimes =
            resultArray
                .Select(result => result.ResponseTimeMs)
                .OrderBy(time => time)
                .ToArray();

        var successes =
            resultArray.Count(result => result.IsSuccess);

        var failures =
            resultArray.Length - successes;

        var throughput =
            resultArray.Length /
            overallStopwatch.Elapsed.TotalSeconds;

        Console.WriteLine();

        Console.WriteLine(
            "Controlled Performance Test Results");

        Console.WriteLine(
            "-----------------------------------");

        Console.WriteLine(
            $"Requests: {resultArray.Length}");

        Console.WriteLine(
            $"Successful: {successes}");

        Console.WriteLine(
            $"Failed: {failures}");

        Console.WriteLine(
            $"Batch Duration: {overallStopwatch.ElapsedMilliseconds} ms");

        Console.WriteLine(
            $"Throughput: {throughput:F2} requests/second");

        Console.WriteLine(
            $"Average: {responseTimes.Average():F2} ms");

        Console.WriteLine(
            $"Minimum: {responseTimes.Min()} ms");

        Console.WriteLine(
            $"Maximum: {responseTimes.Max()} ms");

        Console.WriteLine(
            $"p50: {Percentile(responseTimes, 0.50):F2} ms");

        Console.WriteLine(
            $"p95: {Percentile(responseTimes, 0.95):F2} ms");

        Console.WriteLine(
            $"p99: {Percentile(responseTimes, 0.99):F2} ms");
    }

    private static double Percentile(
        long[] sortedValues,
        double percentile)
    {
        if (sortedValues.Length == 0)
        {
            return 0;
        }

        var position =
            (sortedValues.Length - 1) * percentile;

        var lowerIndex =
            (int)Math.Floor(position);

        var upperIndex =
            (int)Math.Ceiling(position);

        if (lowerIndex == upperIndex)
        {
            return sortedValues[lowerIndex];
        }

        var weight =
            position - lowerIndex;

        return sortedValues[lowerIndex]
            + ((sortedValues[upperIndex]
                - sortedValues[lowerIndex]) * weight);
    }
}