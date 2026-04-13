namespace ASRS.Core.Entities;

public class RfidRackMap
{
    public int Id { get; set; }
    public string CardUid { get; set; } = string.Empty;
    public int Row { get; set; }   // 0-based
    public int Col { get; set; }   // 0-based
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
