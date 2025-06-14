namespace FreelanceAI.Core.Models;

public record ProviderConfig(
    bool Enabled,
    int Priority,
    int RateLimit,
    string RateLimitWindow,
    decimal CostPerToken
);