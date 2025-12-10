using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Api.Models;

public class UpdateTaskStatusRequest
{
    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("^(ToDo|InProgress|Completed)$", ErrorMessage = "Status must be one of: ToDo, InProgress, Completed")]
    public string Status { get; set; } = string.Empty;
}