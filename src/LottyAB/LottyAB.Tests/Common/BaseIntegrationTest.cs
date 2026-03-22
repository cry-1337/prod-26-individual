using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using LottyAB.Contracts.Request.ApproverGroups;
using LottyAB.Contracts.Request.Auth;
using LottyAB.Contracts.Request.Experiments;
using LottyAB.Contracts.Request.FeatureFlags;
using LottyAB.Contracts.Request.Users;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;

using ReviewRequest = LottyAB.Contracts.Request.Experiments.ReviewExperimentRequest;

namespace LottyAB.Tests.Common;

public abstract class BaseIntegrationTest(BaseTestFactory factory) : IClassFixture<BaseTestFactory>
{
    protected readonly HttpClient Client = factory.CreateClient();
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    protected async Task AuthorizeAsAdmin()
    {
        var token = await GetAdminToken();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task<string> GetAdminToken()
    {
        var loginRequest = new LoginRequest("admin@lottyab.com", "lottyab");
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var token = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return token!.AccessToken;
    }

    protected async Task<Guid> CreateFeatureFlag(string key)
    {
        var request = new CreateFeatureFlagRequest(
            Key: key,
            Name: $"Feature {key}",
            Description: "Test feature flag",
            ValueType: EFeatureFlagType.String,
            DefaultValue: "default"
        );

        var response = await Client.PostAsJsonAsync("/api/feature-flags", request);
        response.EnsureSuccessStatusCode();

        var featureFlag = await response.Content.ReadFromJsonAsync<FeatureFlagEntity>(JsonOptions);
        return featureFlag!.Id;
    }

    protected async Task<Guid> CreateExperiment(
        string? name = null,
        string? primaryMetricKey = null,
        string? featureFlagKey = null)
    {
        var featureFlagId = await CreateFeatureFlag(featureFlagKey ?? $"test-{Guid.NewGuid()}");

        var experimentRequest = new CreateExperimentRequest(
            Name: name ?? $"Experiment {Guid.NewGuid()}",
            Description: "Test experiment",
            FeatureFlagId: featureFlagId,
            AudienceFraction: 1.0,
            TargetingRule: null,
            PrimaryMetricKey: primaryMetricKey,
            Variants:
            [
                new("Control", "control", 0.5, true),
                new("Treatment", "treatment", 0.5, false)
            ]
        );

        var createResponse = await Client.PostAsJsonAsync("/api/experiments", experimentRequest);
        createResponse.EnsureSuccessStatusCode();

        var experiment = await createResponse.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        return experiment!.Id;
    }

    protected async Task<Guid> CreateApprovedExperiment(
        string? name = null,
        string? primaryMetricKey = null)
    {
        var experimentId = await CreateExperiment(name, primaryMetricKey ?? "conversion_rate");
        return experimentId;
    }

    protected async Task<Guid> CreateRunningExperiment(
        string? name = null,
        string? primaryMetricKey = null,
        string? featureFlagKey = null)
    {
        var experimentId = await CreateExperiment(name, primaryMetricKey ?? "conversion_rate", featureFlagKey);

        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);
        var reviewRequest = new ReviewRequest(EReviewDecision.Approved);
        await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);
        await Client.PostAsync($"/api/experiments/{experimentId}/start", null);

        return experimentId;
    }

    protected async Task<(Guid UserId, string Token)> CreateUserWithRole(EUserRole role, string? email = null)
    {
        await AuthorizeAsAdmin();

        var userEmail = email ?? $"{role.ToString().ToLower()}-{Guid.NewGuid()}@test.com";
        var password = "Password123!";

        var createRequest = new CreateUserRequest(
            Name: $"Test {role}",
            Email: userEmail,
            Password: password,
            Role: role
        );

        var createResponse = await Client.PostAsJsonAsync("/api/users", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var user = await createResponse.Content.ReadFromJsonAsync<UserEntity>(JsonOptions);

        var loginRequest = new LoginRequest(userEmail, password);
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginData = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);

        return (user!.Id, loginData!.AccessToken);
    }

    protected async Task<Guid> CreateApproverGroup(
        List<Guid>? approverIds = null,
        int approversToStart = 1,
        string? name = null)
    {
        await AuthorizeAsAdmin();

        var request = new CreateApproverGroupRequest(
            Name: name ?? $"Group {Guid.NewGuid()}",
            Description: null,
            ApproversToStart: approversToStart,
            ApproverIds: approverIds
        );

        var response = await Client.PostAsJsonAsync("/api/approver-group", request);
        response.EnsureSuccessStatusCode();
        var group = await response.Content.ReadFromJsonAsync<ApproverGroupEntity>(JsonOptions);
        return group!.Id;
    }

    protected async Task<Guid> CreateExperimentWithGroup(Guid approverGroupId, string? primaryMetricKey = null)
    {
        var featureFlagId = await CreateFeatureFlag($"test-{Guid.NewGuid()}");

        var experimentRequest = new CreateExperimentRequest(
            Name: $"Experiment {Guid.NewGuid()}",
            Description: null,
            FeatureFlagId: featureFlagId,
            AudienceFraction: 1.0,
            TargetingRule: null,
            PrimaryMetricKey: primaryMetricKey,
            Variants:
            [
                new("Control", "control", 0.5, true),
                new("Treatment", "treatment", 0.5, false)
            ],
            ApproverGroupId: approverGroupId
        );

        var createResponse = await Client.PostAsJsonAsync("/api/experiments", experimentRequest);
        createResponse.EnsureSuccessStatusCode();
        var experiment = await createResponse.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        return experiment!.Id;
    }

    protected async Task<(string flagKey, Guid experimentId)> CreateRunningExperimentWithFlag(
        double audienceFraction = 1.0,
        string? targetingRule = null,
        double controlWeight = 0.5,
        double treatmentWeight = 0.5)
    {
        var flagKey = $"test-{Guid.NewGuid()}";
        var featureFlagId = await CreateFeatureFlag(flagKey);

        var experimentRequest = new CreateExperimentRequest(
            Name: $"Test Experiment {Guid.NewGuid()}",
            Description: null,
            FeatureFlagId: featureFlagId,
            AudienceFraction: audienceFraction,
            TargetingRule: targetingRule,
            PrimaryMetricKey: null,
            Variants:
            [
                new("Control", "control", controlWeight, true),
                new("Treatment", "treatment", treatmentWeight, false)
            ]
        );

        var expResponse = await Client.PostAsJsonAsync("/api/experiments", experimentRequest);
        var experiment = await expResponse.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        var experimentId = experiment!.Id;

        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);
        var reviewRequest = new ReviewRequest(EReviewDecision.Approved);
        await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);
        await Client.PostAsync($"/api/experiments/{experimentId}/start", null);

        return (flagKey, experimentId);
    }
}