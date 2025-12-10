using TaskManagement.Domain.Entities;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Application.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<Domain.Entities.Task>> GetAllByUserIdAsync(int userId);
    Task<Domain.Entities.Task?> GetByIdAsync(int id, int userId);
    Task<Domain.Entities.Task> CreateAsync(Domain.Entities.Task task);
    Task<Domain.Entities.Task> UpdateAsync(Domain.Entities.Task task);
    Task<bool> DeleteAsync(int id, int userId);
    Task<bool> ExistsAsync(int id, int userId);
    Task<IEnumerable<Domain.Entities.Task>> GetByStatusAsync(DomainTaskStatus status, int userId);
}