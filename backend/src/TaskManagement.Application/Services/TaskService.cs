using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Models;
using TaskManagement.Domain.Entities;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    }

    public async Task<TaskResult> GetAllTasksAsync(int userId)
    {
        try
        {
            if (userId <= 0)
            {
                return TaskResult.Failed("Invalid user ID");
            }

            var tasks = await _taskRepository.GetAllByUserIdAsync(userId);
            return TaskResult.Successful(tasks);
        }
        catch (Exception ex)
        {
            return TaskResult.Failed($"Failed to retrieve tasks: {ex.Message}");
        }
    }

    public async Task<TaskResult> GetTaskByIdAsync(int taskId, int userId)
    {
        try
        {
            if (taskId <= 0)
            {
                return TaskResult.Failed("Invalid task ID");
            }

            if (userId <= 0)
            {
                return TaskResult.Failed("Invalid user ID");
            }

            var task = await _taskRepository.GetByIdAsync(taskId, userId);
            if (task == null)
            {
                return TaskResult.Failed("Task not found or access denied");
            }

            return TaskResult.Successful(task);
        }
        catch (Exception ex)
        {
            return TaskResult.Failed($"Failed to retrieve task: {ex.Message}");
        }
    }

    public async Task<TaskResult> CreateTaskAsync(string title, string? description, int userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return TaskResult.Failed("Task title is required");
            }

            if (title.Length > 200)
            {
                return TaskResult.Failed("Task title cannot exceed 200 characters");
            }

            if (description?.Length > 1000)
            {
                return TaskResult.Failed("Task description cannot exceed 1000 characters");
            }

            if (userId <= 0)
            {
                return TaskResult.Failed("Invalid user ID");
            }

            var task = new Domain.Entities.Task
            {
                Title = title.Trim(),
                Description = description?.Trim(),
                Status = DomainTaskStatus.ToDo,
                UserId = userId
            };

            var createdTask = await _taskRepository.CreateAsync(task);
            return TaskResult.Successful(createdTask);
        }
        catch (Exception ex)
        {
            return TaskResult.Failed($"Failed to create task: {ex.Message}");
        }
    }

    public async Task<TaskResult> UpdateTaskAsync(int taskId, string title, string? description, int userId)
    {
        try
        {
            if (taskId <= 0)
            {
                return TaskResult.Failed("Invalid task ID");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return TaskResult.Failed("Task title is required");
            }

            if (title.Length > 200)
            {
                return TaskResult.Failed("Task title cannot exceed 200 characters");
            }

            if (description?.Length > 1000)
            {
                return TaskResult.Failed("Task description cannot exceed 1000 characters");
            }

            if (userId <= 0)
            {
                return TaskResult.Failed("Invalid user ID");
            }

            var existingTask = await _taskRepository.GetByIdAsync(taskId, userId);
            if (existingTask == null)
            {
                return TaskResult.Failed("Task not found or access denied");
            }

            existingTask.Title = title.Trim();
            existingTask.Description = description?.Trim();

            var updatedTask = await _taskRepository.UpdateAsync(existingTask);
            return TaskResult.Successful(updatedTask);
        }
        catch (Exception ex)
        {
            return TaskResult.Failed($"Failed to update task: {ex.Message}");
        }
    }

    public async Task<TaskResult> UpdateTaskStatusAsync(int taskId, DomainTaskStatus status, int userId)
    {
        try
        {
            if (taskId <= 0)
            {
                return TaskResult.Failed("Invalid task ID");
            }

            if (userId <= 0)
            {
                return TaskResult.Failed("Invalid user ID");
            }

            if (!Enum.IsDefined(typeof(DomainTaskStatus), status))
            {
                return TaskResult.Failed("Invalid task status");
            }

            var existingTask = await _taskRepository.GetByIdAsync(taskId, userId);
            if (existingTask == null)
            {
                return TaskResult.Failed("Task not found or access denied");
            }

            existingTask.Status = status;

            var updatedTask = await _taskRepository.UpdateAsync(existingTask);
            return TaskResult.Successful(updatedTask);
        }
        catch (Exception ex)
        {
            return TaskResult.Failed($"Failed to update task status: {ex.Message}");
        }
    }

    public async Task<TaskResult> DeleteTaskAsync(int taskId, int userId)
    {
        try
        {
            if (taskId <= 0)
            {
                return TaskResult.Failed("Invalid task ID");
            }

            if (userId <= 0)
            {
                return TaskResult.Failed("Invalid user ID");
            }

            var deleted = await _taskRepository.DeleteAsync(taskId, userId);
            if (!deleted)
            {
                return TaskResult.Failed("Task not found or access denied");
            }

            return TaskResult.Successful(new Domain.Entities.Task { Id = taskId });
        }
        catch (Exception ex)
        {
            return TaskResult.Failed($"Failed to delete task: {ex.Message}");
        }
    }

    public async Task<TaskResult> GetTasksByStatusAsync(DomainTaskStatus status, int userId)
    {
        try
        {
            if (userId <= 0)
            {
                return TaskResult.Failed("Invalid user ID");
            }

            if (!Enum.IsDefined(typeof(DomainTaskStatus), status))
            {
                return TaskResult.Failed("Invalid task status");
            }

            var tasks = await _taskRepository.GetByStatusAsync(status, userId);
            return TaskResult.Successful(tasks);
        }
        catch (Exception ex)
        {
            return TaskResult.Failed($"Failed to retrieve tasks by status: {ex.Message}");
        }
    }
}