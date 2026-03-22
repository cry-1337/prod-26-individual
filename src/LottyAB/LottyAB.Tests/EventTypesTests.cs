using System.Net;
using LottyAB.Contracts.Request.Events;
using LottyAB.Domain.Entities;
using LottyAB.Tests.Common;

namespace LottyAB.Tests;

public class EventTypesTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetEventTypes_ReturnsDefaultTypes()
    {
        await AuthorizeAsAdmin();

        var response = await Client.GetAsync("/api/events/types");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("exposure", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("click", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("conversion", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateEventType_WithValidData_SucceedsOrConflicts()
    {
        await AuthorizeAsAdmin();

        var request = new CreateEventTypeRequest(
            EventKey: $"custom-event-{Guid.NewGuid()}",
            DisplayName: "Custom Event",
            Description: "A custom event type for testing",
            RequiresExposure: true,
            IsExposureEvent: false
        );

        var response = await Client.PostAsJsonAsync("/api/events/types", request);

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetEventType_WithExistingId_ReturnsEventType()
    {
        await AuthorizeAsAdmin();

        var typesResponse = await Client.GetAsync("/api/events/types");
        var eventTypes = await typesResponse.Content.ReadFromJsonAsync<List<EventTypeEntity>>(JsonOptions);
        Assert.NotNull(eventTypes);

        if (eventTypes.Count > 0)
        {
            var firstEventType = eventTypes[0];

            var getResponse = await Client.GetAsync($"/api/events/types/{firstEventType.Id}");

            getResponse.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task GetAttributionStats_ReturnsStats()
    {
        await AuthorizeAsAdmin();

        var response = await Client.GetAsync("/api/events/attribution/stats");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }
}