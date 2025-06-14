namespace FreelanceAI.Core.Models;

public record HealthResponse(
    string Status,
    int HealthyProviders,
    int TotalProviders,
    DateTime Timestamp
);