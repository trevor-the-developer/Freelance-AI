using System.Linq;
using System.Text.Json;
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
    private readonly JsonFileServiceOptions _jsonFileServiceOptions;
    private readonly IJsonFileService _jsonFileService;

    public SmartApiRouter(
        ILogger<SmartApiRouter> logger,
        IEnumerable<IAIProvider> providers,
        IUsageTracker usageTracker,
        IOptions<RouterConfiguration> config,
        JsonFileServiceOptions jsonFileServiceOptions,
        IJsonFileService jsonFileService)
    {
        _logger = logger;
        _providers = providers.OrderBy(p => p.Priority).ToList();
        _usageTracker = usageTracker;
        _config = config.Value;
        _jsonFileServiceOptions = jsonFileServiceOptions;
        _jsonFileService = jsonFileService;
    }

    public async Task<AIResponse> RouteRequestAsync(string prompt, AIRequestOptions options)
    {
        var context = new RequestContext(prompt, options, DateTime.UtcNow);
        var routingResult = new RoutingResult();
        
        foreach (var provider in _providers)
        {
            if (!await IsProviderViable(provider, context))
                continue;

            var attemptResult = await AttemptProviderRequest(provider, prompt, options, context);
            routingResult.AddAttempt(attemptResult);

            if (attemptResult.IsSuccess)
            {
                await LogRoutingHistory(routingResult);
                return attemptResult.Response;
            }
        }

        await LogRoutingHistory(routingResult);
        return CreateFailureResponse(routingResult, context);
    }

    public async Task<List<ProviderStatus>> GetProviderStatusAsync()
    {
        var statusList = new List<ProviderStatus>();

        foreach (var provider in _providers)
        {
            try
            {
                var isHealthy = await provider.CheckHealthAsync();
                var usage = await GetUsageForLimitType(provider.Name);
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

    public string GetProviderLimitType(string providerName)
    {
        var normalizedName = providerName.ToLowerInvariant();
        
        if (_config.ProviderLimits.TryGetValue(normalizedName, out var limitConfig))
        {
            return limitConfig.LimitType;
        }

        return "Day"; // Default fallback
    }

    private async Task<ProviderAttemptResult> AttemptProviderRequest(
        IAIProvider provider, 
        string prompt, 
        AIRequestOptions options, 
        RequestContext context)
    {
        _logger.LogInformation("🚀 Routing request to {Provider}", provider.Name);
        
        try
        {
            var response = await provider.GenerateAsync(prompt, options);
            var tokenCount = EstimateTokens(prompt + response);
            var estimatedCost = EstimateCost(prompt + response, GetProviderCostPerToken(provider.Name));
            var duration = DateTime.UtcNow - context.StartTime;

            await _usageTracker.RecordUsageAsync(provider.Name, tokenCount, estimatedCost);

            var success = new AISuccess(
                Content: response,
                Provider: provider.Name,
                RequestCost: estimatedCost,
                Duration: duration
            );

            var entry = CreateResponseEntry(prompt, options, provider.Name, success: true, 
                content: response, cost: estimatedCost, duration: duration.TotalMilliseconds);

            return new ProviderAttemptResult(true, success, entry, provider.Name, estimatedCost);
        }
        catch (Exception ex)
        {
            _logger.LogError("Provider {Provider} failed: {Error}", provider.Name, ex.Message);
            
            var duration = DateTime.UtcNow - context.StartTime;
            var failure = new AIFailure(
                Error: ex.Message,
                FailedProviders: [provider.Name],
                TotalAttemptedCost: 0,
                Duration: duration
            );

            var entry = CreateResponseEntry(prompt, options, provider.Name, success: false, 
                error: ex.Message, duration: duration.TotalMilliseconds);

            return new ProviderAttemptResult(false, failure, entry, provider.Name, 0);
        }
    }

    private AIResponseEntry CreateResponseEntry(
        string prompt, 
        AIRequestOptions options, 
        string providerName, 
        bool success, 
        string content = null, 
        string error = null, 
        decimal cost = 0, 
        double duration = 0)
    {
        return new AIResponseEntry(
            Id: Guid.NewGuid(),
            Timestamp: DateTime.UtcNow,
            Prompt: prompt,
            MaxTokens: options.MaxTokens,
            Temperature: options.Temperature,
            Model: options.Model,
            Success: success,
            Provider: providerName,
            Content: content,
            Error: error,
            Cost: cost,
            Duration: duration
        );
    }

    private AIFailure CreateFailureResponse(RoutingResult routingResult, RequestContext context)
    {
        return new AIFailure(
            Error: "All AI providers exhausted or unavailable",
            FailedProviders: routingResult.FailedProviders.ToArray(),
            TotalAttemptedCost: routingResult.TotalCost,
            Duration: DateTime.UtcNow - context.StartTime
        );
    }

    private async Task LogRoutingHistory(RoutingResult routingResult)
    {
        if (!_jsonFileServiceOptions.Enabled) return;

        try
        {
            await _jsonFileService.WriteAsync(new AIResponseHistory
            {
                LastUpdated = DateTime.UtcNow,
                Responses = routingResult.ResponseEntries,
                TotalRequests = routingResult.TotalAttempts,
                TotalCost = routingResult.TotalCost
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to write to JSON file, Error: {Error}", ex.Message);
        }
    }

    private async Task<bool> IsProviderViable(IAIProvider provider, RequestContext context)
    {
        // Check health
        if (!await provider.CheckHealthAsync())
        {
            _logger.LogDebug("Provider {Provider} failed health check", provider.Name);
            return false;
        }

        // Check rate limits using time-aware usage
        var usage = await GetUsageForLimitType(provider.Name);
        var limit = GetProviderLimit(provider.Name);
        
        if (usage.RequestCount >= limit)
        {
            var limitType = GetProviderLimitType(provider.Name);
            _logger.LogDebug("Provider {Provider} hit {LimitType} rate limit: {Count}/{Limit}", 
                provider.Name, limitType, usage.RequestCount, limit);
            return false;
        }

        // Check cost limits
        var estimatedCost = EstimateCost(context.Prompt, GetProviderCostPerToken(provider.Name));
        if (usage.TotalCost + estimatedCost > _config.DailyBudget)
        {
            _logger.LogDebug("Provider {Provider} would exceed daily budget: {Current} + {Estimated} > {Budget}", 
                provider.Name, usage.TotalCost, estimatedCost, _config.DailyBudget);
            return false;
        }

        return true;
    }

    private async Task<DailyUsage> GetUsageForLimitType(string providerName)
    {
        var limitType = GetProviderLimitType(providerName);
        
        return limitType.ToLowerInvariant() switch
        {
            "hour" => await _usageTracker.GetTodayUsageAsync(providerName), // Fallback to daily for now
            "day" => await _usageTracker.GetTodayUsageAsync(providerName),
            "month" => await _usageTracker.GetTodayUsageAsync(providerName), // Fallback to daily for now
            "unlimited" => new DailyUsage(
                Provider: providerName,
                Date: DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                RequestCount: 0,
                TokensUsed: 0,
                TotalCost: 0m
            ),
            _ => await _usageTracker.GetTodayUsageAsync(providerName)
        };
    }

    private int GetProviderLimit(string providerName)
    {
        var normalizedName = providerName.ToLowerInvariant();
        
        if (_config.ProviderLimits.TryGetValue(normalizedName, out var limitConfig))
        {
            return limitConfig.RequestLimit;
        }

        _logger.LogWarning("No limit configuration found for provider {Provider}, defaulting to 0", providerName);
        return 0;
    }

    private decimal GetProviderCostPerToken(string providerName)
    {
        var normalizedName = providerName.ToLowerInvariant();
        
        if (_config.ProviderLimits.TryGetValue(normalizedName, out var limitConfig))
        {
            return limitConfig.CostPerToken;
        }

        _logger.LogWarning("No cost configuration found for provider {Provider}, defaulting to 0", providerName);
        return 0.0m;
    }

    private static int EstimateTokens(string text) => 
        (int)Math.Ceiling(text.Length / 4.0); // Rough estimation

    private static decimal EstimateCost(string text, decimal costPerToken) =>
        EstimateTokens(text) * costPerToken / 1000;
}