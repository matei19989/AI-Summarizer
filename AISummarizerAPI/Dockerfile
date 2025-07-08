# ===================================================================
# Multi-stage Dockerfile for ASP.NET Core API - ICU Fixed
# ===================================================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project file first for better caching
COPY ["AISummarizerAPI.csproj", "./"]

# Restore dependencies
RUN dotnet restore "AISummarizerAPI.csproj" --runtime linux-musl-x64

# Copy source code
COPY . .

# Build the application
RUN dotnet build "AISummarizerAPI.csproj" \
    -c Release \
    -r linux-musl-x64 \
    --no-restore \
    --no-self-contained

# Publish stage
FROM build AS publish
RUN dotnet publish "AISummarizerAPI.csproj" \
    -c Release \
    -r linux-musl-x64 \
    --no-build \
    --no-self-contained \
    -o /app/publish

# Runtime stage - FIXED for ICU
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# Install curl and ICU packages for Alpine Linux
RUN apk add --no-cache curl icu-libs

# Create non-root user
RUN addgroup -g 1000 -S appuser && \
    adduser -u 1000 -S appuser -G appuser

WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Fix permissions
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Environment variables
ENV ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:80/api/summarization/health || exit 1

EXPOSE 80

ENTRYPOINT ["dotnet", "AISummarizerAPI.dll"]