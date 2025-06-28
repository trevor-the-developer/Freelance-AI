using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace FreelanceAI.Core.Tests.Assertions;

public static class RouterConfigurationAssertions
{
    public static AndConstraint<ObjectAssertions> BeValidConfiguration(
        this ObjectAssertions assertions)
    {
        return assertions.Match(config => ValidateConfiguration(config),
            "configuration should pass validation");
    }

    public static AndConstraint<ObjectAssertions> BeInvalidConfiguration(
        this ObjectAssertions assertions)
    {
        return assertions.Match(config => !ValidateConfiguration(config),
            "configuration should fail validation");
    }

    private static bool ValidateConfiguration(object config)
    {
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(config, context, results, validateAllProperties: true);
    }
}