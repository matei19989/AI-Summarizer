# ===================================================================
# Multi-stage Dockerfile for ASP.NET Core API - Azure Optimized
# This version is specifically tuned for Azure Web App for Containers
# ===================================================================

# Build stage - use SDK image for compilation
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file first for better Docker layer caching
COPY ["AISummarizerAPI.csproj", "./"]

# Restore dependencies in a separate layer for better caching
RUN dotnet restore "AISummarizerAPI.csproj"

# Copy all source code
COPY . .

# Build the application in Release configuration
RUN dotnet build "AISummarizerAPI.csproj" -c Release --no-restore

# Publish the application
RUN dotnet publish "AISummarizerAPI.csproj" -c Release --no-build -o /app/publish

# ===================================================================
# Runtime stage - optimized for Azure Web App for Containers
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install curl for health checks (Azure uses this)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Set working directory
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# CRITICAL: Azure Web App for Containers expects apps to listen on port 80
# This is different from your local setup which uses 5088
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Azure Web App for Containers automatically maps port 80
EXPOSE 80

# Health check endpoint that Azure can use for monitoring
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/api/summarization/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "AISummarizerAPI.dll"]