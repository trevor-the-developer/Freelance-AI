using FreelanceAI.Core.Models;

namespace FreelanceAI.Core.Configuration;

public class RouterConfiguration
{
    public decimal DailyBudget { get; set; } = 5.00m;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public bool EnableCaching { get; set; } = true;
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new();
}