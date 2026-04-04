namespace ASRS.Core.Entities;

public class QualityInspectionItem
{
	public int Id { get; set; }

	public int QualityInspectionId { get; set; }
	public QualityInspection QualityInspection { get; set; } = null!;

	public string CharacteristicName { get; set; } = string.Empty;
	public string? ExpectedValue { get; set; }
	public string? ActualValue { get; set; }

	public bool IsPassed { get; set; } = false;
	public string? Notes { get; set; }
}
