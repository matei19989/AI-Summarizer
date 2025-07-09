// AISummarizerAPI/Program.cs
using AISummarizerAPI.Extensions.ServiceCollection;
using AISummarizerAPI.Extensions.WebApp;

var builder = WebApplication.CreateBuilder(args);

// Add CORS early
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercel", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            origin.Contains("vercel.app") ||
            origin.Contains("localhost") ||
            origin.Contains("aisummarizer2026"))
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Service Registration
builder.Services.AddConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddHttpClientServices(builder.Configuration);
builder.Services.AddSecurityServices(builder.Configuration, builder.Environment);
builder.Services.AddFrameworkServices();

var app = builder.Build();

// Apply CORS early in pipeline
app.UseCors("AllowVercel");

// Pipeline Configuration
app.ConfigurePipeline(builder.Environment);
app.ConfigureEndpoints();
app.ConfigureHealthChecks();

// Application Startup
await app.ValidateStartupAsync();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 AI Content Summarizer v2.1.0 - Clean Architecture Edition");

app.Run();