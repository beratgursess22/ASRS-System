using ASRS.Core.Enums;

namespace ASRS.Core.Entities;

public class QualityDefect
{
	public int Id { get; set; }

	public int QualityInspectionId { get; set; }
	public QualityInspection QualityInspection { get; set; } = null!;

	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }

	public DefectSeverity Severity { get; set; } = DefectSeverity.Medium;

	public bool IsResolved { get; set; } = false;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? ResolvedAt { get; set; }

	public ICollection<CapaAction> CapaActions { get; set; } = new List<CapaAction>();
}
