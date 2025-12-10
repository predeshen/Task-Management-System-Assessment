using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace TaskManagement.Api.Models;

public class LoginRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    /// <example>admin</example>
    [Required(ErrorMessage = "Username is required")]
    [DefaultValue("admin")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    /// <example>password123</example>
    [Required(ErrorMessage = "Password is required")]
    [DefaultValue("password123")]
    public string Password { get; set; } = string.Empty;
}