#!/bin/bash
# scripts/lib/freelance-ai-functions.sh
# Function library for FreelanceAI CLI

# Configuration
API_BASE="http://localhost:5000/api"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# ========================================
# CORE API FUNCTIONS
# ========================================

# Function to make API calls
call_api() {
    local endpoint=$1
    local data=$2

    response=$(curl -s -X POST "$API_BASE/$endpoint" \
        -H "Content-Type: application/json" \
        -d "$data" 2>/dev/null)

    if [ $? -eq 0 ] && [ -n "$response" ]; then
        echo "$response"
    else
        echo -e "${RED}❌ API call failed. Is the service running?${NC}" >&2
        echo -e "${YELLOW}Start with: dotnet run --project src/FreelanceAI.WebApi${NC}" >&2
        return 1
    fi
}

# Function to make GET API calls
call_api_get() {
    local endpoint=$1

    response=$(curl -s -X GET "$API_BASE/$endpoint" \
        -H "Content-Type: application/json" 2>/dev/null)

    if [ $? -eq 0 ] && [ -n "$response" ]; then
        echo "$response"
    else
        echo -e "${RED}❌ API call failed. Is the service running?${NC}" >&2
        return 1
    fi
}

# ========================================
# UTILITY FUNCTIONS
# ========================================

# Function to extract JSON field
get_json_field() {
    local json=$1
    local field=$2
    echo "$json" | jq -r ".$field // empty" 2>/dev/null
}

# Function to check if jq is installed
check_dependencies() {
    if ! command -v jq &> /dev/null; then
        echo -e "${RED}❌ jq is required but not installed.${NC}" >&2
        echo -e "${YELLOW}Install with: sudo apt install jq (Ubuntu) or brew install jq (macOS)${NC}" >&2
        return 1
    fi
    
    if ! command -v curl &> /dev/null; then
        echo -e "${RED}❌ curl is required but not installed.${NC}" >&2
        return 1
    fi
    
    return 0
}

# Function to format AI response with error handling
format_ai_response() {
    local response=$1
    local success_field=${2:-"content"}
    
    # Check if response contains error
    local error=$(get_json_field "$response" "error")
    if [ -n "$error" ] && [ "$error" != "null" ]; then
        echo -e "${RED}❌ Error: $error${NC}"
        
        # Show failed providers if available
        local failed_providers=$(get_json_field "$response" "failedProviders")
        if [ -n "$failed_providers" ] && [ "$failed_providers" != "null" ]; then
            echo -e "${YELLOW}Failed providers: $failed_providers${NC}"
        fi
        return 1
    fi
    
    # Extract successful response
    local content=$(get_json_field "$response" "$success_field")
    local provider=$(get_json_field "$response" "provider")
    local cost=$(get_json_field "$response" "requestCost")
    local duration=$(get_json_field "$response" "duration")
    
    if [ -n "$content" ] && [ "$content" != "null" ]; then
        echo -e "${GREEN}$content${NC}"
        
        # Show metadata if available
        if [ -n "$provider" ] && [ "$provider" != "null" ]; then
            echo -e "\n${CYAN}📊 Provider: $provider${NC}"
        fi
        if [ -n "$cost" ] && [ "$cost" != "null" ]; then
            echo -e "${CYAN}💰 Cost: \$$cost${NC}"
        fi
        if [ -n "$duration" ] && [ "$duration" != "null" ]; then
            echo -e "${CYAN}⏱️  Duration: $duration${NC}"
        fi
        return 0
    else
        echo -e "${RED}❌ No content in response${NC}"
        return 1
    fi
}

# Function to create AI request data
create_ai_request() {
    local prompt=$1
    local max_tokens=${2:-800}
    local temperature=${3:-0.7}
    
    jq -n \
        --arg prompt "$prompt" \
        --argjson maxTokens "$max_tokens" \
        --argjson temperature "$temperature" \
        '{prompt: $prompt, maxTokens: $maxTokens, temperature: $temperature}'
}

# ========================================
# INPUT HANDLING FUNCTIONS
# ========================================

# Function to get user input with prompt
get_user_input() {
    local prompt_text=$1
    local input_var=$2
    
    echo -n "$prompt_text: "
    read -r user_input
    eval "$input_var='$user_input'"
}

# Function to read code from file or stdin
read_code_input() {
    local input=$1
    local code_var=$2
    
    if [ -z "$input" ]; then
        echo "Enter code (paste and press Ctrl+D when done):"
        local code=$(cat)
    elif [ -f "$input" ]; then
        local code=$(cat "$input")
        echo -e "${BLUE}📁 Processing file: $input${NC}"
    else
        local code="$input"
    fi
    
    eval "$code_var='$code'"
}

# ========================================
# AI COMMAND FUNCTIONS
# ========================================

# Function to explain commands
ai_explain() {
    local command=$1
    
    if [ -z "$command" ]; then
        get_user_input "Enter command to explain" command
    fi

    echo -e "${YELLOW}💡 Explaining command: $command${NC}"

    local prompt="Explain this command in detail, including what it does, its syntax, and provide examples: $command"
    local data=$(create_ai_request "$prompt" 800 0.3)

    local response=$(call_api "ai/generate" "$data")
    if [ $? -eq 0 ]; then
        format_ai_response "$response"
    fi
}

# Function to suggest commands
ai_suggest() {
    local intent=$1
    
    if [ -z "$intent" ]; then
        get_user_input "What do you want to do" intent
    fi

    echo -e "${YELLOW}🚀 Finding suggestions for: $intent${NC}"

    local prompt="Suggest specific commands or steps to accomplish this task: $intent. Provide practical, executable commands with brief explanations."
    local data=$(create_ai_request "$prompt" 600 0.5)

    local response=$(call_api "ai/generate" "$data")
    if [ $? -eq 0 ]; then
        format_ai_response "$response"
    fi
}

# Function to debug errors
ai_debug() {
    local error_msg=$1
    local context=$2
    
    if [ -z "$error_msg" ]; then
        get_user_input "Enter error message" error_msg
        get_user_input "Enter context (optional)" context
    fi

    echo -e "${YELLOW}🔧 Debugging error: $error_msg${NC}"

    local prompt
    if [ -n "$context" ]; then
        prompt="Debug this error and provide solutions: '$error_msg'. Context: $context. Provide step-by-step troubleshooting and potential fixes."
    else
        prompt="Debug this error and provide solutions: '$error_msg'. Provide step-by-step troubleshooting and potential fixes."
    fi
    
    local data=$(create_ai_request "$prompt" 1000 0.4)

    local response=$(call_api "ai/generate" "$data")
    if [ $? -eq 0 ]; then
        format_ai_response "$response"
    fi
}

# Function to generate code
ai_generate() {
    local description=$1
    local language=${2:-"csharp"}
    local type=${3:-"general"}
    
    if [ -z "$description" ]; then
        get_user_input "Describe what you want to generate" description
    fi

    echo -e "${YELLOW}📝 Generating $language code: $description${NC}"

    local prompt="Generate $language code for: $description. Type: $type. Requirements:
- Write clean, production-ready code
- Include comments and documentation
- Follow best practices for $language
- Add error handling where appropriate
- Provide usage examples if helpful"

    local data=$(create_ai_request "$prompt" 1500 0.6)

    local response=$(call_api "ai/generate" "$data")
    if [ $? -eq 0 ]; then
        format_ai_response "$response"
    fi
}

# Function to review code
ai_review() {
    local input=$1
    local language=${2:-"csharp"}
    local code
    
    read_code_input "$input" code

    echo -e "${YELLOW}🔍 Reviewing $language code...${NC}"

    local prompt="Review this $language code and provide feedback:

\`\`\`$language
$code
\`\`\`

Please analyze:
- Code quality and best practices
- Potential bugs or issues
- Performance considerations
- Security concerns
- Suggestions for improvement
- Rate the code quality from 1-10

Provide constructive feedback with specific recommendations."

    local data=$(create_ai_request "$prompt" 1200 0.4)

    local response=$(call_api "ai/generate" "$data")
    if [ $? -eq 0 ]; then
        format_ai_response "$response"
    fi
}

# Function to optimize code
ai_optimize() {
    local input=$1
    local language=${2:-"csharp"}
    local code
    
    read_code_input "$input" code

    echo -e "${YELLOW}⚡ Optimizing $language code...${NC}"

    local prompt="Optimize this $language code for performance, readability, and maintainability:

\`\`\`$language
$code
\`\`\`

Provide:
- Optimized version of the code
- Explanation of changes made
- Performance improvements achieved
- Any trade-offs to consider"

    local data=$(create_ai_request "$prompt" 1500 0.5)

    local response=$(call_api "ai/generate" "$data")
    if [ $? -eq 0 ]; then
        format_ai_response "$response"
    fi
}

# Function to generate tests
ai_test() {
    local input=$1
    local language=${2:-"csharp"}
    local code
    
    read_code_input "$input" code

    echo -e "${YELLOW}🧪 Generating tests for $language code...${NC}"

    local prompt="Generate comprehensive unit tests for this $language code:

\`\`\`$language
$code
\`\`\`

Include:
- Test cases for normal scenarios
- Edge cases and error conditions
- Mock dependencies if needed
- Clear test descriptions
- Follow $language testing best practices"

    local data=$(create_ai_request "$prompt" 1500 0.4)

    local response=$(call_api "ai/generate" "$data")
    if [ $? -eq 0 ]; then
        format_ai_response "$response"
    fi
}

# ========================================
# STATUS AND MONITORING FUNCTIONS
# ========================================

# Function to check service status
check_service_status() {
    echo -e "${YELLOW}🔍 Checking service status...${NC}"

    # Check if API is running and get provider status
    if curl -s "http://localhost:5000/health" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ FreelanceAI API is running${NC}"
        
        # Try to get provider status
        local provider_status=$(call_api_get "ai/status")
        if [ $? -eq 0 ] && [ -n "$provider_status" ]; then
            echo -e "${BLUE}📊 Provider Status:${NC}"
            echo "$provider_status" | jq -r '.[] | "  \(.name): \(if .isHealthy then "✅ Healthy" else "❌ Unhealthy" end) - \(.requestsToday) requests today"' 2>/dev/null || echo "$provider_status"
        fi
        
        # Get today's spend (if endpoint exists)
        local spend=$(curl -s "http://localhost:5000/api/ai/spend" 2>/dev/null)
        if [ $? -eq 0 ] && [ -n "$spend" ] && [ "$spend" != "Not Found" ]; then
            echo -e "${CYAN}💰 Today's spend: \$spend${NC}"
        fi
    else
        echo -e "${RED}❌ FreelanceAI API is not running${NC}"
        echo "Start with: dotnet run --project src/FreelanceAI.WebApi"
    fi

    # Check Ollama (if enabled)
    if curl -s "http://localhost:11434/api/tags" > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Ollama is running${NC}"
        local models=$(curl -s "http://localhost:11434/api/tags" | jq -r '.models[].name' 2>/dev/null | head -3)
        if [ -n "$models" ]; then
            echo -e "${BLUE}Available Ollama models:${NC}"
            echo "$models" | sed 's/^/  - /'
        fi
    else
        echo -e "${YELLOW}ℹ️  Ollama not running (optional)${NC}"
    fi

    # Check Docker services
    if docker ps | grep -q freelance-ai 2>/dev/null; then
        echo -e "${GREEN}✅ Docker services are running${NC}"
    else
        echo -e "${YELLOW}ℹ️  Docker services not detected${NC}"
    fi
}

# Function for interactive chat
ai_chat() {
    echo -e "${CYAN}💬 FreelanceAI Interactive Chat${NC}"
    echo -e "${YELLOW}Type 'exit' to quit, 'clear' to clear history${NC}"
    echo ""
    
    while true; do
        echo -n "You: "
        read -r user_input
        
        case "$user_input" in
            "exit"|"quit"|"q")
                echo -e "${CYAN}👋 Goodbye!${NC}"
                break
                ;;
            "clear")
                clear
                echo -e "${CYAN}💬 FreelanceAI Interactive Chat${NC}"
                echo -e "${YELLOW}Type 'exit' to quit, 'clear' to clear history${NC}"
                echo ""
                continue
                ;;
            "")
                continue
                ;;
        esac
        
        echo -e "${BLUE}AI:${NC}"
        
        local data=$(create_ai_request "$user_input" 800 0.7)
        
        local response=$(call_api "ai/generate" "$data")
        if [ $? -eq 0 ]; then
            format_ai_response "$response" | sed 's/^/  /'
        fi
        echo ""
    done
}

# ========================================
# ENHANCED BOOTSTRAP INTEGRATION FUNCTIONS
# ========================================

# Function to check FreelanceAI health (for integration)
check_freelance_ai_health() {
    if curl -s "http://localhost:5000/health" > /dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

# Function to show FreelanceAI status for integration
show_freelance_ai_status() {
    echo -e "${YELLOW}🔍 Checking FreelanceAI status...${NC}"
    
    if check_freelance_ai_health; then
        echo -e "${GREEN}✅ FreelanceAI API is running${NC}"
        
        # Get provider status
        local provider_status=$(call_api_get "ai/status")
        if [ $? -eq 0 ] && [ -n "$provider_status" ]; then
            echo -e "${BLUE}📊 Provider Status:${NC}"
            echo "$provider_status" | jq -r '.[] | "  \(.name): \(if .isHealthy then "✅ Healthy" else "❌ Unhealthy" end) - \(.requestsToday) requests today"' 2>/dev/null || echo "$provider_status"
        fi
        
        # Get today's spend
        local spend=$(curl -s "http://localhost:5000/api/ai/spend" 2>/dev/null)
        if [ $? -eq 0 ] && [ -n "$spend" ] && [ "$spend" != "Not Found" ]; then
            echo -e "${CYAN}💰 Today's spend: \$${spend}${NC}"
        fi
    else
        echo -e "${RED}❌ FreelanceAI API is not running${NC}"
        echo "Start with: dotnet run --project src/FreelanceAI.WebApi"
        return 1
    fi
}

# Function to format prompts for bootstrap integration
format_prompt_for_subcommand() {
    local subcommand=$1
    local prompt=$2
    
    case $subcommand in
        "explain")
            echo "Please explain the following command or concept in simple terms for a developer: $prompt"
            ;;
        "suggest")
            echo "Suggest practical solutions or commands for this task: $prompt"
            ;;
        "debug")
            echo "Help debug this issue and provide troubleshooting steps: $prompt"
            ;;
        "code")
            echo "Generate clean, production-ready code for: $prompt"
            ;;
        "review")
            echo "Review this code and suggest improvements: $prompt"
            ;;
        "optimise"|"optimize")
            echo "Optimise this code or process: $prompt"
            ;;
        "test")
            echo "Provide testing strategies and examples for: $prompt"
            ;;
        *)
            echo "$prompt"
            ;;
    esac
}

# Function to handle AI commands for bootstrap integration
handle_ai_commands_for_bootstrap() {
    local args=("$@")
    
    if [ ${#args[@]} -eq 0 ]; then
        echo "AI command requires subcommand:"
        echo "  ai explain <command>     - Explain a command or concept"
        echo "  ai suggest <task>        - Get suggestions for a task"
        echo "  ai debug <error>         - Debug help for errors"
        echo "  ai code <task>          - Generate code"
        echo "  ai review <code>        - Review code"
        echo "  ai optimise <task>      - Optimisation suggestions"
        echo "  ai test <task>          - Testing guidance"
        return 1
    fi
    
    local subcommand=${args[0]}
    local prompt="${args[*]:1}"
    
    if [ -z "$prompt" ]; then
        echo -e "${RED}❌ Please provide a prompt for 'ai $subcommand'${NC}"
        return 1
    fi
    
    echo -e "${YELLOW}🤖 AI $subcommand: $prompt${NC}"
    
    # Check if FreelanceAI is available
    if ! check_freelance_ai_health; then
        echo -e "${RED}🔧 FreelanceAI API not available${NC}"
        echo "Please start FreelanceAI with: dotnet run --project src/FreelanceAI.WebApi"
        return 1
    fi
    
    # Format prompt for subcommand
    local formatted_prompt=$(format_prompt_for_subcommand "$subcommand" "$prompt")
    local data=$(create_ai_request "$formatted_prompt" 500 0.7)
    
    local response=$(call_api "ai/generate" "$data")
    if [ $? -eq 0 ]; then
        format_ai_response "$response"
    else
        echo -e "${RED}❌ Failed to get AI response${NC}"
        return 1
    fi
}

# Function to check dependencies for bootstrap integration
check_integration_dependencies() {
    local missing_deps=()
    
    # Check required tools
    if ! command -v curl &> /dev/null; then
        missing_deps+=("curl")
    fi
    
    if ! command -v jq &> /dev/null; then
        missing_deps+=("jq")
    fi
    
    if ! command -v dotnet &> /dev/null; then
        missing_deps+=("dotnet")
    fi
    
    if [ ${#missing_deps[@]} -gt 0 ]; then
        echo -e "${RED}❌ Missing dependencies: ${missing_deps[*]}${NC}"
        echo "Install them with:"
        for dep in "${missing_deps[@]}"; do
            case $dep in
                "curl") echo "  sudo apt install curl" ;;
                "jq") echo "  sudo apt install jq" ;;
                "dotnet") echo "  Install .NET 9.0 SDK from https://dotnet.microsoft.com/download" ;;
            esac
        done
        return 1
    else
        echo -e "${GREEN}✅ All dependencies are installed${NC}"
        return 0
    fi
}

# ========================================
# HELP AND USAGE FUNCTIONS
# ========================================

# Function to show help
show_help() {
    echo -e "${CYAN}FreelanceAI - AI-Powered Development Assistant${NC}"
    echo ""
    echo -e "${YELLOW}Usage:${NC}"
    echo "  freelance-ai explain [command]           - Explain a command"
    echo "  freelance-ai suggest [intent]            - Suggest commands for a task"
    echo "  freelance-ai debug [error] [context]     - Debug an error"
    echo "  freelance-ai generate [description] [language] [type] - Generate code"
    echo "  freelance-ai review [file|code]          - Review code quality"
    echo "  freelance-ai optimize [file|code]        - Optimize code performance"
    echo "  freelance-ai test [file|code]            - Generate unit tests"
    echo "  freelance-ai chat                        - Interactive chat mode"
    echo "  freelance-ai status                      - Check service status"
    echo ""
    echo -e "${YELLOW}Supported Languages:${NC}"
    echo "  csharp, javascript, typescript, html, css, bash, python, java, go, rust"
    echo ""
    echo -e "${YELLOW}Code Types:${NC}"
    echo "  general, component, service, utility, test, configuration, api, model"
    echo ""
    echo -e "${YELLOW}Examples:${NC}"
    echo -e "${GREEN}  freelance-ai explain \"docker-compose up -d\"${NC}"
    echo -e "${GREEN}  freelance-ai suggest \"deploy to azure\"${NC}"
    echo -e "${GREEN}  freelance-ai generate \"REST API controller\" csharp service${NC}"
    echo -e "${GREEN}  freelance-ai review ./Controllers/UserController.cs${NC}"
    echo -e "${GREEN}  freelance-ai optimize ./Services/DataService.cs${NC}"
    echo -e "${GREEN}  freelance-ai test ./Models/User.cs${NC}"
    echo -e "${GREEN}  freelance-ai chat${NC}"
    echo ""
    echo -e "${CYAN}💡 Pro Tips:${NC}"
    echo "  - Check status: freelance-ai status"
    echo "  - Use pipes: echo 'create user model' | freelance-ai generate"
    echo "  - Review files: freelance-ai review src/Controllers/*.cs"
    echo "  - Interactive mode: freelance-ai chat"
}
