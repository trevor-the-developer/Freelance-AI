namespace FreelanceAI.Core.Models;

public record DailyUsageSummary(
    DateTime Date,
    decimal TotalCost,
    int TotalRequests,
    Dictionary<string, ProviderUsage> ProviderBreakdown
);