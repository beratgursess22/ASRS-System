namespace ASRS.Core.DTOs;

public class CreateCapaActionDto
{
	public int QualityDefectId { get; set; }
	public string ActionDescription { get; set; } = string.Empty;
	public string? ResponsibleUserId { get; set; }
	public DateTime? DueDate { get; set; }
}
