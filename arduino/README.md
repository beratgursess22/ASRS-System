# Arduino ASRS Motion-Control Firmware

This folder contains the Arduino firmware that controls the physical movement of the ASRS prototype. Arduino does not make storage decisions; it executes serial commands and drives the stepper motors to store or retrieve items.

## Hardware and Technologies

- Arduino Mega
- RAMPS pin layout
- Stepper motors
- DRV8825/A4988-style stepper drivers
- X and Z limit switches
- GT2 belt/pulley based step-per-millimeter calculation
- Arduino C/C++
- USB/serial communication

## Folder Structure

```text
arduino/
|-- ASRS_Main/
|   |-- ASRS_Main_Single.ino              # Single-file Arduino IDE version
|   |-- Code/
|   |   |-- main.cpp                      # Main loop and command handling
|   |   |-- serial_protocol.cpp           # Serial command parser and responses
|   |   |-- operations.cpp                # STORE/RETRIEVE operations
|   |   |-- axes.cpp                      # X/Y/Z motion and homing
|   |   `-- stepper.cpp                   # Low-level stepper motor helpers
|   |-- library/
|   |   |-- config.h                      # Pins, speeds, rack positions, calibration
|   |   |-- serial_protocol.h
|   |   |-- operations.h
|   |   |-- axes.h
|   |   `-- stepper.h
|   `-- STORE_RETRIEVE_COMMAND_GUIDE.txt  # Command and integration guide
`-- RASPBERRY_PI_INTEGRATION_NOTES.txt
```

## Role in the System

Arduino performs the following tasks:

- Reads commands from the serial port.
- Parses command lines.
- Homes the X and Z axes using limit switches.
- Moves the X axis to the target rack column.
- Moves the Z axis to the target rack row.
- Uses the Y axis to push an item into the rack or retrieve it.
- Reports operation status through the serial port.

The ERP/API layer decides what should happen. Arduino only executes `STORE`, `RETRIEVE`, `HOME`, and `STATUS` commands.

## Supported Commands

Commands use a line-based text protocol. Each command must end with a newline.

```text
STORE:<col>:<row>
RETRIEVE:<col>:<row>
HOME
STATUS
```

Examples:

```text
STORE:0:2
RETRIEVE:1:0
HOME
STATUS
```

## Rack Indexing

Rack coordinates are zero-based in firmware.

```text
col: 0..3
row: 0..2
```

The physical rack contains 4 columns and 3 rows. If the UI displays one-based coordinates, conversion is required:

```text
UI col=1 -> Arduino col=0
UI row=3 -> Arduino row=2
```

## Serial Responses

Arduino reports operation status with:

```text
READY
BUSY
OK:STORE_DONE
OK:RETRIEVE_DONE
ERR:<error_message>
```

`ASRS.API` uses these responses to update `AsrsCommand` status when `AsrsSerialWorker` is enabled.

## Calibration and Configuration

Main configuration file:

```text
ASRS_Main/library/config.h
```

This file defines:

- RAMPS pin assignments
- X, Y, and Z step/dir/enable pins
- X and Z limit switch pins
- Step-per-millimeter value
- Movement speeds
- Maximum axis travel distances
- Rack column and row positions
- Y-axis travel distance
- Entry and exit target Z levels
- Serial baud rate

Current rack constants:

```text
SHELF_COLS = 4
SHELF_ROWS = 3
SHELF_X_POS = 160, 320, 480, 640 mm
SHELF_Z_POS = 250, 500, 750 mm
SERIAL_BAUD_RATE = 9600
STEPS_PER_MM = 160
```

Values that should be verified on the physical prototype:

- `SHELF_X_POS`
- `SHELF_Z_POS`
- `Y_TRAVEL_MM`
- `Z_APPROACH_OFFSET_MM`
- `ENTRY_PICK_TARGET_Z_MM`
- `EXIT_DROP_TARGET_Z_MM`

## STORE Flow

When `STORE:<col>:<row>` is received:

1. The command and rack range are validated.
2. Arduino returns `BUSY`.
3. The required reference and motion sequence starts.
4. The package is picked from the entry point.
5. The X axis moves to the target column.
6. The Z axis moves to the target row.
7. The Y axis places the package into the rack.
8. Arduino returns `OK:STORE_DONE` on success or `ERR:*` on failure

<img width="571" height="453" alt="Screenshot 2026-05-24 at 18 04 35" src="https://github.com/user-attachments/assets/a937e36b-5ed1-42bc-b826-01ba9c9faaf2" />


## RETRIEVE Flow

When `RETRIEVE:<col>:<row>` is received:

1. The command and rack range are validated.
2. Arduino returns `BUSY`.
3. X/Z axes move to the target rack cell.
4. The Y axis retrieves the package from the rack.
5. The package is moved to the exit/drop-off point.
6. Arduino returns `OK:RETRIEVE_DONE` on success or `ERR:*` on failure.

<img width="562" height="535" alt="Screenshot 2026-05-24 at 18 04 41" src="https://github.com/user-attachments/assets/6c57ae05-2c5f-4bbb-9692-06b8d03069fb" />


## Development Notes

- `ASRS_Main_Single.ino` is kept for quick Arduino IDE uploads.
- The modular structure under `Code/` and `library/` improves readability and maintenance.
- ERP/API command formats and the Arduino parser must stay compatible.
- Arduino does not read RFID UIDs; RFID scanning is handled by Raspberry Pi.
