using FreelanceAI.Core.Models;
using Microsoft.Extensions.Options;

namespace FreelanceAI.Core.Validators;

public class JsonFileServiceOptionsValidator : IValidateOptions<JsonFileServiceOptions>
{
    public ValidateOptionsResult Validate(string? name, JsonFileServiceOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.FilePath))
            failures.Add("FilePath cannot be null or empty");

        if (options.MaxFileSizeBytesValue <= 0)
            failures.Add("MaxFileSizeInBytes must be greater than 0");

        if (options.MaxFileAgeValue <= TimeSpan.Zero)
            failures.Add("MaxFileAge must be greater than 0");

        if (string.IsNullOrWhiteSpace(options.RolloverDirectory))
            failures.Add("RolloverDirectory cannot be null or empty");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}