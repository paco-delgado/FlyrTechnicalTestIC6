using FlyrTech.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FlyrTech.Tests;

public class JourneyEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public JourneyEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateSegmentStatus_ShouldReturn200_WhenServiceReturnsSuccess()
    {
        // Arrange
        var mockJourneyService = new Mock<IJourneyService>();
        mockJourneyService
            .Setup(s => s.UpdateSegmentStatusAsync("JRN-001", "SEG-001", "Departed"))
            .ReturnsAsync(UpdateResult.Success);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IJourneyService));
                services.AddSingleton(mockJourneyService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            "/api/journeys/JRN-001/segments/SEG-001/status",
            new { status = "Departed" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.Equal("Departed", body!["newStatus"]);
    }

    [Fact]
    public async Task UpdateSegmentStatus_ShouldReturn404_WhenServiceReturnsNotFound()
    {
        // Arrange
        var mockJourneyService = new Mock<IJourneyService>();
        mockJourneyService
            .Setup(s => s.UpdateSegmentStatusAsync("JRN-404", "SEG-404", "Departed"))
            .ReturnsAsync(UpdateResult.NotFound);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IJourneyService));
                services.AddSingleton(mockJourneyService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            "/api/journeys/JRN-404/segments/SEG-404/status",
            new { status = "Departed" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.Equal("Journey 'JRN-404' or segment 'SEG-404' not found", body!["message"]);
    }

    [Fact]
    public async Task UpdateSegmentStatus_ShouldReturn409_WhenServiceReturnsConflict()
    {
        // Arrange
        var mockJourneyService = new Mock<IJourneyService>();
        mockJourneyService
            .Setup(s => s.UpdateSegmentStatusAsync("JRN-001", "SEG-001", "Departed"))
            .ReturnsAsync(UpdateResult.Conflict);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace the real IJourneyService with our mock
                services.RemoveAll(typeof(IJourneyService));
                services.AddSingleton(mockJourneyService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            "/api/journeys/JRN-001/segments/SEG-001/status",
            new { status = "Departed" });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.Equal("Concurrency conflict. Please retry.", body!["message"]);
    }

    [Fact]
    public async Task UpdateJourneyStatus_ShouldReturn200_WhenServiceReturnsSuccess()
    {
        // Arrange
        var mockJourneyService = new Mock<IJourneyService>();
        mockJourneyService
            .Setup(s => s.UpdateJourneyStatusAsync("JRN-001", "CheckedIn"))
            .ReturnsAsync(UpdateResult.Success);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IJourneyService));
                services.AddSingleton(mockJourneyService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            "/api/journeys/JRN-001/status",
            new { status = "CheckedIn" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.Equal("CheckedIn", body!["newStatus"]);
    }


    [Fact]
    public async Task UpdateJourneyStatus_ShouldReturn404_WhenServiceReturnsNotFound()
    {
        // Arrange
        var mockJourneyService = new Mock<IJourneyService>();
        mockJourneyService
            .Setup(s => s.UpdateJourneyStatusAsync("JRN-404", "CheckedIn"))
            .ReturnsAsync(UpdateResult.NotFound);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IJourneyService));
                services.AddSingleton(mockJourneyService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            "/api/journeys/JRN-404/status",
            new { status = "CheckedIn" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.Equal("Journey 'JRN-404' not found", body!["message"]);
    }

    [Fact]
    public async Task UpdateJourneyStatus_ShouldReturn409_WhenServiceReturnsConflict()
    {
        // Arrange
        var mockJourneyService = new Mock<IJourneyService>();
        mockJourneyService
            .Setup(s => s.UpdateJourneyStatusAsync("JRN-001", "CheckedIn"))
            .ReturnsAsync(UpdateResult.Conflict);

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(IJourneyService));
                services.AddSingleton(mockJourneyService.Object);
            });
        }).CreateClient();

        // Act
        var response = await client.PutAsJsonAsync(
            "/api/journeys/JRN-001/status",
            new { status = "CheckedIn" });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(body);
        Assert.Equal("Concurrency conflict. Please retry.", body!["message"]);
    }
}
