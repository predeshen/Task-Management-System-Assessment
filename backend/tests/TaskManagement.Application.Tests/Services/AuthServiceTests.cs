using Moq;
using NUnit.Framework;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private AuthService _authService;
    private Mock<IUserRepository> _mockUserRepository;
    private Mock<IPasswordService> _mockPasswordService;
    private Mock<IJwtTokenService> _mockJwtTokenService;

    [SetUp]
    public void Setup()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordService = new Mock<IPasswordService>();
        _mockJwtTokenService = new Mock<IJwtTokenService>();
        
        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockPasswordService.Object,
            _mockJwtTokenService.Object);
    }

    [Test]
    public async System.Threading.Tasks.Task LoginAsync_WhenValidCredentials_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var username = "testuser";
        var password = "testpassword";
        var hashedPassword = "hashedpassword";
        var token = "jwt-token";
        
        var user = new User
        {
            Id = 1,
            Username = username,
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync(user);
        _mockPasswordService.Setup(x => x.VerifyPassword(password, hashedPassword))
            .Returns(true);
        _mockJwtTokenService.Setup(x => x.GenerateToken(user))
            .Returns(token);

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Token, Is.EqualTo(token));
        Assert.That(result.User, Is.EqualTo(user));
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public async System.Threading.Tasks.Task LoginAsync_WhenUserNotFound_ShouldReturnFailedResult()
    {
        // Arrange
        var username = "nonexistentuser";
        var password = "testpassword";

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Token, Is.Null);
        Assert.That(result.User, Is.Null);
        Assert.That(result.ErrorMessage, Is.EqualTo("Invalid username or password."));
    }

    [Test]
    public async System.Threading.Tasks.Task LoginAsync_WhenInvalidPassword_ShouldReturnFailedResult()
    {
        // Arrange
        var username = "testuser";
        var password = "wrongpassword";
        var hashedPassword = "hashedpassword";
        
        var user = new User
        {
            Id = 1,
            Username = username,
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(username))
            .ReturnsAsync(user);
        _mockPasswordService.Setup(x => x.VerifyPassword(password, hashedPassword))
            .Returns(false);

        // Act
        var result = await _authService.LoginAsync(username, password);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Token, Is.Null);
        Assert.That(result.User, Is.Null);
        Assert.That(result.ErrorMessage, Is.EqualTo("Invalid username or password."));
    }

    [Test]
    public async System.Threading.Tasks.Task LoginAsync_WhenNullUsername_ShouldReturnFailedResult()
    {
        // Arrange & Act
        var result = await _authService.LoginAsync(null, "password");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Username is required."));
    }

    [TestCase("")]
    [TestCase("   ")]
    public async System.Threading.Tasks.Task LoginAsync_WhenInvalidUsername_ShouldReturnFailedResult(string invalidUsername)
    {
        // Arrange & Act
        var result = await _authService.LoginAsync(invalidUsername, "password");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Username is required."));
    }

    [Test]
    public async System.Threading.Tasks.Task LoginAsync_WhenNullPassword_ShouldReturnFailedResult()
    {
        // Arrange & Act
        var result = await _authService.LoginAsync("username", null);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Password is required."));
    }

    [TestCase("")]
    [TestCase("   ")]
    public async System.Threading.Tasks.Task LoginAsync_WhenInvalidPassword_ShouldReturnFailedResult(string invalidPassword)
    {
        // Arrange & Act
        var result = await _authService.LoginAsync("username", invalidPassword);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Password is required."));
    }

    [Test]
    public async System.Threading.Tasks.Task RegisterAsync_WhenValidCredentials_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var username = "newuser";
        var password = "newpassword";
        var hashedPassword = "hashedpassword";
        var token = "jwt-token";
        
        var createdUser = new User
        {
            Id = 1,
            Username = username,
            PasswordHash = hashedPassword,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(x => x.ExistsAsync(username))
            .ReturnsAsync(false);
        _mockPasswordService.Setup(x => x.HashPassword(password))
            .Returns(hashedPassword);
        _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);
        _mockJwtTokenService.Setup(x => x.GenerateToken(createdUser))
            .Returns(token);

        // Act
        var result = await _authService.RegisterAsync(username, password);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Token, Is.EqualTo(token));
        Assert.That(result.User, Is.EqualTo(createdUser));
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public async System.Threading.Tasks.Task RegisterAsync_WhenUsernameExists_ShouldReturnFailedResult()
    {
        // Arrange
        var username = "existinguser";
        var password = "password";

        _mockUserRepository.Setup(x => x.ExistsAsync(username))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync(username, password);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Token, Is.Null);
        Assert.That(result.User, Is.Null);
        Assert.That(result.ErrorMessage, Is.EqualTo("Username already exists."));
    }

    [TestCase("12345")]
    [TestCase("abc")]
    public async System.Threading.Tasks.Task RegisterAsync_WhenPasswordTooShort_ShouldReturnFailedResult(string shortPassword)
    {
        // Arrange
        var username = "newuser";

        _mockUserRepository.Setup(x => x.ExistsAsync(username))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.RegisterAsync(username, shortPassword);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Password must be at least 6 characters long."));
    }

    [Test]
    public async System.Threading.Tasks.Task RegisterAsync_WhenNullUsername_ShouldReturnFailedResult()
    {
        // Arrange & Act
        var result = await _authService.RegisterAsync(null, "password123");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Username is required."));
    }

    [TestCase("")]
    [TestCase("   ")]
    public async System.Threading.Tasks.Task RegisterAsync_WhenInvalidUsername_ShouldReturnFailedResult(string invalidUsername)
    {
        // Arrange & Act
        var result = await _authService.RegisterAsync(invalidUsername, "password123");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Username is required."));
    }

    [Test]
    public async System.Threading.Tasks.Task RegisterAsync_WhenNullPassword_ShouldReturnFailedResult()
    {
        // Arrange & Act
        var result = await _authService.RegisterAsync("username", null);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Password is required."));
    }

    [Test]
    public async System.Threading.Tasks.Task RegisterAsync_WhenEmptyPassword_ShouldReturnFailedResult()
    {
        // Arrange & Act
        var result = await _authService.RegisterAsync("username", "");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Password is required."));
    }

    [Test]
    public void Constructor_WhenNullUserRepository_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AuthService(
            null,
            _mockPasswordService.Object,
            _mockJwtTokenService.Object));
    }

    [Test]
    public void Constructor_WhenNullPasswordService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AuthService(
            _mockUserRepository.Object,
            null,
            _mockJwtTokenService.Object));
    }

    [Test]
    public void Constructor_WhenNullJwtTokenService_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AuthService(
            _mockUserRepository.Object,
            _mockPasswordService.Object,
            null));
    }
}