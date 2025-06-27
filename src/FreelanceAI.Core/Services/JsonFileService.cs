using System.Text.Json;
using FreelanceAI.Core.Interfaces;
using FreelanceAI.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FreelanceAI.Core.Services;

public class JsonFileService : IJsonFileService
{
    private readonly ILogger<JsonFileService> _logger;
    private readonly JsonFileServiceOptions? _options;
    private readonly SemaphoreSlim _semaphore;
    private readonly bool _enabled;

    public JsonFileService(ILogger<JsonFileService> logger, IOptions<JsonFileServiceOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _semaphore = new SemaphoreSlim(1, 1);
        _enabled = _options?.Enabled ?? false;

        // Ensure directories exist
        EnsureDirectoriesExist();
    }

    public async Task CreateFileAsync()
    {
        if(!_enabled) return;
        
        await _semaphore.WaitAsync();
        try
        {
            if (!await FileExistsAsync())
            {
                await File.WriteAllTextAsync(_options?.FilePath ?? string.Empty, "{}");
                _logger.LogInformation("Created new file: {FilePath}", _options?.FilePath);
            }
            else
            {
                _logger.LogDebug("File already exists: {FilePath}", _options?.FilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating file: {FilePath}", _options?.FilePath);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<T?> LoadAsync<T>() where T : class
    {
        if(!_enabled)  return null;

        await _semaphore.WaitAsync();
        try
        {
            if (!await FileExistsAsync())
            {
                _logger.LogWarning("Attempted to load non-existent file: {FilePath}", _options?.FilePath);
                return null;
            }

            await RolloverIfNeededAsync();

            var jsonContent = await File.ReadAllTextAsync(_options?.FilePath ?? string.Empty);

            if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Trim() == "{}")
            {
                _logger.LogDebug("File is empty or contains empty JSON object: {FilePath}", _options?.FilePath);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(jsonContent, _options?.JsonOptions);
            _logger.LogDebug("Successfully loaded data from: {FilePath}", _options?.FilePath);
            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for file: {FilePath}", _options?.FilePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading file: {FilePath}", _options?.FilePath);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task WriteAsync<T>(T data) where T : class
    {
        if(!_enabled) return;
        
        await _semaphore.WaitAsync();
        try
        {
            if (data == null)
            {
                _logger.LogWarning("Attempted to write null data to file: {FilePath}", _options?.FilePath);
                return;
            }

            await RolloverIfNeededAsync();

            var jsonContent = JsonSerializer.Serialize(data, _options?.JsonOptions);
            await File.WriteAllTextAsync(_options?.FilePath ?? string.Empty, jsonContent);

            _logger.LogDebug("Successfully wrote data to: {FilePath}, Size: {Size} bytes",
                _options?.FilePath, jsonContent.Length);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error for file: {FilePath}", _options?.FilePath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to file: {FilePath}", _options?.FilePath);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<T?> ReadAsync<T>() where T : class
    {
        if(!_enabled) return null;

        // ReadAsync is essentially the same as LoadAsync in this implementation
        return await LoadAsync<T>();
    }

    public async Task<bool> FileExistsAsync()
    {
        if(!_enabled) return true;
        
        return await Task.FromResult(File.Exists(_options?.FilePath));
    }

    public async Task RolloverIfNeededAsync()
    {
        if(!_enabled) return;

        if (!await FileExistsAsync())
            return;

        var fileInfo = new FileInfo(_options?.FilePath ?? string.Empty);
        var shouldRollover = false;
        var reason = string.Empty;

        // Check file size
        if (fileInfo.Length > _options?.MaxFileSizeBytesValue)
        {
            shouldRollover = true;
            reason = $"size exceeded ({fileInfo.Length} bytes > {_options?.MaxFileSizeBytesValue} bytes)";
        }

        // Check file age
        var fileAge = DateTime.Now - fileInfo.CreationTime;
        if (fileAge > _options?.MaxFileAgeValue)
        {
            shouldRollover = true;
            reason = string.IsNullOrEmpty(reason)
                ? $"age exceeded ({fileAge.TotalDays:F1} days > {_options?.MaxFileAgeValue.TotalDays} days)"
                : $"{reason} and age exceeded ({fileAge.TotalDays:F1} days > {_options?.MaxFileAgeValue.TotalDays} days)";
        }

        if (shouldRollover)
        {
            _logger.LogInformation("Rolling over file due to: {Reason}", reason);
            await PerformRolloverAsync();
        }
    }

    public async Task ForceRolloverAsync()
    {
        if(!_enabled) return;

        await _semaphore.WaitAsync();
        try
        {
            if (await FileExistsAsync())
            {
                _logger.LogInformation("Forcing rollover of file: {FilePath}", _options?.FilePath);
                await PerformRolloverAsync();
            }
            else
            {
                _logger.LogWarning("Cannot force rollover - file does not exist: {FilePath}", _options?.FilePath);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task PerformRolloverAsync()
    {
        if(!_enabled) return;

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var fileName = Path.GetFileNameWithoutExtension(_options?.FilePath);
        var extension = Path.GetExtension(_options?.FilePath);
        var rolledOverFileName = $"{fileName}_{timestamp}{extension}";
        var rolledOverPath = Path.Combine(_options?.RolloverDirectory ?? string.Empty, rolledOverFileName);

        try
        {
            File.Move(_options?.FilePath ?? string.Empty, rolledOverPath);
            _logger.LogInformation("File rolled over from {OriginalPath} to {RolledOverPath}",
                _options?.FilePath, rolledOverPath);

            // Create a new empty file
            await CreateFileAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during file rollover from {OriginalPath} to {RolledOverPath}",
                _options?.FilePath, rolledOverPath);
            throw;
        }
    }

    private void EnsureDirectoriesExist()
    {
        if(!_enabled) return;

        try
        {
            // Ensure the main file directory exists
            var fileDirectory = Path.GetDirectoryName(_options?.FilePath);
            if (!string.IsNullOrEmpty(fileDirectory) && !Directory.Exists(fileDirectory))
            {
                Directory.CreateDirectory(fileDirectory);
                _logger.LogDebug("Created directory: {Directory}", fileDirectory);
            }

            // Ensure the rollover directory exists
            if (!Directory.Exists(_options?.RolloverDirectory))
            {
                Directory.CreateDirectory(_options?.RolloverDirectory ?? string.Empty);
                _logger.LogDebug("Created rollover directory: {Directory}", _options?.RolloverDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating directories");
            throw;
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}