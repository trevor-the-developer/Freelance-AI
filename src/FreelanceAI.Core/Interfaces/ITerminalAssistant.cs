namespace FreelanceAI.Core.Interfaces;

public interface ITerminalAssistant
{
    Task<string> ExplainCommandAsync(string command);
    Task<string> SuggestCommandAsync(string intent);
    Task<string> DebugErrorAsync(string error, string context = "");
    Task<string> GenerateScriptAsync(string task, string language = "bash");
    Task<string> OptimizeWorkflowAsync(string currentWorkflow);
}