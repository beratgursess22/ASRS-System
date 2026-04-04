using ASRS.Core.Enums;

namespace ASRS.Core.Entities;

public class CapaAction
{
	public int Id { get; set; }

	public int QualityDefectId { get; set; }
	public QualityDefect QualityDefect { get; set; } = null!;

	public string ActionDescription { get; set; } = string.Empty;

	public string? ResponsibleUserId { get; set; }
	public AppUser? ResponsibleUser { get; set; }

	public DateTime? DueDate { get; set; }
	public CapaStatus Status { get; set; } = CapaStatus.Open;

	public string? CompletionNote { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? CompletedAt { get; set; }
}
