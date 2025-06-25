using System.ComponentModel.DataAnnotations;

namespace FreelanceAI.Core.Configuration;

public record ProviderLimitConfiguration
{
    [Range(0, int.MaxValue, ErrorMessage = "RequestLimit must be a non-negative value")]
    public int RequestLimit { get; init; } = 0;
    
    [Required]
    public string LimitType { get; init; } = "Day"; // Hour, Day, Month, Unlimited
    
    [Range(0, double.MaxValue, ErrorMessage = "CostPerToken must be a non-negative value")]
    public decimal CostPerToken { get; init; } = 0.0m;
    
    [Range(0, double.MaxValue, ErrorMessage = "DailyBudgetLimit must be a non-negative value")]
    public decimal DailyBudgetLimit { get; init; } = 1.0m;
}