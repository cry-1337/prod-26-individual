using System.Net;
using LottyAB.Contracts.Request.Auth;
using LottyAB.Contracts.Request.Events;
using LottyAB.Contracts.Responses;
using LottyAB.Tests.Common;

namespace LottyAB.Tests;

public class SmokeTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task ReadyCheck_ReturnsHealthy()
    {
        var response = await Client.GetAsync("/ready");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var loginRequest = new LoginRequest("admin@lottyab.com", "lottyab");

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.EnsureSuccessStatusCode();
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(loginResponse);
        Assert.False(string.IsNullOrEmpty(loginResponse.AccessToken));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var loginRequest = new LoginRequest("admin@lottyab.com", "wrong-password");

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsNotFound()
    {
        var loginRequest = new LoginRequest("nonexistent@example.com", "password123");

        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Decide_WithNonExistentFeatureFlag_ReturnsNotFound()
    {
        var decideRequest = new
        {
            FeatureFlagKey = "non-existent-flag",
            SubjectId = "test-user-123",
            SubjectAttributes = new Dictionary<string, object>()
        };

        var response = await Client.PostAsJsonAsync("/api/decide", decideRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SendEvents_WithValidEvent_ReturnsSuccess()
    {
        var eventsRequest = new SendEventsRequest([
            new SendEventRequest(
                EventId: Guid.NewGuid().ToString(),
                EventTypeKey: "exposure",
                DecisionId: Guid.NewGuid(),
                SubjectId: "test-user-123",
                Properties: new Dictionary<string, object> { ["test"] = "value" },
                EventTimestamp: DateTime.UtcNow
            )
        ]);

        var response = await Client.PostAsJsonAsync("/api/events", eventsRequest);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SendEvents_WithDuplicateEventIds_ReturnsDuplicateCount()
    {
        var eventId = Guid.NewGuid().ToString();
        var eventsRequest = new SendEventsRequest([
            new SendEventRequest(eventId, "exposure", Guid.NewGuid(), "user-1", null, DateTime.UtcNow),
            new SendEventRequest(eventId, "exposure", Guid.NewGuid(), "user-1", null, DateTime.UtcNow)
        ]);

        var response = await Client.PostAsJsonAsync("/api/events", eventsRequest);

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("duplicates", content, StringComparison.OrdinalIgnoreCase);
    }
}