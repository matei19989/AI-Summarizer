using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register HttpClient for external API calls
// Shared HttpClient configuration for both summarization and URL extraction services
builder.Services.AddHttpClient<ISummarizationService, SummarizationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/1.0");
});

// Day 5 Addition: Register HttpClient for URL content extraction
// Separate HttpClient configuration for URL extraction to allow different timeout settings
builder.Services.AddHttpClient<IUrlContentExtractor, UrlContentExtractor>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15); // Shorter timeout for URL validation
    client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/1.0 (Content Extractor)");
});

// Register our custom services following existing DI patterns
// Day 5 Addition: Register URL content extraction service
builder.Services.AddScoped<IUrlContentExtractor, UrlContentExtractor>();
builder.Services.AddScoped<ISummarizationService, SummarizationService>();

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", corsBuilder =>
    {
        corsBuilder
            .WithOrigins(
                "http://localhost:3000",    // Default React dev server
                "http://localhost:5173",    // Vite dev server
                "http://localhost:4173"     // Vite preview server
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Apply CORS policy - must come before UseRouting
app.UseCors("ReactPolicy");

app.UseAuthorization();

app.MapControllers();

// Add a simple health check endpoint
app.MapGet("/", () => new
{
    Application = "AI Content Summarizer API",
    Version = "1.0.0",
    Status = "Running",
    Timestamp = DateTime.UtcNow,
    Features = new[] { "Text Summarization", "URL Content Extraction" } // Day 5 enhancement
});

app.Run();