using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagement.Api.Models;
using TaskManagement.Application.Interfaces;
using DomainTaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("Invalid user context");
        }
        return userId;
    }

    private static TaskDto MapToDto(Domain.Entities.Task task)
    {
        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status.ToString(),
            UserId = task.UserId,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }

    /// <summary>
    /// Gets all tasks for the authenticated user
    /// </summary>
    /// <returns>List of user's tasks</returns>
    /// <response code="200">Tasks retrieved successfully</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> GetTasks()
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.GetAllTasksAsync(userId);

            if (!result.Success)
            {
                var errorResponse = new ErrorResponse
                {
                    Message = result.ErrorMessage ?? "Failed to retrieve tasks",
                    TraceId = HttpContext.TraceIdentifier
                };
                return StatusCode(500, errorResponse);
            }

            var taskDtos = result.Tasks!.Select(MapToDto);
            return Ok(taskDtos);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            var errorResponse = new ErrorResponse
            {
                Message = "Unauthorized access",
                TraceId = HttpContext.TraceIdentifier
            };
            return Unauthorized(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving tasks");
            var errorResponse = new ErrorResponse
            {
                Message = "An error occurred while retrieving tasks",
                TraceId = HttpContext.TraceIdentifier
            };
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Gets a specific task by ID for the authenticated user
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>Task details</returns>
    /// <response code="200">Task retrieved successfully</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="404">Task not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> GetTask(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.GetTaskByIdAsync(id, userId);

            if (!result.Success)
            {
                var errorResponse = new ErrorResponse
                {
                    Message = result.ErrorMessage ?? "Task not found",
                    TraceId = HttpContext.TraceIdentifier
                };
                return NotFound(errorResponse);
            }

            var taskDto = MapToDto(result.Task!);
            return Ok(taskDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            var errorResponse = new ErrorResponse
            {
                Message = "Unauthorized access",
                TraceId = HttpContext.TraceIdentifier
            };
            return Unauthorized(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving task {TaskId}", id);
            var errorResponse = new ErrorResponse
            {
                Message = "An error occurred while retrieving the task",
                TraceId = HttpContext.TraceIdentifier
            };
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Creates a new task for the authenticated user
    /// </summary>
    /// <param name="request">Task creation data</param>
    /// <returns>Created task details</returns>
    /// <response code="201">Task created successfully</response>
    /// <response code="400">Invalid request - validation errors</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new ValidationError
                    {
                        Field = x.Key,
                        Message = x.Value!.Errors.First().ErrorMessage
                    })
                    .ToList();

                var errorResponse = new ErrorResponse
                {
                    Message = "Validation failed",
                    Errors = errors,
                    TraceId = HttpContext.TraceIdentifier
                };

                return BadRequest(errorResponse);
            }

            var userId = GetCurrentUserId();
            var result = await _taskService.CreateTaskAsync(request.Title, request.Description, userId);

            if (!result.Success)
            {
                var errorResponse = new ErrorResponse
                {
                    Message = result.ErrorMessage ?? "Failed to create task",
                    TraceId = HttpContext.TraceIdentifier
                };
                return StatusCode(500, errorResponse);
            }

            var taskDto = MapToDto(result.Task!);
            return CreatedAtAction(nameof(GetTask), new { id = taskDto.Id }, taskDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            var errorResponse = new ErrorResponse
            {
                Message = "Unauthorized access",
                TraceId = HttpContext.TraceIdentifier
            };
            return Unauthorized(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating task");
            var errorResponse = new ErrorResponse
            {
                Message = "An error occurred while creating the task",
                TraceId = HttpContext.TraceIdentifier
            };
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Updates an existing task for the authenticated user
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="request">Task update data</param>
    /// <returns>Updated task details</returns>
    /// <response code="200">Task updated successfully</response>
    /// <response code="400">Invalid request - validation errors</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="404">Task not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(TaskDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new ValidationError
                    {
                        Field = x.Key,
                        Message = x.Value!.Errors.First().ErrorMessage
                    })
                    .ToList();

                var errorResponse = new ErrorResponse
                {
                    Message = "Validation failed",
                    Errors = errors,
                    TraceId = HttpContext.TraceIdentifier
                };

                return BadRequest(errorResponse);
            }

            var userId = GetCurrentUserId();
            var result = await _taskService.UpdateTaskAsync(id, request.Title, request.Description, userId);

            if (!result.Success)
            {
                var errorResponse = new ErrorResponse
                {
                    Message = result.ErrorMessage ?? "Failed to update task",
                    TraceId = HttpContext.TraceIdentifier
                };

                // Check if it's a not found error
                if (result.ErrorMessage?.Contains("not found") == true || 
                    result.ErrorMessage?.Contains("access denied") == true)
                {
                    return NotFound(errorResponse);
                }

                return StatusCode(500, errorResponse);
            }

            var taskDto = MapToDto(result.Task!);
            return Ok(taskDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            var errorResponse = new ErrorResponse
            {
                Message = "Unauthorized access",
                TraceId = HttpContext.TraceIdentifier
            };
            return Unauthorized(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating task {TaskId}", id);
            var errorResponse = new ErrorResponse
            {
                Message = "An error occurred while updating the task",
                TraceId = HttpContext.TraceIdentifier
            };
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Updates the status of an existing task for the authenticated user
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <param name="request">Status update data</param>
    /// <returns>Updated task details</returns>
    /// <response code="200">Task status updated successfully</response>
    /// <response code="400">Invalid request - validation errors</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="404">Task not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(TaskDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new ValidationError
                    {
                        Field = x.Key,
                        Message = x.Value!.Errors.First().ErrorMessage
                    })
                    .ToList();

                var errorResponse = new ErrorResponse
                {
                    Message = "Validation failed",
                    Errors = errors,
                    TraceId = HttpContext.TraceIdentifier
                };

                return BadRequest(errorResponse);
            }

            if (!Enum.TryParse<DomainTaskStatus>(request.Status, out var status))
            {
                var errorResponse = new ErrorResponse
                {
                    Message = "Invalid status value",
                    TraceId = HttpContext.TraceIdentifier
                };
                return BadRequest(errorResponse);
            }

            var userId = GetCurrentUserId();
            var result = await _taskService.UpdateTaskStatusAsync(id, status, userId);

            if (!result.Success)
            {
                var errorResponse = new ErrorResponse
                {
                    Message = result.ErrorMessage ?? "Failed to update task status",
                    TraceId = HttpContext.TraceIdentifier
                };

                // Check if it's a not found error
                if (result.ErrorMessage?.Contains("not found") == true || 
                    result.ErrorMessage?.Contains("access denied") == true)
                {
                    return NotFound(errorResponse);
                }

                return StatusCode(500, errorResponse);
            }

            var taskDto = MapToDto(result.Task!);
            return Ok(taskDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            var errorResponse = new ErrorResponse
            {
                Message = "Unauthorized access",
                TraceId = HttpContext.TraceIdentifier
            };
            return Unauthorized(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating task status {TaskId}", id);
            var errorResponse = new ErrorResponse
            {
                Message = "An error occurred while updating the task status",
                TraceId = HttpContext.TraceIdentifier
            };
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Deletes a task for the authenticated user
    /// </summary>
    /// <param name="id">Task ID</param>
    /// <returns>No content if successful</returns>
    /// <response code="204">Task deleted successfully</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="404">Task not found or access denied</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> DeleteTask(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _taskService.DeleteTaskAsync(id, userId);

            if (!result.Success)
            {
                var errorResponse = new ErrorResponse
                {
                    Message = result.ErrorMessage ?? "Failed to delete task",
                    TraceId = HttpContext.TraceIdentifier
                };

                // Check if it's a not found error
                if (result.ErrorMessage?.Contains("not found") == true || 
                    result.ErrorMessage?.Contains("access denied") == true)
                {
                    return NotFound(errorResponse);
                }

                return StatusCode(500, errorResponse);
            }

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            var errorResponse = new ErrorResponse
            {
                Message = "Unauthorized access",
                TraceId = HttpContext.TraceIdentifier
            };
            return Unauthorized(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting task {TaskId}", id);
            var errorResponse = new ErrorResponse
            {
                Message = "An error occurred while deleting the task",
                TraceId = HttpContext.TraceIdentifier
            };
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Gets tasks filtered by status for the authenticated user
    /// </summary>
    /// <param name="status">Task status filter (ToDo, InProgress, Completed)</param>
    /// <returns>List of tasks with the specified status</returns>
    /// <response code="200">Tasks retrieved successfully</response>
    /// <response code="400">Invalid status parameter</response>
    /// <response code="401">Unauthorized - invalid or missing token</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> GetTasksByStatus(string status)
    {
        try
        {
            if (!Enum.TryParse<DomainTaskStatus>(status, true, out var taskStatus))
            {
                var errorResponse = new ErrorResponse
                {
                    Message = "Invalid status parameter. Valid values are: ToDo, InProgress, Completed",
                    TraceId = HttpContext.TraceIdentifier
                };
                return BadRequest(errorResponse);
            }

            var userId = GetCurrentUserId();
            var result = await _taskService.GetTasksByStatusAsync(taskStatus, userId);

            if (!result.Success)
            {
                var errorResponse = new ErrorResponse
                {
                    Message = result.ErrorMessage ?? "Failed to retrieve tasks",
                    TraceId = HttpContext.TraceIdentifier
                };
                return StatusCode(500, errorResponse);
            }

            var taskDtos = result.Tasks!.Select(MapToDto);
            return Ok(taskDtos);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            var errorResponse = new ErrorResponse
            {
                Message = "Unauthorized access",
                TraceId = HttpContext.TraceIdentifier
            };
            return Unauthorized(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving tasks by status {Status}", status);
            var errorResponse = new ErrorResponse
            {
                Message = "An error occurred while retrieving tasks",
                TraceId = HttpContext.TraceIdentifier
            };
            return StatusCode(500, errorResponse);
        }
    }
}