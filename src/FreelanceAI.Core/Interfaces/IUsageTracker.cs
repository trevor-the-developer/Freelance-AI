using FreelanceAI.Core.Models;

namespace FreelanceAI.Core.Interfaces;

public interface IUsageTracker
{
    Task<DailyUsage> GetTodayUsageAsync(string provider);
    Task RecordUsageAsync(string provider, int tokens, decimal cost);
    Task<WeeklyReport> GenerateWeeklyReportAsync();
    Task<bool> CheckBudgetLimitAsync(string provider, decimal additionalCost);
}