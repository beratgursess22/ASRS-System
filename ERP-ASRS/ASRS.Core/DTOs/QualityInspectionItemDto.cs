namespace ASRS.Core.DTOs;

public class QualityInspectionItemDto
{
	public int Id { get; set; }
	public string CharacteristicName { get; set; } = string.Empty;
	public string? ExpectedValue { get; set; }
	public string? ActualValue { get; set; }
	public bool IsPassed { get; set; }
	public string? Notes { get; set; }
}
