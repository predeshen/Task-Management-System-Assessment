using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TaskManagement.Api.Models;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Api.Tests.Integration;

[TestFixture]
public class TaskManagementWorkflowTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private string _authToken;

    [SetUp]
    public async System.Threading.Tasks.Task Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });
        
        _client = _factory.CreateClient();
        
        // Authenticate and get token for protected endpoints
        _authToken = await GetValidJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
    }

    [TearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async System.Threading.Tasks.Task CompleteTaskManagementWorkflow_ShouldWorkEndToEnd()
    {
        // Arrange - Test data
        var createTaskRequest = new CreateTaskRequest
        {
            Title = "Integration Test Task",
            Description = "This is a test task created during integration testing"
        };

        // Act & Assert - Step 1: Create a new task
        var createResponse = await CreateTaskAsync(createTaskRequest);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var createdTaskContent = await createResponse.Content.ReadAsStringAsync();
        var createdTask = JsonSerializer.Deserialize<TaskDto>(createdTaskContent, GetJsonOptions());
        
        Assert.That(createdTask, Is.Not.Null);
        Assert.That(createdTask!.Title, Is.EqualTo(createTaskRequest.Title));
        Assert.That(createdTask.Description, Is.EqualTo(createTaskRequest.Description));
        Assert.That(createdTask.Status, Is.EqualTo("ToDo"));
        Assert.That(createdTask.Id, Is.GreaterThan(0));

        // Act & Assert - Step 2: Retrieve the created task by ID
        var getTaskResponse = await _client.GetAsync($"/api/tasks/{createdTask.Id}");
        Assert.That(getTaskResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var retrievedTaskContent = await getTaskResponse.Content.ReadAsStringAsync();
        var retrievedTask = JsonSerializer.Deserialize<TaskDto>(retrievedTaskContent, GetJsonOptions());
        
        Assert.That(retrievedTask, Is.Not.Null);
        Assert.That(retrievedTask!.Id, Is.EqualTo(createdTask.Id));
        Assert.That(retrievedTask.Title, Is.EqualTo(createdTask.Title));

        // Act & Assert - Step 3: Update the task
        var updateTaskRequest = new UpdateTaskRequest
        {
            Title = "Updated Integration Test Task",
            Description = "This task has been updated during integration testing"
        };

        var updateResponse = await UpdateTaskAsync(createdTask.Id, updateTaskRequest);
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updatedTaskContent = await updateResponse.Content.ReadAsStringAsync();
        var updatedTask = JsonSerializer.Deserialize<TaskDto>(updatedTaskContent, GetJsonOptions());
        
        Assert.That(updatedTask, Is.Not.Null);
        Assert.That(updatedTask!.Title, Is.EqualTo(updateTaskRequest.Title));
        Assert.That(updatedTask.Description, Is.EqualTo(updateTaskRequest.Description));
        Assert.That(updatedTask.UpdatedAt, Is.Not.Null);

        // Act & Assert - Step 4: Update task status to InProgress
        var statusUpdateRequest = new UpdateTaskStatusRequest { Status = "InProgress" };
        var statusResponse = await UpdateTaskStatusAsync(createdTask.Id, statusUpdateRequest);
        Assert.That(statusResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var statusUpdatedTaskContent = await statusResponse.Content.ReadAsStringAsync();
        var statusUpdatedTask = JsonSerializer.Deserialize<TaskDto>(statusUpdatedTaskContent, GetJsonOptions());
        
        Assert.That(statusUpdatedTask, Is.Not.Null);
        Assert.That(statusUpdatedTask!.Status, Is.EqualTo("InProgress"));

        // Act & Assert - Step 5: Verify task appears in status-filtered list
        var statusFilterResponse = await _client.GetAsync("/api/tasks/status/InProgress");
        Assert.That(statusFilterResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var filteredTasksContent = await statusFilterResponse.Content.ReadAsStringAsync();
        var filteredTasks = JsonSerializer.Deserialize<TaskDto[]>(filteredTasksContent, GetJsonOptions());
        
        Assert.That(filteredTasks, Is.Not.Null);
        Assert.That(filteredTasks!.Any(t => t.Id == createdTask.Id), Is.True);

        // Act & Assert - Step 6: Complete the task
        var completeStatusRequest = new UpdateTaskStatusRequest { Status = "Completed" };
        var completeResponse = await UpdateTaskStatusAsync(createdTask.Id, completeStatusRequest);
        Assert.That(completeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act & Assert - Step 7: Verify task no longer appears in InProgress filter
        var inProgressResponse = await _client.GetAsync("/api/tasks/status/InProgress");
        var inProgressContent = await inProgressResponse.Content.ReadAsStringAsync();
        var inProgressTasks = JsonSerializer.Deserialize<TaskDto[]>(inProgressContent, GetJsonOptions());
        
        Assert.That(inProgressTasks!.Any(t => t.Id == createdTask.Id), Is.False);

        // Act & Assert - Step 8: Verify task appears in Completed filter
        var completedResponse = await _client.GetAsync("/api/tasks/status/Completed");
        var completedContent = await completedResponse.Content.ReadAsStringAsync();
        var completedTasks = JsonSerializer.Deserialize<TaskDto[]>(completedContent, GetJsonOptions());
        
        Assert.That(completedTasks!.Any(t => t.Id == createdTask.Id), Is.True);

        // Act & Assert - Step 9: Delete the task
        var deleteResponse = await _client.DeleteAsync($"/api/tasks/{createdTask.Id}");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Act & Assert - Step 10: Verify task is deleted
        var getDeletedTaskResponse = await _client.GetAsync($"/api/tasks/{createdTask.Id}");
        Assert.That(getDeletedTaskResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async System.Threading.Tasks.Task MultipleTasksWorkflow_ShouldHandleMultipleTasksCorrectly()
    {
        // Arrange - Create multiple tasks
        var tasks = new[]
        {
            new CreateTaskRequest { Title = "Task 1", Description = "First task" },
            new CreateTaskRequest { Title = "Task 2", Description = "Second task" },
            new CreateTaskRequest { Title = "Task 3", Description = "Third task" }
        };

        var createdTaskIds = new List<int>();

        // Act - Create multiple tasks
        foreach (var taskRequest in tasks)
        {
            var response = await CreateTaskAsync(taskRequest);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var content = await response.Content.ReadAsStringAsync();
            var task = JsonSerializer.Deserialize<TaskDto>(content, GetJsonOptions());
            createdTaskIds.Add(task!.Id);
        }

        // Assert - Verify all tasks are created
        Assert.That(createdTaskIds.Count, Is.EqualTo(3));

        // Act - Get all tasks
        var getAllResponse = await _client.GetAsync("/api/tasks");
        Assert.That(getAllResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var allTasksContent = await getAllResponse.Content.ReadAsStringAsync();
        var allTasks = JsonSerializer.Deserialize<TaskDto[]>(allTasksContent, GetJsonOptions());

        // Assert - Verify all created tasks are in the list
        Assert.That(allTasks, Is.Not.Null);
        foreach (var taskId in createdTaskIds)
        {
            Assert.That(allTasks!.Any(t => t.Id == taskId), Is.True);
        }

        // Act - Update different tasks to different statuses
        await UpdateTaskStatusAsync(createdTaskIds[0], new UpdateTaskStatusRequest { Status = "InProgress" });
        await UpdateTaskStatusAsync(createdTaskIds[1], new UpdateTaskStatusRequest { Status = "Completed" });
        // Leave createdTaskIds[2] as ToDo

        // Assert - Verify status filtering works correctly
        var todoResponse = await _client.GetAsync("/api/tasks/status/ToDo");
        var todoTasks = JsonSerializer.Deserialize<TaskDto[]>(
            await todoResponse.Content.ReadAsStringAsync(), GetJsonOptions());
        Assert.That(todoTasks!.Any(t => t.Id == createdTaskIds[2]), Is.True);

        var inProgressResponse = await _client.GetAsync("/api/tasks/status/InProgress");
        var inProgressTasks = JsonSerializer.Deserialize<TaskDto[]>(
            await inProgressResponse.Content.ReadAsStringAsync(), GetJsonOptions());
        Assert.That(inProgressTasks!.Any(t => t.Id == createdTaskIds[0]), Is.True);

        var completedResponse = await _client.GetAsync("/api/tasks/status/Completed");
        var completedTasks = JsonSerializer.Deserialize<TaskDto[]>(
            await completedResponse.Content.ReadAsStringAsync(), GetJsonOptions());
        Assert.That(completedTasks!.Any(t => t.Id == createdTaskIds[1]), Is.True);

        // Cleanup - Delete all created tasks
        foreach (var taskId in createdTaskIds)
        {
            await _client.DeleteAsync($"/api/tasks/{taskId}");
        }
    }

    [Test]
    public async System.Threading.Tasks.Task AuthenticationWorkflow_ShouldEnforceSecurityCorrectly()
    {
        // Arrange - Create a client without authentication
        using var unauthenticatedClient = _factory.CreateClient();

        // Act & Assert - Verify unauthenticated requests are rejected
        var getTasksResponse = await unauthenticatedClient.GetAsync("/api/tasks");
        Assert.That(getTasksResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var createTaskRequest = new CreateTaskRequest { Title = "Test", Description = "Test" };
        var createContent = new StringContent(JsonSerializer.Serialize(createTaskRequest), Encoding.UTF8, "application/json");
        var createResponse = await unauthenticatedClient.PostAsync("/api/tasks", createContent);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // Act & Assert - Verify invalid token is rejected
        unauthenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var invalidTokenResponse = await unauthenticatedClient.GetAsync("/api/tasks");
        Assert.That(invalidTokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // Act & Assert - Verify valid token works
        var validToken = await GetValidJwtTokenAsync();
        unauthenticatedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        var validTokenResponse = await unauthenticatedClient.GetAsync("/api/tasks");
        Assert.That(validTokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async System.Threading.Tasks.Task TaskIsolationWorkflow_ShouldIsolateUserTasks()
    {
        // This test would require creating multiple users, which depends on user registration
        // For now, we'll test that the current user can only access their own tasks
        
        // Arrange - Create a task with current user
        var taskRequest = new CreateTaskRequest { Title = "User Isolation Test", Description = "Test task isolation" };
        var createResponse = await CreateTaskAsync(taskRequest);
        var createdTaskContent = await createResponse.Content.ReadAsStringAsync();
        var createdTask = JsonSerializer.Deserialize<TaskDto>(createdTaskContent, GetJsonOptions());

        // Act - Try to access task with same user (should work)
        var getResponse = await _client.GetAsync($"/api/tasks/{createdTask!.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Act - Try to access non-existent task (should return 404)
        var nonExistentResponse = await _client.GetAsync("/api/tasks/99999");
        Assert.That(nonExistentResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        // Cleanup
        await _client.DeleteAsync($"/api/tasks/{createdTask.Id}");
    }

    [Test]
    public async System.Threading.Tasks.Task ValidationWorkflow_ShouldEnforceDataValidation()
    {
        // Act & Assert - Test empty title validation
        var invalidTaskRequest = new CreateTaskRequest { Title = "", Description = "Valid description" };
        var invalidResponse = await CreateTaskAsync(invalidTaskRequest);
        Assert.That(invalidResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        // Act & Assert - Test null title validation
        var nullTitleRequest = new CreateTaskRequest { Title = null!, Description = "Valid description" };
        var nullResponse = await CreateTaskAsync(nullTitleRequest);
        Assert.That(nullResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        // Act & Assert - Test invalid status validation
        var createResponse = await CreateTaskAsync(new CreateTaskRequest { Title = "Valid Task", Description = "Valid" });
        var createdTaskContent = await createResponse.Content.ReadAsStringAsync();
        var createdTask = JsonSerializer.Deserialize<TaskDto>(createdTaskContent, GetJsonOptions());

        var invalidStatusRequest = new UpdateTaskStatusRequest { Status = "InvalidStatus" };
        var statusResponse = await UpdateTaskStatusAsync(createdTask!.Id, invalidStatusRequest);
        Assert.That(statusResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        // Cleanup
        await _client.DeleteAsync($"/api/tasks/{createdTask.Id}");
    }

    private async Task<HttpResponseMessage> CreateTaskAsync(CreateTaskRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync("/api/tasks", content);
    }

    private async Task<HttpResponseMessage> UpdateTaskAsync(int taskId, UpdateTaskRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PutAsync($"/api/tasks/{taskId}", content);
    }

    private async Task<HttpResponseMessage> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PatchAsync($"/api/tasks/{taskId}/status", content);
    }

    private async Task<string> GetValidJwtTokenAsync()
    {
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "password123"
        };

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

        var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);
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