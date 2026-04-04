using ASRS.Core.Enums;

namespace ASRS.Core.DTOs;

public class CreateQualityDefectDto
{
	public int QualityInspectionId { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DefectSeverity Severity { get; set; } = DefectSeverity.Medium;
}
