using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskStatus;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TaskManagementDbContext _context;

    public TaskRepository(TaskManagementDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<Domain.Entities.Task>> GetAllByUserIdAsync(int userId)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Domain.Entities.Task?> GetByIdAsync(int id, int userId)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    }

    public async Task<Domain.Entities.Task> CreateAsync(Domain.Entities.Task task)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        task.CreatedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<Domain.Entities.Task> UpdateAsync(Domain.Entities.Task task)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        task.UpdatedAt = DateTime.UtcNow;
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var task = await GetByIdAsync(id, userId);
        if (task == null)
        {
            return false;
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id, int userId)
    {
        return await _context.Tasks
            .AnyAsync(t => t.Id == id && t.UserId == userId);
    }

    public async Task<IEnumerable<Domain.Entities.Task>> GetByStatusAsync(DomainTaskStatus status, int userId)
    {
        return await _context.Tasks
            .Where(t => t.Status == status && t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }
}