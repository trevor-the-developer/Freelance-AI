namespace FreelanceAI.Core.Models;

public record AIRequestOptions(
    int MaxTokens,
    decimal Temperature,
    string Model,
    List<string> StopSequences
);