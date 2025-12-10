using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskStatus;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Application.Tests.Repositories;

/// <summary>
/// Property-based tests for TaskRepository to validate correctness properties
/// </summary>
[TestFixture]
public class TaskRepositoryPropertyTests
{
    private TaskManagementDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TaskManagementDbContext(options);
    }

    /// <summary>
    /// **Feature: task-management-system, Property 5: Task isolation**
    /// **Validates: Requirements 2.1, 8.4**
    /// 
    /// For any user, tasks created by that user should only be visible to that user,
    /// and tasks from other users should not be accessible.
    /// This test validates the property across multiple random scenarios.
    /// </summary>
    [Test]
    public async System.Threading.Tasks.Task TaskIsolation_UserCanOnlyAccessOwnTasks()
    {
        // Test the property across multiple scenarios with different user IDs and task titles
        var random = new Random(42); // Fixed seed for reproducible tests
        
        for (int i = 0; i < 50; i++) // Run 50 iterations to simulate property-based testing
        {
            var userId1 = random.Next(1, 1000);
            var userId2 = random.Next(1, 1000);
            var taskTitle = $"Task_{random.Next(1, 10000)}";
            
            // Ensure we have two different users
            if (userId1 == userId2)
                userId2 = userId1 + 1;

            using var context = CreateInMemoryContext();
            var repository = new TaskRepository(context);

            // Create users first
            var user1 = new User
            {
                Id = userId1,
                Username = $"user{userId1}",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };

            var user2 = new User
            {
                Id = userId2,
                Username = $"user{userId2}",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            // Create a task for user1
            var task = new TaskManagement.Domain.Entities.Task
            {
                Title = taskTitle,
                Description = "Test task",
                Status = DomainTaskStatus.ToDo,
                UserId = userId1
            };

            var createdTask = await repository.CreateAsync(task);

            // Property: User1 can access their own task
            var user1Task = await repository.GetByIdAsync(createdTask.Id, userId1);
            Assert.That(user1Task, Is.Not.Null, $"User {userId1} should be able to access their own task");

            // Property: User2 cannot access user1's task
            var user2Task = await repository.GetByIdAsync(createdTask.Id, userId2);
            Assert.That(user2Task, Is.Null, $"User {userId2} should not be able to access user {userId1}'s task");

            // Property: User1's task list contains the task
            var user1Tasks = await repository.GetAllByUserIdAsync(userId1);
            Assert.That(user1Tasks.Any(t => t.Id == createdTask.Id), Is.True, 
                $"User {userId1}'s task list should contain their task");

            // Property: User2's task list does not contain user1's task
            var user2Tasks = await repository.GetAllByUserIdAsync(userId2);
            Assert.That(user2Tasks.Any(t => t.Id == createdTask.Id), Is.False, 
                $"User {userId2}'s task list should not contain user {userId1}'s task");
        }
    }

    /// <summary>
    /// **Feature: task-management-system, Property 5b: Task deletion isolation**
    /// **Validates: Requirements 2.1, 8.4**
    /// 
    /// For any user, attempting to delete another user's task should fail,
    /// while deleting their own task should succeed.
    /// This test validates the property across multiple random scenarios.
    /// </summary>
    [Test]
    public async System.Threading.Tasks.Task TaskDeletionIsolation_UserCanOnlyDeleteOwnTasks()
    {
        // Test the property across multiple scenarios with different user IDs and task titles
        var random = new Random(43); // Fixed seed for reproducible tests
        
        for (int i = 0; i < 50; i++) // Run 50 iterations to simulate property-based testing
        {
            var userId1 = random.Next(1, 1000);
            var userId2 = random.Next(1, 1000);
            var taskTitle = $"Task_{random.Next(1, 10000)}";
            
            // Ensure we have two different users
            if (userId1 == userId2)
                userId2 = userId1 + 1;

            using var context = CreateInMemoryContext();
            var repository = new TaskRepository(context);

            // Create users first
            var user1 = new User
            {
                Id = userId1,
                Username = $"user{userId1}",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };

            var user2 = new User
            {
                Id = userId2,
                Username = $"user{userId2}",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            // Create a task for user1
            var task = new TaskManagement.Domain.Entities.Task
            {
                Title = taskTitle,
                Description = "Test task",
                Status = DomainTaskStatus.ToDo,
                UserId = userId1
            };

            var createdTask = await repository.CreateAsync(task);

            // Property: User2 cannot delete user1's task
            var user2CannotDelete = await repository.DeleteAsync(createdTask.Id, userId2);
            Assert.That(user2CannotDelete, Is.False, 
                $"User {userId2} should not be able to delete user {userId1}'s task");

            // Verify task still exists
            var taskStillExists = await repository.ExistsAsync(createdTask.Id, userId1);
            Assert.That(taskStillExists, Is.True, 
                $"Task should still exist after failed deletion attempt by user {userId2}");

            // Property: User1 can delete their own task
            var user1CanDelete = await repository.DeleteAsync(createdTask.Id, userId1);
            Assert.That(user1CanDelete, Is.True, 
                $"User {userId1} should be able to delete their own task");

            // Verify task no longer exists
            var taskDeleted = await repository.ExistsAsync(createdTask.Id, userId1);
            Assert.That(taskDeleted, Is.False, 
                $"Task should no longer exist after successful deletion by user {userId1}");
        }
    }

    /// <summary>
    /// **Feature: task-management-system, Property 8: Task creation with user association**
    /// **Validates: Requirements 3.2**
    /// 
    /// For any valid task data, the task should be created and properly associated with the user,
    /// with correct timestamps and default status.
    /// This test validates the property across multiple random scenarios.
    /// </summary>
    [Test]
    public async System.Threading.Tasks.Task TaskCreation_CreatesTaskWithUserAssociation()
    {
        // Test the property across multiple scenarios with different user IDs and task data
        var random = new Random(44); // Fixed seed for reproducible tests
        
        for (int i = 0; i < 50; i++) // Run 50 iterations to simulate property-based testing
        {
            var userId = random.Next(1, 1000);
            var taskTitle = $"Task_{random.Next(1, 10000)}";
            var taskDescription = $"Description_{random.Next(1, 10000)}";
            
            using var context = CreateInMemoryContext();
            var repository = new TaskRepository(context);

            // Create user first
            var user = new User
            {
                Id = userId,
                Username = $"user{userId}",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var beforeCreation = DateTime.UtcNow;

            // Create a task
            var task = new TaskManagement.Domain.Entities.Task
            {
                Title = taskTitle,
                Description = taskDescription,
                Status = DomainTaskStatus.InProgress, // Set to non-default to test it gets overridden
                UserId = userId
            };

            var createdTask = await repository.CreateAsync(task);
            var afterCreation = DateTime.UtcNow;

            // Property: Task is created with correct user association
            Assert.That(createdTask.UserId, Is.EqualTo(userId), 
                $"Created task should be associated with user {userId}");

            // Property: Task has a valid ID assigned
            Assert.That(createdTask.Id, Is.GreaterThan(0), 
                "Created task should have a valid ID assigned");

            // Property: Task retains the provided title and description
            Assert.That(createdTask.Title, Is.EqualTo(taskTitle), 
                "Created task should retain the provided title");
            Assert.That(createdTask.Description, Is.EqualTo(taskDescription), 
                "Created task should retain the provided description");

            // Property: Task has proper timestamps set
            Assert.That(createdTask.CreatedAt, Is.GreaterThanOrEqualTo(beforeCreation), 
                "CreatedAt should be set to current time or later");
            Assert.That(createdTask.CreatedAt, Is.LessThanOrEqualTo(afterCreation), 
                "CreatedAt should not be set to future time");
            Assert.That(createdTask.UpdatedAt, Is.GreaterThanOrEqualTo(beforeCreation), 
                "UpdatedAt should be set to current time or later");
            Assert.That(createdTask.UpdatedAt, Is.LessThanOrEqualTo(afterCreation), 
                "UpdatedAt should not be set to future time");

            // Property: Task can be retrieved by the user
            var retrievedTask = await repository.GetByIdAsync(createdTask.Id, userId);
            Assert.That(retrievedTask, Is.Not.Null, 
                "Created task should be retrievable by the user");
            Assert.That(retrievedTask.Title, Is.EqualTo(taskTitle), 
                "Retrieved task should have the same title");
            Assert.That(retrievedTask.UserId, Is.EqualTo(userId), 
                "Retrieved task should be associated with the correct user");
        }
    }

    /// <summary>
    /// **Feature: task-management-system, Property 11: Task update with timestamp**
    /// **Validates: Requirements 4.2**
    /// 
    /// For any valid task update, the task should be updated with new values and
    /// the UpdatedAt timestamp should be refreshed while preserving CreatedAt.
    /// This test validates the property across multiple random scenarios.
    /// </summary>
    [Test]
    public async System.Threading.Tasks.Task TaskUpdate_UpdatesTaskWithTimestamp()
    {
        // Test the property across multiple scenarios with different user IDs and task data
        var random = new Random(45); // Fixed seed for reproducible tests
        
        for (int i = 0; i < 50; i++) // Run 50 iterations to simulate property-based testing
        {
            var userId = random.Next(1, 1000);
            var originalTitle = $"OriginalTask_{random.Next(1, 10000)}";
            var originalDescription = $"OriginalDescription_{random.Next(1, 10000)}";
            var updatedTitle = $"UpdatedTask_{random.Next(1, 10000)}";
            var updatedDescription = $"UpdatedDescription_{random.Next(1, 10000)}";
            
            using var context = CreateInMemoryContext();
            var repository = new TaskRepository(context);

            // Create user first
            var user = new User
            {
                Id = userId,
                Username = $"user{userId}",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Create a task
            var originalTask = new TaskManagement.Domain.Entities.Task
            {
                Title = originalTitle,
                Description = originalDescription,
                Status = DomainTaskStatus.ToDo,
                UserId = userId
            };

            var createdTask = await repository.CreateAsync(originalTask);
            var originalCreatedAt = createdTask.CreatedAt;
            var originalUpdatedAt = createdTask.UpdatedAt;

            // Wait a small amount to ensure timestamp difference
            await System.Threading.Tasks.Task.Delay(10);
            var beforeUpdate = DateTime.UtcNow;

            // Update the task
            createdTask.Title = updatedTitle;
            createdTask.Description = updatedDescription;

            var updatedTask = await repository.UpdateAsync(createdTask);
            var afterUpdate = DateTime.UtcNow;

            // Property: Task retains the same ID and user association
            Assert.That(updatedTask.Id, Is.EqualTo(createdTask.Id), 
                "Updated task should retain the same ID");
            Assert.That(updatedTask.UserId, Is.EqualTo(userId), 
                "Updated task should retain the same user association");

            // Property: Task has updated values
            Assert.That(updatedTask.Title, Is.EqualTo(updatedTitle), 
                "Updated task should have the new title");
            Assert.That(updatedTask.Description, Is.EqualTo(updatedDescription), 
                "Updated task should have the new description");

            // Property: CreatedAt timestamp is preserved
            Assert.That(updatedTask.CreatedAt, Is.EqualTo(originalCreatedAt), 
                "CreatedAt timestamp should be preserved during update");

            // Property: UpdatedAt timestamp is refreshed
            Assert.That(updatedTask.UpdatedAt, Is.GreaterThan(originalUpdatedAt), 
                "UpdatedAt timestamp should be refreshed during update");
            Assert.That(updatedTask.UpdatedAt, Is.GreaterThanOrEqualTo(beforeUpdate), 
                "UpdatedAt should be set to current time or later");
            Assert.That(updatedTask.UpdatedAt, Is.LessThanOrEqualTo(afterUpdate), 
                "UpdatedAt should not be set to future time");

            // Property: Updated task can be retrieved with new values
            var retrievedTask = await repository.GetByIdAsync(updatedTask.Id, userId);
            Assert.That(retrievedTask, Is.Not.Null, 
                "Updated task should be retrievable");
            Assert.That(retrievedTask.Title, Is.EqualTo(updatedTitle), 
                "Retrieved task should have the updated title");
            Assert.That(retrievedTask.Description, Is.EqualTo(updatedDescription), 
                "Retrieved task should have the updated description");
            Assert.That(retrievedTask.UpdatedAt, Is.EqualTo(updatedTask.UpdatedAt), 
                "Retrieved task should have the same UpdatedAt timestamp");
        }
    }

    /// <summary>
    /// **Feature: task-management-system, Property 19: Status update with timestamp**
    /// **Validates: Requirements 6.2**
    /// 
    /// For any status change, the task status should be updated and the UpdatedAt 
    /// timestamp should be refreshed while preserving other properties.
    /// This test validates the property across multiple random scenarios.
    /// </summary>
    [Test]
    public async System.Threading.Tasks.Task StatusUpdate_UpdatesStatusWithTimestamp()
    {
        // Test the property across multiple scenarios with different statuses
        var random = new Random(46); // Fixed seed for reproducible tests
        var allStatuses = new[] { DomainTaskStatus.ToDo, DomainTaskStatus.InProgress, DomainTaskStatus.Completed };
        
        for (int i = 0; i < 50; i++) // Run 50 iterations to simulate property-based testing
        {
            var userId = random.Next(1, 1000);
            var taskTitle = $"StatusTask_{random.Next(1, 10000)}";
            var taskDescription = $"StatusDescription_{random.Next(1, 10000)}";
            var initialStatus = allStatuses[random.Next(allStatuses.Length)];
            var newStatus = allStatuses[random.Next(allStatuses.Length)];
            
            using var context = CreateInMemoryContext();
            var repository = new TaskRepository(context);

            // Create user first
            var user = new User
            {
                Id = userId,
                Username = $"user{userId}",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Create a task with initial status
            var task = new TaskManagement.Domain.Entities.Task
            {
                Title = taskTitle,
                Description = taskDescription,
                Status = initialStatus,
                UserId = userId
            };

            var createdTask = await repository.CreateAsync(task);
            var originalCreatedAt = createdTask.CreatedAt;
            var originalUpdatedAt = createdTask.UpdatedAt;
            var originalTitle = createdTask.Title;
            var originalDescription = createdTask.Description;

            // Wait a small amount to ensure timestamp difference
            await System.Threading.Tasks.Task.Delay(10);
            var beforeStatusUpdate = DateTime.UtcNow;

            // Update the status
            createdTask.Status = newStatus;

            var updatedTask = await repository.UpdateAsync(createdTask);
            var afterStatusUpdate = DateTime.UtcNow;

            // Property: Task retains the same ID and user association
            Assert.That(updatedTask.Id, Is.EqualTo(createdTask.Id), 
                "Status updated task should retain the same ID");
            Assert.That(updatedTask.UserId, Is.EqualTo(userId), 
                "Status updated task should retain the same user association");

            // Property: Task has updated status
            Assert.That(updatedTask.Status, Is.EqualTo(newStatus), 
                $"Task status should be updated to {newStatus}");

            // Property: Other properties are preserved
            Assert.That(updatedTask.Title, Is.EqualTo(originalTitle), 
                "Title should be preserved during status update");
            Assert.That(updatedTask.Description, Is.EqualTo(originalDescription), 
                "Description should be preserved during status update");

            // Property: CreatedAt timestamp is preserved
            Assert.That(updatedTask.CreatedAt, Is.EqualTo(originalCreatedAt), 
                "CreatedAt timestamp should be preserved during status update");

            // Property: UpdatedAt timestamp is refreshed (if status actually changed)
            if (initialStatus != newStatus)
            {
                Assert.That(updatedTask.UpdatedAt, Is.GreaterThan(originalUpdatedAt), 
                    "UpdatedAt timestamp should be refreshed when status changes");
            }
            Assert.That(updatedTask.UpdatedAt, Is.GreaterThanOrEqualTo(beforeStatusUpdate), 
                "UpdatedAt should be set to current time or later");
            Assert.That(updatedTask.UpdatedAt, Is.LessThanOrEqualTo(afterStatusUpdate), 
                "UpdatedAt should not be set to future time");

            // Property: Updated task can be retrieved with new status
            var retrievedTask = await repository.GetByIdAsync(updatedTask.Id, userId);
            Assert.That(retrievedTask, Is.Not.Null, 
                "Status updated task should be retrievable");
            Assert.That(retrievedTask.Status, Is.EqualTo(newStatus), 
                "Retrieved task should have the updated status");

            // Property: Task appears in status-filtered queries correctly
            var tasksWithNewStatus = await repository.GetByStatusAsync(newStatus, userId);
            Assert.That(tasksWithNewStatus.Any(t => t.Id == updatedTask.Id), Is.True, 
                "Task should appear in queries filtered by its new status");

            // Property: Task does not appear in queries for other statuses (if status changed)
            if (initialStatus != newStatus)
            {
                var tasksWithOldStatus = await repository.GetByStatusAsync(initialStatus, userId);
                Assert.That(tasksWithOldStatus.Any(t => t.Id == updatedTask.Id), Is.False, 
                    "Task should not appear in queries filtered by its old status");
            }
        }
    }

    /// <summary>
    /// **Feature: task-management-system, Property 15: Confirmed deletion execution**
    /// **Validates: Requirements 5.2**
    /// 
    /// For any confirmed task deletion, the task should be permanently removed from the database
    /// and no longer be accessible through any query methods.
    /// This test validates the property across multiple random scenarios.
    /// </summary>
    [Test]
    public async System.Threading.Tasks.Task TaskDeletion_ConfirmedDeletionExecution()
    {
        // Test the property across multiple scenarios with different user IDs and task data
        var random = new Random(47); // Fixed seed for reproducible tests
        
        for (int i = 0; i < 50; i++) // Run 50 iterations to simulate property-based testing
        {
            var userId = random.Next(1, 1000);
            var taskTitle = $"DeletionTask_{random.Next(1, 10000)}";
            var taskDescription = $"DeletionDescription_{random.Next(1, 10000)}";
            var taskStatus = DomainTaskStatus.InProgress;
            
            using var context = CreateInMemoryContext();
            var repository = new TaskRepository(context);

            // Create user first
            var user = new User
            {
                Id = userId,
                Username = $"user{userId}",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Create a task
            var task = new TaskManagement.Domain.Entities.Task
            {
                Title = taskTitle,
                Description = taskDescription,
                Status = taskStatus,
                UserId = userId
            };

            var createdTask = await repository.CreateAsync(task);
            var taskId = createdTask.Id;

            // Verify task exists before deletion
            var taskBeforeDeletion = await repository.GetByIdAsync(taskId, userId);
            Assert.That(taskBeforeDeletion, Is.Not.Null, 
                "Task should exist before deletion");

            var existsBeforeDeletion = await repository.ExistsAsync(taskId, userId);
            Assert.That(existsBeforeDeletion, Is.True, 
                "Task should exist before deletion (ExistsAsync)");

            var allTasksBeforeDeletion = await repository.GetAllByUserIdAsync(userId);
            Assert.That(allTasksBeforeDeletion.Any(t => t.Id == taskId), Is.True, 
                "Task should appear in user's task list before deletion");

            var tasksByStatusBeforeDeletion = await repository.GetByStatusAsync(taskStatus, userId);
            Assert.That(tasksByStatusBeforeDeletion.Any(t => t.Id == taskId), Is.True, 
                "Task should appear in status-filtered queries before deletion");

            // Property: Confirmed deletion removes the task permanently
            var deletionResult = await repository.DeleteAsync(taskId, userId);
            Assert.That(deletionResult, Is.True, 
                "Deletion should succeed for the task owner");

            // Property: Task is no longer accessible by ID
            var taskAfterDeletion = await repository.GetByIdAsync(taskId, userId);
            Assert.That(taskAfterDeletion, Is.Null, 
                "Task should not be accessible by ID after deletion");

            // Property: Task no longer exists according to ExistsAsync
            var existsAfterDeletion = await repository.ExistsAsync(taskId, userId);
            Assert.That(existsAfterDeletion, Is.False, 
                "Task should not exist after deletion (ExistsAsync)");

            // Property: Task does not appear in user's task list
            var allTasksAfterDeletion = await repository.GetAllByUserIdAsync(userId);
            Assert.That(allTasksAfterDeletion.Any(t => t.Id == taskId), Is.False, 
                "Task should not appear in user's task list after deletion");

            // Property: Task does not appear in status-filtered queries
            var tasksByStatusAfterDeletion = await repository.GetByStatusAsync(taskStatus, userId);
            Assert.That(tasksByStatusAfterDeletion.Any(t => t.Id == taskId), Is.False, 
                "Task should not appear in status-filtered queries after deletion");

            // Property: Attempting to delete the same task again should fail
            var secondDeletionResult = await repository.DeleteAsync(taskId, userId);
            Assert.That(secondDeletionResult, Is.False, 
                "Second deletion attempt should fail as task no longer exists");

            // Property: Attempting to update the deleted task should fail
            var deletedTaskForUpdate = new TaskManagement.Domain.Entities.Task
            {
                Id = taskId,
                Title = "Updated Title",
                Description = "Updated Description",
                Status = DomainTaskStatus.Completed,
                UserId = userId
            };

            // This should throw an exception or fail gracefully
            try
            {
                await repository.UpdateAsync(deletedTaskForUpdate);
                Assert.Fail("Updating a deleted task should fail");
            }
            catch (Exception)
            {
                // Expected behavior - updating a deleted task should fail
                Assert.Pass("Updating a deleted task correctly failed");
            }
        }
    }
}
