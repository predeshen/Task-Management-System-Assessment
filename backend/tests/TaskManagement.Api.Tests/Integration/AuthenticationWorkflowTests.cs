using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Tests.Integration;

[TestFixture]
public class AuthenticationWorkflowTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });
        
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async System.Threading.Tasks.Task CompleteAuthenticationFlow_ShouldWorkEndToEnd()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "password123"
        };

        // Act & Assert - Step 1: Login with valid credentials
        var loginResponse = await LoginAsync(loginRequest);
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(loginContent, GetJsonOptions());

        Assert.That(authResponse, Is.Not.Null);
        Assert.That(authResponse!.Token, Is.Not.Null.And.Not.Empty);
        Assert.That(authResponse.User, Is.Not.Null);
        Assert.That(authResponse.User.Username, Is.EqualTo("admin"));

        // Act & Assert - Step 2: Use token to access protected endpoint
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.Token);
        var protectedResponse = await _client.GetAsync("/api/tasks");
        Assert.That(protectedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act & Assert - Step 3: Verify token works for multiple requests
        var secondRequest = await _client.GetAsync("/api/tasks");
        Assert.That(secondRequest.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var thirdRequest = await _client.GetAsync("/api/tasks");
        Assert.That(thirdRequest.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async System.Threading.Tasks.Task InvalidCredentialsFlow_ShouldRejectLogin()
    {
        // Test Case 1: Wrong password
        var wrongPasswordRequest = new LoginRequest
        {
            Username = "admin",
            Password = "wrongpassword"
        };

        var wrongPasswordResponse = await LoginAsync(wrongPasswordRequest);
        Assert.That(wrongPasswordResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var wrongPasswordContent = await wrongPasswordResponse.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(wrongPasswordContent, GetJsonOptions());
        Assert.That(errorResponse, Is.Not.Null);
        Assert.That(errorResponse!.Message, Contains.Substring("Login failed").Or.Contains("Invalid credentials").Or.Contains("Invalid username or password"));

        // Test Case 2: Non-existent user
        var nonExistentUserRequest = new LoginRequest
        {
            Username = "nonexistentuser",
            Password = "password123"
        };

        var nonExistentResponse = await LoginAsync(nonExistentUserRequest);
        Assert.That(nonExistentResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // Test Case 3: Empty credentials
        var emptyCredentialsRequest = new LoginRequest
        {
            Username = "",
            Password = ""
        };

        var emptyResponse = await LoginAsync(emptyCredentialsRequest);
        Assert.That(emptyResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var emptyContent = await emptyResponse.Content.ReadAsStringAsync();
        var validationError = JsonSerializer.Deserialize<ErrorResponse>(emptyContent, GetJsonOptions());
        Assert.That(validationError, Is.Not.Null);
        Assert.That(validationError!.Message, Contains.Substring("Validation"));
    }

    [Test]
    public async System.Threading.Tasks.Task TokenValidationFlow_ShouldEnforceTokenSecurity()
    {
        // Test Case 1: No token provided
        var noTokenResponse = await _client.GetAsync("/api/tasks");
        Assert.That(noTokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // Test Case 2: Invalid token format
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var invalidTokenResponse = await _client.GetAsync("/api/tasks");
        Assert.That(invalidTokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // Test Case 3: Malformed JWT
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.jwt.token");
        var malformedResponse = await _client.GetAsync("/api/tasks");
        Assert.That(malformedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // Test Case 4: Empty token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "");
        var emptyTokenResponse = await _client.GetAsync("/api/tasks");
        Assert.That(emptyTokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // Test Case 5: Wrong authentication scheme
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "somevalue");
        var wrongSchemeResponse = await _client.GetAsync("/api/tasks");
        Assert.That(wrongSchemeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async System.Threading.Tasks.Task ProtectedEndpointsFlow_ShouldRequireAuthentication()
    {
        // Test all protected endpoints without authentication
        var protectedEndpoints = new[]
        {
            "/api/tasks",
            "/api/tasks/1",
            "/api/tasks/status/ToDo"
        };

        foreach (var endpoint in protectedEndpoints)
        {
            var response = await _client.GetAsync(endpoint);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), 
                $"Endpoint {endpoint} should require authentication");
        }

        // Test POST, PUT, PATCH, DELETE methods
        var taskRequest = new CreateTaskRequest { Title = "Test", Description = "Test" };
        var json = JsonSerializer.Serialize(taskRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var postResponse = await _client.PostAsync("/api/tasks", content);
        Assert.That(postResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var putResponse = await _client.PutAsync("/api/tasks/1", content);
        Assert.That(putResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var patchContent = new StringContent(
            JsonSerializer.Serialize(new UpdateTaskStatusRequest { Status = "InProgress" }), 
            Encoding.UTF8, "application/json");
        var patchResponse = await _client.PatchAsync("/api/tasks/1/status", patchContent);
        Assert.That(patchResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var deleteResponse = await _client.DeleteAsync("/api/tasks/1");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async System.Threading.Tasks.Task AuthenticatedEndpointsFlow_ShouldWorkWithValidToken()
    {
        // Arrange - Get valid token
        var token = await GetValidJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act & Assert - Test all endpoints work with valid authentication
        var getTasksResponse = await _client.GetAsync("/api/tasks");
        Assert.That(getTasksResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var getByStatusResponse = await _client.GetAsync("/api/tasks/status/ToDo");
        Assert.That(getByStatusResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Create a task to test other endpoints
        var createRequest = new CreateTaskRequest { Title = "Auth Test Task", Description = "Testing authentication" };
        var createJson = JsonSerializer.Serialize(createRequest);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");

        var createResponse = await _client.PostAsync("/api/tasks", createContent);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var createdTaskContent = await createResponse.Content.ReadAsStringAsync();
        var createdTask = JsonSerializer.Deserialize<TaskDto>(createdTaskContent, GetJsonOptions());

        // Test GET by ID
        var getByIdResponse = await _client.GetAsync($"/api/tasks/{createdTask!.Id}");
        Assert.That(getByIdResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Test PUT (update)
        var updateRequest = new UpdateTaskRequest { Title = "Updated Title", Description = "Updated Description" };
        var updateJson = JsonSerializer.Serialize(updateRequest);
        var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

        var updateResponse = await _client.PutAsync($"/api/tasks/{createdTask.Id}", updateContent);
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Test PATCH (status update)
        var statusRequest = new UpdateTaskStatusRequest { Status = "InProgress" };
        var statusJson = JsonSerializer.Serialize(statusRequest);
        var statusContent = new StringContent(statusJson, Encoding.UTF8, "application/json");

        var statusResponse = await _client.PatchAsync($"/api/tasks/{createdTask.Id}/status", statusContent);
        Assert.That(statusResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Test DELETE
        var deleteResponse = await _client.DeleteAsync($"/api/tasks/{createdTask.Id}");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async System.Threading.Tasks.Task ConcurrentAuthenticationFlow_ShouldHandleMultipleClients()
    {
        // Arrange - Create multiple clients with same credentials
        var clients = new List<HttpClient>();
        var tokens = new List<string>();

        try
        {
            // Act - Login with multiple clients simultaneously
            var loginTasks = Enumerable.Range(0, 3).Select(async i =>
            {
                var client = _factory.CreateClient();
                clients.Add(client);
                
                var token = await GetValidJwtTokenAsync(client);
                tokens.Add(token);
                
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return client;
            });

            await System.Threading.Tasks.Task.WhenAll(loginTasks);

            // Assert - All clients should be able to access protected resources
            var accessTasks = clients.Select(async client =>
            {
                var response = await client.GetAsync("/api/tasks");
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                return response;
            });

            await System.Threading.Tasks.Task.WhenAll(accessTasks);

            // Assert - All tokens should be valid (tokens may be the same if generated quickly)
            Assert.That(tokens.Count, Is.EqualTo(3), "Should have 3 tokens");
            Assert.That(tokens.All(t => !string.IsNullOrEmpty(t)), Is.True, "All tokens should be valid");
        }
        finally
        {
            // Cleanup
            foreach (var client in clients)
            {
                client.Dispose();
            }
        }
    }

    private async Task<HttpResponseMessage> LoginAsync(LoginRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync("/api/auth/login", content);
    }

    private async Task<string> GetValidJwtTokenAsync(HttpClient? client = null)
    {
        var clientToUse = client ?? _client;
        
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "password123"
        };

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

        var loginResponse = await clientToUse.PostAsync("/api/auth/login", loginContent);
        var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
        
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(loginResponseContent, GetJsonOptions());
        return authResponse?.Token ?? throw new InvalidOperationException("Failed to get valid JWT token");
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}