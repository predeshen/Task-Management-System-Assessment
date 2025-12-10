using NUnit.Framework;
using TaskManagement.Domain.Entities;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Tests.Domain;

[TestFixture]
public class TaskStatusValidationTests
{
    [Test]
    public void TaskStatus_WhenSetToToDo_ShouldBeValidEnumValue()
    {
        // Arrange
        var expectedStatus = DomainTaskStatus.ToDo;

        // Act
        var task = new TaskManagement.Domain.Entities.Task
        {
            Id = 1,
            Title = "Test Task",
            Description = "Test Description",
            Status = expectedStatus,
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.That(task.Status, Is.EqualTo(expectedStatus));
        Assert.That(Enum.IsDefined(typeof(DomainTaskStatus), task.Status), Is.True);
    }

    [Test]
    public void TaskStatus_WhenSetToInProgress_ShouldBeValidEnumValue()
    {
        // Arrange
        var expectedStatus = DomainTaskStatus.InProgress;

        // Act
        var task = new TaskManagement.Domain.Entities.Task
        {
            Id = 1,
            Title = "Test Task",
            Description = "Test Description",
            Status = expectedStatus,
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.That(task.Status, Is.EqualTo(expectedStatus));
        Assert.That(Enum.IsDefined(typeof(DomainTaskStatus), task.Status), Is.True);
    }

    [Test]
    public void TaskStatus_WhenSetToCompleted_ShouldBeValidEnumValue()
    {
        // Arrange
        var expectedStatus = DomainTaskStatus.Completed;

        // Act
        var task = new TaskManagement.Domain.Entities.Task
        {
            Id = 1,
            Title = "Test Task",
            Description = "Test Description",
            Status = expectedStatus,
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.That(task.Status, Is.EqualTo(expectedStatus));
        Assert.That(Enum.IsDefined(typeof(DomainTaskStatus), task.Status), Is.True);
    }

    [TestCase(DomainTaskStatus.ToDo)]
    [TestCase(DomainTaskStatus.InProgress)]
    [TestCase(DomainTaskStatus.Completed)]
    public void TaskStatus_WhenConvertedToStringAndBack_ShouldPreserveValue(DomainTaskStatus originalStatus)
    {
        // Arrange
        var statusAsString = originalStatus.ToString();

        // Act
        var parsedStatus = Enum.Parse<DomainTaskStatus>(statusAsString);

        // Assert
        Assert.That(parsedStatus, Is.EqualTo(originalStatus));
    }

    [Test]
    public void TaskStatus_WhenAllValidStatusesChecked_ShouldContainOnlyThreeValues()
    {
        // Arrange
        var expectedStatuses = new[] { DomainTaskStatus.ToDo, DomainTaskStatus.InProgress, DomainTaskStatus.Completed };

        // Act
        var allStatuses = Enum.GetValues<DomainTaskStatus>();

        // Assert
        Assert.That(allStatuses.Length, Is.EqualTo(expectedStatuses.Length));
        Assert.That(allStatuses, Is.SupersetOf(expectedStatuses));
    }
}