using System.ComponentModel.DataAnnotations;
using ASRS.Core.Enums;

namespace ASRS.Core.DTOs;

public class CreateWorkOrderDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public int ProductId { get; set; }

    [Required]
    public int Quantity { get; set; }

    public WorkOrderPriority Priority { get; set; } = WorkOrderPriority.Medium;

    public int? DepartmentId { get; set; }

    public string? AssignedUserId { get; set; }

    [Required]
    public DateTime PlannedStartDate { get; set; }

    [Required]
    public DateTime PlannedEndDate { get; set; }

    public string? Notes { get; set; }
}