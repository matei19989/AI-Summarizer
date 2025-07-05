using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register HttpClient for external API calls
builder.Services.AddHttpClient<ISummarizationService, SummarizationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/1.0");
});

// Register our custom services
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
    Timestamp = DateTime.UtcNow
});

app.Run();