using System.Text.Json;
using FlyrTech.Core;
using FlyrTech.Core.Models;

namespace FlyrTech.Infrastructure;

public class JourneyService : IJourneyService
{
    private readonly ICacheService _cacheService;
    private const string JourneyKeyPrefix = "journey:";
    private const string JourneyIdsKey = "journey:ids";

    public JourneyService(ICacheService cacheService)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<Journey?> GetJourneyAsync(string journeyId)
    {
        if (string.IsNullOrWhiteSpace(journeyId))
            throw new ArgumentException("Journey ID cannot be null or empty", nameof(journeyId));

        var key = GetJourneyKey(journeyId);
        var json = await _cacheService.GetAsync(key);

        if (json == null)
            return null;

        return JsonSerializer.Deserialize<Journey>(json);
    }

    public async Task<bool> UpdateSegmentStatusAsync(string journeyId, string segmentId, string newStatus)
    {
        if (string.IsNullOrWhiteSpace(journeyId))
            throw new ArgumentException("Journey ID cannot be null or empty", nameof(journeyId));
        
        if (string.IsNullOrWhiteSpace(segmentId))
            throw new ArgumentException("Segment ID cannot be null or empty", nameof(segmentId));

        var journey = await GetJourneyAsync(journeyId);
        
        if (journey == null)
            return false;

        var segment = journey.Segments.FirstOrDefault(s => s.SegmentId == segmentId);
        
        if (segment == null)
            return false;

        // Simulate some processing time
        await Task.Delay(10);

        segment.Status = newStatus;

        var key = GetJourneyKey(journeyId);
        var json = JsonSerializer.Serialize(journey);
        await _cacheService.SetAsync(key, json);

        return true;
    }

    public async Task<bool> UpdateJourneyStatusAsync(string journeyId, string newStatus)
    {
        if (string.IsNullOrWhiteSpace(journeyId))
            throw new ArgumentException("Journey ID cannot be null or empty", nameof(journeyId));

        var journey = await GetJourneyAsync(journeyId);
        
        if (journey == null)
            return false;

        journey.Status = newStatus;

        var key = GetJourneyKey(journeyId);
        var json = JsonSerializer.Serialize(journey);
        await _cacheService.SetAsync(key, json);

        return true;
    }

    public async Task<List<string>> GetAllJourneyIdsAsync()
    {
        var json = await _cacheService.GetAsync(JourneyIdsKey);
        
        if (json == null)
            return new List<string>();

        return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }

    public async Task InitializeCacheAsync(List<Journey> journeys)
    {
        if (journeys == null || journeys.Count == 0)
            return;

        foreach (var journey in journeys)
        {
            var key = GetJourneyKey(journey.Id);
            var json = JsonSerializer.Serialize(journey);
            await _cacheService.SetAsync(key, json);
        }

        var journeyIds = journeys.Select(j => j.Id).ToList();
        var idsJson = JsonSerializer.Serialize(journeyIds);
        await _cacheService.SetAsync(JourneyIdsKey, idsJson);
    }

    private static string GetJourneyKey(string journeyId) => $"{JourneyKeyPrefix}{journeyId}";
}
