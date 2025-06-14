using FreelanceAI.Core.Models;

namespace FreelanceAI.Core.Interfaces;

public interface IAIProvider
{
    string Name { get; }
    int Priority { get; }
    decimal CostPerToken { get; }
    bool IsAvailable { get; }
    Task<string> GenerateAsync(string prompt, AIRequestOptions options);
    Task<bool> CheckHealthAsync();
}