namespace ASRS.Core.Entities;

public class RackCell
{
    public int Id { get; set; }
    public int Row { get; set; }   // 0-based
    public int Col { get; set; }   // 0-based
    public bool IsOccupied { get; set; }
    public int? LastCommandId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
