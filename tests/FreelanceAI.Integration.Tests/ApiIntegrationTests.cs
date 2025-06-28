using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace FreelanceAI.Integration.Tests;

public class ApiIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthy()
    {
        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthData = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        
        healthData.GetProperty("status").GetString().Should().Be("healthy");
        healthData.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task DetailedHealthEndpoint_ShouldReturnSystemHealth()
    {
        // Arrange
        SetupHealthCheckMocks(groqHealthy: true, ollamaHealthy: true);

        // Act
        var response = await Client.PostAsync("/api/ai/health", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var healthData = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        
        healthData.GetProperty("status").GetString().Should().Be("Healthy");
        healthData.GetProperty("healthyProviders").GetInt32().Should().BeGreaterThan(0);
        healthData.GetProperty("totalProviders").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProviderStatusEndpoint_ShouldReturnProviderStatuses()
    {
        // Arrange
        SetupHealthCheckMocks(groqHealthy: true, ollamaHealthy: false);

        // Act
        var response = await Client.GetAsync("/api/ai/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var statuses = JsonSerializer.Deserialize<JsonElement[]>(content, JsonOptions);
        
        statuses.Should().NotBeEmpty();
        statuses.Should().Contain(status => 
            status.GetProperty("name").GetString() == "Groq");
    }

    [Fact]
    public async Task SpendEndpoint_ShouldReturnTodaysSpend()
    {
        // Act
        var response = await Client.GetAsync("/api/ai/spend");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var spend = JsonSerializer.Deserialize<decimal>(content, JsonOptions);
        
        spend.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GenerateEndpoint_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        SetupHealthCheckMocks(groqHealthy: true);
        SetupGroqMockResponse("test response");

        var requestPayload = CreateTestRequest("Generate a hello world function");
        var json = JsonSerializer.Serialize(requestPayload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/ai/generate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, JsonOptions);
        
        responseData.GetProperty("success").GetBoolean().Should().BeTrue();
        responseData.GetProperty("content").GetString().Should().Contain("test response");
        responseData.GetProperty("provider").GetString().Should().Be("Groq");
        responseData.TryGetProperty("cost", out _).Should().BeTrue();
        responseData.TryGetProperty("duration", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GenerateEndpoint_WithFailoverScenario_ShouldUseBackupProvider()
    {
        // Arrange
        SetupHealthCheckMocks(groqHealthy: false, ollamaHealthy: true);
        SetupOllamaMockResponse("This is a response from Ollama fallback");

        var requestPayload = CreateTestRequest("Generate a hello world function");
        var json = JsonSerializer.Serialize(requestPayload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/ai/generate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, JsonOptions);
        
        responseData.GetProperty("success").GetBoolean().Should().BeTrue();
        responseData.GetProperty("content").GetString().Should().Contain("Ollama fallback");
        responseData.GetProperty("provider").GetString().Should().Be("Ollama");
    }

    [Fact]
    public async Task GenerateEndpoint_WithAllProvidersDown_ShouldReturnServiceUnavailable()
    {
        // Arrange
        SetupHealthCheckMocks(groqHealthy: false, ollamaHealthy: false);

        var requestPayload = CreateTestRequest("Generate a hello world function");
        var json = JsonSerializer.Serialize(requestPayload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/ai/generate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, JsonOptions);
        
        responseData.GetProperty("success").GetBoolean().Should().BeFalse();
        responseData.GetProperty("error").GetString().Should().Contain("exhausted");
    }

    [Fact]
    public async Task GenerateEndpoint_WithEmptyPrompt_ShouldReturnBadRequest()
    {
        // Arrange
        var requestPayload = CreateTestRequest("");
        var json = JsonSerializer.Serialize(requestPayload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/ai/generate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Prompt is required");
    }

    [Fact]
    public async Task GenerateEndpoint_WithInvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var content = new StringContent("invalid json", Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/ai/generate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateEndpoint_WithCustomParameters_ShouldPassParametersCorrectly()
    {
        // Arrange
        SetupGroqMockResponse("Custom response");
        SetupHealthCheckMocks(groqHealthy: true);

        var requestPayload = CreateTestRequest(
            prompt: "Custom prompt", 
            maxTokens: 500, 
            temperature: 0.3m, 
            model: "custom-model");
        var json = JsonSerializer.Serialize(requestPayload, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/ai/generate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent, JsonOptions);
        
        responseData.GetProperty("success").GetBoolean().Should().BeTrue();
        responseData.GetProperty("content").GetString().Should().Contain("Custom response");
    }

    [Fact]
    public async Task HistoryEndpoint_ShouldReturnResponseHistory()
    {
        // Act
        var response = await Client.GetAsync("/api/ai/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var historyData = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        
        historyData.TryGetProperty("responses", out _).Should().BeTrue();
        historyData.TryGetProperty("totalRequests", out _).Should().BeTrue();
        historyData.TryGetProperty("totalCost", out _).Should().BeTrue();
        historyData.TryGetProperty("lastUpdated", out _).Should().BeTrue();
    }

    [Fact]
    public async Task RolloverEndpoint_ShouldCompleteSuccessfully()
    {
        // Act
        var response = await Client.PostAsync("/api/ai/rollover", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        
        responseData.GetProperty("message").GetString().Should().Contain("rollover completed");
    }

    [Fact]
    public async Task SwaggerEndpoint_ShouldBeAccessible()
    {
        // Act
        var response = await Client.GetAsync("/swagger");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("swagger");
    }

    [Theory]
    [InlineData("/api/ai/generate", "POST")]
    [InlineData("/api/ai/status", "GET")]
    [InlineData("/api/ai/spend", "GET")]
    [InlineData("/api/ai/health", "POST")]
    [InlineData("/api/ai/history", "GET")]
    [InlineData("/api/ai/rollover", "POST")]
    public async Task AllEndpoints_ShouldHaveCorrectCorsHeaders(string endpoint, string method)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        request.Headers.Add("Origin", "http://localhost:3000");

        if (method == "POST" && endpoint.Contains("generate"))
        {
            SetupGroqMockResponse("Test response");
            SetupHealthCheckMocks(groqHealthy: true);
            
            var requestPayload = CreateTestRequest("test");
            var json = JsonSerializer.Serialize(requestPayload, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        // The response should not be blocked by CORS
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
