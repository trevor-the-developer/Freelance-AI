# Building a Free AI-Powered Freelance Development Workflow with C# .NET on Debian 12

Ever wanted to replicate the power of Warp Terminal and Claude Code but without the monthly subscription costs? As a freelancer, every spend counts, and this guide shows you how to build a decent AI workflow using completely free APIs and open source tools, all orchestrated with C# .NET running natively on Debian 12.

This setup will give you AI-powered terminal assistance, intelligent code generation for C#/.NET, JavaScript, and web technologies, plus smart project management - all running locally with some clever free API integration.

## What You're Getting

By the end of this guide, you'll have:
- **AI Terminal Assistant** (replaces Warp's AI features)
- **Code Generation Service** (replaces Claude Code functionality)
- **Project Manager** (handles guardrails, workflows, client requirements)
- **Multi-language Support** (C#, JavaScript, HTML/CSS)
- **Cost: £0/$0/month** (using free API tiers in a smart way)
- **Native Debian 12 Integration**

## Prerequisites

**Hardware:**
- Decent laptop (I prototyped this solution on my i9 64GB machine, but 16GB+ RAM will work fine)
- At least 20GB free disk space 
- SSD recommended for Docker performance (I have a fast NVMe drive)

**Software (Debian 12):**
- .NET 8 SDK
- Docker Engine + Docker Compose
- Git, curl, jq
- Ollama (for local AI models)

**Free API Keys (5 minutes to sign up):**
- Groq API (100 requests/hour free)
- Together AI ($25 free credit)
- Hugging Face (1000 requests/month free)

## Debian 12 Installation & Setup

### Step 1: Install Prerequisites

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install .NET 8 SDK
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools' >> ~/.bashrc
source ~/.bashrc

# Install Docker
sudo apt install docker.io docker-compose docker-buildx -y
sudo usermod -aG docker $USER
newgrp docker

# Install development tools
sudo apt install git curl jq build-essential -y

# Install Ollama (for local AI models)
curl -fsSL https://ollama.ai/install.sh | sh

# Verify installations
dotnet --version
docker --version
ollama --version
```

### Step 2: Project Setup

```bash
# Create project directory
mkdir freelance-ai && cd freelance-ai

# Run automated setup
curl -fsSL https://raw.githubusercontent.com/your-repo/freelance-ai/main/scripts/debian-setup.sh | bash
```

Or manual setup:

```bash
# Create project structure
mkdir -p {src/{FreelanceAI.Core,FreelanceAI.ApiRouter,FreelanceAI.TerminalAssistant,FreelanceAI.CodeService,FreelanceAI.ProjectManager,FreelanceAI.WebApi},docker,configs/{guardrails,project-templates},scripts,shared,tests,generated}

# Create .NET solution
dotnet new sln -n FreelanceAI

# Create projects
dotnet new classlib -n FreelanceAI.Core -o src/FreelanceAI.Core
dotnet new classlib -n FreelanceAI.ApiRouter -o src/FreelanceAI.ApiRouter
dotnet new classlib -n FreelanceAI.TerminalAssistant -o src/FreelanceAI.TerminalAssistant
dotnet new classlib -n FreelanceAI.CodeService -o src/FreelanceAI.CodeService
dotnet new classlib -n FreelanceAI.ProjectManager -o src/FreelanceAI.ProjectManager
dotnet new webapi -n FreelanceAI.WebApi -o src/FreelanceAI.WebApi

# Add projects to solution
dotnet sln add src/FreelanceAI.Core
dotnet sln add src/FreelanceAI.ApiRouter
dotnet sln add src/FreelanceAI.TerminalAssistant
dotnet sln add src/FreelanceAI.CodeService
dotnet sln add src/FreelanceAI.ProjectManager
dotnet sln add src/FreelanceAI.WebApi

# Set up project references
dotnet add src/FreelanceAI.WebApi reference src/FreelanceAI.Core
dotnet add src/FreelanceAI.WebApi reference src/FreelanceAI.ApiRouter
dotnet add src/FreelanceAI.WebApi reference src/FreelanceAI.TerminalAssistant
dotnet add src/FreelanceAI.WebApi reference src/FreelanceAI.CodeService
dotnet add src/FreelanceAI.WebApi reference src/FreelanceAI.ProjectManager

dotnet add src/FreelanceAI.ApiRouter reference src/FreelanceAI.Core
dotnet add src/FreelanceAI.TerminalAssistant reference src/FreelanceAI.Core
dotnet add src/FreelanceAI.CodeService reference src/FreelanceAI.Core
dotnet add src/FreelanceAI.ProjectManager reference src/FreelanceAI.Core

# Add NuGet packages
dotnet add src/FreelanceAI.WebApi package Microsoft.AspNetCore.OpenApi
dotnet add src/FreelanceAI.WebApi package Swashbuckle.AspNetCore
dotnet add src/FreelanceAI.WebApi package StackExchange.Redis
dotnet add src/FreelanceAI.ApiRouter package StackExchange.Redis
dotnet add src/FreelanceAI.ApiRouter package System.Text.Json

# Clean up default files
rm src/FreelanceAI.Core/Class1.cs
rm src/FreelanceAI.ApiRouter/Class1.cs
rm src/FreelanceAI.TerminalAssistant/Class1.cs
rm src/FreelanceAI.CodeService/Class1.cs
rm src/FreelanceAI.ProjectManager/Class1.cs
rm src/FreelanceAI.WebApi/WeatherForecast.cs
rm src/FreelanceAI.WebApi/Controllers/WeatherForecastController.cs
```

### Step 3: Environment Configuration

```bash
# Create environment file
cat > .env << 'EOF'
# Free API Keys (sign up links in comments)
GROQ_API_KEY=your_groq_key_here                    # https://console.groq.com
TOGETHER_API_KEY=your_together_key_here            # https://together.ai  
HUGGINGFACE_API_KEY=your_huggingface_key_here      # https://huggingface.co

# Configuration
DAILY_BUDGET=5.00
REDIS_CONNECTION=localhost:6379
OLLAMA_URL=http://localhost:11434

# Logging
LOG_LEVEL=Information
ASPNETCORE_ENVIRONMENT=Development
EOF

echo "📝 Created .env file - update with your API keys"
```

## Core Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Terminal      │    │   Code Service   │    │  Project        │
│   Assistant     │◄──►│   (C#/JS/Web)    │◄──►│  Manager        │
│                 │    │                  │    │  (Guardrails)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                        │                       │
         └────────────────────────┼───────────────────────┘
                                  │
                    ┌─────────────▼──────────────┐
                    │     AI API Router          │
                    │  (Groq → Together → HF)    │
                    └────────────────────────────┘
```

## TODO add more sections (when I finish break-fixing my stuff).
