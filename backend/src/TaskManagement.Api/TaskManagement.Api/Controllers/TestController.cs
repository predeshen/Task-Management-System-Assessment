using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Test endpoint to verify JWT authentication is working
    /// </summary>
    /// <returns>User information from JWT token</returns>
    /// <response code="200">Authentication successful - returns user info from token</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    [HttpGet("protected")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult GetProtectedData()
    {
        var userId = User.FindFirst("userId")?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        
        return Ok(new
        {
            Message = "This is a protected endpoint",
            UserId = userId,
            Username = username,
            Claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }

    /// <summary>
    /// Public test endpoint that doesn't require authentication
    /// </summary>
    /// <returns>Public message</returns>
    [HttpGet("public")]
    [ProducesResponseType(200)]
    public IActionResult GetPublicData()
    {
        return Ok(new
        {
            Message = "This is a public endpoint - no authentication required",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test endpoint that throws different types of exceptions for testing global exception handling
    /// </summary>
    /// <param name="exceptionType">Type of exception to throw (argument, unauthorized, notfound, invalid, server)</param>
    /// <returns>Never returns - always throws an exception</returns>
    [HttpGet("exception/{exceptionType}")]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public IActionResult ThrowException(string exceptionType)
    {
        return exceptionType.ToLower() switch
        {
            "argument" => throw new ArgumentException("This is a test argument exception"),
            "unauthorized" => throw new UnauthorizedAccessException("This is a test unauthorized exception"),
            "notfound" => throw new KeyNotFoundException("This is a test not found exception"),
            "invalid" => throw new InvalidOperationException("This is a test invalid operation exception"),
            "server" => throw new Exception("This is a test server exception"),
            _ => throw new ArgumentException("Invalid exception type. Use: argument, unauthorized, notfound, invalid, or server")
        };
    }
}