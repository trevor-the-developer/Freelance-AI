using FreelanceAI.Core.Interfaces;
using FreelanceAI.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace FreelanceAI.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly ISmartApiRouter _router;
    private readonly ILogger<AIController> _logger;

    public AIController(ISmartApiRouter router, ILogger<AIController> logger)
    {
        _router = router;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<object>> GenerateAsync([FromBody] GenerateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest("Prompt is required");
            }

            var options = BuildAIRequestOptions(request);

            var response = await _router.RouteRequestAsync(request.Prompt, options);
            
            LogBasedOnResponseType(response);
            
            return HandleResponseTypes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing AI request");
            return StatusCode(500, new { 
                success = false, 
                error = "Internal server error" 
            });
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult<List<ProviderStatus>>> GetStatusAsync()
    {
        try
        {
            var status = await _router.GetProviderStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provider status");
            return StatusCode(500, new { error = "Failed to get provider status" });
        }
    }

    [HttpGet("spend")]
    public async Task<ActionResult<decimal>> GetTodaySpendAsync()
    {
        try
        {
            var todaySpend = await _router.GetTodaySpendAsync();
            return Ok(todaySpend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's spend");
            return StatusCode(500, 0.0m);
        }
    }

    [HttpPost("health")]
    public async Task<ActionResult<HealthResponse>> HealthCheckAsync()
    {
        try
        {
            var providers = await _router.GetProviderStatusAsync();
            var healthyCount = providers.Count(p => p.IsHealthy);
            var totalProviders = providers.Count;

            var isHealthy = healthyCount > 0;
            var status = isHealthy ? "Healthy" : "Unhealthy";

            return Ok(new HealthResponse(
                Status: status,
                HealthyProviders: healthyCount,
                TotalProviders: totalProviders,
                Timestamp: DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new HealthResponse(
                Status: "Error",
                HealthyProviders: 0,
                TotalProviders: 0,
                Timestamp: DateTime.UtcNow
            ));
        }
    }

    private ActionResult ServiceUnavailable(object value) =>
        StatusCode(503, value);
    
    private static AIRequestOptions BuildAIRequestOptions(GenerateRequest request)
    {
        var options = new AIRequestOptions(
            MaxTokens: request.MaxTokens ?? 1000,
            Temperature: request.Temperature ?? 0.7m,
            Model: request.Model ?? "default",
            StopSequences: request.StopSequences ?? new List<string>()
        );
        return options;
    }

    private void LogBasedOnResponseType(AIResponse response)
    {
        switch (response)
        {
            case AISuccess success:
                _logger.LogInformation("Generated response using {Provider} in {Duration}ms", 
                    success.Provider, success.Duration.TotalMilliseconds);
                break;
            case AIFailure failure:
                _logger.LogWarning("All providers failed: {Error}", failure.Error);
                break;
        }
    }

    private ActionResult<object> HandleResponseTypes(AIResponse response)
    {
        return response switch
        {
            AISuccess success => Ok(new {
                success = true,
                content = success.Content,
                provider = success.Provider,
                cost = success.RequestCost,
                duration = success.Duration.TotalMilliseconds
            }),
            AIFailure failure => ServiceUnavailable(new {
                success = false,
                error = failure.Error,
                failedProviders = failure.FailedProviders,
                totalAttemptedCost = failure.TotalAttemptedCost,
                duration = failure.Duration.TotalMilliseconds
            }),
            _ => StatusCode(500, new { 
                success = false, 
                error = "Unknown response type" 
            })
        };
    }
}