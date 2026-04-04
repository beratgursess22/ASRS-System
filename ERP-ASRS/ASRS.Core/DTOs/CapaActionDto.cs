using ASRS.Core.Enums;

namespace ASRS.Core.DTOs;

public class CapaActionDto
{
	public int Id { get; set; }
	public int QualityDefectId { get; set; }
	public string ActionDescription { get; set; } = string.Empty;
	public string? ResponsibleUserId { get; set; }
	public string? ResponsibleUserName { get; set; }
	public DateTime? DueDate { get; set; }
	public CapaStatus Status { get; set; }
	public string? CompletionNote { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? CompletedAt { get; set; }
}
