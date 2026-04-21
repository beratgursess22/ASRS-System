using ASRS.Core.Entities;
using ASRS.Core.Enums;
using ASRS.API.Services;
using ASRS.DAL.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ASRS.API.Controllers;

[ApiController]
[Route("api/asrs")]
public class AsrsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AsrsController> _logger;
    private readonly IConfiguration _configuration;

    public AsrsController(AppDbContext db, ILogger<AsrsController> logger, IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
    }

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
        _logger.LogInformation("RFID_SCAN_START RawUid='{RawUid}'", req.CardUid);

        string normalizedUid;
        try
        {
            normalizedUid = RfidUidNormalizer.Normalize(req.CardUid);
            _logger.LogInformation("RFID_UID_NORMALIZED RawUid='{RawUid}' NormalizedUid='{NormalizedUid}'", req.CardUid, normalizedUid);
        }
        catch (FormatException)
        {
            _logger.LogWarning("RFID_UID_NORMALIZE_FAILED RawUid='{RawUid}' Reason=INVALID_RFID_UID_FORMAT", req.CardUid);
            return BadRequest("INVALID_RFID_UID_FORMAT");
        }

        if (string.IsNullOrWhiteSpace(normalizedUid))
        {
            _logger.LogWarning("RFID_UID_EMPTY_AFTER_NORMALIZE RawUid='{RawUid}'", req.CardUid);
            return BadRequest("EMPTY_RFID_UID");
        }

        _logger.LogInformation("RFID_MAP_LOOKUP_START NormalizedUid='{NormalizedUid}'", normalizedUid);
        var map = await _db.RfidRackMaps.FirstOrDefaultAsync(x => x.CardUid == normalizedUid && x.IsActive);
        if (map is null)
        {
            _logger.LogWarning("RFID_MAP_NOT_FOUND NormalizedUid='{NormalizedUid}'", normalizedUid);
            _db.RfidEvents.Add(new RfidEvent { CardUid = normalizedUid, Result = "RFID_NOT_MAPPED" });
            await _db.SaveChangesAsync();
            _logger.LogInformation("RFID_EVENT_SAVED NormalizedUid='{NormalizedUid}' Result='RFID_NOT_MAPPED'", normalizedUid);
            return NotFound("RFID_NOT_MAPPED");
        }
        _logger.LogInformation("RFID_MAP_FOUND NormalizedUid='{NormalizedUid}' Row={Row} Col={Col} IsActive={IsActive}", normalizedUid, map.Row, map.Col, map.IsActive);

        _logger.LogInformation("RACK_CELL_LOOKUP_START Row={Row} Col={Col}", map.Row, map.Col);
        var cell = await _db.RackCells.FirstOrDefaultAsync(x => x.Row == map.Row && x.Col == map.Col);
        if (cell is null)
        {
            _logger.LogWarning("RACK_CELL_NOT_FOUND Row={Row} Col={Col}", map.Row, map.Col);
            return NotFound("CELL_NOT_FOUND");
        }
        _logger.LogInformation("RACK_CELL_FOUND Row={Row} Col={Col} IsOccupied={IsOccupied}", cell.Row, cell.Col, cell.IsOccupied);

        if (cell.IsOccupied)
        {
            _logger.LogWarning("RACK_CELL_ALREADY_OCCUPIED Row={Row} Col={Col}", cell.Row, cell.Col);
            return Conflict("CELL_ALREADY_OCCUPIED");
        }

        _logger.LogInformation("ASRS_COMMAND_CREATE_START Type={Type} Source={Source} Row={Row} Col={Col}",
            AsrsCommandType.Store, AsrsCommandSource.Rfid, map.Row, map.Col);
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
        _logger.LogInformation("ASRS_COMMAND_SAVED CommandId={CommandId} Status={Status} Type={Type} Row={Row} Col={Col}",
            cmd.Id, cmd.Status, cmd.Type, cmd.Row, cmd.Col);

        _logger.LogInformation("RFID_EVENT_CREATE_START NormalizedUid='{NormalizedUid}' CommandId={CommandId} Result='QUEUED'",
            normalizedUid, cmd.Id);
        _db.RfidEvents.Add(new RfidEvent { CardUid = normalizedUid, ResultCommandId = cmd.Id, Result = "QUEUED" });
        await _db.SaveChangesAsync();
        _logger.LogInformation("RFID_EVENT_SAVED NormalizedUid='{NormalizedUid}' CommandId={CommandId} Result='QUEUED'",
            normalizedUid, cmd.Id);

        _logger.LogInformation("RFID_SCAN_SUCCESS CommandId={CommandId} Row={Row} Col={Col} Status={Status}",
            cmd.Id, cmd.Row, cmd.Col, cmd.Status);

        return Ok(new { accepted = true, commandId = cmd.Id, row = cmd.Row, col = cmd.Col, status = cmd.Status.ToString() });
    }

    [HttpGet("commands/next")]
    public async Task<IActionResult> NextCommand()
    {
        if (IsApiSerialBridgeActive())
            return Conflict("PI_PULL_BRIDGE_DISABLED_WHEN_API_SERIAL_ENABLED");

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
        if (IsApiSerialBridgeActive())
            return Conflict("PI_PULL_BRIDGE_DISABLED_WHEN_API_SERIAL_ENABLED");

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

    private bool IsApiSerialBridgeActive()
        => _configuration.GetValue("AsrsSerial:Enabled", false);

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

    [HttpGet("rfid-maps")]
    public async Task<IActionResult> RfidMaps()
    {
        var maps = await _db.RfidRackMaps
            .OrderBy(x => x.Row)
            .ThenBy(x => x.Col)
            .Select(x => new
            {
                x.CardUid,
                x.Row,
                x.Col,
                rowUi = x.Row + 1,
                colUi = x.Col + 1,
                x.IsActive,
                x.UpdatedAt
            })
            .ToListAsync();

        return Ok(maps);
    }
}

public record RetrieveRequest(int Row, int Col);
public record RfidScanRequest(string CardUid);
public record AckRequest(string Signal, string? Message);
