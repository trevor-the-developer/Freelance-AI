using System.ComponentModel.DataAnnotations;

namespace FreelanceAI.Core.Configuration;

public record RouterConfiguration
{
    [Range(0, double.MaxValue, ErrorMessage = "DailyBudget must be a positive value")]
    public decimal DailyBudget { get; init; } = 10.0m;

    [Range(1, 10, ErrorMessage = "MaxRetries must be between 1 and 10")]
    public int MaxRetries { get; init; } = 3;

    public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromMinutes(5);

    public bool EnableCostTracking { get; init; } = true;

    public bool EnableRateLimiting { get; init; } = true;

    public Dictionary<string, ProviderLimitConfiguration> ProviderLimits { get; init; } = new();
}