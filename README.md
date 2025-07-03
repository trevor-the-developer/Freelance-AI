# FreelanceAI - Smart AI Router for Development Work

> **TL;DR**: A production-ready, intelligent AI routing system that automatically selects the best AI provider (Groq, Ollama)
> for your development tasks. Features a comprehensive REST API, CLI interface, cost optimization, and enterprise-grade
> monitoring with automatic failover capabilities.

## 🎯 **What is FreelanceAI?**

FreelanceAI is a **smart AI request routing service** built with .NET 9 that intelligently routes AI requests to the best available provider based on health, cost, rate limits, and performance. It provides a robust foundation for AI-powered development workflows with enterprise-grade reliability.

### **🆕 Latest Updates (December 2025)**

- ✅ **Upgraded to .NET 9** for latest performance improvements
- ✅ **Comprehensive Testing Suite** with 65 tests covering unit, integration, and end-to-end scenarios
- ✅ **Enhanced Documentation** with architectural analysis and testing guides
- ✅ **Production Deployment Ready** with Docker support and monitoring
- ✅ **Advanced Analytics** with response history tracking and cost analysis
- ✅ **Code Quality** with automated testing and FluentAssertions for robust validation

### **Core Vision**

- 🚀 **Intelligent request routing** with automatic provider failover
- 🧠 **Smart provider selection** based on health, cost, and performance
- 💰 **Advanced cost optimisation** and real-time usage tracking
- 🔧 **Developer-focused** API with comprehensive monitoring
- 🏗️ **Production-ready architecture** with extensible provider system
- 📊 **Enterprise monitoring** with detailed analytics and reporting

### **Integration and Testing Updates**

- **CLI Synchronization**: Aligned with the warp-terminal-clone-bootstrap project.
- **Documentation Enhanced**: More detailed instructions and alignment.
- **Consistent Configurations**: Updated configuration paths and key integrations.

---

## 🚀 **Quick Start**

### **1. Prerequisites**

```bash
# Required
- .NET 9.0 SDK
- Groq API key (free tier available)

# Optional
- Docker & Docker Compose
- Ollama (local AI fallback)
```

### **2. Setup**

```bash
# Clone and build
git clone <your-repo>
cd FreelanceAI

# Configure API key
# Edit src/FreelanceAI.WebApi/appsettings.Development.json
# Add your Groq API key to "Groq:ApiKey"

# Build and run
dotnet build
dotnet run --project src/FreelanceAI.WebApi
```

### **3. Test CLI**

```bash
# Make CLI executable
chmod +x scripts/freelance-ai

# Test commands
./scripts/freelance-ai status
./scripts/freelance-ai chat
./scripts/freelance-ai code "create a simple REST API controller"
```

### **4. Client Test**

- clone the following repo: [warp-terminal-clone](https://github.com/trevor-the-developer/warp-terminal-clone)
- OR use the bootstrapper to create a fresh local
  repo: [warp-terminal-clone-bootstrap](https://github.com/trevor-the-developer/warp-terminal-clone-bootstrap)
- Run the following command in the Wurp (Warp Terminal Clone) project folder
    - `scripts/wurp-terminal check` to check dependencies
    - `./scripts/wurp-terminal install` to perform a full installation (build, publish, integrate)
    - `./scripts/wurp-terminal` to execute the terminal client
    - `./scripts/wurp-terminal status` show installation status
    - `wurp-terminal` use the installed binary

- Example commands
    - `ai explain "docker ps"        # Explain what a command does`
    - `ai suggest "deploy app"       # Get command suggestions for tasks`
    - `ai debug "permission denied"  # Get debugging help for errors`

- Many more examples in
  the [README.md](https://github.com/trevor-the-developer/warp-terminal-clone-bootstrap/blob/main/README.md) of the
  repository!

---

## 📋 **Features**

### **Smart AI Routing**

- **Priority-based provider selection** (Groq → Ollama)
- **Automatic failover** when providers are unavailable
- **Health monitoring** and cost tracking
- **Rate limiting** and budget controls

### **CLI Interface**

```bash
./scripts/freelance-ai <command> [prompt]

# Available commands:
status          # Check service and provider status
chat           # Interactive chat mode
code           # Generate code
review         # Code review
suggest        # Get suggestions
debug          # Debug assistance
optimise       # Optimisation tips
test           # Testing guidance
explain        # Explain concepts/commands
```

### **API Endpoints**

```http
# Core AI Operations
POST /api/ai/generate   # AI content generation with smart routing
GET  /api/ai/status     # Provider status and availability
GET  /api/ai/spend      # Today's total costs across all providers
POST /api/ai/health     # Detailed system health check

# Response Management
GET  /api/ai/history    # Request/response history and analytics
POST /api/ai/rollover   # Force log file rollover

# System Health
GET  /health           # Basic health endpoint

# Interactive Documentation
GET  /swagger          # Swagger UI for API testing
```

---

## 🏗️ **Architecture**

### **Project Structure**

```
src/
├── FreelanceAI.Core/              # Domain models & interfaces
│   ├── Models/                    # Request/response models
│   │   ├── JsonFileServiceOptions.cs
│   │   ├── GenerateRequest.cs
│   │   ├── AIResponse.cs
│   │   ├── ProviderStatus.cs
│   │   └── HealthResponse.cs
│   ├── Interfaces/                # Provider & service contracts
│   │   ├── IAIProvider.cs
│   │   ├── ISmartApiRouter.cs
│   │   ├── IUsageTracker.cs
│   │   └── IJsonFileService.cs
│   ├── Configuration/             # Router configuration
│   │   ├── RouterConfiguration.cs
│   │   └── ProviderLimitConfiguration.cs
│   ├── Services/                  # Core services
│   │   ├── JsonFileService.cs
│   │   └── UsageTracker.cs
│   └── Constants/                 # Provider constants
│       ├── GroqConstants.cs
│       └── OllamaConstants.cs
├── FreelanceAI.ApiRouter/         # Core routing logic
│   ├── SmartApiRouter.cs          # Main routing engine
│   └── Providers/                 # AI provider implementations
│       ├── GroqProvider.cs
│       └── OllamaProvider.cs
└── FreelanceAI.WebApi/            # HTTP API layer
    ├── Controllers/               # REST endpoints
    │   └── AIController.cs
    ├── Program.cs                 # DI configuration
    └── appsettings.json           # Configuration

# Documentation & Testing
├── API_TESTING_GUIDE.md           # Comprehensive testing guide
├── CODE_REVIEW_AND_IMPROVEMENTS.md # Code analysis and suggestions
├── test-api.sh                    # Automated test script
└── scripts/                       # CLI utilities
    └── freelance-ai               # CLI interface
```

### **Core Components**

#### **SmartApiRouter**

- Routes requests to best available provider
- Tracks usage and costs
- Implements fallback strategies
- Monitors provider health

#### **AI Providers**

- **GroqProvider**: Fast, free-tier primary provider
- **OllamaProvider**: Local fallback for reliability

#### **Usage Tracker**

- Real-time cost monitoring
- Daily/weekly usage reports
- Budget limit enforcement

---

## 🔧 **Configuration**

### **Provider Settings**

```json
{
  "Groq": {
    "ApiKey": "your-groq-api-key",
    "BaseUrl": "https://api.groq.com/openai/v1/",
    "Model": "llama-3.3-70b-versatile",
    "MaxTokens": 32768,
    "Enabled": true
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama2",
    "MaxTokens": 4096,
    "Enabled": false
  },
  "Router": {
    "DailyBudget": 5.0,
    "MaxRetries": 3,
    "EnableCostTracking": true
  }
}
```

### **Environment Setup**

```bash
# Development
export GROQ_API_KEY="your-key-here"
export FREELANCE_AI_URL="http://localhost:5000"

# Production
export ASPNETCORE_ENVIRONMENT="Production"
export Router__DailyBudget="10.0"
```

---

## 📊 **Usage Examples**

### **Code Generation**

```bash
# Generate a REST API controller
./scripts/freelance-ai code "Create a UserController with CRUD operations"

# Generate frontend component
./scripts/freelance-ai code "React component for user profile form"

# Database migration
./scripts/freelance-ai code "Entity Framework migration for adding user roles"
```

### **Code Review & Debugging**

```bash
# Review code quality
./scripts/freelance-ai review "$(cat UserController.cs)"

# Debug specific error
./scripts/freelance-ai debug "NullReferenceException in UserService.GetUser()"

# Performance optimisation
./scripts/freelance-ai optimise "slow LINQ query with multiple joins"
```

### **Learning & Explanation**

```bash
# Explain commands
./scripts/freelance-ai explain "docker-compose up -d"

# Get suggestions
./scripts/freelance-ai suggest "deploy ASP.NET Core app to Azure"

# Testing guidance
./scripts/freelance-ai test "unit testing async methods with xUnit"
```

### **Interactive Mode**

```bash
./scripts/freelance-ai chat
# Starts interactive session for back-and-forth conversations
```

---

## 🧪 **Testing**

### **Quick Testing**

```bash
# 1. Start the API
dotnet run --project src/FreelanceAI.WebApi

# 2. Run comprehensive test suite
./test-api.sh

# 3. Test individual endpoints
curl http://localhost:5000/health
curl http://localhost:5000/api/ai/status
curl -X POST http://localhost:5000/api/ai/generate \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Hello world in C#", "maxTokens": 100}'

# 3.1 Testing history endpoint
# Ensure you have generated a response (see above)
curl http://localhost:5000/api/ai/history

# This will display the stored local history if enabled in the `"JsonFileServiceOptions":` configuration section:
"Enabled": "true"

# Example output:
   {
      "id": "e1aefbcd-8e1f-40e3-8225-b1d5ada894df",
      "timestamp": "2025-06-26T07:19:18.8631974Z",
      "prompt": "Please explain the following command or concept in simple terms for a developer: ls",
      "maxTokens": 500,
      "temperature": 0.7,
      "model": null,
      "success": true,
      "provider": "Groq",
      "content": "**What is \u0060ls\u0060?**\n\n\u0060ls\u0060 is a basic command in Linux and Unix-like operating systems that stands for \u0022list\u0022. It is used to display a list of files and directories in the current working directory.\n\n**How does it work?**\n\nWhen you run the \u0060ls\u0060 command, it displays the following information:\n\n* A list of files and directories in the current directory\n* The names of the files and directories\n* The type of each item (file or directory)\n\n**Common uses of \u0060ls\u0060**\n\n* To view the contents of the current directory\n* To check if a file or directory exists\n* To get a list of files and directories to use in other commands\n\n**Basic \u0060ls\u0060 command options**\n\n* \u0060ls -a\u0060 : Displays all files and directories, including hidden ones (those that start with a dot \u0060.\u0060)\n* \u0060ls -l\u0060 : Displays a detailed list of files and directories, including permissions, ownership, and timestamps\n* \u0060ls -d\u0060 : Displays only the names of directories\n\n**Example usage**\n\n\u0060\u0060\u0060bash\n# Display the contents of the current directory\nls\n\n# Display all files and directories, including hidden ones\nls -a\n\n# Display a detailed list of files and directories\nls -l\n\u0060\u0060\u0060\n\n**Tips for developers**\n\n* Use \u0060ls\u0060 to quickly check the contents of a directory before running other commands\n* Use \u0060ls -l\u0060 to get detailed information about files and directories, such as permissions and ownership\n* Use \u0060ls -a\u0060 to include hidden files and directories in the list",
      "error": null,
      "cost": 0.0000372,
      "duration": 2209.2682
    }
  ],
  "lastUpdated": "2025-06-26T07:19:18.8634939Z",
  "totalRequests": 9,
  "totalCost": 0.0003158
```

# 4. Test CLI commands
./scripts/freelance-ai status
./scripts/freelance-ai code "simple hello world method"
```

### **Comprehensive Testing**

```bash
# Interactive API testing
open http://localhost:5000/swagger

# Load testing
ab -n 100 -c 10 -p test-data.json -T application/json http://localhost:5000/api/ai/generate

# Test different scenarios (see API_TESTING_GUIDE.md)
# - Empty prompt validation
# - Rate limiting behavior
# - Provider failover
# - Cost tracking accuracy
```

### **Testing Documentation**

- **API_TESTING_GUIDE.md** - Comprehensive testing scenarios and examples
- **test-api.sh** - Automated test script for all endpoints
- **current_state_roadmap.md** - Current development state and future roadmap
- **SOLUTION_ARCHITECTURE_ANALYSIS.md** - Deep architectural analysis and design patterns
- **CODE_REVIEW_AND_IMPROVEMENTS.md** - Code quality analysis and improvement suggestions
- **PROJECT_DEVELOPMENT_SUMMARY.md** - Complete development summary and documentation

### **Test Architecture**

```
tests/
├── FreelanceAI.Core.Tests/        # Unit tests for core business logic
│   ├── Configuration/             # Configuration validation tests
│   ├── Models/                    # Model validation and behavior tests
│   └── Helpers/                   # Test utilities and helpers
├── FreelanceAI.ApiRouter.Tests/   # Integration tests for routing logic
│   └── SmartApiRouterTests.cs     # Router behavior and provider tests
└── FreelanceAI.Integration.Tests/ # End-to-end API tests
    ├── ApiIntegrationTests.cs     # Full request flow testing
    ├── IntegrationTestBase.cs     # Test base class and setup
    └── appsettings.Test.json       # Test configuration
```

### **Running Tests**

```bash
# Run all tests (65 total tests across all projects)
dotnet test

# Run specific test project
dotnet test tests/FreelanceAI.Core.Tests/         # Core business logic tests
dotnet test tests/FreelanceAI.ApiRouter.Tests/   # API router integration tests
dotnet test tests/FreelanceAI.Integration.Tests/ # End-to-end API tests

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run with detailed output
dotnet test --verbosity detailed
```

### **Expected Results**

```json
// Provider status response
[
  {
    "name": "Groq",
    "isHealthy": true,
    "requestsToday": 0,
    "costToday": 0.0,
    "remainingRequests": 100
  },
  {
    "name": "Ollama",
    "isHealthy": false,
    "requestsToday": 0,
    "costToday": 0.0,
    "remainingRequests": 0
  }
]
```

---

## 🐳 **Docker Support**

### **Docker Compose**

```bash
# Start with Ollama
docker-compose up -d

# Check services
docker-compose ps
```

### **Dockerfile**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY . .
EXPOSE 5000
ENTRYPOINT ["dotnet", "FreelanceAI.WebApi.dll"]
```

---

## 📈 **Monitoring & Analytics**

### **Cost Tracking**

```bash
# Check today's spend
./scripts/freelance-ai status

# View detailed usage
curl http://localhost:5000/api/ai/spend
```

### **Performance Metrics**

- Response times per provider
- Success/failure rates
- Token usage patterns
- Cost per request tracking

---

### Use cases

- Test creation
- Test refactoring
- DSL

---

## 🌟 **Enhanced Features**

### **Production-Ready Features**

- **Smart Request Routing** - Intelligent provider selection based on health, cost, and performance
- **Comprehensive API** - Full REST API with Swagger documentation
- **Response History** - Complete request/response tracking with analytics
- **Cost Optimisation** - Real-time cost tracking and budget management
- **Health Monitoring** - Detailed provider health checks and system status
- **File Management** - Automatic log rotation and backup capabilities
- **Configuration Management** - Flexible JSON-based configuration with hot reload support
- **Enterprise Logging** - Structured logging with multiple output formats

### **Advanced Monitoring**

- **Provider Analytics** - Real-time provider performance metrics
- **Usage Tracking** - Detailed usage patterns and cost analysis
- **Health Dashboards** - System health monitoring and alerting
- **Response Analytics** - Request/response history with filtering and search

## 🔮 **Future Roadmap**

### **Phase 1: Foundation** ✅

- [x] Smart AI routing with priority-based selection
- [x] CLI interface with multiple commands
- [x] Advanced cost tracking and budget management
- [x] Provider fallbacks with health monitoring
- [x] REST API with comprehensive endpoints
- [x] Response history and analytics
- [x] Swagger documentation and testing
- [x] Production-ready logging and monitoring

### **Phase 2: Enhancement** 🚧

- [x] Comprehensive test suite and documentation
- [ ] Request caching and deduplication
- [ ] Circuit breaker patterns
- [ ] Advanced metrics collection
- [ ] Web UI dashboard
- [ ] Custom provider plugins

### **Phase 3: Agentic Workflows** 🔮

- [ ] Multi-step task planning
- [ ] Context-aware conversations
- [ ] Integration with development tools
- [ ] Automated code reviews
- [ ] Project scaffolding
- [ ] Documentation generation

### **Phase 4: Advanced AI** 🌟

- [ ] Code repository analysis
- [ ] Intelligent refactoring suggestions
- [ ] Automated testing generation
- [ ] Performance optimisation recommendations
- [ ] Security vulnerability detection

---

## 🛠️ **Development**

### **Adding New Providers**

1. Implement `IAIProvider` interface
2. Register in `Program.cs`
3. Add configuration section
4. Update provider priority logic

### **Extending CLI Commands**

1. Add command to `freelance-ai-functions.sh`
2. Update prompt templates
3. Test with various scenarios

### **API Extensions**

1. Add new endpoints to `AIController`
2. Implement business logic in `SmartApiRouter`
3. Update OpenAPI documentation

---

## 🤝 **Contributing**

### **Development Setup**

```bash
# Fork and clone
git clone <your-fork>
cd FreelanceAI

# Create feature branch
git checkout -b feature/your-feature

# Make changes and test
dotnet test
./scripts/freelance-ai status

# Commit and push
git commit -m "Add: your feature description"
git push origin feature/your-feature
```

### **Guidelines**

- Follow C# coding conventions
- Add unit tests for new features
- Update documentation
- Test CLI functionality
- Ensure Docker compatibility

---

## 📄 **License**

MIT License - see LICENSE file for details.

---

## 🆘 **Support**

- **Issues**: GitHub Issues
- **Discussions**: GitHub Discussions
- **Documentation**: This README + code comments
- **Examples**: `/examples` directory (coming soon)

---

**Ready to supercharge your development workflow? Start with `./scripts/freelance-ai status` and explore the
possibilities! 🚀**
