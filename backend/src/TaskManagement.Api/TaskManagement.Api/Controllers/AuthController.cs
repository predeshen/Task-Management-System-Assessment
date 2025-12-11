using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaskManagement.Api.Models;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Models;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, IOptions<JwtSettings> jwtSettings)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
    }

    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user information if successful</returns>
    /// <response code="200">Login successful - returns JWT token and user info</response>
    /// <response code="400">Invalid request - validation errors</response>
    /// <response code="401">Unauthorized - invalid credentials</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new ValidationError
                    {
                        Field = x.Key,
                        Message = x.Value!.Errors.First().ErrorMessage
                    })
                    .ToList();

                var errorResponse = new ErrorResponse
                {
                    Message = "Validation failed",
                    Errors = errors,
                    TraceId = HttpContext.TraceIdentifier
                };

                return BadRequest(errorResponse);
            }

            var result = await _authService.LoginAsync(request.Username, request.Password);

            if (!result.Success)
            {
                var errorResponse = new ErrorResponse
                {
                    Message = result.ErrorMessage ?? "Login failed",
                    TraceId = HttpContext.TraceIdentifier
                };

                return Unauthorized(errorResponse);
            }

            var response = new AuthResponse
            {
                Token = result.Token!,
                User = new UserDto
                {
                    Id = result.User!.Id,
                    Username = result.User.Username
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes)
            };

            _logger.LogInformation("User {Username} logged in successfully", request.Username);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for user {Username}", request.Username);
            
            var errorResponse = new ErrorResponse
            {
                Message = "An error occurred during login",
                TraceId = HttpContext.TraceIdentifier
            };

            return StatusCode(500, errorResponse);
        }
    }
}