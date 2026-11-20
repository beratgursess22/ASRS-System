# Raspberry Pi RFID Bridge

This folder contains the Raspberry Pi applications that connect the MFRC522 RFID reader and Arduino to the ERP-ASRS API. The same gateway is used for Edge and Cloud experiments; only the API address changes.

## Technologies

- Python 3
- MFRC522 RFID reader
- Raspberry Pi GPIO
- `requests`
- `mfrc522`
- `RPi.GPIO`
- `pyserial`
- systemd service

## Folder Structure

```text
raspberry/
|-- asrs_gateway.py             # API command polling, serial dispatch, and ACK
|-- rfid_bridge.py              # RFID reader loop and API POST client
|-- tests/                      # Software-only gateway tests
|-- requirements.txt            # Python dependencies
`-- asrs-rfid-bridge.service    # systemd service definition
```

## Role in the System

Raspberry Pi:

- Initializes the MFRC522 RFID reader.
- Reads RFID card UIDs.
- Removes the BCC byte when 5-byte UID responses contain a 4-byte UID plus checksum.
- Sends the UID to the API in hexadecimal format.
- Polls the API command queue.
- Sends accepted commands to Arduino over serial.
- Reports `BUSY`, `OK`, and `ERR` signals to the API.
- Persists dispatch state before serial output to prevent automatic duplicate movement.
- Writes timestamped gateway events as JSON Lines for later measurements.
- Prevents repeated sends of the same card within a short cooldown window.
- Writes success and error logs to stdout.
- Cleans up GPIO on shutdown.

The API/ERP layer makes the storage decision. The RFID bridge handles RFID input, while the gateway handles the command queue and Arduino communication. The Pi does not perform rack planning in Cloud mode.

## Main Application

`rfid_bridge.py` runs continuously and polls for RFID cards.

Default API endpoint:

```text
http://localhost:5217/api/asrs/rfid-scan
```

JSON payload:

```json
{
  "cardUid": "AA BB CC DD"
}
```

The API response is logged. Long response bodies are shortened in logs.

## Environment Variables

The application supports:

```text
ASRS_API_URL
ASRS_HTTP_TIMEOUT_SEC
ASRS_RFID_POLL_INTERVAL_SEC
ASRS_RFID_SAME_CARD_COOLDOWN_SEC
ASRS_ARCHITECTURE
ASRS_RFID_EVENTS_PATH
```

Defaults:

```text
ASRS_API_URL=http://localhost:5217/api/asrs/rfid-scan
ASRS_HTTP_TIMEOUT_SEC=8
ASRS_RFID_POLL_INTERVAL_SEC=0.2
ASRS_RFID_SAME_CARD_COOLDOWN_SEC=2.5
ASRS_ARCHITECTURE=edge
ASRS_RFID_EVENTS_PATH=~/.local/state/asrs-gateway/<architecture>/rfid-events.jsonl
```

## Dependencies

`requirements.txt`:

```text
requests==2.32.3
mfrc522==0.0.7
pyserial==3.5
```

Install:

```bash
pip install -r requirements.txt
```

## Running

Manual run:

```bash
python3 rfid_bridge.py
```

Run with a custom API address:

```bash
ASRS_API_URL=http://<api-host>:5217/api/asrs/rfid-scan python3 rfid_bridge.py
```

## Command Gateway

Dry-run is enabled by default. It exercises polling and acknowledgements without opening a serial port or sending a motor command. Use only a synthetic test command: the command is closed as failed with `DRY_RUN_NO_HARDWARE`, and rack occupancy is not changed.

```bash
ASRS_ARCHITECTURE=edge \
ASRS_API_BASE_URL=http://127.0.0.1:5217/api/asrs \
python3 asrs_gateway.py --once
```

Cloud mode uses the same program and changes only the API address:

```bash
ASRS_ARCHITECTURE=cloud \
ASRS_API_BASE_URL=http://<cloud-api-host>:5217/api/asrs \
python3 asrs_gateway.py --once
```

Physical serial dispatch must only be enabled while an operator is present and after the Arduino port has been verified:

```bash
ASRS_GATEWAY_DRY_RUN=false \
ASRS_SERIAL_PORT=/dev/ttyUSB0 \
python3 asrs_gateway.py
```

Gateway configuration:

```text
ASRS_API_BASE_URL
ASRS_ARCHITECTURE=edge|cloud
ASRS_GATEWAY_DRY_RUN=true|false
ASRS_SERIAL_PORT
ASRS_SERIAL_BAUD_RATE
ASRS_SERIAL_TIMEOUT_SEC
ASRS_HTTP_TIMEOUT_SEC
ASRS_COMMAND_POLL_INTERVAL_SEC
ASRS_ACK_RETRY_COUNT
ASRS_ACK_RETRY_DELAY_SEC
ASRS_GATEWAY_STATE_PATH
ASRS_GATEWAY_EVENTS_PATH
```

The default state and event files are stored under:

```text
~/.local/state/asrs-gateway/<architecture>/state.json
~/.local/state/asrs-gateway/<architecture>/events.jsonl
~/.local/state/asrs-gateway/<architecture>/rfid-events.jsonl
```

The event files contain UTC timestamps, monotonic timestamps, request durations, command IDs, serial events, and the selected architecture. These are measurement records; software-only dry runs and hardware experiments must be stored and reported separately.

If the gateway loses the serial connection after starting a command, it preserves an unresolved state and blocks later commands. Do not delete or edit the state file until the physical rack and API command status have been manually reconciled.

Edge and Cloud use separate state directories because their databases can generate the same command IDs. When the database baseline is restored between experimental runs, assign a new run-specific `ASRS_GATEWAY_STATE_PATH` only after confirming that no physical command is in progress. Keep the old state and event files with that run's results.

The project uses one active Pi gateway. Command polling leaves a command queued until the gateway reports an Arduino signal, so a lost HTTP polling response cannot silently strand the command as sent. Do not run multiple gateway instances against the same API.

## systemd Service

Service file:

```text
asrs-rfid-bridge.service
```

The current service definition uses:

```text
WorkingDirectory=/home/isu/Desktop/ASRS-System/raspberry
ExecStart=/usr/bin/python3 /home/isu/Desktop/ASRS-System/raspberry/rfid_bridge.py
```

If the project path or Linux user is different, update `User`, `WorkingDirectory`, and `ExecStart`.

Install service:

```bash
sudo cp asrs-rfid-bridge.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable asrs-rfid-bridge
sudo systemctl start asrs-rfid-bridge
```

Follow logs:

```bash
journalctl -u asrs-rfid-bridge -f
```

Check service status:

```bash
systemctl status asrs-rfid-bridge
```

## API Interaction

Raspberry Pi sends the UID to `POST /api/asrs/rfid-scan`. The API then:

1. Normalizes the UID.
2. Looks for an active `RfidRackMap`.
3. Queues a `Store` command if the mapped rack cell is empty.
4. Records the event as `RfidEvent`.

## Development Notes

- `AsrsSerial:Enabled` must be `false` in the API when the Pi gateway is used.
- Edge and Cloud experiments must use the same gateway settings except for the API address and architecture label.
- Run software-only checks with `python3 -m unittest discover -s tests -v` from this directory.
- SPI and GPIO permissions must be configured correctly on the physical Raspberry Pi.
