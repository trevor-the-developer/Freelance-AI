namespace FreelanceAI.Core.Models;

public record AIFailure(
    string Error,
    string[] FailedProviders,
    decimal TotalAttemptedCost,
    TimeSpan Duration
) : AIResponse(Duration);