using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Api.Models;

public class TaskDto
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
    
    [Required]
    public string Status { get; set; } = string.Empty;
    
    public int UserId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}