```
├── ALIGNMENT_AND_REVIEW_REPORT.md
├── API_TESTING_GUIDE.md
├── clean-repo.sh
├── current_state_roadmap.md
├── docker
│   └── Dockerfile
├── docker-compose.yml
├── FreelanceAI.sln
├── global.json
├── README.md
├── scripts
│   ├── build-freelance-ai.sh
│   ├── freelance-ai
│   └── lib
│       └── freelance-ai-functions.sh
├── src
│   ├── FreelanceAI.ApiRouter
│   │   ├── FreelanceAI.ApiRouter.csproj
│   │   ├── Providers
│   │   │   ├── GroqProvider.cs
│   │   │   └── OllamaProvider.cs
│   │   └── SmartApiRouter.cs
│   ├── FreelanceAI.Core
│   │   ├── Configuration
│   │   │   ├── ProviderLimitConfiguration.cs
│   │   │   └── RouterConfiguration.cs
│   │   ├── Constants
│   │   │   ├── GroqConstants.cs
│   │   │   └── OllamaConstants.cs
│   │   ├── Extensions
│   │   │   └── ServiceCollectionExtensions.cs
│   │   ├── FreelanceAI.Core.csproj
│   │   ├── Interfaces
│   │   │   ├── IAIProvider.cs
│   │   │   ├── IJsonFileService.cs
│   │   │   ├── ISmartApiRouter.cs
│   │   │   └── IUsageTracker.cs
│   │   ├── Models
│   │   │   ├── AIFailure.cs
│   │   │   ├── AIRequestOptions.cs
│   │   │   ├── AIResponse.cs
│   │   │   ├── AIResponseEntry.cs
│   │   │   ├── AIResponseHistory.cs
│   │   │   ├── AISuccess.cs
│   │   │   ├── DailyUsage.cs
│   │   │   ├── DailyUsageSummary.cs
│   │   │   ├── GenerateRequest.cs
│   │   │   ├── HealthResponse.cs
│   │   │   ├── JsonFileServiceOptions.cs
│   │   │   ├── ProviderAttemptResult.cs
│   │   │   ├── ProviderConfig.cs
│   │   │   ├── ProviderStatus.cs
│   │   │   ├── ProviderUsage.cs
│   │   │   ├── RequestContext.cs
│   │   │   ├── RoutingRequest.cs
│   │   │   └── WeeklyReport.cs
│   │   ├── Services
│   │   │   ├── JsonFileService.cs
│   │   │   └── UsageTracker.cs
│   │   └── Validators
│   │       └── JsonFileServiceOptionsValidator.cs
│   └── FreelanceAI.WebApi
│       ├── appsettings.json
│       ├── appsettings.Test.json
│       ├── Controllers
│       │   └── AIController.cs
│       ├── FreelanceAI.WebApi.csproj
│       ├── FreelanceAI.WebApi.http
│       ├── Program.cs
│       └── Properties
│           └── launchSettings.json
├── test-api.sh
└── tests
    ├── FreelanceAI.ApiRouter.Tests
    │   ├── FreelanceAI.ApiRouter.Tests.csproj
    │   └── SmartApiRouterTests.cs
    ├── FreelanceAI.Core.Tests
    │   ├── Assertions
    │   │   └── RouterConfigurationAssertions.cs
    │   ├── Configuration
    │   │   └── RouterConfigurationTests.cs
    │   ├── FreelanceAI.Core.Tests.csproj
    │   ├── Helpers
    │   │   └── TestHelpers.cs
    │   ├── Models
    │   │   └── AIRequestOptionsTests.cs
    │   └── TestData
    │       └── RouterConfigurationTestData.cs
    └── FreelanceAI.Integration.Tests
        ├── ApiIntegrationTests.cs
        ├── appsettings.Test.json
        ├── FreelanceAI.Integration.Tests.csproj
        └── IntegrationTestBase.cs

27 directories, 67 files
```
