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
public class ErrorHandlingWorkflowTests
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
        
        // Get authentication token for protected endpoints
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
    public async System.Threading.Tasks.Task ValidationErrorsWorkflow_ShouldReturnProperErrorResponses()
    {
        // Test Case 1: Empty task title
        var emptyTitleRequest = new CreateTaskRequest { Title = "", Description = "Valid description" };
        var emptyTitleResponse = await CreateTaskAsync(emptyTitleRequest);
        
        Assert.That(emptyTitleResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertValidationError(emptyTitleResponse, "Title");

        // Test Case 2: Null task title
        var nullTitleRequest = new CreateTaskRequest { Title = null!, Description = "Valid description" };
        var nullTitleResponse = await CreateTaskAsync(nullTitleRequest);
        
        Assert.That(nullTitleResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertValidationError(nullTitleResponse, "Title");

        // Test Case 3: Very long title (if validation exists)
        var longTitle = new string('A', 1000);
        var longTitleRequest = new CreateTaskRequest { Title = longTitle, Description = "Valid description" };
        var longTitleResponse = await CreateTaskAsync(longTitleRequest);
        
        // This might pass if no length validation exists, which is also valid
        if (longTitleResponse.StatusCode == HttpStatusCode.BadRequest)
        {
            await AssertValidationError(longTitleResponse, "Title");
        }

        // Test Case 4: Invalid status in update request
        var validTask = await CreateValidTaskAsync();
        var invalidStatusRequest = new UpdateTaskStatusRequest { Status = "InvalidStatus" };
        var invalidStatusResponse = await UpdateTaskStatusAsync(validTask.Id, invalidStatusRequest);
        
        Assert.That(invalidStatusResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        await AssertErrorResponse(invalidStatusResponse, "Invalid status");

        // Cleanup
        await _client.DeleteAsync($"/api/tasks/{validTask.Id}");
    }

    [Test]
    public async System.Threading.Tasks.Task NotFoundErrorsWorkflow_ShouldReturnNotFoundResponses()
    {
        // Test Case 1: Get non-existent task
        var getNonExistentResponse = await _client.GetAsync("/api/tasks/99999");
        Assert.That(getNonExistentResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        await AssertErrorResponse(getNonExistentResponse, "not found");

        // Test Case 2: Update non-existent task
        var updateRequest = new UpdateTaskRequest { Title = "Updated", Description = "Updated" };
        var updateResponse = await UpdateTaskAsync(99999, updateRequest);
        Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        // Test Case 3: Update status of non-existent task
        var statusRequest = new UpdateTaskStatusRequest { Status = "InProgress" };
        var statusResponse = await UpdateTaskStatusAsync(99999, statusRequest);
        Assert.That(statusResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        // Test Case 4: Delete non-existent task
        var deleteResponse = await _client.DeleteAsync("/api/tasks/99999");
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async System.Threading.Tasks.Task MalformedRequestsWorkflow_ShouldHandleInvalidJson()
    {
        // Test Case 1: Invalid JSON syntax
        var invalidJsonContent = new StringContent("{ invalid json }", Encoding.UTF8, "application/json");
        var invalidJsonResponse = await _client.PostAsync("/api/tasks", invalidJsonContent);
        Assert.That(invalidJsonResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        // Test Case 2: Empty request body
        var emptyContent = new StringContent("", Encoding.UTF8, "application/json");
        var emptyResponse = await _client.PostAsync("/api/tasks", emptyContent);
        Assert.That(emptyResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        // Test Case 3: Wrong content type
        var wrongContentType = new StringContent("{\"title\":\"Test\"}", Encoding.UTF8, "text/plain");
        var wrongTypeResponse = await _client.PostAsync("/api/tasks", wrongContentType);
        Assert.That(wrongTypeResponse.StatusCode, Is.EqualTo(HttpStatusCode.UnsupportedMediaType));

        // Test Case 4: Missing required fields
        var missingFieldsContent = new StringContent("{\"description\":\"Only description\"}", Encoding.UTF8, "application/json");
        var missingFieldsResponse = await _client.PostAsync("/api/tasks", missingFieldsContent);
        Assert.That(missingFieldsResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async System.Threading.Tasks.Task AuthenticationErrorsWorkflow_ShouldHandleAuthFailures()
    {
        // Test Case 1: Expired or invalid token
        var invalidClient = _factory.CreateClient();
        invalidClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");
        
        var invalidTokenResponse = await invalidClient.GetAsync("/api/tasks");
        Assert.That(invalidTokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // Test Case 2: Missing authorization header
        var noAuthClient = _factory.CreateClient();
        var noAuthResponse = await noAuthClient.GetAsync("/api/tasks");
        Assert.That(noAuthResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // Test Case 3: Wrong authentication scheme
        var wrongSchemeClient = _factory.CreateClient();
        wrongSchemeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "somevalue");
        var wrongSchemeResponse = await wrongSchemeClient.GetAsync("/api/tasks");
        Assert.That(wrongSchemeResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        // Test Case 4: Invalid login credentials
        var invalidLoginRequest = new LoginRequest { Username = "invalid", Password = "invalid" };
        var invalidLoginResponse = await LoginAsync(invalidLoginRequest, noAuthClient);
        Assert.That(invalidLoginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        invalidClient.Dispose();
        noAuthClient.Dispose();
        wrongSchemeClient.Dispose();
    }

    [Test]
    public async System.Threading.Tasks.Task ConcurrentOperationsWorkflow_ShouldHandleRaceConditions()
    {
        // Create a task for concurrent operations
        var task = await CreateValidTaskAsync();

        // Test Case 1: Concurrent updates to the same task
        var updateTasks = Enumerable.Range(0, 5).Select(async i =>
        {
            var updateRequest = new UpdateTaskRequest 
            { 
                Title = $"Concurrent Update {i}", 
                Description = $"Update from thread {i}" 
            };
            return await UpdateTaskAsync(task.Id, updateRequest);
        });

        var updateResults = await System.Threading.Tasks.Task.WhenAll(updateTasks);
        
        // At least one update should succeed
        Assert.That(updateResults.Any(r => r.StatusCode == HttpStatusCode.OK), Is.True);

        // Test Case 2: Concurrent status updates
        var statusTasks = new[]
        {
            UpdateTaskStatusAsync(task.Id, new UpdateTaskStatusRequest { Status = "InProgress" }),
            UpdateTaskStatusAsync(task.Id, new UpdateTaskStatusRequest { Status = "Completed" }),
            UpdateTaskStatusAsync(task.Id, new UpdateTaskStatusRequest { Status = "ToDo" })
        };

        var statusResults = await System.Threading.Tasks.Task.WhenAll(statusTasks);
        
        // All status updates should either succeed or fail gracefully
        foreach (var result in statusResults)
        {
            Assert.That(result.StatusCode, Is.AnyOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Conflict));
        }

        // Test Case 3: Delete while updating
        var deleteTask = _client.DeleteAsync($"/api/tasks/{task.Id}");
        var finalUpdateTask = UpdateTaskAsync(task.Id, new UpdateTaskRequest { Title = "Final Update", Description = "Final" });

        await System.Threading.Tasks.Task.WhenAll(deleteTask, finalUpdateTask);

        var deleteResult = await deleteTask;
        var updateResult = await finalUpdateTask;

        // Either delete succeeds and update fails, or update succeeds and delete fails, or both succeed
        var deleteSucceeded = deleteResult.StatusCode == HttpStatusCode.NoContent;
        var updateSucceeded = updateResult.StatusCode == HttpStatusCode.OK;

        Assert.That(deleteSucceeded || updateSucceeded, Is.True, "Either delete or update should succeed");
        
        // In race conditions, both operations might succeed depending on timing
        if (deleteSucceeded && !updateSucceeded)
        {
            Assert.That(updateResult.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }

    [Test]
    public async System.Threading.Tasks.Task EdgeCasesWorkflow_ShouldHandleUnusualInputs()
    {
        // Test Case 1: Very long description
        var longDescription = new string('A', 10000);
        var longDescRequest = new CreateTaskRequest { Title = "Long Desc Test", Description = longDescription };
        var longDescResponse = await CreateTaskAsync(longDescRequest);
        
        // Should either succeed or fail with validation error
        Assert.That(longDescResponse.StatusCode, Is.AnyOf(HttpStatusCode.Created, HttpStatusCode.BadRequest));

        // Test Case 2: Special characters in title and description
        var specialCharsRequest = new CreateTaskRequest 
        { 
            Title = "Test with émojis 🚀 and spëcial chars", 
            Description = "Description with\nnewlines\tand\ttabs and unicode: 你好世界" 
        };
        var specialCharsResponse = await CreateTaskAsync(specialCharsRequest);
        Assert.That(specialCharsResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        if (specialCharsResponse.StatusCode == HttpStatusCode.Created)
        {
            var content = await specialCharsResponse.Content.ReadAsStringAsync();
            var createdTask = JsonSerializer.Deserialize<TaskDto>(content, GetJsonOptions());
            
            // Verify special characters are preserved
            Assert.That(createdTask!.Title, Is.EqualTo(specialCharsRequest.Title));
            Assert.That(createdTask.Description, Is.EqualTo(specialCharsRequest.Description));

            // Cleanup
            await _client.DeleteAsync($"/api/tasks/{createdTask.Id}");
        }

        // Test Case 3: Null description (should be allowed)
        var nullDescRequest = new CreateTaskRequest { Title = "Null Desc Test", Description = null };
        var nullDescResponse = await CreateTaskAsync(nullDescRequest);
        Assert.That(nullDescResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        if (nullDescResponse.StatusCode == HttpStatusCode.Created)
        {
            var content = await nullDescResponse.Content.ReadAsStringAsync();
            var createdTask = JsonSerializer.Deserialize<TaskDto>(content, GetJsonOptions());
            
            // Cleanup
            await _client.DeleteAsync($"/api/tasks/{createdTask!.Id}");
        }
    }

    private async Task<TaskDto> CreateValidTaskAsync()
    {
        var request = new CreateTaskRequest { Title = "Test Task", Description = "Test Description" };
        var response = await CreateTaskAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TaskDto>(content, GetJsonOptions())!;
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

    private async Task<HttpResponseMessage> LoginAsync(LoginRequest request, HttpClient? client = null)
    {
        var clientToUse = client ?? _client;
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await clientToUse.PostAsync("/api/auth/login", content);
    }

    private async Task<string> GetValidJwtTokenAsync()
    {
        var loginRequest = new LoginRequest { Username = "admin", Password = "password123" };
        var loginResponse = await LoginAsync(loginRequest);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(loginContent, GetJsonOptions());
        return authResponse?.Token ?? throw new InvalidOperationException("Failed to get valid JWT token");
    }

    private static async System.Threading.Tasks.Task AssertValidationError(HttpResponseMessage response, string fieldName)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        try
        {
            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, GetJsonOptions());
            
            Assert.That(errorResponse, Is.Not.Null);
            Assert.That(errorResponse!.Message, Contains.Substring("Validation").Or.Contains("validation").Or.Contains("required"));
            
            if (errorResponse.Errors != null && errorResponse.Errors.Any())
            {
                Assert.That(errorResponse.Errors.Any(e => e.Field.Contains(fieldName, StringComparison.OrdinalIgnoreCase)), Is.True);
            }
        }
        catch (JsonException)
        {
            // If JSON deserialization fails, just check that the response contains validation-related text
            Assert.That(content, Contains.Substring("validation").Or.Contains("required").Or.Contains(fieldName).IgnoreCase);
        }
    }

    private static async System.Threading.Tasks.Task AssertErrorResponse(HttpResponseMessage response, string expectedMessagePart)
    {
        var content = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, GetJsonOptions());
        
        Assert.That(errorResponse, Is.Not.Null);
        
        // Check if the expected message part is in the main message or in any of the error messages
        var mainMessageContains = errorResponse!.Message.Contains(expectedMessagePart, StringComparison.OrdinalIgnoreCase);
        var errorMessagesContain = errorResponse.Errors?.Any(e => 
            e.Message.Contains(expectedMessagePart, StringComparison.OrdinalIgnoreCase)) ?? false;
        
        Assert.That(mainMessageContains || errorMessagesContain, Is.True, 
            $"Expected message part '{expectedMessagePart}' not found in main message '{errorResponse.Message}' or error messages");
        
        Assert.That(errorResponse.TraceId, Is.Not.Null.And.Not.Empty);
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }
}