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
