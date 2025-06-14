# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Install curl for debugging if needed
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy everything first to avoid layer caching issues
COPY . .

# Clear any existing packages and obj/bin folders
RUN find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
RUN find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true

# Clear NuGet cache completely
RUN dotnet nuget locals all --clear

# Restore packages with verbose output for debugging
RUN dotnet restore --verbosity detailed

# Build the solution
RUN dotnet build -c Release

# Publish the WebApi project specifically
RUN dotnet publish src/FreelanceAI.WebApi/FreelanceAI.WebApi.csproj -c Release -o /app/publish

# Use the official .NET 8 runtime image for running
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "FreelanceAI.WebApi.dll"]