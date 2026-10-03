using PerformanceTester.Services;
using PerformanceTester.Tests;

var service =
    new PerformanceTestService();

Console.WriteLine();
Console.WriteLine("==== Performance Tester ====");
Console.WriteLine();
Console.WriteLine("1. Single Request Test");
Console.WriteLine("2. Controlled Load Test");
Console.WriteLine("3. Spike Test");
Console.WriteLine("4. Delayed Burst Test");
Console.WriteLine("5. Rate Limiter Test");
Console.WriteLine();

Console.Write("Select Test: ");

var choice =
    Console.ReadLine();

Console.WriteLine();

switch (choice)
{
    case "1":
        await SingleRequestTest.RunAsync(
            service,
            count: 5);
        break;

    case "2":
        await ControlledLoadTest.RunAsync(
            service);
        break;

    case "3":
        await SpikeTest.RunAsync(
            service,
            totalRequests: 1000);
        break;

    case "4":
        await DelayedBurstTest.RunAsync(
            service,
            burstSize: 10,
            delayMs: 10,
            totalRequests: 1000);
        break;

    case "5":
        await RateLimiterTest.RunAsync(
            service,
            requestCount: 1000);
        break;

    default:
        Console.WriteLine(
            "Invalid Selection.");
        break;
}

Console.WriteLine();
Console.WriteLine("Press Enter to Exit...");
Console.ReadLine();