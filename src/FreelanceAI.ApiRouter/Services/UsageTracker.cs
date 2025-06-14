using FreelanceAI.Core.Interfaces;
using FreelanceAI.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FreelanceAI.ApiRouter.Services;

public class UsageTracker : IUsageTracker
{
    private readonly ILogger<UsageTracker> _logger;
    private readonly ConcurrentDictionary<string, List<UsageRecord>> _usageData;

    public UsageTracker(ILogger<UsageTracker> logger)
    {
        _logger = logger;
        _usageData = new ConcurrentDictionary<string, List<UsageRecord>>();
    }

    public async Task RecordUsageAsync(string provider, int tokens, decimal cost)
    {
        var usage = new UsageRecord(
            Timestamp: DateTime.UtcNow,
            TokenCount: tokens,
            Cost: cost
        );

        var key = GetDailyKey(provider, DateTime.UtcNow);
        
        _usageData.AddOrUpdate(key, 
            new List<UsageRecord> { usage },
            (_, existing) => 
            {
                existing.Add(usage);
                return existing;
            });

        _logger.LogDebug("Recorded usage for {Provider}: {Tokens} tokens, ${Cost:F4}", 
            provider, tokens, cost);

        await Task.CompletedTask;
    }

    public async Task<DailyUsage> GetTodayUsageAsync(string provider)
    {
        var key = GetDailyKey(provider, DateTime.UtcNow);
        
        if (!_usageData.TryGetValue(key, out var records) || !records.Any())
        {
            return new DailyUsage(
                Provider: provider,
                Date: DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                RequestCount: 0,
                TokensUsed: 0,
                TotalCost: 0m
            );
        }

        var usage = new DailyUsage(
            Provider: provider,
            Date: DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            RequestCount: records.Count,
            TokensUsed: records.Sum(r => r.TokenCount),
            TotalCost: records.Sum(r => r.Cost)
        );

        _logger.LogDebug("Retrieved usage for {Provider}: {Requests} requests, ${Cost:F4}", 
            provider, usage.RequestCount, usage.TotalCost);

        return await Task.FromResult(usage);
    }

    public async Task<WeeklyReport> GenerateWeeklyReportAsync()
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-6);
        var totalCost = 0m;
        var totalRequests = 0;
        var providerUsage = new Dictionary<string, List<DailyUsage>>();

        // Get all unique providers
        var providers = _usageData.Keys
            .Select(ExtractProviderName)
            .Distinct()
            .ToList();

        // Generate daily usage for each provider across the week
        foreach (var provider in providers)
        {
            var dailyUsageList = new List<DailyUsage>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var key = GetDailyKey(provider, date);
                
                if (_usageData.TryGetValue(key, out var records) && records.Any())
                {
                    var cost = records.Sum(r => r.Cost);
                    var requests = records.Count;
                    var tokens = records.Sum(r => r.TokenCount);

                    dailyUsageList.Add(new DailyUsage(
                        Provider: provider,
                        Date: date.ToString("yyyy-MM-dd"),
                        RequestCount: requests,
                        TokensUsed: tokens,
                        TotalCost: cost
                    ));

                    totalCost += cost;
                    totalRequests += requests;
                }
                else
                {
                    // Add empty day
                    dailyUsageList.Add(new DailyUsage(
                        Provider: provider,
                        Date: date.ToString("yyyy-MM-dd"),
                        RequestCount: 0,
                        TokensUsed: 0,
                        TotalCost: 0m
                    ));
                }
            }

            providerUsage[provider] = dailyUsageList;
        }

        var report = new WeeklyReport(
            StartDate: startDate,
            EndDate: endDate,
            ProviderUsage: providerUsage,
            TotalCost: totalCost,
            TotalRequests: totalRequests
        );

        _logger.LogInformation("Generated weekly report: {Requests} requests, ${Cost:F4} total cost", 
            totalRequests, totalCost);

        return await Task.FromResult(report);
    }

    public async Task<bool> CheckBudgetLimitAsync(string provider, decimal additionalCost)
    {
        try
        {
            var usage = await GetTodayUsageAsync(provider);
            var projectedCost = usage.TotalCost + additionalCost;
            
            // Simple daily budget check - you might want to make this configurable
            var dailyBudgetLimit = GetDailyBudgetLimit(provider);
            
            var withinBudget = projectedCost <= dailyBudgetLimit;
            
            _logger.LogDebug("Budget check for {Provider}: ${Current:F4} + ${Additional:F4} = ${Projected:F4} (Limit: ${Limit:F4}) - {Result}", 
                provider, usage.TotalCost, additionalCost, projectedCost, dailyBudgetLimit, 
                withinBudget ? "Within Budget" : "Exceeds Budget");

            return withinBudget;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking budget limit for {Provider}", provider);
            return false; // Fail safe - deny if we can't check
        }
    }

    private static decimal GetDailyBudgetLimit(string provider) => provider.ToLower() switch
    {
        "groq" => 0m,      // Free tier
        "together" => 5.0m, // $5 daily limit
        "huggingface" => 1.0m, // $1 daily limit
        "ollama" => 0m,    // Local, no cost
        _ => 1.0m          // Default $1 limit
    };

    private static string GetDailyKey(string providerName, DateTime date) =>
        $"{providerName}:{date:yyyy-MM-dd}";

    private static string ExtractProviderName(string key) =>
        key.Split(':')[0];

    private static DateTime ExtractDate(string key) =>
        DateTime.ParseExact(key.Split(':')[1], "yyyy-MM-dd", null);

    private record UsageRecord(DateTime Timestamp, int TokenCount, decimal Cost);
}