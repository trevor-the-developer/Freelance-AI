namespace FreelanceAI.Core.Models;

public record RoutingResult
{
    public List<string> FailedProviders { get; init; } = new();
    public List<AIResponseEntry> ResponseEntries { get; init; } = new();
    public decimal TotalCost { get; private set; }
    public int TotalAttempts { get; private set; }

    public void AddAttempt(ProviderAttemptResult attempt)
    {
        TotalAttempts++;
        TotalCost += attempt.Cost;
        ResponseEntries.Add(attempt.ResponseEntry);
        
        if (!attempt.IsSuccess)
        {
            FailedProviders.Add(attempt.ProviderName);
        }
    }
}