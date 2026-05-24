# Raspberry Pi RFID Bridge

This folder contains the Python bridge application that connects an MFRC522 RFID reader to the ERP-ASRS API. The Raspberry Pi reads card UIDs and reports them to the API.

## Technologies

- Python 3
- MFRC522 RFID reader
- Raspberry Pi GPIO
- `requests`
- `mfrc522`
- `RPi.GPIO`
- systemd service

## Folder Structure

```text
raspberry/
|-- rfid_bridge.py              # RFID reader loop and API POST client
|-- requirements.txt            # Python dependencies
`-- asrs-rfid-bridge.service    # systemd service definition
```

## Role in the System

Raspberry Pi:

- Initializes the MFRC522 RFID reader.
- Reads RFID card UIDs.
- Removes the BCC byte when 5-byte UID responses contain a 4-byte UID plus checksum.
- Sends the UID to the API in hexadecimal format.
- Prevents repeated sends of the same card within a short cooldown window.
- Writes success and error logs to stdout.
- Cleans up GPIO on shutdown.

The API/ERP layer makes the storage decision. In the current implementation, the Raspberry Pi bridge only handles RFID reading and HTTP POST communication.

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
```

Defaults:

```text
ASRS_API_URL=http://localhost:5217/api/asrs/rfid-scan
ASRS_HTTP_TIMEOUT_SEC=8
ASRS_RFID_POLL_INTERVAL_SEC=0.2
ASRS_RFID_SAME_CARD_COOLDOWN_SEC=2.5
```

## Dependencies

`requirements.txt`:

```text
requests==2.32.3
mfrc522==0.0.7
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

- The current Python bridge does not send serial commands directly to Arduino.
- Arduino serial communication can be handled by `AsrsSerialWorker` in the API.
- An alternative architecture can extend Raspberry Pi to pull commands from `/api/asrs/commands/next` and send them to Arduino over serial.
- SPI and GPIO permissions must be configured correctly on the physical Raspberry Pi.
