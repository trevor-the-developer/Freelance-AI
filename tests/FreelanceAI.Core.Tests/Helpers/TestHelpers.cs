using FreelanceAI.Core.Interfaces;
using FreelanceAI.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace FreelanceAI.Core.Tests.Helpers;

public static class TestHelpers
{
    /// <summary>
    ///     Creates a mock AI provider with specified configuration
    /// </summary>
    public static Mock<IAIProvider> CreateMockProvider(
        string name,
        bool isHealthy = true,
        int priority = 1,
        decimal costPerToken = 0.0001m,
        string? responseContent = null)
    {
        var mock = new Mock<IAIProvider>();

        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.Priority).Returns(priority);
        mock.Setup(p => p.CostPerToken).Returns(costPerToken);
        mock.Setup(p => p.IsAvailable).Returns(isHealthy);
        mock.Setup(p => p.CheckHealthAsync()).ReturnsAsync(isHealthy);

        if (isHealthy)
        {
            var content = responseContent ?? $"Response from {name}";
            mock.Setup(p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<AIRequestOptions>()))
                .ReturnsAsync(content);
        }
        else
        {
            mock.Setup(p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<AIRequestOptions>()))
                .ThrowsAsync(new InvalidOperationException($"{name} is unavailable"));
        }

        return mock;
    }

    /// <summary>
    ///     Creates a mock logger for testing
    /// </summary>
    public static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    ///     Creates a mock usage tracker
    /// </summary>
    public static Mock<IUsageTracker> CreateMockUsageTracker()
    {
        var mock = new Mock<IUsageTracker>();

        // Default empty usage
        mock.Setup(u => u.GetTodayUsageAsync(It.IsAny<string>()))
            .ReturnsAsync(new DailyUsage("test-provider", DateTime.UtcNow.ToString("yyyy-MM-dd"), 0, 0, 0m));

        mock.Setup(u => u.RecordUsageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    /// <summary>
    ///     Creates a mock JSON file service
    /// </summary>
    public static Mock<IJsonFileService> CreateMockJsonFileService()
    {
        var mock = new Mock<IJsonFileService>();

        mock.Setup(j => j.WriteAsync(It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        mock.Setup(j => j.LoadAsync<AIResponseHistory>())
            .ReturnsAsync(new AIResponseHistory());

        mock.Setup(j => j.ForceRolloverAsync())
            .Returns(Task.CompletedTask);

        return mock;
    }

    /// <summary>
    ///     Creates test AI request options
    /// </summary>
    public static AIRequestOptions CreateTestRequestOptions(
        int maxTokens = 1000,
        decimal temperature = 0.7m,
        string model = "test-model")
    {
        return new AIRequestOptions(maxTokens, temperature, model, new List<string>());
    }

    /// <summary>
    ///     Creates test provider configuration
    /// </summary>
    public static ProviderConfig CreateTestProviderConfig(
        bool enabled = true,
        int priority = 1,
        int rateLimit = 100,
        string rateLimitWindow = "Hour",
        decimal costPerToken = 0.0001m)
    {
        return new ProviderConfig(enabled, priority, rateLimit, rateLimitWindow, costPerToken);
    }
}

/// <summary>
///     Test implementation of IAIProvider for unit testing
/// </summary>
public class TestAIProvider : IAIProvider
{
    private readonly string _responseContent;
    private readonly TimeSpan _responseDelay;

    private readonly bool _shouldSucceed;

    public TestAIProvider(
        string name,
        int priority = 1,
        bool shouldSucceed = true,
        string responseContent = "Test response",
        TimeSpan responseDelay = default)
    {
        Name = name;
        Priority = priority;
        CostPerToken = 0.0001m;
        IsAvailable = shouldSucceed;
        _shouldSucceed = shouldSucceed;
        _responseContent = responseContent;
        _responseDelay = responseDelay == default ? TimeSpan.FromMilliseconds(100) : responseDelay;
    }

    public string Name { get; }
    public int Priority { get; }
    public decimal CostPerToken { get; }
    public bool IsAvailable { get; set; }

    public async Task<string> GenerateAsync(string prompt, AIRequestOptions options)
    {
        await Task.Delay(_responseDelay);

        if (!_shouldSucceed) throw new InvalidOperationException($"{Name} provider is unavailable");

        return $"{_responseContent} for prompt: {prompt}";
    }

    public async Task<bool> CheckHealthAsync()
    {
        await Task.Delay(10); // Simulate small delay
        return IsAvailable;
    }
}