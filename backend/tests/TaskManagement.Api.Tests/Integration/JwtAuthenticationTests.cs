using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TaskManagement.Api.Models;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Models;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Api.Tests.Integration;

[TestFixture]
public class JwtAuthenticationTests
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
    public async System.Threading.Tasks.Task GetPublicEndpoint_WhenNoAuthentication_ShouldReturnOk()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/test/public");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Contains.Substring("This is a public endpoint"));
    }

    [Test]
    public async System.Threading.Tasks.Task GetProtectedEndpoint_WhenNoAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/api/test/protected");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async System.Threading.Tasks.Task GetProtectedEndpoint_WhenInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/test/protected");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async System.Threading.Tasks.Task GetProtectedEndpoint_WhenMalformedToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.jwt.token");

        // Act
        var response = await _client.GetAsync("/api/test/protected");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async System.Threading.Tasks.Task GetProtectedEndpoint_WhenValidToken_ShouldReturnOk()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/test/protected");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Contains.Substring("This is a protected endpoint"));
        Assert.That(content, Contains.Substring("admin")); // Should contain username from token
    }

    [Test]
    public async System.Threading.Tasks.Task AuthenticationFlow_WhenLoginThenAccessProtected_ShouldWork()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "password123"
        };

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

        // Act - Login
        var loginResponse = await _client.PostAsync("/api/auth/login", loginContent);

        // Assert - Login successful
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(loginResponseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(authResponse, Is.Not.Null);
        Assert.That(authResponse!.Token, Is.Not.Null.And.Not.Empty);

        // Act - Access protected endpoint with token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.Token);
        var protectedResponse = await _client.GetAsync("/api/test/protected");

        // Assert - Protected endpoint accessible
        Assert.That(protectedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var protectedContent = await protectedResponse.Content.ReadAsStringAsync();
        Assert.That(protectedContent, Contains.Substring("This is a protected endpoint"));
        Assert.That(protectedContent, Contains.Substring("admin"));
    }

    [Test]
    public async System.Threading.Tasks.Task MultipleRequests_WhenSameToken_ShouldAllSucceed()
    {
        // Arrange
        var token = await GetValidJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Make multiple requests with same token
        var response1 = await _client.GetAsync("/api/test/protected");
        var response2 = await _client.GetAsync("/api/test/protected");
        var response3 = await _client.GetAsync("/api/test/protected");

        // Assert - All requests should succeed
        Assert.That(response1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response3.StatusCode, Is.EqualTo(HttpStatusCode.OK));
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
        
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(loginResponseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return authResponse?.Token ?? throw new InvalidOperationException("Failed to get valid JWT token");
    }
}