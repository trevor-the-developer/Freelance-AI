using FreelanceAI.Core.Configuration;
using Xunit;

namespace FreelanceAI.Core.Tests.TestData;

public static class RouterConfigurationTestData
{
    private static readonly ProviderLimitConfiguration ValidProviderLimit = new()
    {
        RequestLimit = 100,
        LimitType = "Hour",
        CostPerToken = 0.0001m,
        DailyBudgetLimit = 5.0m
    };

    public static readonly TheoryData<decimal> ValidBudgetValues = new()
    {
        0.0m, 5.0m, 100.0m, 1000.0m
    };

    public static readonly TheoryData<decimal> InvalidBudgetValues = new()
    {
        -1.0m, -100.0m
    };

    public static readonly TheoryData<int> ValidRetryValues = new()
    {
        1, 3, 5, 10
    };

    public static readonly TheoryData<int> InvalidRetryValues = new()
    {
        0, -1, 11, 100
    };
}