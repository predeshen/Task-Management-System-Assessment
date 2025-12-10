using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using System.Security.Claims;
using TaskManagement.Api.Controllers;

namespace TaskManagement.Api.Tests.Controllers;

[TestFixture]
public class TestControllerTests
{
    private TestController _controller;

    [SetUp]
    public void Setup()
    {
        _controller = new TestController();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Test]
    public void GetPublicData_WhenCalled_ShouldReturnOkWithPublicMessage()
    {
        // Arrange & Act
        var result = _controller.GetPublicData();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        
        var response = okResult.Value;
        Assert.That(response, Is.Not.Null);
        
        // Use reflection to check the anonymous object properties
        var messageProperty = response!.GetType().GetProperty("Message");
        var timestampProperty = response.GetType().GetProperty("Timestamp");
        
        Assert.That(messageProperty, Is.Not.Null);
        Assert.That(timestampProperty, Is.Not.Null);
        
        var message = messageProperty!.GetValue(response)?.ToString();
        Assert.That(message, Is.EqualTo("This is a public endpoint - no authentication required"));
    }

    [Test]
    public void GetProtectedData_WhenAuthenticatedUser_ShouldReturnOkWithUserInfo()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("userId", "123"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim("role", "user")
        };
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = _controller.GetProtectedData();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        
        var response = okResult.Value;
        Assert.That(response, Is.Not.Null);
        
        // Use reflection to check the anonymous object properties
        var messageProperty = response!.GetType().GetProperty("Message");
        var userIdProperty = response.GetType().GetProperty("UserId");
        var usernameProperty = response.GetType().GetProperty("Username");
        var claimsProperty = response.GetType().GetProperty("Claims");
        
        Assert.That(messageProperty, Is.Not.Null);
        Assert.That(userIdProperty, Is.Not.Null);
        Assert.That(usernameProperty, Is.Not.Null);
        Assert.That(claimsProperty, Is.Not.Null);
        
        var message = messageProperty!.GetValue(response)?.ToString();
        var userId = userIdProperty!.GetValue(response)?.ToString();
        var username = usernameProperty!.GetValue(response)?.ToString();
        
        Assert.That(message, Is.EqualTo("This is a protected endpoint"));
        Assert.That(userId, Is.EqualTo("123"));
        Assert.That(username, Is.EqualTo("testuser"));
    }

    [Test]
    public void GetProtectedData_WhenNoUserClaims_ShouldReturnOkWithNullValues()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // Empty identity
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = _controller.GetProtectedData();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        
        var response = okResult.Value;
        Assert.That(response, Is.Not.Null);
        
        // Use reflection to check the anonymous object properties
        var userIdProperty = response!.GetType().GetProperty("UserId");
        var usernameProperty = response.GetType().GetProperty("Username");
        
        var userId = userIdProperty!.GetValue(response);
        var username = usernameProperty!.GetValue(response);
        
        Assert.That(userId, Is.Null);
        Assert.That(username, Is.Null);
    }

    [Test]
    public void GetProtectedData_WhenPartialClaims_ShouldReturnAvailableClaimsOnly()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "partialuser")
            // Missing userId claim
        };
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = _controller.GetProtectedData();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        
        var response = okResult.Value;
        Assert.That(response, Is.Not.Null);
        
        var userIdProperty = response!.GetType().GetProperty("UserId");
        var usernameProperty = response.GetType().GetProperty("Username");
        
        var userId = userIdProperty!.GetValue(response);
        var username = usernameProperty!.GetValue(response)?.ToString();
        
        Assert.That(userId, Is.Null); // Missing userId claim
        Assert.That(username, Is.EqualTo("partialuser")); // Present username claim
    }

    [Test]
    public void GetProtectedData_WhenMultipleClaims_ShouldReturnAllClaims()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("userId", "456"),
            new Claim(ClaimTypes.Name, "multiuser"),
            new Claim("role", "admin"),
            new Claim("department", "IT")
        };
        
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = _controller.GetProtectedData();

        // Assert
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result;
        
        var response = okResult.Value;
        var claimsProperty = response!.GetType().GetProperty("Claims");
        var claimsValue = claimsProperty!.GetValue(response);
        
        Assert.That(claimsValue, Is.Not.Null);
        
        // The claims should be a collection
        var claimsList = claimsValue as System.Collections.IEnumerable;
        Assert.That(claimsList, Is.Not.Null);
        
        var claimsCount = 0;
        foreach (var claim in claimsList!)
        {
            claimsCount++;
        }
        
        Assert.That(claimsCount, Is.EqualTo(4)); // Should have all 4 claims
    }
}