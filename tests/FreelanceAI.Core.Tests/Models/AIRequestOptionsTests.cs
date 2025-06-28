using FluentAssertions;
using FreelanceAI.Core.Models;
using Xunit;

namespace FreelanceAI.Core.Tests.Models;

public class AIRequestOptionsTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        const int maxTokens = 1000;
        const decimal temperature = 0.7m;
        const string model = "test-model";
        var stopSequences = new List<string> { "STOP", "END" };

        // Act
        var options = new AIRequestOptions(maxTokens, temperature, model, stopSequences);

        // Assert
        options.MaxTokens.Should().Be(maxTokens);
        options.Temperature.Should().Be(temperature);
        options.Model.Should().Be(model);
        options.StopSequences.Should().BeEquivalentTo(stopSequences);
    }

    [Fact]
    public void Constructor_WithEmptyStopSequences_ShouldCreateInstance()
    {
        // Arrange & Act
        var options = new AIRequestOptions(1000, 0.7m, "test-model", new List<string>());

        // Assert
        options.StopSequences.Should().NotBeNull();
        options.StopSequences.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public void Temperature_WithValidRange_ShouldBeAccepted(decimal temperature)
    {
        // Arrange & Act
        var options = new AIRequestOptions(1000, temperature, "test-model", new List<string>());

        // Assert
        options.Temperature.Should().Be(temperature);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(4096)]
    [InlineData(32768)]
    public void MaxTokens_WithValidValues_ShouldBeAccepted(int maxTokens)
    {
        // Arrange & Act
        var options = new AIRequestOptions(maxTokens, 0.7m, "test-model", new List<string>());

        // Assert
        options.MaxTokens.Should().Be(maxTokens);
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var stopSequences = new List<string> { "STOP" };
        var options1 = new AIRequestOptions(1000, 0.7m, "test-model", stopSequences);
        var options2 = new AIRequestOptions(1000, 0.7m, "test-model", stopSequences);

        // Act & Assert
        options1.Should().Be(options2);
        (options1 == options2).Should().BeTrue();
        options1.GetHashCode().Should().Be(options2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var options1 = new AIRequestOptions(1000, 0.7m, "test-model", new List<string>());
        var options2 = new AIRequestOptions(2000, 0.7m, "test-model", new List<string>());

        // Act & Assert
        options1.Should().NotBe(options2);
        (options1 == options2).Should().BeFalse();
    }
}
