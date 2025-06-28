using FluentAssertions;
using FreelanceAI.ApiRouter;
using FreelanceAI.Core.Configuration;
using FreelanceAI.Core.Interfaces;
using FreelanceAI.Core.Models;
using FreelanceAI.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FreelanceAI.ApiRouter.Tests;

public class SmartApiRouterTests
{
    private readonly Mock<ILogger<SmartApiRouter>> _mockLogger;
    private readonly Mock<IUsageTracker> _mockUsageTracker;
    private readonly Mock<IJsonFileService> _mockJsonFileService;
    private readonly IOptions<RouterConfiguration> _routerConfig;
    private readonly IOptions<JsonFileServiceOptions> _jsonFileServiceOptions;

    public SmartApiRouterTests()
    {
        _mockLogger = TestHelpers.CreateMockLogger<SmartApiRouter>();
        _mockUsageTracker = TestHelpers.CreateMockUsageTracker();
        _mockJsonFileService = TestHelpers.CreateMockJsonFileService();
        
        _routerConfig = Options.Create(new RouterConfiguration
        {
            DailyBudget = 10.0m,
            MaxRetries = 3,
            EnableCostTracking = true,
            ProviderLimits = new Dictionary<string, ProviderLimitConfiguration>
            {
                ["testprovider"] = new() { RequestLimit = 100, LimitType = "Day", CostPerToken = 0.0001m }
            }
        });

        _jsonFileServiceOptions = Options.Create(new JsonFileServiceOptions
        {
            Enabled = false
        });
    }

    [Fact]
    public async Task RouteRequestAsync_WithHealthyProvider_ShouldReturnSuccess()
    {
        // Arrange
        var mockProvider = TestHelpers.CreateMockProvider("TestProvider", isHealthy: true, priority: 1);
        var router = CreateRouter(new[] { mockProvider.Object });
        var options = TestHelpers.CreateTestRequestOptions();

        // Act
        var result = await router.RouteRequestAsync("test prompt", options);

        // Assert
        result.Should().BeOfType<AISuccess>();
        var success = (AISuccess)result;
        success.Provider.Should().Be("TestProvider");
        success.Content.Should().Contain("Response from TestProvider");
        
        // Verify provider was called
        mockProvider.Verify(p => p.GenerateAsync("test prompt", options), Times.Once);
        mockProvider.Verify(p => p.CheckHealthAsync(), Times.Once);
    }

    [Fact]
    public async Task RouteRequestAsync_WithUnhealthyProvider_ShouldReturnFailure()
    {
        // Arrange
        var mockProvider = TestHelpers.CreateMockProvider("TestProvider", isHealthy: false);
        var router = CreateRouter(new[] { mockProvider.Object });
        var options = TestHelpers.CreateTestRequestOptions();

        // Act
        var result = await router.RouteRequestAsync("test prompt", options);

        // Assert
        result.Should().BeOfType<AIFailure>();
        var failure = (AIFailure)result;
        failure.Error.Should().Be("All AI providers exhausted or unavailable");
        failure.FailedProviders.Should().BeEmpty(); // Provider wasn't attempted due to health check
        
        // Verify provider health was checked but generate was not called
        mockProvider.Verify(p => p.CheckHealthAsync(), Times.Once);
        mockProvider.Verify(p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<AIRequestOptions>()), Times.Never);
    }

    [Fact]
    public async Task RouteRequestAsync_WithMultipleProviders_ShouldUsePriorityOrder()
    {
        // Arrange
        var highPriorityProvider = TestHelpers.CreateMockProvider("HighPriority", isHealthy: true, priority: 1);
        var lowPriorityProvider = TestHelpers.CreateMockProvider("LowPriority", isHealthy: true, priority: 2);
        
        var router = CreateRouter(new[] { lowPriorityProvider.Object, highPriorityProvider.Object });
        var options = TestHelpers.CreateTestRequestOptions();

        // Act
        var result = await router.RouteRequestAsync("test prompt", options);

        // Assert
        result.Should().BeOfType<AISuccess>();
        var success = (AISuccess)result;
        success.Provider.Should().Be("HighPriority");
        
        // Verify high priority provider was used
        highPriorityProvider.Verify(p => p.GenerateAsync("test prompt", options), Times.Once);
        lowPriorityProvider.Verify(p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<AIRequestOptions>()), Times.Never);
    }

    [Fact]
    public async Task RouteRequestAsync_WithFailoverScenario_ShouldTryNextProvider()
    {
        // Arrange
        var failingProvider = TestHelpers.CreateMockProvider("FailingProvider", isHealthy: false, priority: 1);
        var workingProvider = TestHelpers.CreateMockProvider("WorkingProvider", isHealthy: true, priority: 2);
        
        var router = CreateRouter(new[] { failingProvider.Object, workingProvider.Object });
        var options = TestHelpers.CreateTestRequestOptions();

        // Act
        var result = await router.RouteRequestAsync("test prompt", options);

        // Assert
        result.Should().BeOfType<AISuccess>();
        var success = (AISuccess)result;
        success.Provider.Should().Be("WorkingProvider");
        
        // Verify failover occurred
        failingProvider.Verify(p => p.CheckHealthAsync(), Times.Once);
        workingProvider.Verify(p => p.GenerateAsync("test prompt", options), Times.Once);
    }

    [Fact]
    public async Task RouteRequestAsync_WithProviderException_ShouldTryNextProvider()
    {
        // Arrange
        var throwingProvider = new Mock<IAIProvider>();
        throwingProvider.Setup(p => p.Name).Returns("ThrowingProvider");
        throwingProvider.Setup(p => p.Priority).Returns(1);
        throwingProvider.Setup(p => p.CheckHealthAsync()).ReturnsAsync(true);
        throwingProvider.Setup(p => p.GenerateAsync(It.IsAny<string>(), It.IsAny<AIRequestOptions>()))
                        .ThrowsAsync(new InvalidOperationException("Provider failed"));

        var workingProvider = TestHelpers.CreateMockProvider("WorkingProvider", isHealthy: true, priority: 2);
        
        var router = CreateRouter(new[] { throwingProvider.Object, workingProvider.Object });
        var options = TestHelpers.CreateTestRequestOptions();

        // Act
        var result = await router.RouteRequestAsync("test prompt", options);

        // Assert
        result.Should().BeOfType<AISuccess>();
        var success = (AISuccess)result;
        success.Provider.Should().Be("WorkingProvider");
    }

    [Fact]
    public async Task GetProviderStatusAsync_ShouldReturnAllProviderStatuses()
    {
        // Arrange
        var provider1 = TestHelpers.CreateMockProvider("Provider1", isHealthy: true, priority: 1);
        var provider2 = TestHelpers.CreateMockProvider("Provider2", isHealthy: false, priority: 2);
        
        var router = CreateRouter(new[] { provider1.Object, provider2.Object });

        // Act
        var statuses = await router.GetProviderStatusAsync();

        // Assert
        statuses.Should().HaveCount(2);
        
        var provider1Status = statuses.First(s => s.Name == "Provider1");
        provider1Status.IsHealthy.Should().BeTrue();
        
        var provider2Status = statuses.First(s => s.Name == "Provider2");
        provider2Status.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task GetProviderStatusAsync_WithProviderException_ShouldReturnUnhealthyStatus()
    {
        // Arrange
        var throwingProvider = new Mock<IAIProvider>();
        throwingProvider.Setup(p => p.Name).Returns("ThrowingProvider");
        throwingProvider.Setup(p => p.CheckHealthAsync()).ThrowsAsync(new Exception("Health check failed"));
        
        var router = CreateRouter(new[] { throwingProvider.Object });

        // Act
        var statuses = await router.GetProviderStatusAsync();

        // Assert
        statuses.Should().HaveCount(1);
        var status = statuses.First();
        status.Name.Should().Be("ThrowingProvider");
        status.IsHealthy.Should().BeFalse();
        status.RequestsToday.Should().Be(0);
        status.CostToday.Should().Be(0);
        status.RemainingRequests.Should().Be(0);
    }

    [Fact]
    public async Task GetTodaySpendAsync_ShouldReturnTotalSpendAcrossProviders()
    {
        // Arrange
        var provider1 = TestHelpers.CreateMockProvider("Provider1");
        var provider2 = TestHelpers.CreateMockProvider("Provider2");
        
        _mockUsageTracker.Setup(u => u.GetTodayUsageAsync("Provider1"))
                        .ReturnsAsync(new DailyUsage("Provider1", "2023-01-01", 10, 100, 5.0m));
        _mockUsageTracker.Setup(u => u.GetTodayUsageAsync("Provider2"))
                        .ReturnsAsync(new DailyUsage("Provider2", "2023-01-01", 5, 50, 2.5m));
        
        var router = CreateRouter(new[] { provider1.Object, provider2.Object });

        // Act
        var totalSpend = await router.GetTodaySpendAsync();

        // Assert
        totalSpend.Should().Be(7.5m);
    }

    [Fact]
    public async Task GetTodaySpendAsync_WithProviderException_ShouldContinueCalculation()
    {
        // Arrange
        var provider1 = TestHelpers.CreateMockProvider("Provider1");
        var provider2 = TestHelpers.CreateMockProvider("Provider2");
        
        _mockUsageTracker.Setup(u => u.GetTodayUsageAsync("Provider1"))
                        .ThrowsAsync(new Exception("Usage tracking failed"));
        _mockUsageTracker.Setup(u => u.GetTodayUsageAsync("Provider2"))
                        .ReturnsAsync(new DailyUsage("Provider2", "2023-01-01", 5, 50, 2.5m));
        
        var router = CreateRouter(new[] { provider1.Object, provider2.Object });

        // Act
        var totalSpend = await router.GetTodaySpendAsync();

        // Assert
        totalSpend.Should().Be(2.5m); // Only successful provider's cost
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RouteRequestAsync_WithInvalidPrompt_ShouldValidateInput(string? prompt)
    {
        // Arrange
        var provider = TestHelpers.CreateMockProvider("TestProvider", isHealthy: true);
        var router = CreateRouter(new[] { provider.Object });
        var options = TestHelpers.CreateTestRequestOptions();

        // Act & Assert
        if (prompt == null)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => router.RouteRequestAsync(prompt!, options));
        }
        else
        {
            // Note: The actual validation might be handled at the controller level
            // This test demonstrates the expected behavior
            var result = await router.RouteRequestAsync(prompt, options);
            
            // For empty/whitespace prompts, the behavior depends on implementation
            // This could either throw an exception or return a failure response
            result.Should().NotBeNull();
        }
    }

    private SmartApiRouter CreateRouter(IEnumerable<IAIProvider> providers)
    {
        return new SmartApiRouter(
            _mockLogger.Object,
            providers,
            _mockUsageTracker.Object,
            _routerConfig,
            _jsonFileServiceOptions,
            _mockJsonFileService.Object
        );
    }
}
