# FreelanceAI - Smart AI Router for Development Work

> **TL;DR**: A lightweight, intelligent AI routing system that automatically selects the best AI provider (Groq, Ollama) for your development tasks. Features a CLI interface for instant code generation, debugging, and explanations with cost tracking and fallback capabilities.

## 🎯 **What is FreelanceAI?**

FreelanceAI is a **foundational AI development assistant** designed to provide a **Warp Terminal-like experience** for .NET, frontend, and documentation development. This is our **lightweight MVP** that we'll expand into a full agentic workflow system.

### **Core Vision**
- 🚀 **Instant AI assistance** through CLI and API
- 🧠 **Smart provider routing** with automatic fallbacks
- 💰 **Cost optimization** and usage tracking
- 🔧 **Developer-focused** responses with production-ready code
- 🏗️ **Extensible foundation** for future agentic workflows

---

## 🚀 **Quick Start**

### **1. Prerequisites**
```bash
# Required
- .NET 8.0 SDK
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
- OR use the bootstrapper to create a fresh local repo: [warp-terminal-clone-bootstrap](https://github.com/trevor-the-developer/warp-terminal-clone-bootstrap)
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
 
- Many more examples in the [README.md](https://github.com/trevor-the-developer/warp-terminal-clone-bootstrap/blob/main/README.md) of the repository!
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
optimize       # Optimization tips
test           # Testing guidance
explain        # Explain concepts/commands
```

### **API Endpoints**
```http
GET  /api/ai/status     # Provider status
GET  /api/ai/spend      # Today's costs
POST /api/ai/generate   # AI generation
POST /api/ai/health     # System health
```

---

## 🏗️ **Architecture**

### **Project Structure**
```
src/
├── FreelanceAI.Core/          # Domain models & interfaces
│   ├── Models/                # Request/response models
│   ├── Interfaces/            # Provider & service contracts
│   ├── Configuration/         # Router configuration
│   └── Constants/             # Provider constants
├── FreelanceAI.ApiRouter/     # Core routing logic
│   ├── SmartApiRouter.cs      # Main routing engine
│   ├── Providers/             # AI provider implementations
│   └── Services/              # Usage tracking
└── FreelanceAI.WebApi/        # HTTP API layer
    ├── Controllers/           # REST endpoints
    └── Program.cs             # DI configuration
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

# Performance optimization
./scripts/freelance-ai optimize "slow LINQ query with multiple joins"
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

### **Manual Testing**
```bash
# 1. Start the API
dotnet run --project src/FreelanceAI.WebApi

# 2. Test health endpoint
curl http://localhost:5000/health

# 3. Test provider status
curl http://localhost:5000/api/ai/status

# 4. Test AI generation
curl -X POST http://localhost:5000/api/ai/generate \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Hello world in C#", "maxTokens": 100}'

# 5. Test CLI commands
./scripts/freelance-ai status
./scripts/freelance-ai code "simple hello world method"
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
FROM mcr.microsoft.com/dotnet/aspnet:8.0
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

## 🔮 **Future Roadmap**

### **Phase 1: Foundation** ✅
- [x] Smart AI routing
- [x] CLI interface
- [x] Basic cost tracking
- [x] Provider fallbacks

### **Phase 2: Enhancement** 🚧
- [ ] Web UI dashboard
- [ ] Advanced analytics
- [ ] Custom provider plugins
- [ ] Response caching

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
- [ ] Performance optimization recommendations
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

**Ready to supercharge your development workflow? Start with `./scripts/freelance-ai status` and explore the possibilities! 🚀**
