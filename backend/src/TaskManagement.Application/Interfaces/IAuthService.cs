using TaskManagement.Application.Models;

namespace TaskManagement.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task<AuthResult> RegisterAsync(string username, string password);
}