using NUnit.Framework;
using TaskManagement.Application.Models;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Tests.Services;

[TestFixture]
public class JwtTokenServiceTests
{
    private JwtTokenService _jwtTokenService;
    private JwtSettings _jwtSettings;

    [SetUp]
    public void Setup()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "TaskManagement-SuperSecretKey-ForTesting-DoNotUseInProduction-2024-MinimumLength32Characters",
            Issuer = "TaskManagement.Api.Test",
            Audience = "TaskManagement.Client.Test",
            ExpirationMinutes = 60
        };
        _jwtTokenService = new JwtTokenService(_jwtSettings);
    }

    [Test]
    public void GenerateToken_WhenValidUser_ShouldReturnValidJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var token = _jwtTokenService.GenerateToken(user);

        // Assert
        Assert.That(token, Is.Not.Null);
        Assert.That(token, Is.Not.Empty);
        Assert.That(token.Split('.').Length, Is.EqualTo(3)); // JWT has 3 parts separated by dots
    }

    [Test]
    public void GenerateToken_WhenNullUser_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => _jwtTokenService.GenerateToken(null));
    }

    [Test]
    public void GenerateToken_WhenSameUserCalledTwice_ShouldReturnDifferentTokens()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var token1 = _jwtTokenService.GenerateToken(user);
        System.Threading.Thread.Sleep(1000); // Ensure different timestamps
        var token2 = _jwtTokenService.GenerateToken(user);

        // Assert
        Assert.That(token1, Is.Not.EqualTo(token2));
    }

    [Test]
    public void ValidateToken_WhenValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow
        };
        var token = _jwtTokenService.GenerateToken(user);

        // Act
        var isValid = _jwtTokenService.ValidateToken(token);

        // Assert
        Assert.That(isValid, Is.True);
    }

    [Test]
    public void ValidateToken_WhenNullToken_ShouldReturnFalse()
    {
        // Arrange & Act
        var isValid = _jwtTokenService.ValidateToken(null);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ValidateToken_WhenInvalidToken_ShouldReturnFalse(string invalidToken)
    {
        // Arrange & Act
        var isValid = _jwtTokenService.ValidateToken(invalidToken);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void ValidateToken_WhenMalformedToken_ShouldReturnFalse()
    {
        // Arrange
        var malformedToken = "this.is.not.a.valid.jwt.token";

        // Act
        var isValid = _jwtTokenService.ValidateToken(malformedToken);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void ValidateToken_WhenTokenWithWrongSignature_ShouldReturnFalse()
    {
        // Arrange
        var wrongSettings = new JwtSettings
        {
            SecretKey = "DifferentSecretKey-ForTesting-WrongSignature-2024-MinimumLength32Characters",
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            ExpirationMinutes = _jwtSettings.ExpirationMinutes
        };
        var wrongJwtService = new JwtTokenService(wrongSettings);
        
        var user = new User { Id = 1, Username = "testuser", PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
        var tokenWithWrongSignature = wrongJwtService.GenerateToken(user);

        // Act
        var isValid = _jwtTokenService.ValidateToken(tokenWithWrongSignature);

        // Assert
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void GetUserIdFromToken_WhenValidToken_ShouldReturnCorrectUserId()
    {
        // Arrange
        var expectedUserId = 123;
        var user = new User
        {
            Id = expectedUserId,
            Username = "testuser",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow
        };
        var token = _jwtTokenService.GenerateToken(user);

        // Act
        var userId = _jwtTokenService.GetUserIdFromToken(token);

        // Assert
        Assert.That(userId, Is.EqualTo(expectedUserId));
    }

    [Test]
    public void GetUserIdFromToken_WhenNullToken_ShouldReturnNull()
    {
        // Arrange & Act
        var userId = _jwtTokenService.GetUserIdFromToken(null);

        // Assert
        Assert.That(userId, Is.Null);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void GetUserIdFromToken_WhenInvalidToken_ShouldReturnNull(string invalidToken)
    {
        // Arrange & Act
        var userId = _jwtTokenService.GetUserIdFromToken(invalidToken);

        // Assert
        Assert.That(userId, Is.Null);
    }

    [Test]
    public void GetUserIdFromToken_WhenMalformedToken_ShouldReturnNull()
    {
        // Arrange
        var malformedToken = "this.is.not.a.valid.jwt.token";

        // Act
        var userId = _jwtTokenService.GetUserIdFromToken(malformedToken);

        // Assert
        Assert.That(userId, Is.Null);
    }

    [Test]
    public void Constructor_WhenNullJwtSettings_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new JwtTokenService(null));
    }

    [Test]
    public void ValidateToken_WhenTokenFromDifferentIssuer_ShouldReturnFalse()
    {
        // Arrange
        var differentIssuerSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.SecretKey,
            Issuer = "DifferentIssuer",
            Audience = _jwtSettings.Audience,
            ExpirationMinutes = _jwtSettings.ExpirationMinutes
        };
        var differentIssuerService = new JwtTokenService(differentIssuerSettings);
        
        var user = new User { Id = 1, Username = "testuser", PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
        var tokenFromDifferentIssuer = differentIssuerService.GenerateToken(user);

        // Act
        var isValid = _jwtTokenService.ValidateToken(tokenFromDifferentIssuer);

        // Assert
        Assert.That(isValid, Is.False);
    }
}