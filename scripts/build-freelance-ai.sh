#!/bin/bash
# scripts/build-freelance-ai.sh
# Enhanced build script for FreelanceAI aligned with warp-terminal-clone-bootstrap

# Get the directory of the script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Source the function library
if [ -f "$SCRIPT_DIR/lib/freelance-ai-functions.sh" ]; then
    source "$SCRIPT_DIR/lib/freelance-ai-functions.sh"
else
    echo "❌ Function library not found at $SCRIPT_DIR/lib/freelance-ai-functions.sh"
    exit 1
fi

# Configuration
BINARY_NAME="freelance-ai-api"
PUBLISH_DIR="$PROJECT_ROOT/bin/Release/net9.0/linux-x64/publish"
USER_BIN="$HOME/.local/bin"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Print colored output
print_color() {
    local color_name=$1
    local message=$2
    case $color_name in
        "red") echo -e "${RED}${message}${NC}" ;;
        "green") echo -e "${GREEN}${message}${NC}" ;;
        "yellow") echo -e "${YELLOW}${message}${NC}" ;;
        "blue") echo -e "${BLUE}${message}${NC}" ;;
        "cyan") echo -e "${CYAN}${message}${NC}" ;;
        *) echo "$message" ;;
    esac
}

print_status() {
    local status=$1
    local message=$2
    case $status in
        "success") print_color "green" "✅ $message" ;;
        "error") print_color "red" "❌ $message" ;;
        "warning") print_color "yellow" "⚠️  $message" ;;
        "info") print_color "cyan" "ℹ️  $message" ;;
        "working") print_color "yellow" "🔨 $message" ;;
        *) echo "$message" ;;
    esac
}

# Check dependencies
check_build_dependencies() {
    print_status "info" "Checking build dependencies..."
    
    # Check .NET SDK
    if ! command -v dotnet &> /dev/null; then
        print_status "error" ".NET SDK not found. Please install .NET 9.0 SDK"
        return 1
    fi
    
    local dotnet_version=$(dotnet --version)
    print_status "success" ".NET SDK version: $dotnet_version"
    
    # Check jq (for configuration)
    if ! command -v jq &> /dev/null; then
        print_status "warning" "jq not found. Install with: sudo apt install jq"
    else
        print_status "success" "jq found"
    fi
    
    return 0
}

# Build the application
build_app() {
    print_status "working" "Building FreelanceAI..."
    
    cd "$PROJECT_ROOT"
    
    # Clean first
    dotnet clean
    
    # Build
    if dotnet build --configuration Release; then
        print_status "success" "Build completed successfully"
        return 0
    else
        print_status "error" "Build failed"
        return 1
    fi
}

# Publish the application
publish_app() {
    print_status "working" "Publishing FreelanceAI as single file..."
    
    cd "$PROJECT_ROOT"
    
    # Create publish directory if it doesn't exist
    mkdir -p "$(dirname "$PUBLISH_DIR")"
    
    # Publish as single file
    if dotnet publish src/FreelanceAI.WebApi \
        --configuration Release \
        --runtime linux-x64 \
        --self-contained false \
        --output "$PUBLISH_DIR" \
        /p:PublishSingleFile=true \
        /p:DebugType=None \
        /p:DebugSymbols=false; then
        
        print_status "success" "Publish completed successfully"
        print_status "info" "Published to: $PUBLISH_DIR"
        
        # Make executable
        chmod +x "$PUBLISH_DIR/FreelanceAI.WebApi"
        
        return 0
    else
        print_status "error" "Publish failed"
        return 1
    fi
}

# Install application
install_app() {
    print_status "working" "Installing FreelanceAI..."
    
    # Ensure user bin directory exists
    mkdir -p "$USER_BIN"
    
    # Check if published binary exists
    local binary_path="$PUBLISH_DIR/FreelanceAI.WebApi"
    if [ ! -f "$binary_path" ]; then
        print_status "error" "Published binary not found. Please run publish first."
        return 1
    fi
    
    # Create symlink
    local symlink_path="$USER_BIN/$BINARY_NAME"
    if [ -L "$symlink_path" ]; then
        rm "$symlink_path"
    fi
    
    ln -s "$binary_path" "$symlink_path"
    print_status "success" "Symlink created: $symlink_path"
    
    # Check PATH
    if [[ ":$PATH:" != *":$USER_BIN:"* ]]; then
        print_status "warning" "Add $USER_BIN to your PATH:"
        echo "  echo 'export PATH=\"$USER_BIN:\$PATH\"' >> ~/.bashrc"
        echo "  source ~/.bashrc"
    else
        print_status "success" "PATH configured correctly"
    fi
    
    return 0
}

# Run tests
run_tests() {
    print_status "working" "Running tests..."
    
    cd "$PROJECT_ROOT"
    
    if dotnet test --logger console --verbosity minimal; then
        print_status "success" "All tests passed"
        return 0
    else
        print_status "error" "Some tests failed"
        return 1
    fi
}

# Check status
show_status() {
    print_color "cyan" "🚀 FreelanceAI Status"
    echo ""
    
    # Check if application is built
    local binary_path="$PUBLISH_DIR/FreelanceAI.WebApi"
    if [ -f "$binary_path" ]; then
        print_status "success" "Application built and published"
        echo "   Location: $binary_path"
    else
        print_status "error" "Application not built"
    fi
    
    # Check symlink
    local symlink_path="$USER_BIN/$BINARY_NAME"
    if [ -L "$symlink_path" ]; then
        print_status "success" "Symlink installed"
        echo "   Location: $symlink_path"
    else
        print_status "warning" "Symlink not installed"
    fi
    
    # Check PATH
    if [[ ":$PATH:" == *":$USER_BIN:"* ]]; then
        print_status "success" "PATH configured correctly"
    else
        print_status "warning" "~/.local/bin not in PATH"
    fi
    
    # Check if API is running
    if curl -s "http://localhost:5000/health" > /dev/null 2>&1; then
        print_status "success" "FreelanceAI API is running"
    else
        print_status "info" "FreelanceAI API is not running"
        echo "   Start with: $BINARY_NAME or dotnet run --project src/FreelanceAI.WebApi"
    fi
    
    echo ""
}

# Show help
show_help() {
    print_color "cyan" "FreelanceAI - Build & Installation Script"
    echo ""
    print_color "yellow" "Usage:"
    echo "  ./scripts/build-freelance-ai.sh [command] [options]"
    echo ""
    print_color "yellow" "Commands:"
    echo "  check         - Check dependencies"
    echo "  build         - Build the application"
    echo "  publish       - Build and publish as single file"
    echo "  install       - Full installation (build, publish, integrate)"
    echo "  test          - Run test suite"
    echo "  status        - Show installation status"
    echo "  help          - Show this help"
    echo ""
    print_color "yellow" "Examples:"
    echo "  ./scripts/build-freelance-ai.sh install"
    echo "  ./scripts/build-freelance-ai.sh status"
    echo "  ./scripts/build-freelance-ai.sh test"
}

# Main command routing
main() {
    local command=${1:-help}
    
    case $command in
        "check")
            check_build_dependencies
            ;;
        "build")
            check_build_dependencies && build_app
            ;;
        "publish")
            check_build_dependencies && build_app && publish_app
            ;;
        "install")
            check_build_dependencies && build_app && publish_app && install_app
            ;;
        "test")
            check_build_dependencies && run_tests
            ;;
        "status")
            show_status
            ;;
        "help" | "--help" | "-h")
            show_help
            ;;
        *)
            print_status "error" "Unknown command: $command"
            show_help
            exit 1
            ;;
    esac
}

# Execute main function
main "$@"
