using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Models;

public class TaskResult
{
    public bool Success { get; set; }
    public Domain.Entities.Task? Task { get; set; }
    public IEnumerable<Domain.Entities.Task>? Tasks { get; set; }
    public string? ErrorMessage { get; set; }

    public static TaskResult Successful(Domain.Entities.Task task)
    {
        return new TaskResult
        {
            Success = true,
            Task = task
        };
    }

    public static TaskResult Successful(IEnumerable<Domain.Entities.Task> tasks)
    {
        return new TaskResult
        {
            Success = true,
            Tasks = tasks
        };
    }

    public static TaskResult Failed(string errorMessage)
    {
        return new TaskResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}