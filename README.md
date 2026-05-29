# ASRS-System

ASRS-System is an integrated smart warehouse project that combines an ERP management layer with a physical Automated Storage and Retrieval System (AS/RS). The system is built from three main parts: a .NET-based ERP/Web/API application, Arduino-based motion control firmware, and a Raspberry Pi RFID bridge.

## Project Purpose

The project was developed to manage products, materials, bills of materials, work orders, purchasing, suppliers, quality control, and ASRS rack operations from a single software platform. The ERP layer acts as the decision and record center, the Arduino executes physical movements, and the Raspberry Pi connects RFID scanning with the API layer.

## Main Folders

```text
ASRS-System/
|-- ERP-ASRS/      # .NET ERP, MVC web UI, API, business logic, EF Core data layer
|-- arduino/       # Arduino Mega/RAMPS motion-control firmware
|-- raspberry/     # Python RFID bridge between MFRC522 and ASRS.API
|-- docs/          # Final project report and report summary
`-- README.md      # General project documentation
```

## System Components

### ERP-ASRS

`ERP-ASRS/` is the software and decision center of the project. It contains the ASP.NET Core MVC web interface, ASP.NET Core Web API, business services, Entity Framework Core data model, and MySQL database integration.

Main modules:

- User, role, and department management
- Product and material management
- Bill of Materials (BOM)
- Work order management
- Purchase requests and purchase orders
- Supplier and supplier price management
- Quality inspection, defects, and CAPA actions
- ASRS rack cells, RFID mappings, command queue, and system status

Details: [ERP-ASRS/README.md](./ERP-ASRS/README.md)

### Arduino

`arduino/` contains the motion-control firmware for the ASRS prototype. The Arduino receives line-based serial commands and controls the stepper motors for a 4-column by 3-row rack structure. Supported commands are `STORE`, `RETRIEVE`, `HOME`, and `STATUS`.

Details: [arduino/README.md](./arduino/README.md)

### Raspberry Pi

`raspberry/` contains the Python RFID bridge. It reads card UIDs from an MFRC522 RFID reader and sends them to `ASRS.API` through HTTP POST requests. It also includes a systemd service definition for running the bridge continuously on a Raspberry Pi.

Details: [raspberry/README.md](./raspberry/README.md)

## Project Report

The complete technical report is stored under the `docs/` folder:

- [Project Report Summary](./docs/README.md)
- [SmartRack Final Report PDF](./docs/SmartRack_FinalReport.pdf)

The PDF report is available at `docs/SmartRack_FinalReport.pdf`.

## System Workflow

<img width="793" height="529" alt="Screenshot 2026-05-24 at 18 53 10" src="https://github.com/user-attachments/assets/b597d63e-039d-4593-a2d9-236eaceb679d" />


## Technology Stack

- .NET 9
- ASP.NET Core MVC
- ASP.NET Core Web API
- Entity Framework Core 9
- Pomelo EntityFrameworkCore MySQL Provider
- MySQL
- ASP.NET Core Identity
- Razor Views, Bootstrap, CSS, JavaScript
- Python 3
- `requests`, `mfrc522`, `RPi.GPIO`
- Arduino C/C++
- Arduino Mega, RAMPS, DRV8825/A4988-style stepper drivers
- MFRC522 RFID reader

## How to Run

This project can be run in two different modes:

1. **Software-only mode:** Runs the ERP web application, API, and MySQL database. This mode is enough to review and test the software modules without Arduino or Raspberry Pi.
2. **Full hardware mode:** Runs the complete ASRS prototype with Arduino motion control and Raspberry Pi RFID scanning.

### 1. Requirements

Install the following tools before running the project:

- .NET 9 SDK
- MySQL Server 8.x
- Python 3
- Arduino IDE, only for hardware mode
- Raspberry Pi with SPI/GPIO enabled, only for hardware mode
- Arduino Mega/RAMPS board, stepper drivers, motors, limit switches, MFRC522 RFID reader, and required wiring, only for hardware mode

### 2. Database Setup

Create the MySQL database and user:

```sql
CREATE DATABASE asrs_db;

CREATE USER 'asrs_user'@'localhost' IDENTIFIED BY '123456';
GRANT ALL PRIVILEGES ON asrs_db.* TO 'asrs_user'@'localhost';
FLUSH PRIVILEGES;
```

Creating the database only creates an empty database. The project tables are created by running the Entity Framework migration command in the next steps.

The default connection string is stored in:

```text
ERP-ASRS/ASRS.Web/appsettings.json
ERP-ASRS/ASRS.API/appsettings.json
```

Default connection string:

```text
Server=localhost;Database=asrs_db;User=asrs_user;Password=123456;
```

If your MySQL username or password is different, update the connection string in both files.

### 3. Build the ERP Solution

From the root project folder:

```bash
cd ERP-ASRS
dotnet restore
dotnet build
```

### 4. Apply Database Migrations

Run the Entity Framework migrations to create the required tables inside `asrs_db`:

```bash
dotnet ef database update --project ASRS.DAL
```

If `dotnet ef` is not installed, install it first:

```bash
dotnet tool install --global dotnet-ef
```

After this step, the database tables such as users, products, materials, work orders, rack cells, RFID mappings, and ASRS commands will be created.

### 5. Run the API

The API handles RFID scan requests, ASRS commands, rack state, and optional Arduino serial communication.

Open a terminal and run:

```bash
cd ERP-ASRS/ASRS.API
dotnet run
```

Default API URL:

```text
http://localhost:5217
```

Swagger UI is available when the API is running.

### 6. Run the Web Application

Open a second terminal and run:

```bash
cd ERP-ASRS/ASRS.Web
dotnet run
```

Default web URL:

```text
http://localhost:5222
```

The web application communicates with the API through this setting:

```text
ERP-ASRS/ASRS.Web/appsettings.json
```

Default API base URL:

```text
http://localhost:5217/
```

For normal software testing, keep both `ASRS.API` and `ASRS.Web` running at the same time.

### 7. Software-only Test Flow

In software-only mode, Arduino and Raspberry Pi are not required.

Recommended run order:

1. Start MySQL.
2. Apply database migrations.
3. Run `ASRS.API`.
4. Run `ASRS.Web`.
5. Open the web application in the browser.
6. Test ERP modules such as products, materials, work orders, purchasing, suppliers, quality control, and ASRS rack screens.

The API can run without Arduino because serial communication is disabled by default:

```json
"AsrsSerial": {
  "Enabled": false
}
```

### 8. Full Hardware Run Flow

Use this mode only when the physical ASRS prototype is connected.

Recommended run order:

1. Start MySQL.
2. Run `ASRS.API`.
3. Run `ASRS.Web`.
4. Upload the Arduino firmware.
5. Connect Arduino to the API machine through USB serial.
6. Connect the Raspberry Pi RFID reader.
7. Run the Raspberry Pi RFID bridge.
8. Scan an RFID card and observe the ASRS command flow.

### 9. Arduino Setup

Open the Arduino firmware in Arduino IDE:

```text
arduino/ASRS_Main/ASRS_Main_Single.ino
```

Before running the physical system, connect and check:

- Arduino Mega/RAMPS board
- Stepper motor drivers
- X, Y, and Z stepper motors
- X and Z limit switches
- External motor power supply
- USB serial connection between Arduino and the API machine

Upload the firmware to the Arduino board.

The Arduino receives line-based serial commands:

```text
STORE:<col>:<row>
RETRIEVE:<col>:<row>
HOME
STATUS
```

Rack coordinates are zero-based:

```text
col: 0..3
row: 0..2
```

If the API will send commands directly to Arduino, update `ERP-ASRS/ASRS.API/appsettings.json`:

```json
"AsrsSerial": {
  "Enabled": true,
  "PortName": "/dev/ttyUSB1",
  "BaudRate": 9600,
  "PollIntervalMs": 400,
  "CommandTimeoutSec": 180
}
```

On Windows, the serial port may look like:

```text
COM3
```

On Linux/Raspberry Pi, it may look like:

```text
/dev/ttyUSB0
/dev/ttyUSB1
/dev/ttyACM0
```

### 10. Raspberry Pi RFID Bridge Setup

This step is required only for RFID hardware mode.

Connect the MFRC522 RFID reader to the Raspberry Pi using SPI wiring. SPI must be enabled on the Raspberry Pi.

Install Python dependencies:

```bash
cd raspberry
pip install -r requirements.txt
```

Run the RFID bridge:

```bash
python3 rfid_bridge.py
```

By default, the bridge sends RFID scans to:

```text
http://localhost:5217/api/asrs/rfid-scan
```

If the API is running on another computer, provide the API address manually:

```bash
ASRS_API_URL=http://<api-host>:5217/api/asrs/rfid-scan python3 rfid_bridge.py
```

Example:

```bash
ASRS_API_URL=http://192.168.1.25:5217/api/asrs/rfid-scan python3 rfid_bridge.py
```

### 11. Complete System Flow

When the full system is running:

1. The Raspberry Pi reads an RFID card UID.
2. The Raspberry Pi sends the UID to `ASRS.API`.
3. The API checks the RFID-to-rack mapping in MySQL.
4. The API creates a store command for the related rack cell.
5. The Arduino receives the command through serial communication.
6. The Arduino moves the ASRS mechanism and returns a status response.
7. The API updates the command and rack state in the database.
8. The web application displays the updated ASRS state.

### 12. Important Notes

- MySQL must be running before starting `ASRS.API` or `ASRS.Web`.
- `ASRS.API` should be started before the Raspberry Pi RFID bridge.
- `ASRS.Web` and `ASRS.API` should run at the same time for normal testing.
- The system can be reviewed in software-only mode without Arduino and Raspberry Pi.
- Physical hardware should not be powered before checking motor driver wiring, power supply polarity, limit switches, and safe power-off conditions.
- If the API cannot connect to MySQL, check the connection string in both `appsettings.json` files.
- If Arduino does not respond, check the serial port name, baud rate, USB connection, and firmware upload.

## Documentation Note

This file provides the general system overview. Each subfolder has its own README file explaining its folder structure, responsibilities, technologies, and integration points in more detail.
