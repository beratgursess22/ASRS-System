# ERP-ASRS

ERP-ASRS, ASRS-System projesinin .NET tabanli ERP, web arayuzu, API, is kurallari ve veritabani katmanlarini iceren cozumudur. Bu klasor yazilim tarafindaki karar merkezidir: kullanici islemleri, stok ve uretim surecleri, satin alma, kalite kontrol ve fiziksel ASRS mekanizmasina gidecek komutlar burada yonetilir.

## Kullanilan Teknolojiler

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

## Klasor Yapisi

```text
ERP-ASRS/
|-- ASRS.Core/      # Entity, DTO, enum ve servis interface tanimlari
|-- ASRS.DAL/       # AppDbContext, EF Core konfigurasyonu ve migration dosyalari
|-- ASRS.BLL/       # Is kurallari ve servis implementasyonlari
|-- ASRS.Web/       # ASP.NET Core MVC web arayuzu
|-- ASRS.API/       # RFID, ASRS komut kuyrugu ve seri haberlesme API'si
|-- ASRS.sln        # Visual Studio/.NET solution dosyasi
|-- README.md       # Bu dokuman
`-- *.txt           # Mimari ve entegrasyon notlari
```

## Katmanli Mimari

```text
ASRS.Web --\
            +-- ASRS.BLL -- ASRS.DAL -- ASRS.Core
ASRS.API --/
```

Bu yapiyla web arayuzu ve API, is kurallarini BLL uzerinden kullanir. Veritabani erisimi DAL katmaninda toplanir. Core katmani ise sistemin ortak modellerini ve sozlesmelerini barindirir.

## Projeler

### ASRS.Core

Sistemin cekirdek model katmanidir. Veritabani tablolarina karsilik gelen entity siniflari, katmanlar arasi veri tasiyan DTO'lar, enum'lar ve servis interface'leri burada bulunur.

Onemli entity gruplari:

- Kimlik ve organizasyon: `AppUser`, `AppRole`, `Department`
- Katalog ve stok: `Product`, `Material`, `BillOfMaterial`
- Uretim: `WorkOrder`
- Satin alma: `PurchaseRequest`, `PurchaseRequestItem`, `PurchaseOrder`, `PurchaseOrderItem`
- Tedarikci: `Supplier`, `SupplierItemPrice`
- Kalite: `QualityInspection`, `QualityInspectionItem`, `QualityDefect`, `CapaAction`
- ASRS entegrasyonu: `RackCell`, `RfidRackMap`, `AsrsCommand`, `RfidEvent`

### ASRS.DAL

Veritabani erisim katmanidir. `AppDbContext`, ASP.NET Identity tablolarini ve proje tablolarini ayni context uzerinde yonetir.

Bu katmanda:

- DbSet tanimlari
- Entity iliskileri
- Unique index tanimlari
- EF Core migration dosyalari
- 3x4 raf hucre seed'i

bulunur.

Aktif DbSet'ler:

```text
Departments, Products, Materials, BillOfMaterials, WorkOrders,
PurchaseRequests, PurchaseRequestItems, PurchaseOrders, PurchaseOrderItems,
Suppliers, SupplierItemPrices, QualityInspections, QualityInspectionItems,
QualityDefects, CapaActions, RackCells, RfidRackMaps, AsrsCommands, RfidEvents
```

### ASRS.BLL

Is kurallarinin uygulandigi katmandir. Controller'larin dogrudan veritabani mantigi yazmasi yerine servisler kullanilir.

Servisler:

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

Kullanici arayuzudur. ASP.NET Core MVC, Razor View ve Bootstrap/CSS ile gelistirilmistir.

Controller gruplari:

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

Web tarafinda ayrica `.step` ve `.stp` dosyalari static olarak yayinlanacak sekilde ayarlanmistir. `wwwroot/models/` ve `wwwroot/3d/step-viewer/` altinda ASRS sistem model gosterimi icin dosyalar bulunur.

### ASRS.API

Donanim entegrasyonu ve ASRS komut yonetimi icin kullanilan API katmanidir. Swagger aktiftir. API, MySQL veritabanina baglanir ve `AsrsSerialWorker` background service'i ile Arduino seri haberlesmesini opsiyonel olarak dogrudan yonetebilir.

Onemli endpoint'ler:

```text
POST /api/asrs/rfid-scan
POST /api/asrs/retrieve
GET  /api/asrs/commands/next
POST /api/asrs/commands/{id}/ack
GET  /api/asrs/rack-state
GET  /api/asrs/system-status
GET  /api/asrs/rfid-maps
```

## ASRS Entegrasyon Mantigi

RFID depolama akisinda:

1. Raspberry Pi kart UID bilgisini `POST /api/asrs/rfid-scan` endpoint'ine gonderir.
2. API UID bilgisini `RfidUidNormalizer` ile normalize eder.
3. `RfidRackMaps` tablosunda aktif UID-raf eslesmesi aranir.
4. Eslesen `RackCell` bos ise `AsrsCommand` tablosuna `Store` komutu eklenir.
5. `RfidEvent` ile olay kaydi tutulur.

Geri alma akisinda:

1. Web dashboard veya API `POST /api/asrs/retrieve` ile row/col bilgisi gonderir.
2. Ilgili raf hucresi doluysa `Retrieve` komutu kuyruga alinir.
3. Komut tamamlaninca raf hucresi bos olarak isaretlenir.

Komut calistirma icin iki mod vardir:

- API seri worker modu: `AsrsSerial:Enabled=true` ise API, kuyruktaki komutu dogrudan seri porttan Arduino'ya gonderir.
- Raspberry pull modu: worker kapaliyken Raspberry Pi `/commands/next` ile komut cekebilir ve `/ack` ile sonucu API'ye bildirebilir.

## ASRS Seri Worker Ayarlari

`ASRS.API/appsettings.json` veya environment konfigurasyonunda kullanilan ayarlar:

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

Worker Arduino'ya su formatta komut gonderir:

```text
STORE:<col>:<row>
RETRIEVE:<col>:<row>
HOME
STATUS
```

Arduino'dan gelen `BUSY`, `OK:*`, `ERR:*` ve `READY` cevaplarina gore `AsrsCommand` durumu guncellenir.

## Veritabani

Veritabani MySQL uzerindedir. Varsayilan connection string `appsettings.json` icinden okunur. API tarafinda connection string bulunamazsa kodda fallback olarak su deger kullanilir:

```text
Server=localhost;Database=asrs_db;User=root;Password=123456;
```

Migration dosyalari:

```text
ASRS.DAL/Migrations/
```

Veritabani guncellemek icin:

```bash
dotnet ef database update --project ASRS.DAL --startup-project ASRS.Web
```

## Calistirma

Cozumu derlemek icin:

```bash
dotnet restore
dotnet build
```

Web uygulamasi:

```bash
cd ASRS.Web
dotnet run
```

API uygulamasi:

```bash
cd ASRS.API
dotnet run
```

## Notlar

- `ASRS.Web/Program.cs` icinde kapsamli seed bloklari vardir; mevcut durumda yorum satiri halindedir.
- `ASRS.API` acilista varsayilan RFID-raf eslesmelerini `AsrsRfidMapSeeder` ile seed eder.
- Raf modeli kod tarafinda 0-based tutulur: `row=0..2`, `col=0..3`. UI tarafinda gerekiyorsa 1-based gosterim yapilir.
