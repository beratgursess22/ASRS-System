using ASRS.Core.Enums;

namespace ASRS.Core.Entities;

public class AsrsCommand
{
    public int Id { get; set; }
    public AsrsCommandType Type { get; set; }
    public int? Row { get; set; }
    public int? Col { get; set; }
    public AsrsCommandSource Source { get; set; }
    public AsrsCommandStatus Status { get; set; } = AsrsCommandStatus.Queued;
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
