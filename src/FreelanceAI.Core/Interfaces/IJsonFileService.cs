namespace FreelanceAI.Core.Interfaces;

public interface IJsonFileService
{
    Task CreateFileAsync();
    Task<T?> LoadAsync<T>() where T : class;
    Task WriteAsync<T>(T data) where T : class;
    Task<T?> ReadAsync<T>() where T : class;
    Task<bool> FileExistsAsync();
    Task RolloverIfNeededAsync();
    Task ForceRolloverAsync();
}