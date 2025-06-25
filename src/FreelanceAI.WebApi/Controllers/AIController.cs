using FreelanceAI.Core.Interfaces;
using FreelanceAI.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace FreelanceAI.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly IJsonFileService _fileService;
    private readonly ILogger<AIController> _logger;
    private readonly ISmartApiRouter _router;

    public AIController(
        ISmartApiRouter router,
        ILogger<AIController> logger,
        IJsonFileService fileService)
    {
        _router = router;
        _logger = logger;
        _fileService = fileService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<object>> GenerateAsync([FromBody] GenerateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Prompt)) return BadRequest("Prompt is required");

            var options = BuildAIRequestOptions(request);
            var response = await _router.RouteRequestAsync(request.Prompt, options);

            // Log the response to file
            await LogResponseToFile(request, response);

            LogBasedOnResponseType(response);

            return HandleResponseTypes(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing AI request");
            return StatusCode(500, new
            {
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
                status,
                healthyCount,
                totalProviders,
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new HealthResponse(
                "Error",
                0,
                0,
                DateTime.UtcNow
            ));
        }
    }

    // New endpoint to get response history
    [HttpGet("history")]
    public async Task<ActionResult<AIResponseHistory>> GetResponseHistoryAsync()
    {
        try
        {
            var history = await _fileService.LoadAsync<AIResponseHistory>();
            return Ok(history ?? new AIResponseHistory());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading response history");
            return StatusCode(500, new { error = "Failed to load response history" });
        }
    }

    // New endpoint to force file rollover
    [HttpPost("rollover")]
    public async Task<ActionResult> ForceRolloverAsync()
    {
        try
        {
            await _fileService.ForceRolloverAsync();
            return Ok(new { message = "File rollover completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forced rollover");
            return StatusCode(500, new { error = "Failed to rollover file" });
        }
    }

    private ActionResult ServiceUnavailable(object value)
    {
        return StatusCode(503, value);
    }

    private static AIRequestOptions BuildAIRequestOptions(GenerateRequest request)
    {
        var options = new AIRequestOptions(
            request.MaxTokens ?? 1000,
            request.Temperature ?? 0.7m,
            request.Model ?? "default",
            request.StopSequences ?? new List<string>()
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
            AISuccess success => Ok(new
            {
                success = true,
                content = success.Content,
                provider = success.Provider,
                cost = success.RequestCost,
                duration = success.Duration.TotalMilliseconds
            }),
            AIFailure failure => ServiceUnavailable(new
            {
                success = false,
                error = failure.Error,
                failedProviders = failure.FailedProviders,
                totalAttemptedCost = failure.TotalAttemptedCost,
                duration = failure.Duration.TotalMilliseconds
            }),
            _ => StatusCode(500, new
            {
                success = false,
                error = "Unknown response type"
            })
        };
    }

    private async Task LogResponseToFile(GenerateRequest request, AIResponse response)
    {
        try
        {
            // Load existing history or create new
            var history = await _fileService.LoadAsync<AIResponseHistory>() ?? new AIResponseHistory();

            // Add new entry
            var entry = new AIResponseEntry(
                Guid.NewGuid(),
                DateTime.UtcNow,
                request.Prompt,
                request.MaxTokens,
                request.Temperature,
                request.Model,
                response is AISuccess,
                response is AISuccess success ? success.Provider : null,
                response is AISuccess successContent ? successContent.Content : null,
                response is AIFailure failure ? failure.Error : null,
                response is AISuccess successCost ? successCost.RequestCost : 0,
                response.Duration.TotalMilliseconds
            );

            // Create new history with updated values
            var updatedResponses = new List<AIResponseEntry>(history.Responses) { entry };
            var updatedHistory = history with
            {
                Responses = updatedResponses,
                LastUpdated = DateTime.UtcNow,
                TotalRequests = updatedResponses.Count,
                TotalCost = updatedResponses.Sum(r => r.Cost)
            };

            // Save back to file
            await _fileService.WriteAsync(updatedHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log response to file");
            // Don't throw - this is non-critical functionality
        }
    }
}