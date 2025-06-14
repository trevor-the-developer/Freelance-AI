using System.Linq; // Add this for LINQ methods
using FreelanceAI.Core.Configuration;
using FreelanceAI.Core.Interfaces;
using FreelanceAI.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreelanceAI.ApiRouter;

public class SmartApiRouter : ISmartApiRouter
{
    private readonly ILogger<SmartApiRouter> _logger;
    private readonly List<IAIProvider> _providers;
    private readonly IUsageTracker _usageTracker;
    private readonly RouterConfiguration _config;

    public SmartApiRouter(
        ILogger<SmartApiRouter> logger,
        IEnumerable<IAIProvider> providers,
        IUsageTracker usageTracker,
        IOptions<RouterConfiguration> config)
    {
        _logger = logger;
        _providers = providers.OrderBy(p => p.Priority).ToList();
        _usageTracker = usageTracker;
        _config = config.Value;
    }

    public async Task<AIResponse> RouteRequestAsync(string prompt, AIRequestOptions options)
    {
        var context = new RequestContext(prompt, options, DateTime.UtcNow);
        var failedProviders = new List<string>();
        var totalAttemptedCost = 0m;
    
        foreach (var provider in _providers)
        {
            try
            {
                if (!await IsProviderViable(provider, context))
                    continue;

                _logger.LogInformation("🚀 Routing request to {Provider}", provider.Name);

                var response = await provider.GenerateAsync(prompt, options);
                var tokenCount = EstimateTokens(prompt + response);
                var estimatedCost = EstimateCost(prompt + response, provider.CostPerToken);
            
                await _usageTracker.RecordUsageAsync(provider.Name, tokenCount, estimatedCost);
                totalAttemptedCost += estimatedCost;

                return new AISuccess(
                    Content: response,
                    Provider: provider.Name,
                    RequestCost: estimatedCost,
                    Duration: DateTime.UtcNow - context.StartTime
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Provider {Provider} failed: {Error}", provider.Name, ex.Message);
                failedProviders.Add(provider.Name);
            
                // Optional: Add estimated cost for failed attempts if the provider charges for failures
                // var failureCost = EstimateFailureCost(prompt, provider.CostPerToken);
                // totalAttemptedCost += failureCost;
            }
        }

        return new AIFailure(
            Error: "All AI providers exhausted or unavailable",
            FailedProviders: failedProviders.ToArray(),
            TotalAttemptedCost: totalAttemptedCost,
            Duration: DateTime.UtcNow - context.StartTime
        );
    }

    public async Task<List<ProviderStatus>> GetProviderStatusAsync()
    {
        var statusList = new List<ProviderStatus>();

        foreach (var provider in _providers)
        {
            try
            {
                var isHealthy = await provider.CheckHealthAsync();
                var usage = await _usageTracker.GetTodayUsageAsync(provider.Name);
                var limit = GetProviderLimit(provider.Name);

                statusList.Add(new ProviderStatus(
                    Name: provider.Name,
                    IsHealthy: isHealthy,
                    RequestsToday: usage.RequestCount,
                    CostToday: usage.TotalCost,
                    RemainingRequests: Math.Max(0, limit - usage.RequestCount)
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for provider {Provider}", provider.Name);
                statusList.Add(new ProviderStatus(
                    Name: provider.Name,
                    IsHealthy: false,
                    RequestsToday: 0,
                    CostToday: 0,
                    RemainingRequests: 0
                ));
            }
        }

        return statusList;
    }

    public async Task<decimal> GetTodaySpendAsync()
    {
        decimal totalSpend = 0;

        foreach (var provider in _providers)
        {
            try
            {
                var usage = await _usageTracker.GetTodayUsageAsync(provider.Name);
                totalSpend += usage.TotalCost;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting spend for provider {Provider}", provider.Name);
            }
        }

        return totalSpend;
    }

    private async Task<bool> IsProviderViable(IAIProvider provider, RequestContext context)
    {
        // Check health
        if (!await provider.CheckHealthAsync())
        {
            _logger.LogDebug("Provider {Provider} failed health check", provider.Name);
            return false;
        }

        // Check rate limits
        var usage = await _usageTracker.GetTodayUsageAsync(provider.Name);
        var limit = GetProviderLimit(provider.Name);
        
        if (usage.RequestCount >= limit)
        {
            _logger.LogDebug("Provider {Provider} hit rate limit: {Count}/{Limit}", 
                provider.Name, usage.RequestCount, limit);
            return false;
        }

        // Check cost limits
        var estimatedCost = EstimateCost(context.Prompt, provider.CostPerToken);
        if (usage.TotalCost + estimatedCost > _config.DailyBudget)
        {
            _logger.LogDebug("Provider {Provider} would exceed daily budget: {Current} + {Estimated} > {Budget}", 
                provider.Name, usage.TotalCost, estimatedCost, _config.DailyBudget);
            return false;
        }

        return true;
    }

    private static int GetProviderLimit(string providerName) => providerName.ToLower() switch
    {
        "groq" => 100,     // per hour
        "together" => 1000, // per day
        "huggingface" => 1000, // per month
        "ollama" => int.MaxValue, // unlimited local
        _ => 0
    };

    private static int EstimateTokens(string text) => 
        (int)Math.Ceiling(text.Length / 4.0); // Rough estimation

    private static decimal EstimateCost(string text, decimal costPerToken) =>
        EstimateTokens(text) * costPerToken / 1000;
}