using FreelanceAI.Core.Models;

namespace FreelanceAI.Core.Interfaces;

public interface ISmartApiRouter
{
    Task<AIResponse> RouteRequestAsync(string prompt, AIRequestOptions options);
    Task<List<ProviderStatus>> GetProviderStatusAsync();
    Task<decimal> GetTodaySpendAsync();
}