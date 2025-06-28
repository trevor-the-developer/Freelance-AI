using FluentAssertions;
using FluentAssertions.Execution;
using FreelanceAI.Core.Configuration;
using FreelanceAI.Core.Tests.Assertions;
using FreelanceAI.Core.Tests.TestData;
using Xunit;

namespace FreelanceAI.Core.Tests.Configuration;

public class RouterConfigurationTests
{
    #region Test Data

    private static readonly ProviderLimitConfiguration ValidProviderLimit = new()
    {
        RequestLimit = 100,
        LimitType = "Hour",
        CostPerToken = 0.0001m,
        DailyBudgetLimit = 5.0m
    };

    #endregion

    #region Default Values Tests

    [Fact]
    public void DefaultConfiguration_ShouldHaveExpectedValues()
    {
        // Act
        var config = new RouterConfiguration();

        // Assert
        using var _ = new AssertionScope();
        config.DailyBudget.Should().Be(10.0m);
        config.MaxRetries.Should().Be(3);
        config.HealthCheckInterval.Should().Be(TimeSpan.FromMinutes(5));
        config.EnableCostTracking.Should().BeTrue();
        config.EnableRateLimiting.Should().BeTrue();
        config.ProviderLimits.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void DefaultConfiguration_ShouldPassValidation()
    {
        // Arrange
        var config = new RouterConfiguration();

        // Act & Assert
        config.Should().BeValidConfiguration();
    }

    #endregion

    #region Daily Budget Tests

    [Theory]
    [MemberData(nameof(RouterConfigurationTestData.ValidBudgetValues),
        MemberType = typeof(RouterConfigurationTestData))]
    public void DailyBudget_WithValidValues_ShouldBeAcceptedAndPassValidation(decimal budget)
    {
        // Act
        var config = new RouterConfiguration { DailyBudget = budget };

        // Assert
        using var _ = new AssertionScope();
        config.DailyBudget.Should().Be(budget);
        config.Should().BeValidConfiguration();
    }

    [Theory]
    [MemberData(nameof(RouterConfigurationTestData.InvalidBudgetValues),
        MemberType = typeof(RouterConfigurationTestData))]
    public void DailyBudget_WithNegativeValues_ShouldFailValidation(decimal budget)
    {
        // Act
        var config = new RouterConfiguration { DailyBudget = budget };

        // Assert
        config.Should().BeInvalidConfiguration();
    }

    #endregion

    #region Max Retries Tests

    [Theory]
    [MemberData(nameof(RouterConfigurationTestData.ValidRetryValues), MemberType = typeof(RouterConfigurationTestData))]
    public void MaxRetries_WithValidValues_ShouldBeAcceptedAndPassValidation(int retries)
    {
        // Act
        var config = new RouterConfiguration { MaxRetries = retries };

        // Assert
        using var _ = new AssertionScope();
        config.MaxRetries.Should().Be(retries);
        config.Should().BeValidConfiguration();
    }

    [Theory]
    [MemberData(nameof(RouterConfigurationTestData.InvalidRetryValues),
        MemberType = typeof(RouterConfigurationTestData))]
    public void MaxRetries_WithInvalidValues_ShouldFailValidation(int retries)
    {
        // Act
        var config = new RouterConfiguration { MaxRetries = retries };

        // Assert
        config.Should().BeInvalidConfiguration();
    }

    #endregion

    #region Provider Limits Tests

    [Fact]
    public void ProviderLimits_WhenProviderAdded_ShouldBeAccessible()
    {
        // Arrange
        var config = new RouterConfiguration();
        const string providerName = "groq";

        // Act
        var updatedConfig = config with
        {
            ProviderLimits = new Dictionary<string, ProviderLimitConfiguration>
            {
                [providerName] = ValidProviderLimit
            }
        };

        // Assert
        using var _ = new AssertionScope();
        updatedConfig.ProviderLimits.Should().ContainKey(providerName);
        updatedConfig.ProviderLimits[providerName].Should().BeEquivalentTo(ValidProviderLimit);
    }

    [Fact]
    public void ProviderLimits_WithMultipleProviders_ShouldMaintainAllEntries()
    {
        // Arrange
        var config = new RouterConfiguration();
        var providerLimits = new Dictionary<string, ProviderLimitConfiguration>
        {
            ["groq"] = ValidProviderLimit,
            ["openai"] = ValidProviderLimit with { CostPerToken = 0.0002m },
            ["anthropic"] = ValidProviderLimit with { RequestLimit = 50 }
        };

        // Act
        var updatedConfig = config with { ProviderLimits = providerLimits };

        // Assert
        using var _ = new AssertionScope();
        updatedConfig.ProviderLimits.Should().HaveCount(3);
        updatedConfig.ProviderLimits.Keys.Should().BeEquivalentTo("groq", "openai", "anthropic");
    }

    #endregion

    #region Record Behavior Tests

    [Fact]
    public void RecordEquality_WithIdenticalConfigurations_ShouldBeEqual()
    {
        // Arrange
        var providerLimits = CreateProviderLimitsDict();
        var config1 = CreateTestConfiguration(providerLimits);
        var config2 = CreateTestConfiguration(providerLimits);

        // Assert
        using var _ = new AssertionScope();
        config1.Should().Be(config2);
        (config1 == config2).Should().BeTrue();
        config1.GetHashCode().Should().Be(config2.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WithDifferentBudgets_ShouldNotBeEqual()
    {
        // Arrange
        var config1 = new RouterConfiguration { DailyBudget = 10.0m };
        var config2 = new RouterConfiguration { DailyBudget = 20.0m };

        // Assert
        using var _ = new AssertionScope();
        config1.Should().NotBe(config2);
        (config1 == config2).Should().BeFalse();
    }

    [Fact]
    public void WithExpression_ShouldCreateNewInstanceWithUpdatedValues()
    {
        // Arrange
        var originalConfig = new RouterConfiguration { DailyBudget = 10.0m, MaxRetries = 3 };
        const decimal newBudget = 20.0m;
        const int newRetries = 5;

        // Act
        var updatedConfig = originalConfig with
        {
            DailyBudget = newBudget,
            MaxRetries = newRetries
        };

        // Assert
        using var _ = new AssertionScope();
        originalConfig.DailyBudget.Should().Be(10.0m);
        originalConfig.MaxRetries.Should().Be(3);
        updatedConfig.DailyBudget.Should().Be(newBudget);
        updatedConfig.MaxRetries.Should().Be(newRetries);
        updatedConfig.Should().NotBeSameAs(originalConfig);
    }

    [Fact]
    public void WithExpression_ModifyingProviderLimits_ShouldNotAffectOriginal()
    {
        // Arrange
        var originalLimits = CreateProviderLimitsDict();
        var originalConfig = new RouterConfiguration { ProviderLimits = originalLimits };

        var newLimits = new Dictionary<string, ProviderLimitConfiguration>
        {
            ["new-provider"] = ValidProviderLimit
        };

        // Act
        var updatedConfig = originalConfig with { ProviderLimits = newLimits };

        // Assert
        using var _ = new AssertionScope();
        originalConfig.ProviderLimits.Should().ContainKey("groq");
        originalConfig.ProviderLimits.Should().NotContainKey("new-provider");
        updatedConfig.ProviderLimits.Should().ContainKey("new-provider");
        updatedConfig.ProviderLimits.Should().NotContainKey("groq");
    }

    #endregion

    #region Helper Methods

    private static Dictionary<string, ProviderLimitConfiguration> CreateProviderLimitsDict()
    {
        return new Dictionary<string, ProviderLimitConfiguration>
        {
            ["groq"] = ValidProviderLimit
        };
    }

    private static RouterConfiguration CreateTestConfiguration(
        Dictionary<string, ProviderLimitConfiguration>? providerLimits = null)
    {
        return new RouterConfiguration
        {
            DailyBudget = 10.0m,
            MaxRetries = 3,
            EnableCostTracking = true,
            ProviderLimits = providerLimits ?? new Dictionary<string, ProviderLimitConfiguration>()
        };
    }

    #endregion
}