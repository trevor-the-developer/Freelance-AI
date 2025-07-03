#!/bin/bash

# Example: Using individual cleanup functions
# This demonstrates how to use specific cleanup functions from the library

set -euo pipefail

# Get script directory and source the functions library
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/cleanup-functions.sh"

# Example 1: Clean only .NET artifacts from a specific project
clean_dotnet_only() {
    local project_path="$1"
    
    log_info "Cleaning only .NET artifacts from: $project_path"
    
    if validate_base_directory "$project_path"; then
        clean_dotnet_artifacts "$project_path"
        log_success "Completed .NET-only cleanup"
    else
        log_error "Invalid project path: $project_path"
        return 1
    fi
}

# Example 2: Clean IDE files only
clean_ide_only() {
    local project_path="$1"
    
    log_info "Cleaning only IDE artifacts from: $project_path"
    
    if validate_base_directory "$project_path"; then
        clean_ide_artifacts "$project_path"
        log_success "Completed IDE-only cleanup"
    else
        log_error "Invalid project path: $project_path"
        return 1
    fi
}

# Example 3: Custom cleanup sequence
custom_cleanup() {
    local project_path="$1"
    
    log_info "Running custom cleanup sequence"
    
    if ! validate_base_directory "$project_path"; then
        log_error "Invalid project path: $project_path"
        return 1
    fi
    
    # Only clean build artifacts and temporary files, keep everything else
    clean_dotnet_artifacts "$project_path"
    clean_temporary_files "$project_path"
    clean_empty_directories "$project_path"
    
    # Show final status
    local final_size
    final_size=$(get_repository_size "$project_path")
    log_info "Repository size after custom cleanup: $final_size"
    
    show_git_status "$project_path"
}

# Show usage information
show_usage() {
    cat << EOF
Example Cleanup Script

This script demonstrates how to use individual cleanup functions
from the cleanup-functions.sh library.

USAGE:
    $0 [COMMAND] [PATH]

COMMANDS:
    dotnet-only [PATH]  Clean only .NET build artifacts
    ide-only [PATH]     Clean only IDE-specific files  
    custom [PATH]       Run custom cleanup sequence
    help               Show this help message

EXAMPLES:
    $0 dotnet-only ./src/MyProject
    $0 ide-only .
    $0 custom /path/to/repository

EOF
}

# Main function
main() {
    local command="${1:-help}"
    local target_path="${2:-$SCRIPT_DIR}"
    
    case "$command" in
        dotnet-only)
            clean_dotnet_only "$target_path"
            ;;
        ide-only)
            clean_ide_only "$target_path"
            ;;
        custom)
            custom_cleanup "$target_path"
            ;;
        help|--help|-h)
            show_usage
            ;;
        *)
            log_error "Unknown command: $command"
            show_usage
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@"
