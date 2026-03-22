using System.Net;
using LottyAB.Contracts.Request.FeatureFlags;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using LottyAB.Tests.Common;

namespace LottyAB.Tests;

public class FeatureFlagsTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateFeatureFlag_WithValidData_ReturnsCreated()
    {
        await AuthorizeAsAdmin();

        var request = new CreateFeatureFlagRequest(
            Key: $"test-flag-{Guid.NewGuid()}",
            Name: "Test Feature Flag",
            Description: "Test description",
            ValueType: EFeatureFlagType.Boolean,
            DefaultValue: "true"
        );

        var response = await Client.PostAsJsonAsync("/api/feature-flags", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var featureFlag = await response.Content.ReadFromJsonAsync<FeatureFlagEntity>(JsonOptions);
        Assert.NotNull(featureFlag);
        Assert.Equal(request.Key, featureFlag.Key);
    }

    [Fact]
    public async Task CreateFeatureFlag_WithDuplicateKey_ReturnsConflict()
    {
        await AuthorizeAsAdmin();

        var key = $"duplicate-flag-{Guid.NewGuid()}";
        var request = new CreateFeatureFlagRequest(
            Key: key,
            Name: "Duplicate Test",
            Description: null,
            ValueType: EFeatureFlagType.String,
            DefaultValue: "value"
        );

        await Client.PostAsJsonAsync("/api/feature-flags", request);
        var response = await Client.PostAsJsonAsync("/api/feature-flags", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetFeatureFlags_ReturnsPagedResults()
    {
        await AuthorizeAsAdmin();

        var response = await Client.GetAsync("/api/feature-flags?pageNumber=1&pageSize=10");

        response.EnsureSuccessStatusCode();
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<FeatureFlagEntity>>(JsonOptions);
        Assert.NotNull(pagedResponse);
        Assert.NotNull(pagedResponse.Items);
    }

    [Fact]
    public async Task GetFeatureFlag_WithValidId_ReturnsFeatureFlag()
    {
        await AuthorizeAsAdmin();

        var createRequest = new CreateFeatureFlagRequest(
            Key: $"get-test-{Guid.NewGuid()}",
            Name: "Get Test",
            Description: null,
            ValueType: EFeatureFlagType.Number,
            DefaultValue: "42"
        );

        var createResponse = await Client.PostAsJsonAsync("/api/feature-flags", createRequest);
        var createdFlag = await createResponse.Content.ReadFromJsonAsync<FeatureFlagEntity>(JsonOptions);
        Assert.NotNull(createdFlag);

        var getResponse = await Client.GetAsync($"/api/feature-flags/{createdFlag.Id}");

        getResponse.EnsureSuccessStatusCode();
        var featureFlag = await getResponse.Content.ReadFromJsonAsync<FeatureFlagEntity>(JsonOptions);
        Assert.NotNull(featureFlag);
        Assert.Equal(createRequest.Key, featureFlag.Key);
    }

    [Fact]
    public async Task UpdateFeatureFlag_WithValidData_ReturnsUpdated()
    {
        await AuthorizeAsAdmin();

        var createRequest = new CreateFeatureFlagRequest(
            Key: $"update-test-{Guid.NewGuid()}",
            Name: "Update Test",
            Description: null,
            ValueType: EFeatureFlagType.String,
            DefaultValue: "old"
        );

        var createResponse = await Client.PostAsJsonAsync("/api/feature-flags", createRequest);
        var createdFlag = await createResponse.Content.ReadFromJsonAsync<FeatureFlagEntity>(JsonOptions);
        Assert.NotNull(createdFlag);

        var updateRequest = new UpdateFeatureFlagRequest(
            Name: "Updated Name",
            Description: "Updated description",
            DefaultValue: "new"
        );

        var updateResponse = await Client.PutAsJsonAsync($"/api/feature-flags/{createdFlag.Id}", updateRequest);

        updateResponse.EnsureSuccessStatusCode();
        var updatedFlag = await updateResponse.Content.ReadFromJsonAsync<FeatureFlagEntity>(JsonOptions);
        Assert.NotNull(updatedFlag);
        Assert.Equal("Updated Name", updatedFlag.Name);
    }

    [Fact]
    public async Task DeleteFeatureFlag_WithValidId_ReturnsNoContent()
    {
        await AuthorizeAsAdmin();

        var createRequest = new CreateFeatureFlagRequest(
            Key: $"delete-test-{Guid.NewGuid()}",
            Name: "Delete Test",
            Description: null,
            ValueType: EFeatureFlagType.Boolean,
            DefaultValue: "false"
        );

        var createResponse = await Client.PostAsJsonAsync("/api/feature-flags", createRequest);
        var createdFlag = await createResponse.Content.ReadFromJsonAsync<FeatureFlagEntity>(JsonOptions);
        Assert.NotNull(createdFlag);

        var deleteResponse = await Client.DeleteAsync($"/api/feature-flags/{createdFlag.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateFeatureFlag_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new CreateFeatureFlagRequest(
            Key: "unauthorized-test",
            Name: "Test",
            Description: null,
            ValueType: EFeatureFlagType.Boolean,
            DefaultValue: "true"
        );

        var response = await Client.PostAsJsonAsync("/api/feature-flags", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}