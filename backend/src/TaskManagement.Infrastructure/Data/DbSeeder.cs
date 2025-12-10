using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using DomainTask = TaskManagement.Domain.Entities.Task;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Infrastructure.Data;

public static class DbSeeder
{
    public static async System.Threading.Tasks.Task SeedAsync(TaskManagementDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var users = new List<User>
        {
            new User
            {
                Username = "admin",
                PasswordHash = "$2a$11$5Ot8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "testuser",
                PasswordHash = "$2a$11$5Ot8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8Z8",
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        var tasks = new List<DomainTask>
        {
            new DomainTask
            {
                Title = "Setup Development Environment",
                Description = "Configure development tools and environment",
                Status = DomainTaskStatus.Completed,
                UserId = users[0].Id,
                CreatedAt = DateTime.UtcNow
            },
            new DomainTask
            {
                Title = "Implement Authentication",
                Description = "Create user authentication system with JWT",
                Status = DomainTaskStatus.InProgress,
                UserId = users[0].Id,
                CreatedAt = DateTime.UtcNow
            },
            new DomainTask
            {
                Title = "Design Database Schema",
                Description = "Create Entity Framework models and migrations",
                Status = DomainTaskStatus.ToDo,
                UserId = users[1].Id,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Tasks.AddRangeAsync(tasks);
        await context.SaveChangesAsync();
    }
}