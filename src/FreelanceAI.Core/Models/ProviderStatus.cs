namespace FreelanceAI.Core.Models;

public record ProviderStatus(
    string Name,
    bool IsHealthy,
    int RequestsToday,
    decimal CostToday,
    int RemainingRequests
);
