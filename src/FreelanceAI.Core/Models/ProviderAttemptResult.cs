namespace FreelanceAI.Core.Models;

public record ProviderAttemptResult(
    bool IsSuccess,
    AIResponse Response,
    AIResponseEntry ResponseEntry,
    string ProviderName,
    decimal Cost
);