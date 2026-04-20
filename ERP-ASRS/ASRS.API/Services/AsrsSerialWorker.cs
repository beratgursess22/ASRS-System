using System.IO.Ports;
using ASRS.Core.Entities;
using ASRS.Core.Enums;
using ASRS.DAL.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ASRS.API.Services;

public class AsrsSerialWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AsrsSerialWorker> _logger;
    private readonly IConfiguration _configuration;

    public AsrsSerialWorker(
        IServiceProvider serviceProvider,
        ILogger<AsrsSerialWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue("AsrsSerial:Enabled", false);
        if (!enabled)
        {
            _logger.LogInformation("ASRS serial worker disabled (AsrsSerial:Enabled=false).");
            return;
        }

        var portName = _configuration["AsrsSerial:PortName"] ?? "/dev/ttyUSB0";
        var baudRate = _configuration.GetValue("AsrsSerial:BaudRate", 9600);
        var pollMs = _configuration.GetValue("AsrsSerial:PollIntervalMs", 400);
        var commandTimeoutSec = _configuration.GetValue("AsrsSerial:CommandTimeoutSec", 180);

        SerialPort? serial = null;
        _logger.LogInformation("ASRS serial worker starting on {PortName} @ {BaudRate}.", portName, baudRate);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (serial is null)
                    serial = CreateAndOpenSerial(portName, baudRate);

                var cmd = await GetNextQueuedCommandAsync(stoppingToken);
                if (cmd is null)
                {
                    await Task.Delay(pollMs, stoppingToken);
                    continue;
                }

                string commandText;
                try
                {
                    commandText = BuildArduinoCommandAsync(cmd);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to build Arduino command (CommandId={CommandId})", cmd.Id);
                    await MarkFailedAsync(cmd.Id, $"BUILD_CMD_FAILED:{ex.Message}", stoppingToken);
                    continue;
                }
                _logger.LogInformation("Sending to Arduino: {Command} (CommandId={CommandId})", commandText, cmd.Id);
                var port = serial;
                if (port is null)
                    throw new InvalidOperationException("Serial port is not open.");
                port.Write(commandText + "\n");

                await WaitAndProcessArduinoResponsesAsync(port, cmd.Id, cmd.Type, commandTimeoutSec, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ASRS serial worker loop error.");
                CloseAndDispose(ref serial);
                await Task.Delay(1000, stoppingToken);
            }
        }

        CloseAndDispose(ref serial);
    }

    private static SerialPort CreateAndOpenSerial(string portName, int baudRate)
    {
        var port = new SerialPort(portName, baudRate)
        {
            NewLine = "\n",
            ReadTimeout = 500,
            WriteTimeout = 1000,
            DtrEnable = false,
            RtsEnable = false
        };
        port.Open();
        return port;
    }

    private async Task<AsrsCommand?> GetNextQueuedCommandAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cmd = await db.AsrsCommands
            .Where(x => x.Status == AsrsCommandStatus.Queued)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (cmd is null)
            return null;

        cmd.Status = AsrsCommandStatus.Sent;
        cmd.SentAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return cmd;
    }

    private static string BuildArduinoCommandAsync(AsrsCommand cmd)
    {
        return cmd.Type switch
        {
            AsrsCommandType.Store => $"STORE:{RequireCol(cmd)}:{RequireRow(cmd)}",
            AsrsCommandType.Retrieve => $"RETRIEVE:{RequireCol(cmd)}:{RequireRow(cmd)}",
            AsrsCommandType.Home => "HOME",
            AsrsCommandType.Status => "STATUS",
            _ => throw new InvalidOperationException($"Unsupported command type: {cmd.Type}")
        };
    }

    private static int RequireRow(AsrsCommand cmd)
    {
        if (!cmd.Row.HasValue)
            throw new InvalidOperationException($"Command {cmd.Id} has null Row for {cmd.Type}.");
        return cmd.Row.Value;
    }

    private static int RequireCol(AsrsCommand cmd)
    {
        if (!cmd.Col.HasValue)
            throw new InvalidOperationException($"Command {cmd.Id} has null Col for {cmd.Type}.");
        return cmd.Col.Value;
    }

    private async Task WaitAndProcessArduinoResponsesAsync(
        SerialPort serial,
        int commandId,
        AsrsCommandType commandType,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < deadline)
        {
            string? line = null;
            try
            {
                line = serial.ReadLine();
            }
            catch (TimeoutException)
            {
                // read polling
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var raw = line.Trim();
            var upper = raw.ToUpperInvariant();
            _logger.LogInformation("Arduino -> {Line} (CommandId={CommandId})", raw, commandId);

            if (upper == "BUSY")
            {
                await MarkBusyAsync(commandId, cancellationToken);
                continue;
            }

            if (upper.StartsWith("OK"))
            {
                if (IsTerminalOkForCommand(upper, commandType))
                {
                    await MarkDoneAsync(commandId, cancellationToken);
                    return;
                }

                _logger.LogWarning(
                    "Arduino returned non-terminal OK for command type {CommandType}: {Line} (CommandId={CommandId})",
                    commandType, raw, commandId);
                continue;
            }

            if (upper.StartsWith("ERR") || upper.StartsWith("ERROR"))
            {
                await MarkFailedAsync(commandId, raw, cancellationToken);
                return;
            }

            if (upper == "READY")
            {
                // Some sketches send READY periodically; ignore.
                continue;
            }
        }
        await MarkFailedAsync(commandId, "TIMEOUT_WAITING_ARDUINO_RESPONSE", cancellationToken);
    }

    private static bool IsTerminalOkForCommand(string upperResponse, AsrsCommandType commandType)
    {
        // Accept plain OK as completion for compatibility with minimal sketches.
        if (upperResponse == "OK")
            return true;

        return commandType switch
        {
            AsrsCommandType.Store => upperResponse.Contains("STORE_DONE"),
            AsrsCommandType.Retrieve => upperResponse.Contains("RETRIEVE_DONE"),
            AsrsCommandType.Home => upperResponse.Contains("HOMED"),
            AsrsCommandType.Status => true,
            _ => false
        };
    }

    private async Task MarkBusyAsync(int commandId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cmd = await db.AsrsCommands.FirstOrDefaultAsync(x => x.Id == commandId, cancellationToken);
        if (cmd is null)
            return;
        cmd.Status = AsrsCommandStatus.Busy;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkDoneAsync(int commandId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cmd = await db.AsrsCommands.FirstOrDefaultAsync(x => x.Id == commandId, cancellationToken);
        if (cmd is null)
            return;

        cmd.Status = AsrsCommandStatus.Done;
        cmd.CompletedAt = DateTime.UtcNow;

        if (cmd.Row.HasValue && cmd.Col.HasValue)
        {
            var cell = await db.RackCells.FirstOrDefaultAsync(
                x => x.Row == cmd.Row.Value && x.Col == cmd.Col.Value,
                cancellationToken);
            if (cell is not null)
            {
                if (cmd.Type == AsrsCommandType.Store)
                    cell.IsOccupied = true;
                if (cmd.Type == AsrsCommandType.Retrieve)
                    cell.IsOccupied = false;
                cell.LastCommandId = cmd.Id;
                cell.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkFailedAsync(int commandId, string error, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cmd = await db.AsrsCommands.FirstOrDefaultAsync(x => x.Id == commandId, cancellationToken);
        if (cmd is null)
            return;
        cmd.Status = AsrsCommandStatus.Failed;
        cmd.Error = error;
        cmd.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void CloseAndDispose(ref SerialPort? serial)
    {
        if (serial is null)
            return;

        try { serial.Close(); } catch { /* ignore */ }
        try { serial.Dispose(); } catch { /* ignore */ }
        serial = null;
    }
}
