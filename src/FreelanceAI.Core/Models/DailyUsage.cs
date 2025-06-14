namespace FreelanceAI.Core.Models;

public record DailyUsage(
    string Provider,
    string Date,
    int RequestCount,
    int TokensUsed,
    decimal TotalCost
);
