using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WireMock.Server;
using WireMock.Settings;

namespace FreelanceAI.Integration.Tests;

public class IntegrationTestBase : IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly WireMockServer GroqMockServer;
    protected readonly WireMockServer OllamaMockServer;

    protected readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IntegrationTestBase()
    {
        // Setup WireMock servers for external dependencies
        GroqMockServer = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 9999,
            StartAdminInterface = false
        });

        OllamaMockServer = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 9998,
            StartAdminInterface = false
        });

        // Create test application factory
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.Test.json", optional: false);
                });
                builder.ConfigureServices(services =>
                {
                    // Override any services if needed for testing
                    services.Configure<LoggerFilterOptions>(options =>
                    {
                        options.MinLevel = LogLevel.Warning;
                    });
                });
            });

        Client = Factory.CreateClient();
    }

    /// <summary>
    /// Sets up a mock Groq API response
    /// </summary>
    protected void SetupGroqMockResponse(string content, int statusCode = 200)
    {
        var responseBody = new
        {
            choices = new[]
            {
                new
                {
                    message = new { content }
                }
            }
        };

        GroqMockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/chat/completions")
                .UsingPost())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(responseBody, JsonOptions)));
    }

    /// <summary>
    /// Sets up a mock Ollama API response
    /// </summary>
    protected void SetupOllamaMockResponse(string content, int statusCode = 200)
    {
        var responseBody = new
        {
            response = content
        };

        OllamaMockServer
            .Given(WireMock.RequestBuilders.Request.Create()
                .WithPath("/api/generate")
                .UsingPost())
            .RespondWith(WireMock.ResponseBuilders.Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(responseBody, JsonOptions)));
    }

    /// <summary>
    /// Sets up mock health check responses
    /// </summary>
    protected void SetupHealthCheckMocks(bool groqHealthy = true, bool ollamaHealthy = true)
    {
        // Groq health check (using simple completion request)
        if (groqHealthy)
        {
            SetupGroqMockResponse("test");
        }
        else
        {
            GroqMockServer
                .Given(WireMock.RequestBuilders.Request.Create()
                    .WithPath("/chat/completions")
                    .UsingPost())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(503)
                    .WithBody("Service Unavailable"));
        }

        // Ollama health check
        if (ollamaHealthy)
        {
            var healthResponse = new
            {
                models = new[]
                {
                    new { name = "test-model", size = "1.2GB" }
                }
            };

            OllamaMockServer
                .Given(WireMock.RequestBuilders.Request.Create()
                    .WithPath("/api/tags")
                    .UsingGet())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(healthResponse, JsonOptions)));
        }
        else
        {
            OllamaMockServer
                .Given(WireMock.RequestBuilders.Request.Create()
                    .WithPath("/api/tags")
                    .UsingGet())
                .RespondWith(WireMock.ResponseBuilders.Response.Create()
                    .WithStatusCode(503)
                    .WithBody("Service Unavailable"));
        }
    }

    /// <summary>
    /// Resets all mock servers
    /// </summary>
    protected void ResetMocks()
    {
        GroqMockServer.Reset();
        OllamaMockServer.Reset();
    }

    /// <summary>
    /// Creates a test request payload
    /// </summary>
    protected object CreateTestRequest(
        string prompt = "test prompt",
        int? maxTokens = null,
        decimal? temperature = null,
        string? model = null)
    {
        var request = new Dictionary<string, object>
        {
            ["prompt"] = prompt
        };

        if (maxTokens.HasValue)
            request["maxTokens"] = maxTokens.Value;

        if (temperature.HasValue)
            request["temperature"] = temperature.Value;

        if (!string.IsNullOrEmpty(model))
            request["model"] = model;

        return request;
    }

    public void Dispose()
    {
        GroqMockServer?.Stop();
        GroqMockServer?.Dispose();
        OllamaMockServer?.Stop();
        OllamaMockServer?.Dispose();
        Client?.Dispose();
        Factory?.Dispose();
        GC.SuppressFinalize(this);
    }
}
