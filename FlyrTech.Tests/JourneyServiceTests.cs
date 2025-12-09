using FlyrTech.Core;
using FlyrTech.Core.Models;
using FlyrTech.Infrastructure;
using StackExchange.Redis;
using Xunit;

namespace FlyrTech.Tests;

public class JourneyServiceTests
{
    private readonly ICacheService _cacheService;
    private readonly IJourneyService _journeyService;

    public JourneyServiceTests()
    {
        // Connect to Redis for testing
        var redis = ConnectionMultiplexer.Connect("localhost:6379");
        _cacheService = new RedisCacheService(redis);
        _journeyService = new JourneyService(_cacheService);
    }

    [Fact]
    public async Task UpdateSegmentStatus_ConcurrentUpdates_ShouldPersistAllChanges()
    {
        // Arrange
        var journey = CreateTestJourney();
        var journeyService = _journeyService;
        
        await journeyService.InitializeCacheAsync(new List<Journey> { journey });

        var updates = new List<(string segmentId, string status)>
        {
            ("SEG-001", "Departed"),
            ("SEG-002", "Boarding"),
            ("SEG-003", "Delayed"),
            ("SEG-004", "Cancelled"),
            ("SEG-005", "Departed"),
            ("SEG-006", "Boarding"),
            ("SEG-007", "Delayed"),
            ("SEG-008", "Cancelled"),
            ("SEG-009", "Departed"),
            ("SEG-010", "Boarding"),
            ("SEG-011", "Delayed"),
            ("SEG-012", "Cancelled"),
            ("SEG-013", "Departed"),
            ("SEG-014", "Boarding"),
            ("SEG-015", "Delayed"),
            ("SEG-016", "Cancelled"),
            ("SEG-017", "Departed"),
            ("SEG-018", "Boarding"),
            ("SEG-019", "Delayed"),
            ("SEG-020", "Cancelled")
        };

        // Act
        var tasks = updates.Select(update => 
            journeyService.UpdateSegmentStatusAsync(journey.Id, update.segmentId, update.status)
        ).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        var results = await Task.WhenAll(tasks);
        var allSuccessful = results.All(r => r);
        Assert.True(allSuccessful, "All update operations should return true");

        var finalJourney = await journeyService.GetJourneyAsync(journey.Id);
        Assert.NotNull(finalJourney);

        var failures = new List<string>();
        foreach (var (segmentId, expectedStatus) in updates)
        {
            var segment = finalJourney.Segments.FirstOrDefault(s => s.SegmentId == segmentId);
            Assert.NotNull(segment);

            if (segment.Status != expectedStatus)
            {
                failures.Add($"Segment {segmentId}: expected '{expectedStatus}', got '{segment.Status}'");
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public async Task UpdateSegmentStatus_SequentialUpdates_ShouldWorkCorrectly()
    {
        // Arrange
        var journey = CreateTestJourney();
        var journeyService = _journeyService;
        
        await journeyService.InitializeCacheAsync(new List<Journey> { journey });

        // Act
        for (int i = 1; i <= 20; i++)
        {
            var segmentId = $"SEG-{i:D3}";
            var status = i % 4 == 0 ? "Cancelled" : i % 3 == 0 ? "Delayed" : i % 2 == 0 ? "Boarding" : "Departed";
            await journeyService.UpdateSegmentStatusAsync(journey.Id, segmentId, status);
        }

        // Assert
        var finalJourney = await journeyService.GetJourneyAsync(journey.Id);
        Assert.NotNull(finalJourney);

        for (int i = 1; i <= 20; i++)
        {
            var segmentId = $"SEG-{i:D3}";
            var expectedStatus = i % 4 == 0 ? "Cancelled" : i % 3 == 0 ? "Delayed" : i % 2 == 0 ? "Boarding" : "Departed";
            var segment = finalJourney.Segments.First(s => s.SegmentId == segmentId);
            Assert.Equal(expectedStatus, segment.Status);
        }
    }

    private Journey CreateTestJourney()
    {
        var segments = new List<Segment>();
        
        for (int i = 1; i <= 20; i++)
        {
            segments.Add(new Segment
            {
                SegmentId = $"SEG-{i:D3}",
                Origin = "MAD",
                Destination = "BCN",
                DepartureTime = DateTime.UtcNow.AddDays(i),
                ArrivalTime = DateTime.UtcNow.AddDays(i).AddHours(2),
                FlightNumber = $"IB{3000 + i}",
                Carrier = "Iberia",
                Status = "Scheduled",
                Price = 100.00m + i
            });
        }

        return new Journey
        {
            Id = "JRN-TEST-001",
            PassengerName = "Test User",
            PassengerEmail = "test@test.com",
            BookingDate = DateTime.UtcNow,
            Status = "Confirmed",
            TotalPrice = segments.Sum(s => s.Price),
            Segments = segments,
            Metadata = new Dictionary<string, string>
            {
                { "testRun", "true" }
            }
        };
    }
}
