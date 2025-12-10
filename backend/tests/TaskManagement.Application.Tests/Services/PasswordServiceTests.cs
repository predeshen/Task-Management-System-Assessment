using NUnit.Framework;
using TaskManagement.Application.Services;

namespace TaskManagement.Application.Tests.Services;

[TestFixture]
public class PasswordServiceTests
{
    private PasswordService _passwordService;

    [SetUp]
    public void Setup()
    {
        _passwordService = new PasswordService();
    }

    [Test]
    public void HashPassword_WhenValidPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hashedPassword = _passwordService.HashPassword(password);

        // Assert
        Assert.That(hashedPassword, Is.Not.Null);
        Assert.That(hashedPassword, Is.Not.EqualTo(password));
        Assert.That(hashedPassword.Length, Is.GreaterThan(50));
        Assert.That(hashedPassword, Does.StartWith("$2a$"));
    }

    [Test]
    public void HashPassword_WhenSamePasswordHashedTwice_ShouldReturnDifferentHashes()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert
        Assert.That(hash1, Is.Not.EqualTo(hash2));
        Assert.That(hash1, Is.Not.Null);
        Assert.That(hash2, Is.Not.Null);
    }

    [Test]
    public void HashPassword_WhenNullPassword_ShouldThrowArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(null));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void HashPassword_WhenInvalidPassword_ShouldThrowArgumentException(string invalidPassword)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(invalidPassword));
    }

    [Test]
    public void VerifyPassword_WhenCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void VerifyPassword_WhenIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var correctPassword = "TestPassword123!";
        var incorrectPassword = "WrongPassword456!";
        var hashedPassword = _passwordService.HashPassword(correctPassword);

        // Act
        var result = _passwordService.VerifyPassword(incorrectPassword, hashedPassword);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void VerifyPassword_WhenNullPassword_ShouldReturnFalse()
    {
        // Arrange & Act
        var result = _passwordService.VerifyPassword(null, "validhash");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void VerifyPassword_WhenNullHash_ShouldReturnFalse()
    {
        // Arrange & Act
        var result = _passwordService.VerifyPassword("validpassword", null);

        // Assert
        Assert.That(result, Is.False);
    }

    [TestCase("", "validhash")]
    [TestCase("   ", "validhash")]
    [TestCase("validpassword", "")]
    [TestCase("validpassword", "   ")]
    public void VerifyPassword_WhenInvalidInputs_ShouldReturnFalse(string password, string hashedPassword)
    {
        // Arrange & Act
        var result = _passwordService.VerifyPassword(password, hashedPassword);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void VerifyPassword_WhenMalformedHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "TestPassword123!";
        var malformedHash = "this-is-not-a-valid-bcrypt-hash";

        // Act
        var result = _passwordService.VerifyPassword(password, malformedHash);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HashPassword_WhenComplexPassword_ShouldHashSuccessfully()
    {
        // Arrange
        var complexPassword = "P@ssw0rd!2024#$%^&*()_+-=[]{}|;':\",./<>?`~";

        // Act
        var hashedPassword = _passwordService.HashPassword(complexPassword);
        var verificationResult = _passwordService.VerifyPassword(complexPassword, hashedPassword);

        // Assert
        Assert.That(hashedPassword, Is.Not.Null);
        Assert.That(verificationResult, Is.True);
    }

    [Test]
    public void HashPassword_WhenLongPassword_ShouldHashSuccessfully()
    {
        // Arrange
        var longPassword = new string('a', 1000);

        // Act
        var hashedPassword = _passwordService.HashPassword(longPassword);
        var verificationResult = _passwordService.VerifyPassword(longPassword, hashedPassword);

        // Assert
        Assert.That(hashedPassword, Is.Not.Null);
        Assert.That(verificationResult, Is.True);
    }

    [Test]
    public void HashPassword_WhenUnicodePassword_ShouldHashSuccessfully()
    {
        // Arrange
        var unicodePassword = "пароль密码パスワード🔒";

        // Act
        var hashedPassword = _passwordService.HashPassword(unicodePassword);
        var verificationResult = _passwordService.VerifyPassword(unicodePassword, hashedPassword);

        // Assert
        Assert.That(hashedPassword, Is.Not.Null);
        Assert.That(verificationResult, Is.True);
    }
}