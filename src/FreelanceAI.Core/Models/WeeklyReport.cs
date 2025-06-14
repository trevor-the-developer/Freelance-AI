namespace FreelanceAI.Core.Models;

public record WeeklyReport(
    DateTime StartDate,
    DateTime EndDate,
    Dictionary<string, List<DailyUsage>> ProviderUsage,
    decimal TotalCost,
    int TotalRequests
);