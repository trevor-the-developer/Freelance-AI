using System.Text.Json;

namespace FreelanceAI.Core.Models;

public record JsonFileServiceOptions
{
    public string FilePath { get; init; } = "data.json";
    public string MaxFileSizeInBytes { get; init; } = "10485760"; // 10MB as string for JSON config
    public string MaxFileAge { get; init; } = "7"; // Days as string for JSON config
    public string RolloverDirectory { get; init; } = "archived";
    public bool Enabled { get; init; } = false; // disabled by default

    // Computed properties that handle the string conversion
    public long MaxFileSizeBytesValue
    {
        get
        {
            if (string.IsNullOrWhiteSpace(MaxFileSizeInBytes))
                return 10 * 1024 * 1024; // 10MB default

            // Handle expressions like "5 * 1024 * 1024"
            if (MaxFileSizeInBytes.Contains("*"))
                try
                {
                    var expression = MaxFileSizeInBytes.Replace(" ", "");
                    var parts = expression.Split('*').Select(long.Parse).ToArray();
                    return parts.Aggregate(1L, (acc, val) => acc * val);
                }
                catch
                {
                    return 10 * 1024 * 1024; // Fallback to default
                }

            // Handle plain numbers
            return long.TryParse(MaxFileSizeInBytes, out var size) ? size : 10 * 1024 * 1024;
        }
    }

    public TimeSpan MaxFileAgeValue
    {
        get
        {
            if (string.IsNullOrWhiteSpace(MaxFileAge))
                return TimeSpan.FromDays(7); // Default

            return double.TryParse(MaxFileAge, out var days)
                ? TimeSpan.FromDays(days)
                : TimeSpan.FromDays(7);
        }
    }

    // Keep the JsonOptions for internal use
    public JsonSerializerOptions JsonOptions { get; init; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}