using ASRS.Core.Entities;
using ASRS.Core.Enums;
using ASRS.DAL.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASRS.API.Controllers;

[ApiController]
[Route("api/asrs")]
public class AsrsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AsrsController(AppDbContext db) => _db = db;

    [HttpPost("retrieve")]
    public async Task<IActionResult> Retrieve([FromBody] RetrieveRequest req)
    {
        var cell = await _db.RackCells.FirstOrDefaultAsync(x => x.Row == req.Row && x.Col == req.Col);
        if (cell is null) 
            return NotFound("CELL_NOT_FOUND");
        if (!cell.IsOccupied) 
            return Conflict("CELL_EMPTY");

        var cmd = new AsrsCommand
        {
            Type = AsrsCommandType.Retrieve,
            Row = req.Row,
            Col = req.Col,
            Source = AsrsCommandSource.Dashboard,
            Status = AsrsCommandStatus.Queued
        };
        _db.AsrsCommands.Add(cmd);
        await _db.SaveChangesAsync();
        return Ok(new { accepted = true, commandId = cmd.Id, row = cmd.Row, col = cmd.Col, status = cmd.Status.ToString() });
    }

    [HttpPost("rfid-scan")]
    public async Task<IActionResult> RfidScan([FromBody] RfidScanRequest req)
    {
        var map = await _db.RfidRackMaps.FirstOrDefaultAsync(x => x.CardUid == req.CardUid && x.IsActive);
        if (map is null)
        {
            _db.RfidEvents.Add(new RfidEvent { CardUid = req.CardUid, Result = "RFID_NOT_MAPPED" });
            await _db.SaveChangesAsync();
            return NotFound("RFID_NOT_MAPPED");
        }

        var cell = await _db.RackCells.FirstOrDefaultAsync(x => x.Row == map.Row && x.Col == map.Col);
        if (cell is null) 
            return NotFound("CELL_NOT_FOUND");
        if (cell.IsOccupied) 
            return Conflict("CELL_ALREADY_OCCUPIED");

        var cmd = new AsrsCommand
        {
            Type = AsrsCommandType.Store,
            Row = map.Row,
            Col = map.Col,
            Source = AsrsCommandSource.Rfid,
            Status = AsrsCommandStatus.Queued
        };
        _db.AsrsCommands.Add(cmd);
        await _db.SaveChangesAsync();

        _db.RfidEvents.Add(new RfidEvent { CardUid = req.CardUid, ResultCommandId = cmd.Id, Result = "QUEUED" });
        await _db.SaveChangesAsync();

        return Ok(new { accepted = true, commandId = cmd.Id, row = cmd.Row, col = cmd.Col, status = cmd.Status.ToString() });
    }

    [HttpGet("commands/next")]
    public async Task<IActionResult> NextCommand()
    {
        var cmd = await _db.AsrsCommands
            .Where(x => x.Status == AsrsCommandStatus.Queued)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (cmd is null) return NoContent();

        cmd.Status = AsrsCommandStatus.Sent;
        cmd.SentAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = cmd.Id,
            type = cmd.Type.ToString().ToUpper(),
            row = cmd.Row,
            col = cmd.Col
        });
    }

    [HttpPost("commands/{id:int}/ack")]
    public async Task<IActionResult> Ack(int id, [FromBody] AckRequest req)
    {
        var cmd = await _db.AsrsCommands.FirstOrDefaultAsync(x => x.Id == id);
        if (cmd is null) 
            return NotFound("COMMAND_NOT_FOUND");

        if (req.Signal == "BUSY")
        {
            cmd.Status = AsrsCommandStatus.Busy;
        }
        else if (req.Signal == "OK")
        {
            cmd.Status = AsrsCommandStatus.Done;
            cmd.CompletedAt = DateTime.UtcNow;

            if (cmd.Row.HasValue && cmd.Col.HasValue)
            {
                var cell = await _db.RackCells.FirstOrDefaultAsync(x => x.Row == cmd.Row.Value && x.Col == cmd.Col.Value);
                if (cell != null)
                {
                    if (cmd.Type == AsrsCommandType.Store) cell.IsOccupied = true;
                    if (cmd.Type == AsrsCommandType.Retrieve) cell.IsOccupied = false;
                    cell.LastCommandId = cmd.Id;
                    cell.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
        else if (req.Signal == "ERR")
        {
            cmd.Status = AsrsCommandStatus.Failed;
            cmd.Error = req.Message;
            cmd.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            return BadRequest("INVALID_SIGNAL");
        }

        await _db.SaveChangesAsync();
        return Ok(new { ok = true, commandId = cmd.Id, status = cmd.Status.ToString() });
    }

    [HttpGet("rack-state")]
    public async Task<IActionResult> RackState()
    {
        var data = await _db.RackCells
            .OrderBy(x => x.Row).ThenBy(x => x.Col)
            .Select(x => new { x.Row, x.Col, x.IsOccupied, x.UpdatedAt })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("system-status")]
    public async Task<IActionResult> SystemStatus()
    {
        var queuedCount = await _db.AsrsCommands.CountAsync(x => x.Status == AsrsCommandStatus.Queued);
        var sentCount = await _db.AsrsCommands.CountAsync(x => x.Status == AsrsCommandStatus.Sent);
        var busyCount = await _db.AsrsCommands.CountAsync(x => x.Status == AsrsCommandStatus.Busy);
        var failedCount = await _db.AsrsCommands.CountAsync(x => x.Status == AsrsCommandStatus.Failed);

        var lastCommand = await _db.AsrsCommands
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                type = x.Type.ToString(),
                status = x.Status.ToString(),
                x.Row,
                x.Col,
                x.CreatedAt,
                x.SentAt,
                x.CompletedAt
            })
            .FirstOrDefaultAsync();

        var asrsState = busyCount > 0 ? "BUSY" : "READY";
        var queueState = (queuedCount + sentCount + busyCount) > 0 ? "ACTIVE" : "IDLE";
        var arduinoState = busyCount > 0 ? "WORKING" : "WAITING";

        return Ok(new
        {
            asrsState,
            queueState,
            arduinoState,
            queuedCount,
            sentCount,
            busyCount,
            failedCount,
            lastCommand
        });
    }
}

public record RetrieveRequest(int Row, int Col);
public record RfidScanRequest(string CardUid);
public record AckRequest(string Signal, string? Message);
