using ASRS.Core.Enums;

namespace ASRS.Core.DTOs;

public class QualityDefectDto
{
	public int Id { get; set; }
	public int QualityInspectionId { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DefectSeverity Severity { get; set; }
	public bool IsResolved { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? ResolvedAt { get; set; }
	public List<CapaActionDto> CapaActions { get; set; } = new();
}
