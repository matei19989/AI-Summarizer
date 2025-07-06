# ===================================================================
# Multi-stage Dockerfile for ASP.NET Core API
# Stage 1: Build environment with full SDK
# Stage 2: Runtime environment with minimal footprint
# ===================================================================

# Build stage - use SDK image for compilation
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file first for better Docker layer caching
# This allows Docker to cache the restore step if dependencies don't change
COPY ["AISummarizerAPI.csproj", "./"]

# Restore dependencies in a separate layer for better caching
RUN dotnet restore "AISummarizerAPI.csproj"

# Copy all source code
COPY . .

# Build the application in Release configuration
# --no-restore saves time since we already restored
RUN dotnet build "AISummarizerAPI.csproj" -c Release --no-restore

# Publish the application
# Creates a self-contained deployment ready for runtime
RUN dotnet publish "AISummarizerAPI.csproj" -c Release --no-build -o /app/publish

# ===================================================================
# Runtime stage - use minimal runtime image for smaller final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security best practices
# Running as root in containers is a security risk
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Set working directory
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Change ownership of the app directory to our non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Configure ASP.NET Core to listen on all interfaces
# This is necessary for the container to accept external connections
ENV ASPNETCORE_URLS=http://+:5088
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose the port the application listens on
EXPOSE 5088

# Add health check for container orchestration
# This allows Docker/Kubernetes to know if the container is healthy
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5088/api/summarization/health || exit 1

# Set the entry point to run the application
ENTRYPOINT ["dotnet", "AISummarizerAPI.dll"]