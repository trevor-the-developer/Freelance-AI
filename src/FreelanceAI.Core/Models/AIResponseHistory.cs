namespace FreelanceAI.Core.Models;

public record AIResponseHistory(
    List<AIResponseEntry> Responses,
    DateTime LastUpdated,
    int TotalRequests,
    decimal TotalCost
)
{
    // Parameterless constructor for JSON deserialization
    public AIResponseHistory() : this(new List<AIResponseEntry>(), DateTime.UtcNow, 0, 0m) { }
}