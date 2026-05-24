# ERP-ASRS

`ERP-ASRS` contains the .NET-based ERP, web interface, API, business logic, and database layers of ASRS-System. It is the software decision center of the project: user operations, stock and production flows, purchasing, quality control, and ASRS commands are managed here.

## Technologies

- .NET 9
- ASP.NET Core MVC
- ASP.NET Core Web API
- ASP.NET Core Identity
- Entity Framework Core 9
- Pomelo EntityFrameworkCore MySQL Provider
- MySQL
- Swagger / Swashbuckle
- System.IO.Ports
- Razor Views, Bootstrap, CSS, JavaScript

## Folder Structure

```text
ERP-ASRS/
|-- ASRS.Core/      # Entities, DTOs, enums, and service interfaces
|-- ASRS.DAL/       # AppDbContext, EF Core configuration, migrations
|-- ASRS.BLL/       # Business rules and service implementations
|-- ASRS.Web/       # ASP.NET Core MVC web interface
|-- ASRS.API/       # RFID, ASRS command queue, and serial communication API
|-- ASRS.sln        # Visual Studio/.NET solution file
|-- README.md       # This document
`-- *.txt           # Architecture and integration notes
```

## Layered Architecture

```text
ASRS.Web --\
            +-- ASRS.BLL -- ASRS.DAL -- ASRS.Core
ASRS.API --/
```

`ASRS.Web` and `ASRS.API` use business rules through the BLL layer. Database access is centralized in the DAL layer. The Core layer contains the shared domain models and contracts.

## Projects

### ASRS.Core

The core domain layer contains entity classes, DTOs, enums, and service interfaces.

Important entity groups:

- Identity and organization: `AppUser`, `AppRole`, `Department`
- Catalog and stock: `Product`, `Material`, `BillOfMaterial`
- Production: `WorkOrder`
- Purchasing: `PurchaseRequest`, `PurchaseRequestItem`, `PurchaseOrder`, `PurchaseOrderItem`
- Suppliers: `Supplier`, `SupplierItemPrice`
- Quality: `QualityInspection`, `QualityInspectionItem`, `QualityDefect`, `CapaAction`
- ASRS integration: `RackCell`, `RfidRackMap`, `AsrsCommand`, `RfidEvent`

### ASRS.DAL

The data access layer contains `AppDbContext`, Entity Framework Core configuration, and migrations. `AppDbContext` manages both ASP.NET Identity tables and project-specific tables.

This layer includes:

- DbSet definitions
- Entity relationships
- Unique indexes
- EF Core migration files
- 3x4 rack cell seed data

Active DbSets:

```text
Departments, Products, Materials, BillOfMaterials, WorkOrders,
PurchaseRequests, PurchaseRequestItems, PurchaseOrders, PurchaseOrderItems,
Suppliers, SupplierItemPrices, QualityInspections, QualityInspectionItems,
QualityDefects, CapaActions, RackCells, RfidRackMaps, AsrsCommands, RfidEvents
```

### ASRS.BLL

The business logic layer contains the service implementations used by the controllers.

Services:

- `UserService`
- `ProductService`
- `MaterialService`
- `BomService`
- `WorkOrderService`
- `PurchaseRequestService`
- `PurchaseOrderService`
- `SupplierService`
- `QualityInspectionService`
- `QualityDefectService`
- `CapaService`

### ASRS.Web

The web application is built with ASP.NET Core MVC, Razor Views, Bootstrap, and custom CSS/JavaScript.

Controller groups:

- `AccountController`
- `DashboardController`
- `UserController`
- `ProductController`
- `MaterialController`
- `WorkOrderController`
- `PurchaseRequestController`
- `PurchaseOrderController`
- `SupplierController`
- `QualityController`
- `CapaController`
- `AsrsProxyController`

The web application is also configured to serve `.step` and `.stp` model files. ASRS model and viewer assets are stored under `wwwroot/models/` and `wwwroot/3d/step-viewer/`.

### ASRS.API

The API layer handles hardware integration and ASRS command management. Swagger is enabled. The API connects to the MySQL database and can optionally manage Arduino serial communication through the `AsrsSerialWorker` background service.

Important endpoints:

```text
POST /api/asrs/rfid-scan
POST /api/asrs/retrieve
GET  /api/asrs/commands/next
POST /api/asrs/commands/{id}/ack
GET  /api/asrs/rack-state
GET  /api/asrs/system-status
GET  /api/asrs/rfid-maps
```

## ASRS Integration Logic

RFID-based storage flow:

1. Raspberry Pi sends the scanned card UID to `POST /api/asrs/rfid-scan`.
2. The API normalizes the UID with `RfidUidNormalizer`.
3. The API looks for an active UID-to-rack mapping in `RfidRackMaps`.
4. If the mapped `RackCell` is empty, a `Store` command is created in `AsrsCommands`.
5. The event is recorded in `RfidEvents`.

Retrieval flow:

1. The dashboard or API sends row/column information to `POST /api/asrs/retrieve`.
2. If the selected rack cell is occupied, a `Retrieve` command is queued.
3. When the command is completed, the rack cell is marked as empty.

There are two command execution modes:

- API serial worker mode: when `AsrsSerial:Enabled=true`, the API sends queued commands directly to Arduino over a serial port.
- Raspberry pull mode: when the worker is disabled, Raspberry Pi can pull commands from `/commands/next` and report results through `/ack`.

## ASRS Serial Worker Settings

Example configuration:

```json
{
  "AsrsSerial": {
    "Enabled": false,
    "PortName": "/dev/ttyUSB0",
    "BaudRate": 9600,
    "PollIntervalMs": 400,
    "CommandTimeoutSec": 180
  }
}
```

The worker sends commands to Arduino in this format:

```text
STORE:<col>:<row>
RETRIEVE:<col>:<row>
HOME
STATUS
```

Arduino responses such as `BUSY`, `OK:*`, `ERR:*`, and `READY` are used to update `AsrsCommand` status.

## Database

The database runs on MySQL. Connection strings are read from `appsettings.json`. If the API connection string is missing, the code falls back to:

```text
Server=localhost;Database=asrs_db;User=root;Password=123456;
```

Migration files are located in:

```text
ASRS.DAL/Migrations/
```

Apply migrations:

```bash
dotnet ef database update --project ASRS.DAL --startup-project ASRS.Web
```

## Running

Build the solution:

```bash
dotnet restore
dotnet build
```

Run the web application:

```bash
cd ASRS.Web
dotnet run
```

Run the API application:

```bash
cd ASRS.API
dotnet run
```

## Notes

- `ASRS.Web/Program.cs` contains extensive seed blocks, but they are currently commented out.
- `ASRS.API` seeds default RFID-rack mappings at startup through `AsrsRfidMapSeeder`.
- Rack coordinates are stored as zero-based values in code: `row=0..2`, `col=0..3`. UI layers may display them as one-based values when needed.
