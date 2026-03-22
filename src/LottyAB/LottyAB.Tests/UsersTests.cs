using System.Net;
using System.Net.Http.Headers;
using LottyAB.Contracts.Request.Auth;
using LottyAB.Contracts.Request.Users;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using LottyAB.Tests.Common;

namespace LottyAB.Tests;

public class UsersTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var registerRequest = new RegisterRequest(
            Email: $"test-{Guid.NewGuid()}@example.com",
            Password: "SecurePassword123!",
            Name: "Test User"
        );

        var response = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_TwoDistinctUsers_BothSucceed()
    {
        var registerRequest1 = new RegisterRequest(
            Email: $"user1-{Guid.NewGuid()}@example.com",
            Password: "Password123!",
            Name: "User 1"
        );

        var registerRequest2 = new RegisterRequest(
            Email: $"user2-{Guid.NewGuid()}@example.com",
            Password: "Password123!",
            Name: "User 2"
        );

        var response1 = await Client.PostAsJsonAsync("/api/auth/register", registerRequest1);
        var response2 = await Client.PostAsJsonAsync("/api/auth/register", registerRequest2);

        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
    }

    [Fact]
    public async Task CreateUser_AsAdmin_ReturnsCreated()
    {
        await AuthorizeAsAdmin();

        var createRequest = new CreateUserRequest(
            Email: $"admin-created-{Guid.NewGuid()}@example.com",
            Password: "Password123!",
            Name: "Admin Created User",
            Role: EUserRole.Viewer
        );

        var response = await Client.PostAsJsonAsync("/api/users", createRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_ReturnsPagedResults()
    {
        await AuthorizeAsAdmin();

        var response = await Client.GetAsync("/api/users?pageNumber=1&pageSize=10");

        response.EnsureSuccessStatusCode();
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<UserEntity>>(JsonOptions);
        Assert.NotNull(pagedResponse);
        Assert.NotNull(pagedResponse.Items);
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsUser()
    {
        await AuthorizeAsAdmin();

        var response = await Client.GetAsync("/api/users/00000000-0000-0000-0000-000000000001");

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserEntity>(JsonOptions);
        Assert.NotNull(user);
        Assert.Equal("admin@lottyab.com", user.Email);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsUpdated()
    {
        await AuthorizeAsAdmin();

        var createRequest = new CreateUserRequest(
            Email: $"update-test-{Guid.NewGuid()}@example.com",
            Password: "Password123!",
            Name: "Update Test",
            Role: EUserRole.Viewer
        );

        var createResponse = await Client.PostAsJsonAsync("/api/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserEntity>(JsonOptions);
        Assert.NotNull(createdUser);

        var updateRequest = new UpdateUserRequest(
            Name: "Updated Name",
            Role: EUserRole.Experimenter,
            IsActive: true
        );

        var updateResponse = await Client.PutAsJsonAsync($"/api/users/{createdUser.Id}", updateRequest);

        updateResponse.EnsureSuccessStatusCode();
        var updatedUser = await updateResponse.Content.ReadFromJsonAsync<UserEntity>(JsonOptions);
        Assert.NotNull(updatedUser);
        Assert.Equal("Updated Name", updatedUser.Name);
    }

    [Fact]
    public async Task DeactivateUser_WithValidId_ReturnsNoContent()
    {
        await AuthorizeAsAdmin();

        var createRequest = new CreateUserRequest(
            Email: $"deactivate-{Guid.NewGuid()}@example.com",
            Password: "Password123!",
            Name: "Deactivate Test",
            Role: EUserRole.Viewer
        );

        var createResponse = await Client.PostAsJsonAsync("/api/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserEntity>(JsonOptions);
        Assert.NotNull(createdUser);

        var deactivateResponse = await Client.DeleteAsync($"/api/users/{createdUser.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deactivateResponse.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithoutAdminRole_ReturnsForbidden()
    {
        var registerRequest = new RegisterRequest(
            Email: $"non-admin-{Guid.NewGuid()}@example.com",
            Password: "Password123!",
            Name: "Non Admin"
        );
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerData = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(registerData);

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerData.AccessToken);

        var createRequest = new CreateUserRequest(
            Email: $"forbidden-{Guid.NewGuid()}@example.com",
            Password: "Password123!",
            Name: "Forbidden Test",
            Role: EUserRole.Viewer
        );

        var response = await Client.PostAsJsonAsync("/api/users", createRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}