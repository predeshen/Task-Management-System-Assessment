using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Models;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    }

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return AuthResult.Failed("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Failed("Password is required.");
        }

        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
        {
            return AuthResult.Failed("Invalid username or password.");
        }

        if (!_passwordService.VerifyPassword(password, user.PasswordHash))
        {
            return AuthResult.Failed("Invalid username or password.");
        }

        var token = _jwtTokenService.GenerateToken(user);
        return AuthResult.Successful(token, user);
    }

    public async Task<AuthResult> RegisterAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return AuthResult.Failed("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Failed("Password is required.");
        }

        if (password.Length < 6)
        {
            return AuthResult.Failed("Password must be at least 6 characters long.");
        }

        if (await _userRepository.ExistsAsync(username))
        {
            return AuthResult.Failed("Username already exists.");
        }

        var hashedPassword = _passwordService.HashPassword(password);
        var user = new User
        {
            Username = username,
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.CreateAsync(user);
        var token = _jwtTokenService.GenerateToken(createdUser);
        
        return AuthResult.Successful(token, createdUser);
    }
}