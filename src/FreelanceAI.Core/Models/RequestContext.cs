namespace FreelanceAI.Core.Models;

public record RequestContext(
    string Prompt,
    AIRequestOptions Options,
    DateTime StartTime
);