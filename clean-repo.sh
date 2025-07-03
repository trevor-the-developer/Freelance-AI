#!/bin/bash

# Repository Cleanup Script
# This script removes all build artifacts, cache files, and temporary files
# from the FreelanceAI .NET project repository

set -euo pipefail

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Source the cleanup functions library
FUNCTIONS_LIB="$SCRIPT_DIR/lib/cleanup-functions.sh"

if [[ ! -f "$FUNCTIONS_LIB" ]]; then
    echo "Error: Functions library not found at: $FUNCTIONS_LIB"
    echo "Please ensure the lib/cleanup-functions.sh file exists."
    exit 1
fi

# shellcheck source=lib/cleanup-functions.sh
source "$FUNCTIONS_LIB"

# Function to show help
show_help() {
    cat << EOF
Repository Cleanup Script for FreelanceAI .NET Project

USAGE:
    $0 [OPTIONS]

OPTIONS:
    -h, --help      Show this help message
    -v, --verbose   Enable verbose output
    --dry-run       Show what would be deleted without actually deleting

DESCRIPTION:
    This script removes all build artifacts, cache files, and temporary files
    from the FreelanceAI .NET project repository, including:
    
    • .NET build outputs (bin/, obj/, *.dll, *.pdb, *.exe)
    • NuGet packages and cache files
    • Test results and coverage reports
    • IDE-specific files (.idea/, .vs/, temp VS Code configs)
    • Temporary and log files
    • MSBuild and Roslyn artifacts
    • Empty directories
    
    The script preserves:
    • Source code files
    • Configuration files (appsettings.json, etc.)
    • Documentation files
    • Git repository data
    • Essential VS Code settings

EXAMPLES:
    $0                  # Clean the repository
    $0 --dry-run        # Show what would be cleaned
    $0 --verbose        # Clean with detailed output

EOF
}

# Wrapper function for backward compatibility
cleanup_repository() {
    # Use the comprehensive cleanup function from the library
    perform_full_cleanup "$SCRIPT_DIR"
}

# Parse command line arguments
VERBOSE=false
DRY_RUN=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        *)
            log_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Enable verbose output if requested and export for functions library
if [[ "$VERBOSE" == true ]]; then
    set -x
fi
export VERBOSE

# Main execution
main() {
    log_info "FreelanceAI Repository Cleanup Script"
    log_info "====================================="
    
    if [[ "$DRY_RUN" == true ]]; then
        perform_dry_run "$SCRIPT_DIR"
    else
        cleanup_repository
    fi
}

# Run main function
main "$@"
