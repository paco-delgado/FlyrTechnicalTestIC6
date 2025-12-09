using FlyrTech.Core.Models;

namespace FlyrTech.Core;

/// <summary>
/// Service interface for managing journeys in cache
/// </summary>
public interface IJourneyService
{
    /// <summary>
    /// Gets a journey by ID
    /// </summary>
    Task<Journey?> GetJourneyAsync(string journeyId);

    /// <summary>
    /// Updates a specific segment status within a journey
    /// </summary>
    Task<bool> UpdateSegmentStatusAsync(string journeyId, string segmentId, string newStatus);

    /// <summary>
    /// Updates the overall journey status
    /// </summary>
    Task<bool> UpdateJourneyStatusAsync(string journeyId, string newStatus);

    /// <summary>
    /// Gets all journey IDs
    /// </summary>
    Task<List<string>> GetAllJourneyIdsAsync();

    /// <summary>
    /// Initializes the cache with a list of journeys
    /// </summary>
    Task InitializeCacheAsync(List<Journey> journeys);
}
