namespace FreelanceAI.Core.Models;

public record AISuccess(
    string Content,
    string Provider,
    decimal RequestCost,
    TimeSpan Duration
) : AIResponse(Duration);