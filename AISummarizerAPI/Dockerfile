# ===================================================================
# Multi-stage Dockerfile for ASP.NET Core API - Security & Performance Optimized
# ===================================================================

# Stage 1: Base image with common dependencies
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app
EXPOSE 80
# Install curl for health checks in a single layer
RUN apk add --no-cache curl && \
    # Create non-root user for security
    addgroup -g 1000 -S appuser && \
    adduser -u 1000 -S appuser -G appuser

# Stage 2: Build environment
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy only project file first for better caching
COPY ["AISummarizerAPI.csproj", "./"]
# Restore as a separate layer to cache dependencies
RUN dotnet restore "AISummarizerAPI.csproj" --runtime linux-musl-x64

# Copy remaining source code
COPY . .

# Build in Release mode with runtime identifier for Alpine
RUN dotnet build "AISummarizerAPI.csproj" \
    -c Release \
    -r linux-musl-x64 \
    --no-restore \
    --no-self-contained

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "AISummarizerAPI.csproj" \
    -c Release \
    -r linux-musl-x64 \
    --no-build \
    --no-self-contained \
    -o /app/publish

# Stage 4: Final runtime image
FROM base AS runtime

# Copy published app from publish stage
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Environment variables with secure defaults
ENV ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check with proper dependencies
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5088/api/summarization/health || exit 1

ENTRYPOINT ["dotnet", "AISummarizerAPI.dll"]