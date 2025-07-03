# Cleanup Functions Library

This directory contains reusable functions for repository cleanup operations.

## Files

### `cleanup-functions.sh`
A comprehensive library of bash functions for cleaning development repositories.

#### Core Functions

**Logging Functions:**
- `log_info()` - Information messages (blue)
- `log_success()` - Success messages (green)
- `log_warning()` - Warning messages (yellow)
- `log_error()` - Error messages (red)
- `log_debug()` - Debug messages (cyan, only shown with VERBOSE=true)

**Utility Functions:**
- `safe_remove()` - Safely remove files/directories with logging
- `find_and_remove()` - Find and remove files/directories by pattern
- `remove_files_by_extension()` - Remove files by extension
- `validate_base_directory()` - Validate directory permissions
- `get_repository_size()` - Get directory size using du
- `show_git_status()` - Show git status if in a git repository

**Cleanup Functions:**
- `clean_dotnet_artifacts()` - Clean .NET build artifacts (bin/, obj/, *.dll, etc.)
- `clean_nuget_artifacts()` - Clean NuGet packages and cache
- `clean_test_artifacts()` - Clean test results and coverage reports
- `clean_ide_artifacts()` - Clean IDE-specific files (.idea/, .vs/, etc.)
- `clean_build_outputs()` - Clean build output directories
- `clean_temporary_files()` - Clean temporary and log files
- `clean_msbuild_artifacts()` - Clean MSBuild artifacts
- `clean_ef_artifacts()` - Clean Entity Framework artifacts
- `clean_nodejs_artifacts()` - Clean Node.js artifacts
- `clean_empty_directories()` - Remove empty directories

**Main Functions:**
- `perform_full_cleanup()` - Orchestrates complete repository cleanup
- `perform_dry_run()` - Shows what would be removed without deleting

## Usage

To use these functions in your own scripts:

```bash
#!/bin/bash

# Source the functions library
source "path/to/lib/cleanup-functions.sh"

# Use any function
log_info "Starting cleanup"
clean_dotnet_artifacts "/path/to/project"
perform_full_cleanup "/path/to/repository"
```

## Environment Variables

- `VERBOSE` - Set to "true" to enable debug logging

## Features

- **Safe Operations**: All functions include error checking and logging
- **Modular Design**: Use individual cleanup functions or the full cleanup
- **Colored Output**: Easy-to-read colored console output
- **Dry Run Support**: Preview what would be removed
- **Cross-Platform**: Works on Linux, macOS, and WSL
- **Git Aware**: Shows git status after cleanup

## Examples

```bash
# Clean only .NET artifacts
clean_dotnet_artifacts "/path/to/project"

# Clean everything with verbose output
VERBOSE=true perform_full_cleanup "/path/to/repository"

# Preview what would be cleaned
perform_dry_run "/path/to/repository"
```
