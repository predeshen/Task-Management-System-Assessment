using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Models;

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public User? User { get; set; }
    public string? ErrorMessage { get; set; }

    public static AuthResult Successful(string token, User user)
    {
        return new AuthResult
        {
            Success = true,
            Token = token,
            User = user
        };
    }

    public static AuthResult Failed(string errorMessage)
    {
        return new AuthResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}