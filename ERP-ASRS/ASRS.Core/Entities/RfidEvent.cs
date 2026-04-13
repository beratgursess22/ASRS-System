namespace ASRS.Core.Entities;

public class RfidEvent
{
    public int Id { get; set; }
    public string CardUid { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public int? ResultCommandId { get; set; }
    public string Result { get; set; } = string.Empty;
}
