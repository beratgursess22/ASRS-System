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

<img width="819" height="533" alt="Screenshot 2026-05-24 at 18 51 57" src="https://github.com/user-attachments/assets/a80ff730-2564-4ff9-8f19-513cf2c4a643" />


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

## Quick Start

Build the ERP solution:

```bash
cd ERP-ASRS
dotnet restore
dotnet build
```

Run the web application:

```bash
cd ERP-ASRS/ASRS.Web
dotnet run
```

Run the API application:

```bash
cd ERP-ASRS/ASRS.API
dotnet run
```

Run the Raspberry Pi RFID bridge:

```bash
cd raspberry
pip install -r requirements.txt
python3 rfid_bridge.py
```

Upload the Arduino firmware from `arduino/ASRS_Main/ASRS_Main_Single.ino` using the Arduino IDE. The modular source files are located under `Code/` and `library/`.

## Documentation Note

This file provides the general system overview. Each subfolder has its own README file explaining its folder structure, responsibilities, technologies, and integration points in more detail.
