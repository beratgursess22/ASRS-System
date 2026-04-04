namespace ASRS.Core.DTOs;

public class CreateQualityInspectionItemDto
{
	public string CharacteristicName { get; set; } = string.Empty;
	public string? ExpectedValue { get; set; }
	public string? ActualValue { get; set; }
	public bool IsPassed { get; set; } = false;
	public string? Notes { get; set; }
}
