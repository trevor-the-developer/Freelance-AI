using FreelanceAI.ApiRouter;
using FreelanceAI.ApiRouter.Providers;
using FreelanceAI.ApiRouter.Services;
using FreelanceAI.Core.Configuration;
using FreelanceAI.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure options
builder.Services.Configure<RouterConfiguration>(
    builder.Configuration.GetSection("Router"));

// Register HttpClient for providers
builder.Services.AddHttpClient<GroqProvider>();
builder.Services.AddHttpClient<OllamaProvider>();

// Register core services
builder.Services.AddSingleton<IUsageTracker, UsageTracker>();
builder.Services.AddScoped<ISmartApiRouter, SmartApiRouter>();

// Register AI providers
builder.Services.AddScoped<IAIProvider, GroqProvider>();
builder.Services.AddScoped<IAIProvider, OllamaProvider>();

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();