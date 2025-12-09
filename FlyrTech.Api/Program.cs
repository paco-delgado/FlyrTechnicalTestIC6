using System.Text.Json;
using FlyrTech.Core;
using FlyrTech.Core.Models;
using FlyrTech.Infrastructure;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Redis connection as Singleton
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] 
    ?? throw new InvalidOperationException("Redis connection string is not configured");

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    return ConnectionMultiplexer.Connect(configuration);
});

// Register cache service
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Register journey service
builder.Services.AddSingleton<IJourneyService, JourneyService>();

var app = builder.Build();

// Initialize cache with journey data on startup
await InitializeCacheAsync(app.Services);

async Task InitializeCacheAsync(IServiceProvider services)
{
    var journeyService = services.GetRequiredService<IJourneyService>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var journeysFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "journeys.json");
        
        if (!File.Exists(journeysFilePath))
        {
            logger.LogWarning("Journeys data file not found at {Path}", journeysFilePath);
            return;
        }

        var json = await File.ReadAllTextAsync(journeysFilePath);
        var data = JsonSerializer.Deserialize<JourneysData>(json);

        if (data?.Journeys != null && data.Journeys.Count > 0)
        {
            await journeyService.InitializeCacheAsync(data.Journeys);
            logger.LogInformation("Cache initialized with {Count} journeys", data.Journeys.Count);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing cache with journey data");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Cache demo endpoint
app.MapGet("/api/cache/demo", async (ICacheService cacheService) =>
{
    const string cacheKey = "demo:timestamp";
    
    // Try to get value from cache
    var cachedValue = await cacheService.GetAsync(cacheKey);
    
    if (cachedValue != null)
    {
        return Results.Ok(new 
        { 
            source = "cache",
            value = cachedValue,
            message = "Value retrieved from Redis cache"
        });
    }
    
    // Value not in cache, generate new value
    var newValue = $"Generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
    
    // Store in cache with 60 seconds expiration
    await cacheService.SetAsync(cacheKey, newValue, TimeSpan.FromSeconds(60));
    
    return Results.Ok(new 
    { 
        source = "computed",
        value = newValue,
        message = "New value generated and cached for 60 seconds"
    });
})
.WithName("GetCacheDemo")
.WithOpenApi()
.WithDescription("Demonstrates cache usage with 60-second expiration");

// Additional endpoint to get cache value by key
app.MapGet("/api/cache/{key}", async (string key, ICacheService cacheService) =>
{
    var value = await cacheService.GetAsync(key);
    
    if (value == null)
    {
        return Results.NotFound(new { message = $"Key '{key}' not found in cache" });
    }
    
    return Results.Ok(new { key, value });
})
.WithName("GetCacheByKey")
.WithOpenApi();

// Endpoint to set cache value
app.MapPost("/api/cache/{key}", async (string key, CacheRequest request, ICacheService cacheService) =>
{
    var expiration = request.ExpirationSeconds.HasValue 
        ? TimeSpan.FromSeconds(request.ExpirationSeconds.Value) 
        : (TimeSpan?)null;
    
    await cacheService.SetAsync(key, request.Value, expiration);
    
    return Results.Ok(new 
    { 
        message = "Value cached successfully",
        key,
        expirationSeconds = request.ExpirationSeconds
    });
})
.WithName("SetCacheValue")
.WithOpenApi();

// Endpoint to delete cache value
app.MapDelete("/api/cache/{key}", async (string key, ICacheService cacheService) =>
{
    var deleted = await cacheService.RemoveAsync(key);
    
    if (!deleted)
    {
        return Results.NotFound(new { message = $"Key '{key}' not found in cache" });
    }
    
    return Results.Ok(new { message = "Value deleted successfully", key });
})
.WithName("DeleteCacheValue")
.WithOpenApi();

// Journey endpoints
app.MapGet("/api/journeys", async (IJourneyService journeyService) =>
{
    var journeyIds = await journeyService.GetAllJourneyIdsAsync();
    return Results.Ok(new { journeyIds, count = journeyIds.Count });
})
.WithName("GetAllJourneys")
.WithOpenApi();

app.MapGet("/api/journeys/{journeyId}", async (string journeyId, IJourneyService journeyService) =>
{
    var journey = await journeyService.GetJourneyAsync(journeyId);
    
    if (journey == null)
    {
        return Results.NotFound(new { message = $"Journey '{journeyId}' not found" });
    }
    
    return Results.Ok(journey);
})
.WithName("GetJourneyById")
.WithOpenApi();

app.MapPut("/api/journeys/{journeyId}/segments/{segmentId}/status", 
    async (string journeyId, string segmentId, UpdateStatusRequest request, IJourneyService journeyService) =>
{
    var success = await journeyService.UpdateSegmentStatusAsync(journeyId, segmentId, request.Status);
    
    if (!success)
    {
        return Results.NotFound(new { message = $"Journey '{journeyId}' or segment '{segmentId}' not found" });
    }
    
    return Results.Ok(new 
    { 
        message = "Segment status updated successfully",
        journeyId,
        segmentId,
        newStatus = request.Status
    });
})
.WithName("UpdateSegmentStatus")
.WithOpenApi()
.WithDescription("Updates a segment status");

app.MapPut("/api/journeys/{journeyId}/status", 
    async (string journeyId, UpdateStatusRequest request, IJourneyService journeyService) =>
{
    var success = await journeyService.UpdateJourneyStatusAsync(journeyId, request.Status);
    
    if (!success)
    {
        return Results.NotFound(new { message = $"Journey '{journeyId}' not found" });
    }
    
    return Results.Ok(new 
    { 
        message = "Journey status updated successfully",
        journeyId,
        newStatus = request.Status
    });
})
.WithName("UpdateJourneyStatus")
.WithOpenApi();

app.Run();

// Request models
record CacheRequest(string Value, int? ExpirationSeconds);
record UpdateStatusRequest(string Status);
record JourneysData(List<Journey> Journeys);
