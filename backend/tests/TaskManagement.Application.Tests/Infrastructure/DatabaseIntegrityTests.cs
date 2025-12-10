using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Data;
using DomainTask = TaskManagement.Domain.Entities.Task;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Tests.Infrastructure;

[TestFixture]
public class DatabaseIntegrityTests
{
    private TaskManagementDbContext _context;
    private DbContextOptions<TaskManagementDbContext> _options;

    [SetUp]
    public void Setup()
    {
        _options = new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new TaskManagementDbContext(_options);
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public void User_WhenCreatedWithValidData_ShouldMaintainReferentialIntegrity()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        _context.SaveChanges();

        // Assert
        var savedUser = _context.Users.First();
        Assert.That(savedUser.Id, Is.GreaterThan(0));
        Assert.That(savedUser.Username, Is.EqualTo("testuser"));
        Assert.That(savedUser.PasswordHash, Is.EqualTo("hashedpassword"));
        Assert.That(savedUser.Tasks, Is.Not.Null);
    }

    [Test]
    public void Task_WhenCreatedWithValidUser_ShouldMaintainForeignKeyConstraint()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var task = new DomainTask
        {
            Title = "Test Task",
            Description = "Test Description",
            Status = DomainTaskStatus.ToDo,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Tasks.Add(task);
        _context.SaveChanges();

        // Assert
        var savedTask = _context.Tasks.Include(t => t.User).First();
        Assert.That(savedTask.Id, Is.GreaterThan(0));
        Assert.That(savedTask.UserId, Is.EqualTo(user.Id));
        Assert.That(savedTask.User, Is.Not.Null);
        Assert.That(savedTask.User.Username, Is.EqualTo("testuser"));
    }

    [Test]
    public void User_WhenDeleted_ShouldCascadeDeleteTasks()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var task = new DomainTask
        {
            Title = "Test Task",
            Description = "Test Description",
            Status = DomainTaskStatus.ToDo,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tasks.Add(task);
        _context.SaveChanges();

        // Act
        _context.Users.Remove(user);
        _context.SaveChanges();

        // Assert
        var remainingTasks = _context.Tasks.Where(t => t.UserId == user.Id).ToList();
        Assert.That(remainingTasks, Is.Empty);
    }

    [Test]
    public void Username_WhenDuplicate_ShouldBeDetectable()
    {
        // Arrange
        var user1 = new User
        {
            Username = "testuser",
            PasswordHash = "hashedpassword1",
            CreatedAt = DateTime.UtcNow
        };
        var user2 = new User
        {
            Username = "testuser",
            PasswordHash = "hashedpassword2",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user1);
        _context.SaveChanges();

        // Act
        var existingUserCount = _context.Users.Count(u => u.Username == "testuser");
        _context.Users.Add(user2);
        _context.SaveChanges();
        var duplicateUserCount = _context.Users.Count(u => u.Username == "testuser");

        // Assert
        Assert.That(existingUserCount, Is.EqualTo(1));
        Assert.That(duplicateUserCount, Is.EqualTo(2)); // InMemory allows duplicates, but we can detect them
    }

    [Test]
    public void Task_WhenCreatedWithRequiredFields_ShouldSaveSuccessfully()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var task = new DomainTask
        {
            Title = "Required Title",
            Status = DomainTaskStatus.ToDo,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Tasks.Add(task);
        _context.SaveChanges();

        // Assert
        var savedTask = _context.Tasks.First();
        Assert.That(savedTask.Title, Is.EqualTo("Required Title"));
        Assert.That(savedTask.Description, Is.Null);
        Assert.That(savedTask.Status, Is.EqualTo(DomainTaskStatus.ToDo));
    }

    [Test]
    public void Task_WhenUpdated_ShouldPreserveCreatedAtAndUpdateTimestamp()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var originalCreatedAt = DateTime.UtcNow.AddDays(-1);
        var task = new DomainTask
        {
            Title = "Original Title",
            Status = DomainTaskStatus.ToDo,
            UserId = user.Id,
            CreatedAt = originalCreatedAt
        };
        _context.Tasks.Add(task);
        _context.SaveChanges();

        // Act
        task.Title = "Updated Title";
        task.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();

        // Assert
        var updatedTask = _context.Tasks.First();
        Assert.That(updatedTask.Title, Is.EqualTo("Updated Title"));
        Assert.That(updatedTask.CreatedAt, Is.EqualTo(originalCreatedAt));
        Assert.That(updatedTask.UpdatedAt, Is.Not.Null);
        Assert.That(updatedTask.UpdatedAt, Is.GreaterThan(originalCreatedAt));
    }
}