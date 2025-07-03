#!/bin/bash

# Repository Cleanup Functions Library
# This file contains all the utility functions for cleaning build artifacts,
# cache files, and temporary files from development repositories

# Ensure we don't execute this file directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    echo "Error: This file should be sourced, not executed directly."
    echo "Usage: source ${BASH_SOURCE[0]}"
    exit 1
fi

# Color codes for output
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly CYAN='\033[0;36m'
readonly NC='\033[0m' # No Color

# Logging functions with colored output
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_debug() {
    if [[ "${VERBOSE:-false}" == "true" ]]; then
        echo -e "${CYAN}[DEBUG]${NC} $1"
    fi
}

# Function to safely remove directories/files with logging
safe_remove() {
    local target="$1"
    local description="$2"
    
    if [[ -z "$target" ]]; then
        log_error "safe_remove: No target specified"
        return 1
    fi
    
    if [[ -e "$target" ]]; then
        log_info "Removing $description: $target"
        if rm -rf "$target"; then
            log_success "Removed $description"
        else
            log_error "Failed to remove $description: $target"
            return 1
        fi
    else
        log_debug "Skipping $description (not found): $target"
    fi
}

# Function to find and remove files/directories matching a pattern
find_and_remove() {
    local pattern="$1"
    local description="$2"
    local base_dir="${3:-.}"
    local type="${4:-d}"  # default to directories
    
    if [[ -z "$pattern" || -z "$description" ]]; then
        log_error "find_and_remove: Pattern and description are required"
        return 1
    fi
    
    log_info "Finding and removing $description..."
    
    # Use find to locate matching files/directories
    local found_items
    found_items=$(find "$base_dir" -name "$pattern" -type "$type" 2>/dev/null || true)
    
    if [[ -n "$found_items" ]]; then
        local count=0
        while IFS= read -r item; do
            if [[ -n "$item" && -e "$item" ]]; then
                log_debug "Removing: $item"
                rm -rf "$item"
                ((count++))
            fi
        done <<< "$found_items"
        log_success "Removed all $description ($count items)"
    else
        log_debug "No $description found"
    fi
}

# Function to remove files by extension
remove_files_by_extension() {
    local extension="$1"
    local description="$2"
    local base_dir="${3:-.}"
    
    if [[ -z "$extension" ]]; then
        log_error "remove_files_by_extension: Extension is required"
        return 1
    fi
    
    log_debug "Removing $description (*.$extension files)..."
    
    local count
    count=$(find "$base_dir" -name "*.$extension" -type f -delete -print 2>/dev/null | wc -l || echo "0")
    
    if [[ "$count" -gt 0 ]]; then
        log_success "Removed $count $description files"
    else
        log_debug "No $description files found"
    fi
}

# Function to clean .NET build artifacts
clean_dotnet_artifacts() {
    local base_dir="${1:-.}"
    
    log_info "Cleaning .NET build artifacts..."
    
    # Remove bin and obj directories
    find_and_remove "bin" ".NET bin directories" "$base_dir"
    find_and_remove "obj" ".NET obj directories" "$base_dir"
    
    # Remove compiled files
    remove_files_by_extension "dll" ".NET assembly files" "$base_dir"
    remove_files_by_extension "pdb" ".NET debug symbol files" "$base_dir"
    remove_files_by_extension "exe" ".NET executable files" "$base_dir"
    remove_files_by_extension "cache" ".NET cache files" "$base_dir"
    
    log_success "Completed .NET artifacts cleanup"
}

# Function to clean NuGet artifacts
clean_nuget_artifacts() {
    local base_dir="${1:-.}"
    
    log_info "Cleaning NuGet artifacts..."
    
    find_and_remove "packages" "NuGet packages directories" "$base_dir"
    remove_files_by_extension "nupkg" "NuGet package files" "$base_dir"
    
    log_success "Completed NuGet artifacts cleanup"
}

# Function to clean test artifacts
clean_test_artifacts() {
    local base_dir="${1:-.}"
    
    log_info "Cleaning test artifacts..."
    
    find_and_remove "TestResults" "test result directories" "$base_dir"
    find_and_remove "test-results" "test result directories" "$base_dir"
    find_and_remove "coverage" "coverage directories" "$base_dir"
    
    remove_files_by_extension "trx" "test result files" "$base_dir"
    remove_files_by_extension "coverage" "coverage files" "$base_dir"
    remove_files_by_extension "coveragexml" "coverage XML files" "$base_dir"
    
    log_success "Completed test artifacts cleanup"
}

# Function to clean IDE-specific files
clean_ide_artifacts() {
    local base_dir="${1:-.}"
    
    log_info "Cleaning IDE artifacts..."
    
    # JetBrains Rider/IntelliJ
    safe_remove "$base_dir/.idea" "JetBrains IDE directory"
    remove_files_by_extension "iml" "IntelliJ module files" "$base_dir"
    remove_files_by_extension "ipr" "IntelliJ project files" "$base_dir"
    remove_files_by_extension "iws" "IntelliJ workspace files" "$base_dir"
    
    # Visual Studio
    safe_remove "$base_dir/.vs" "Visual Studio directory"
    remove_files_by_extension "suo" "Visual Studio solution user options files" "$base_dir"
    remove_files_by_extension "user" "Visual Studio user files" "$base_dir"
    remove_files_by_extension "userosscache" "Visual Studio user cache files" "$base_dir"
    
    # Clean specific Visual Studio files
    find "$base_dir" -name "*.sln.docstates" -type f -delete 2>/dev/null || true
    
    # Visual Studio Code (selective cleanup)
    safe_remove "$base_dir/.vscode/launch.json" "VS Code launch configuration"
    safe_remove "$base_dir/.vscode/tasks.json" "VS Code tasks configuration"
    # Keep .vscode/settings.json and .vscode/extensions.json as they are often project-specific
    
    log_success "Completed IDE artifacts cleanup"
}

# Function to clean build outputs
clean_build_outputs() {
    local base_dir="${1:-.}"
    
    log_info "Cleaning build outputs..."
    
    find_and_remove "publish" "publish directories" "$base_dir"
    find_and_remove "dist" "distribution directories" "$base_dir"
    find_and_remove "build" "build directories" "$base_dir"
    find_and_remove "out" "output directories" "$base_dir"
    
    log_success "Completed build outputs cleanup"
}

# Function to clean temporary files
clean_temporary_files() {
    local base_dir="${1:-.}"
    
    log_info "Cleaning temporary files..."
    
    # Common temporary file extensions
    local temp_extensions=("log" "temp" "tmp" "swp" "swo" "bak" "orig")
    
    for ext in "${temp_extensions[@]}"; do
        remove_files_by_extension "$ext" "temporary $ext files" "$base_dir"
    done
    
    # System-specific temporary files
    find "$base_dir" -name "*~" -type f -delete 2>/dev/null || true
    find "$base_dir" -name ".DS_Store" -type f -delete 2>/dev/null || true
    find "$base_dir" -name "Thumbs.db" -type f -delete 2>/dev/null || true
    find "$base_dir" -name "desktop.ini" -type f -delete 2>/dev/null || true
    
    log_success "Completed temporary files cleanup"
}

# Function to clean MSBuild artifacts
clean_msbuild_artifacts() {
    local base_dir="${1:-.}"
    
    log_info "Cleaning MSBuild artifacts..."
    
    remove_files_by_extension "binlog" "MSBuild binary log files" "$base_dir"
    find "$base_dir" -name "msbuild.log" -type f -delete 2>/dev/null || true
    
    log_success "Completed MSBuild artifacts cleanup"
}

# Function to clean Entity Framework artifacts
clean_ef_artifacts() {
    local base_dir="${1:-.}"
    
    log_info "Cleaning Entity Framework artifacts..."
    
    find "$base_dir" -name "*.edmx.diagram" -type f -delete 2>/dev/null || true
    
    log_success "Completed Entity Framework artifacts cleanup"
}

# Function to clean Node.js artifacts (for projects with frontend components)
clean_nodejs_artifacts() {
    local base_dir="${1:-.}"
    
    log_info "Cleaning Node.js artifacts..."
    
    find_and_remove "node_modules" "Node.js modules directories" "$base_dir"
    
    # Remove lock files (these can be regenerated)
    find "$base_dir" -name "package-lock.json" -type f -delete 2>/dev/null || true
    find "$base_dir" -name "yarn.lock" -type f -delete 2>/dev/null || true
    find "$base_dir" -name "pnpm-lock.yaml" -type f -delete 2>/dev/null || true
    
    log_success "Completed Node.js artifacts cleanup"
}

# Function to remove empty directories
clean_empty_directories() {
    local base_dir="${1:-.}"
    
    log_info "Removing empty directories..."
    
    # Find and remove empty directories (may need multiple passes)
    local removed_count=1
    local pass=1
    
    while [[ $removed_count -gt 0 && $pass -le 5 ]]; do
        removed_count=$(find "$base_dir" -type d -empty -delete -print 2>/dev/null | wc -l || echo "0")
        log_debug "Pass $pass: Removed $removed_count empty directories"
        ((pass++))
    done
    
    log_success "Completed empty directories cleanup"
}

# Function to get repository size
get_repository_size() {
    local base_dir="${1:-.}"
    
    if command -v du >/dev/null 2>&1; then
        du -sh "$base_dir" 2>/dev/null | cut -f1 || echo "unknown"
    else
        echo "unknown"
    fi
}

# Function to show git status if in a git repository
show_git_status() {
    local base_dir="${1:-.}"
    
    if [[ -d "$base_dir/.git" ]]; then
        log_info "Git status after cleanup:"
        (cd "$base_dir" && git status --porcelain 2>/dev/null) || log_warning "Could not get git status"
    else
        log_debug "Not a git repository, skipping git status"
    fi
}

# Function to perform dry run - shows what would be removed
perform_dry_run() {
    local base_dir="${1:-.}"
    
    log_warning "DRY RUN MODE - No files will be deleted"
    log_info "The following would be removed from: $base_dir"
    echo
    
    # List what would be removed
    echo "📁 Directories:"
    find "$base_dir" -name "bin" -type d 2>/dev/null | head -10 || true
    find "$base_dir" -name "obj" -type d 2>/dev/null | head -10 || true
    find "$base_dir" -name ".idea" -type d 2>/dev/null || true
    find "$base_dir" -name ".vs" -type d 2>/dev/null || true
    find "$base_dir" -name "node_modules" -type d 2>/dev/null | head -5 || true
    
    echo
    echo "📄 Files:"
    find "$base_dir" -name "*.dll" -type f 2>/dev/null | head -10 || true
    find "$base_dir" -name "*.pdb" -type f 2>/dev/null | head -10 || true
    find "$base_dir" -name "*.cache" -type f 2>/dev/null | head -10 || true
    find "$base_dir" -name "*.log" -type f 2>/dev/null | head -10 || true
    
    echo
    log_warning "Use the script without --dry-run to actually clean the repository"
}

# Function to validate base directory
validate_base_directory() {
    local base_dir="$1"
    
    if [[ ! -d "$base_dir" ]]; then
        log_error "Directory does not exist: $base_dir"
        return 1
    fi
    
    if [[ ! -r "$base_dir" ]]; then
        log_error "Directory is not readable: $base_dir"
        return 1
    fi
    
    if [[ ! -w "$base_dir" ]]; then
        log_error "Directory is not writable: $base_dir"
        return 1
    fi
    
    return 0
}

# Main cleanup orchestrator function
perform_full_cleanup() {
    local base_dir="${1:-.}"
    
    # Validate the base directory
    if ! validate_base_directory "$base_dir"; then
        return 1
    fi
    
    log_info "Starting comprehensive repository cleanup for: $(realpath "$base_dir")"
    log_info "============================================"
    
    # Store initial size
    local initial_size
    initial_size=$(get_repository_size "$base_dir")
    log_info "Repository size before cleanup: $initial_size"
    
    # Navigate to the base directory
    local original_pwd
    original_pwd=$(pwd)
    cd "$base_dir"
    
    # Perform all cleanup operations
    clean_dotnet_artifacts "$base_dir"
    clean_nuget_artifacts "$base_dir"
    clean_test_artifacts "$base_dir"
    clean_ide_artifacts "$base_dir"
    clean_build_outputs "$base_dir"
    clean_temporary_files "$base_dir"
    clean_msbuild_artifacts "$base_dir"
    clean_ef_artifacts "$base_dir"
    clean_nodejs_artifacts "$base_dir"
    clean_empty_directories "$base_dir"
    
    # Return to original directory
    cd "$original_pwd"
    
    log_info "============================================"
    log_success "Repository cleanup completed successfully!"
    
    # Show final statistics
    local final_size
    final_size=$(get_repository_size "$base_dir")
    log_info "Repository size after cleanup: $final_size"
    
    show_git_status "$base_dir"
}
