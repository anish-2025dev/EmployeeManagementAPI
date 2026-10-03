namespace PerformanceTester.Models;

public class TestResult
{
    public double ResponseTimeMs { get; set; }

    public bool IsSuccess { get; set; }

    public int StatusCode { get; set; }

    public string? ErrorMessage { get; set; }
}