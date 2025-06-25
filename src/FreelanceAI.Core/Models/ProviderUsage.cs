namespace FreelanceAI.Core.Models;

public record ProviderUsage(
    string ProviderName,
    int RequestCount,
    decimal TotalCost,
    DateTime Date
);