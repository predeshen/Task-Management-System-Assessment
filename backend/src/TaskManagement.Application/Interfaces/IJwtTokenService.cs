using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
    int? GetUserIdFromToken(string token);
}