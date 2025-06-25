namespace FreelanceAI.Core.Models;

public record AIResponseEntry(
    Guid Id,
    DateTime Timestamp,
    string Prompt,
    int? MaxTokens,
    decimal? Temperature,
    string? Model,
    bool Success,
    string? Provider,
    string? Content,
    string? Error,
    decimal Cost,
    double Duration
);