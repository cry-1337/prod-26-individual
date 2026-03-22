using System.Net;
using System.Net.Http.Headers;
using LottyAB.Contracts.Request.ApproverGroups;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using LottyAB.Tests.Common;

namespace LottyAB.Tests;

public class ApproverGroupsTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateApproverGroup_AsAdmin_ReturnsCreated()
    {
        await AuthorizeAsAdmin();

        var request = new CreateApproverGroupRequest(
            Name: "QA Approvers",
            Description: "Quality Assurance team",
            ApproversToStart: 2,
            ApproverIds: null
        );

        var response = await Client.PostAsJsonAsync("/api/approver-group", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var group = await response.Content.ReadFromJsonAsync<ApproverGroupEntity>(JsonOptions);
        Assert.NotNull(group);
        Assert.Equal("QA Approvers", group.Name);
        Assert.Equal(2, group.ApproversToStart);
    }

    [Fact]
    public async Task CreateApproverGroup_NonAdmin_ReturnsForbidden()
    {
        var (_, token) = await CreateUserWithRole(EUserRole.Approver);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateApproverGroupRequest("Group", null, 1, null);

        var response = await Client.PostAsJsonAsync("/api/approver-group", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetApproverGroup_ExistingId_ReturnsGroup()
    {
        var (approverId, _) = await CreateUserWithRole(EUserRole.Approver);
        var groupId = await CreateApproverGroup(approverIds: [approverId], approversToStart: 1, name: "Get Test Group");

        await AuthorizeAsAdmin();
        var response = await Client.GetAsync($"/api/approver-group/{groupId}");

        response.EnsureSuccessStatusCode();
        var group = await response.Content.ReadFromJsonAsync<ApproverGroupEntity>(JsonOptions);
        Assert.NotNull(group);
        Assert.Equal(groupId, group.Id);
        Assert.Equal("Get Test Group", group.Name);
        Assert.Single(group.Approvers);
    }

    [Fact]
    public async Task GetApproverGroups_ReturnsPagedList()
    {
        await CreateApproverGroup(name: "Paged Group A");
        await CreateApproverGroup(name: "Paged Group B");

        await AuthorizeAsAdmin();
        var response = await Client.GetAsync("/api/approver-group?page=0&size=20");

        response.EnsureSuccessStatusCode();
        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<ApproverGroupEntity>>(JsonOptions);
        Assert.NotNull(paged);
        Assert.True(paged.TotalCount >= 2);
    }

    [Fact]
    public async Task UpdateApproverGroup_ChangesFields()
    {
        var groupId = await CreateApproverGroup(name: "Before Update", approversToStart: 1);

        await AuthorizeAsAdmin();
        var updateRequest = new UpdateApproverGroupRequest(
            Name: "After Update",
            Description: "Updated description",
            ApproversToStart: 3,
            ApproverIds: null
        );

        var response = await Client.PutAsJsonAsync($"/api/approver-group/{groupId}", updateRequest);

        response.EnsureSuccessStatusCode();
        var group = await response.Content.ReadFromJsonAsync<ApproverGroupEntity>(JsonOptions);
        Assert.NotNull(group);
        Assert.Equal("After Update", group.Name);
        Assert.Equal(3, group.ApproversToStart);
    }

    [Fact]
    public async Task DeleteApproverGroup_ExistingId_ReturnsNoContent()
    {
        var groupId = await CreateApproverGroup(name: "To Delete");

        await AuthorizeAsAdmin();
        var response = await Client.DeleteAsync($"/api/approver-group/{groupId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetApproverGroup_NonExistingId_ReturnsNotFound()
    {
        await AuthorizeAsAdmin();

        var response = await Client.GetAsync($"/api/approver-group/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReviewExperiment_ByApproverInGroup_Succeeds()
    {
        var (approverId, approverToken) = await CreateUserWithRole(EUserRole.Approver);
        var groupId = await CreateApproverGroup(approverIds: [approverId], approversToStart: 1);

        await AuthorizeAsAdmin();
        var experimentId = await CreateExperimentWithGroup(groupId);
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", approverToken);
        var reviewRequest = new { Decision = "Approved", Comment = "LGTM" };
        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);

        response.EnsureSuccessStatusCode();
        var experiment = await response.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.Equal(EExperimentStatus.Approved, experiment!.Status);
    }

    [Fact]
    public async Task ReviewExperiment_ByApproverNotInGroup_ReturnsUnprocessableEntity()
    {
        var (approverId, _) = await CreateUserWithRole(EUserRole.Approver);
        var (_, outsiderToken) = await CreateUserWithRole(EUserRole.Approver);
        var groupId = await CreateApproverGroup(approverIds: [approverId], approversToStart: 1);

        await AuthorizeAsAdmin();
        var experimentId = await CreateExperimentWithGroup(groupId);
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        var reviewRequest = new { Decision = "Approved", Comment = "Trying to sneak in" };
        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ReviewExperiment_WithThreshold2_RequiresTwoApprovals()
    {
        var (approver1Id, approver1Token) = await CreateUserWithRole(EUserRole.Approver);
        var (approver2Id, approver2Token) = await CreateUserWithRole(EUserRole.Approver);
        var groupId = await CreateApproverGroup(
            approverIds: [approver1Id, approver2Id],
            approversToStart: 2);

        await AuthorizeAsAdmin();
        var experimentId = await CreateExperimentWithGroup(groupId);
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", approver1Token);
        var firstReview = new { Decision = "Approved", Comment = "First approval" };
        var firstResponse = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", firstReview);
        firstResponse.EnsureSuccessStatusCode();
        var afterFirst = await firstResponse.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.Equal(EExperimentStatus.InReview, afterFirst!.Status);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", approver2Token);
        var secondReview = new { Decision = "Approved", Comment = "Second approval" };
        var secondResponse = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", secondReview);
        secondResponse.EnsureSuccessStatusCode();
        var afterSecond = await secondResponse.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.Equal(EExperimentStatus.Approved, afterSecond!.Status);
    }

    [Fact]
    public async Task ReviewExperiment_DuplicateReview_ReturnsUnprocessableEntity()
    {
        var (approverId, approverToken) = await CreateUserWithRole(EUserRole.Approver);
        var groupId = await CreateApproverGroup(approverIds: [approverId], approversToStart: 2);

        await AuthorizeAsAdmin();
        var experimentId = await CreateExperimentWithGroup(groupId);
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", approverToken);
        var reviewRequest = new { Decision = "Approved", Comment = "First" };

        var first = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);
        first.EnsureSuccessStatusCode();

        var second = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
    }

    [Fact]
    public async Task ReviewExperiment_WithoutGroup_AnyApproverCanReview()
    {
        var (_, approverToken) = await CreateUserWithRole(EUserRole.Approver);

        await AuthorizeAsAdmin();
        var experimentId = await CreateExperiment();
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", approverToken);
        var reviewRequest = new { Decision = "Approved", Comment = "No group, should work" };
        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);

        response.EnsureSuccessStatusCode();
        var experiment = await response.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.Equal(EExperimentStatus.Approved, experiment!.Status);
    }

    [Fact]
    public async Task ReviewExperiment_WithChangesRequested_ByGroupMember_StatusToDraft()
    {
        var (approverId, approverToken) = await CreateUserWithRole(EUserRole.Approver);
        var groupId = await CreateApproverGroup(approverIds: [approverId], approversToStart: 1);

        await AuthorizeAsAdmin();
        var experimentId = await CreateExperimentWithGroup(groupId);
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", approverToken);
        var reviewRequest = new { Decision = "ChangesRequested", Comment = "Needs fixing" };
        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);

        response.EnsureSuccessStatusCode();
        var experiment = await response.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.Equal(EExperimentStatus.Draft, experiment!.Status);
    }

    [Fact]
    public async Task ReviewExperiment_Rejected_ByGroupMember_StatusToRejected()
    {
        var (approverId, approverToken) = await CreateUserWithRole(EUserRole.Approver);
        var groupId = await CreateApproverGroup(approverIds: [approverId], approversToStart: 1);

        await AuthorizeAsAdmin();
        var experimentId = await CreateExperimentWithGroup(groupId);
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", approverToken);
        var reviewRequest = new { Decision = "Rejected", Comment = "Not safe to launch" };
        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);

        response.EnsureSuccessStatusCode();
        var experiment = await response.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.Equal(EExperimentStatus.Rejected, experiment!.Status);
    }

    [Fact]
    public async Task DeleteApproverGroup_NonExistingId_ReturnsNotFound()
    {
        await AuthorizeAsAdmin();

        var response = await Client.DeleteAsync($"/api/approver-group/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateApproverGroup_NonExistingId_ReturnsNotFound()
    {
        await AuthorizeAsAdmin();

        var updateRequest = new UpdateApproverGroupRequest("Name", null, 1, null);
        var response = await Client.PutAsJsonAsync($"/api/approver-group/{Guid.NewGuid()}", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateApproverGroup_WithMultipleApprovers_AllAppearsInGroup()
    {
        var (id1, _) = await CreateUserWithRole(EUserRole.Approver);
        var (id2, _) = await CreateUserWithRole(EUserRole.Approver);
        var (id3, _) = await CreateUserWithRole(EUserRole.Approver);

        var groupId = await CreateApproverGroup(
            approverIds: [id1, id2, id3],
            approversToStart: 2,
            name: "Multi Approver Group"
        );

        await AuthorizeAsAdmin();
        var response = await Client.GetAsync($"/api/approver-group/{groupId}");

        response.EnsureSuccessStatusCode();
        var group = await response.Content.ReadFromJsonAsync<ApproverGroupEntity>(JsonOptions);
        Assert.NotNull(group);
        Assert.Equal(3, group.Approvers.Count);
        Assert.Equal(2, group.ApproversToStart);
    }

    [Fact]
    public async Task UpdateApproverGroup_ReplaceApprovers_NewApproverCanReview()
    {
        var (oldApproverId, _) = await CreateUserWithRole(EUserRole.Approver);
        var (newApproverId, newApproverToken) = await CreateUserWithRole(EUserRole.Approver);
        var groupId = await CreateApproverGroup(approverIds: [oldApproverId], approversToStart: 1);

        await AuthorizeAsAdmin();
        var updateRequest = new UpdateApproverGroupRequest("Updated Group", null, 1, [newApproverId]);
        await Client.PutAsJsonAsync($"/api/approver-group/{groupId}", updateRequest);

        var experimentId = await CreateExperimentWithGroup(groupId);
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newApproverToken);
        var reviewRequest = new { Decision = "Approved", Comment = "New approver" };
        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);

        response.EnsureSuccessStatusCode();
        var experiment = await response.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.Equal(EExperimentStatus.Approved, experiment!.Status);
    }
}