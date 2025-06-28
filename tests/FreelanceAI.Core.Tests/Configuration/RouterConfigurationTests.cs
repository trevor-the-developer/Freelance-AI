using FluentAssertions;
using FreelanceAI.Core.Configuration;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace FreelanceAI.Core.Tests.Configuration;

public class RouterConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var config = new RouterConfiguration();

        // Assert
        config.DailyBudget.Should().Be(10.0m);
        config.MaxRetries.Should().Be(3);
        config.HealthCheckInterval.Should().Be(TimeSpan.FromMinutes(5));
        config.EnableCostTracking.Should().BeTrue();
        config.EnableRateLimiting.Should().BeTrue();
        config.ProviderLimits.Should().NotBeNull();
        config.ProviderLimits.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(5.0)]
    [InlineData(100.0)]
    [InlineData(1000.0)]
    public void DailyBudget_WithValidValues_ShouldBeAccepted(decimal budget)
    {
        // Arrange & Act
        var config = new RouterConfiguration { DailyBudget = budget };

        // Assert
        config.DailyBudget.Should().Be(budget);
        ValidateConfiguration(config).Should().BeTrue();
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-100.0)]
    public void DailyBudget_WithNegativeValues_ShouldFailValidation(decimal budget)
    {
        // Arrange & Act
        var config = new RouterConfiguration { DailyBudget = budget };

        // Assert
        ValidateConfiguration(config).Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void MaxRetries_WithValidValues_ShouldBeAccepted(int retries)
    {
        // Arrange & Act
        var config = new RouterConfiguration { MaxRetries = retries };

        // Assert
        config.MaxRetries.Should().Be(retries);
        ValidateConfiguration(config).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void MaxRetries_WithInvalidValues_ShouldFailValidation(int retries)
    {
        // Arrange & Act
        var config = new RouterConfiguration { MaxRetries = retries };

        // Assert
        ValidateConfiguration(config).Should().BeFalse();
    }

    [Fact]
    public void ProviderLimits_WhenAdded_ShouldBeAccessible()
    {
        // Arrange
        var config = new RouterConfiguration();
        var providerLimit = new ProviderLimitConfiguration
        {
            RequestLimit = 100,
            LimitType = "Hour",
            CostPerToken = 0.0001m,
            DailyBudgetLimit = 5.0m
        };

        // Act
        var newConfig = config with
        {
            ProviderLimits = new Dictionary<string, ProviderLimitConfiguration>
            {
                ["groq"] = providerLimit
            }
        };

        // Assert
        newConfig.ProviderLimits.Should().ContainKey("groq");
        newConfig.ProviderLimits["groq"].Should().Be(providerLimit);
    }

    [Fact]
    public void RecordEquality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var providerLimits = new Dictionary<string, ProviderLimitConfiguration>
        {
            ["groq"] = new() { RequestLimit = 100, LimitType = "Day", CostPerToken = 0.0001m }
        };
        
        var config1 = new RouterConfiguration
        {
            DailyBudget = 10.0m,
            MaxRetries = 3,
            EnableCostTracking = true,
            ProviderLimits = providerLimits
        };

        var config2 = new RouterConfiguration
        {
            DailyBudget = 10.0m,
            MaxRetries = 3,
            EnableCostTracking = true,
            ProviderLimits = providerLimits  // Same reference
        };

        // Act & Assert
        config1.Equals(config2).Should().BeTrue();
        (config1 == config2).Should().BeTrue();
        config1.GetHashCode().Should().Be(config2.GetHashCode());
    }

    [Fact]
    public void RecordEquality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var config1 = new RouterConfiguration { DailyBudget = 10.0m };
        var config2 = new RouterConfiguration { DailyBudget = 20.0m };

        // Act & Assert
        config1.Should().NotBe(config2);
    }

    [Fact]
    public void WithExpression_ShouldCreateNewInstance()
    {
        // Arrange
        var originalConfig = new RouterConfiguration { DailyBudget = 10.0m };

        // Act
        var newConfig = originalConfig with { DailyBudget = 20.0m };

        // Assert
        originalConfig.DailyBudget.Should().Be(10.0m);
        newConfig.DailyBudget.Should().Be(20.0m);
        newConfig.Should().NotBeSameAs(originalConfig);
    }

    private static bool ValidateConfiguration(RouterConfiguration config)
    {
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(config, context, results, true);
    }
}
