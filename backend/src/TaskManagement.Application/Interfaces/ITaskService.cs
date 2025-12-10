using TaskManagement.Application.Models;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Interfaces;

public interface ITaskService
{
    Task<TaskResult> GetAllTasksAsync(int userId);
    Task<TaskResult> GetTaskByIdAsync(int taskId, int userId);
    Task<TaskResult> CreateTaskAsync(string title, string? description, int userId);
    Task<TaskResult> UpdateTaskAsync(int taskId, string title, string? description, int userId);
    Task<TaskResult> UpdateTaskStatusAsync(int taskId, DomainTaskStatus status, int userId);
    Task<TaskResult> DeleteTaskAsync(int taskId, int userId);
    Task<TaskResult> GetTasksByStatusAsync(DomainTaskStatus status, int userId);
}