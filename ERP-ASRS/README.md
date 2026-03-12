# ASRS-ERP Sistem — Kapsamlı Dokümantasyon

**Proje Tarihi:** March 12, 2026  
**Mimari:** Katmanlı Mimari (Layered Architecture) + Clean Architecture  
**Teknoloji Stack:** .NET 9, ASP.NET Core MVC, Entity Framework Core, MySQL 8.0

---

## 📋 İçindekiler

1. [Sistem Mimarisi](#sistem-mimarisi)
2. [Data Layer — Entities & DTOs](#data-layer--entities--dtos)
3. [Business Logic Layer — Services](#business-logic-layer--services)
4. [Presentation Layer — Controllers & Views](#presentation-layer--controllers--views)
5. [Database Schema & Relationships](#database-schema--relationships)
6. [Ürün-Malzeme-BOM Modülü Detayı](#ürün-malzeme-bom-modülü-detayı)
7. [Stock Kontrol Mekanizması](#stock-kontrol-mekanizması)
8. [API Integrasyon Akışı](#api-integrasyon-akışı)

---

## 🏗️ Sistem Mimarisi

ASRS-ERP sistemi katmanlı (layered) mimariye uygun şekilde tasarlanmıştır. Bu mimari dört ana seviye yerine getirir:

**Presentation Layer (ASRS.Web + ASRS.API):** Kontroller, Razor view'ları (.cshtml) ve API endpoint'leri kullanıcı arayüzü oluşturur.

**Business Logic Layer (ASRS.BLL):** Service sınıfları (UserService, ProductService, MaterialService vb.), iş kuralları ve algoritma implementasyonları burada yer alır. Presentation Layer'dan IService interface'leri aracılığıyla erişilir.

**Data Access Layer (ASRS.DAL):** AppDbContext (Entity Framework), GenericRepository<T> pattern'i ve database migration'ları veri erişim işlemlerini yönetir. BLL'den DbContext ve Repository aracılığıyla erişilir. MySQL 8.0 veritabanı bu seviyede iletişim kurar.

**Core Layer (ASRS.Core):** Entity sınıfları (POCO), DTO'lar (Data Transfer Objects), enum'lar (iş kuralı sabitler) ve service kontraktlarını tanımlayan interface'ler bu katmanda bulunur.

Bu katmanlı yapı, kodu bakım ve genişletme için kolaylaştırır. Her katman bağımsız olarak geliştirilebilir ve test edilebilir.

---

## 📊 Data Layer — Entities & DTOs

### 🔹 ENTITIES (Veritabanı Tabloları)

#### **1. Kimlik Doğrulama & Kullanıcı Yönetimi**

**AppUser Tablosu:** Kullanıcıları temsil eder. Id (birincil anahtar), UserName (benzersiz), Email (benzersiz), FirstName, LastName, DepartmentId (departmana bağlantı), IsActive (etkinlik durumu), CreatedAt (oluşturma zamanı) ve PasswordHash (ASP.NET Identity tarafından yönetilen şifre) alanlarına sahiptir.

**AppRole Tablosu:** Rolleri temsil eder. Id (birincil anahtar), Name (rol adı: Yönetici, Depo, Lojistik, Üretim, Kalite, Montaj), Description (açıklama) ve NormalizedName (ASP.NET Identity tarafından yönetilen normalizasyon) alanlarına sahiptir.

**Roller Listesi:** Yönetici (tam yetki), Depo (stok yönetimi), Lojistik (lojistik işlemleri), Üretim (üretim siparişleri), Kalite (kalite kontrolü), Montaj (montaj işlemleri).

#### **2. Örgütsel Yapı**

**Department Tablosu:** Departmanları temsil eder. Id (birincil anahtar), Name (departman adı), Description (açıklama), IsActive (etkinlik durumu), CreatedAt (oluşturma zamanı) alanlarına sahiptir. Kullanıcılar (AppUser) ve üretim siparişleri (WorkOrder) bu departmana bağlıdır (1:N ilişki).

#### **3. Ürün & Malzeme Yönetimi** ⭐ **YENİ MODÜL**

```
┌──────────────────────────────────────────────────────────┐
│                 Product (Üretilmiş Ürün)                │
├──────────────────────────────────────────────────────────┤
│ Id (PK)                   │ int                          │
│ Code (unique)             │ string (PRD-001)             │
│ Name                      │ string                       │
│ Category                  │ string                       │
│ Unit                      │ string (adet, kg, lt, m)     │
│ StockQuantity             │ int                          │
│ MinStockLevel             │ int                          │
│ Description               │ string?                      │
│ IsActive                  │ bool                         │
│ CreatedAt                 │ DateTime                     │
│                                                          │
│ Relationships:                                           │
│ ├─ BillOfMaterials (1:N) → BillOfMaterial               │
│ └─ WorkOrders (1:N) → WorkOrder                         │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│              Material (Ham Madde/Bileşen)                │
├──────────────────────────────────────────────────────────┤
│ Id (PK)                   │ int                          │
│ Code (unique)             │ string (MAT-001)             │
│ Name                      │ string                       │
│ Unit                      │ string (adet, kg, lt, m)     │
│ StockQuantity             │ int                          │
│ MinStockLevel             │ int                          │
│ Description               │ string?                      │
│ IsActive                  │ bool                         │
│ CreatedAt                 │ DateTime                     │
│                                                          │
│ Relationships:                                           │
│ └─ BillOfMaterials (1:N) → BillOfMaterial               │
└──────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────┐
│          BillOfMaterial (Ürün Reçetesi/BOM)             │
├──────────────────────────────────────────────────────────┤
│ Id (PK)                   │ int                          │
│ ProductId (FK)            │ int → Product                │
│ ComponentProductId (FK)   │ int? → Product (nullable)    │
│ MaterialId (FK)           │ int? → Material (nullable)   │
│ RequiredQuantity          │ int (gereken miktar)         │
│ Notes                     │ string? (açıklama)           │
│ CreatedAt                 │ DateTime                     │
│                                                          │
│ Relationships:                                           │
│ ├─ Product (N:1)          → Product (Ana Ürün)          │
│ ├─ ComponentProduct (N:1) → Product (Bileşen Ürün)      │
│ └─ Material (N:1)         → Material (Ham Madde)         │
│                                                          │
│ ANAHTAR KURALI:                                          │
│ ComponentProductId XOR MaterialId (biri NULL olmak zorunda)
└──────────────────────────────────────────────────────────┘
```

#### **4. İş Emirleri**
⭐ **YENİ MODÜL**

**Product Tablosu (İlk Ürün):** Üretilen bitmiş ürünleri temsil eder. Id (birincil anahtar), Code (benzersiz ürün kodu), Name (ürün adı), Category (kategori), Unit (ölçü birimi), StockQuantity (depo miktarı), MinStockLevel (minimum stok seviyesi), Description (açıklama), IsActive (etkinlik durumu), CreatedAt (oluşturma zamanı) alanlarına sahiptir.

**Material Tablosu (YENİ):** Ham maddeler ve bileşenleri temsil eder (M6 Vida, T-Nut, Rulmandı, Kablo vb.). Id (birincil anahtar), Code (benzersiz malzeme kodu), Name (malzeme adı), Unit (ölçü birimi), StockQuantity (depo miktarı), MinStockLevel (minimum stok seviyesi), Description (açıklama), IsActive (etkinlik durumu), CreatedAt (oluşturma zamanı) alanlarına sahiptir.

**BillOfMaterial Tablosu (GÜNCELLEME):** Ürün reçetelerini temsil eder. Id (birincil anahtar), ProductId (ürüne bağlantı), ComponentProductId (ürün bileşeni, nullable), MaterialId (malzeme bileşeni, nullable), RequiredQuantity (gerekli miktar), Notes (notlar), CreatedAt (oluşturma zamanı) alanlarına sahiptir. ComponentProductId XOR MaterialId kısıtlaması uygulanır; bir bileşen SADECE ürün VEYA malzeme olabilir.

**WorkOrder Tablosu (İş Emri):** Üretim emirlerini temsil eder. Id (birincil anahtar), OrderNumber (benzersiz sipariş numarası, format WO-20260312-001), Title (başlık), ProductId (üretilecek ürün bağlantısı), Quantity (üretim miktarı), Priority (öncelik seviyesi), Status (durum: Planning/InProgress/Completed/Cancelled), DepartmentId (sorumlu departman), CreatedByUserId (oluşturabilir), PlannedStartDate ve PlannedEndDate (planlanan tarihler), ActualStartDate ve CompletedAt (gerçekleşen tarihler), Notes (notlar), CreatedAt (oluşturma zamanı) alanlarına sahiptir.

### 🔹 DTOs (Veri Transfer Nesneleri)

**Authentication Layer DTOs:**
- LoginDto: Email, Password, RememberMe alanları içerir
- UserDto: Id, Email, FirstName, LastName, DepartmentId, IsActive ve Roles listesi içerir

**Product Management DTOs:**
- ProductListDto: Ürün listesinde gösterilmek üzere Id, Code, Name, Category, Unit, StockQuantity, MinStockLevel, Description, IsActive, CreatedAt alanları içerir
- CreateProductDto: Yeni ürün oluştururken Code, Name, Category, Unit, StockQuantity, MinStockLevel ve Description adıyla veri iletilir

**Material Management DTOs (YENİ):**
- MaterialListDto: Malzeme listesinde gösterilmek üzere Id, Code, Name, Unit, StockQuantity, MinStockLevel, Description, IsActive, CreatedAt alanları içerir
- CreateMaterialDto: Yeni malzeme oluştururken Code, Name, Unit, StockQuantity, MinStockLevel ve Description adıyla veri iletilir

**Bill of Material DTOs:**
- BomItemDto: Form giriş verileri için ComponentProductId (nullable), MaterialId (nullable), RequiredQuantity ve Notes alanları içerir
- BomItemListDto: UI'de gösterilmek üzere Id, ProductId, ComponentProductId, MaterialId, ComponentCode, ComponentName, ComponentType ("Product"/"Material"), RequiredQuantity, StockQuantity, IsStockSufficient (⭐ STOCK CONTROL) ve Notes alanları içerir

**WorkOrder DTOs:**
- WorkOrderDto: İş emri listelemek için Id, OrderNumber, Title, ProductId, ProductCode, ProductName, Quantity, Priority, Status, DepartmentId, CreatedByUserId, PlannedStartDate, CompletedAt, Notes alanları içerir
- CreateWorkOrderDto: Yeni iş emri oluştururken Title, ProductId, Quantity, Priority, DepartmentId, PlannedStartDate, PlannedEndDate ve Notes adıyla veri iletilir

---

### 🔹 ENUMs (İş Kuralı Sabitler)

```csharp
// WorkOrderStatus.cs
[Flags]
public enum WorkOrderStatus
{
    Draft = 0,              // Taslak
    Approved = 1,           // Onaylandı
    InProgress = 2,         // İşlemde
    WaitingForMaterial = 3, // Malzeme Bekliyor
    Completed = 4,          // Tamamlandı
    Cancelled = 5           // İptal Edildi
}

// WorkOrderPriority.cs
public enum WorkOrderPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
```

---

## 🔧 Business Logic Layer — Services

### 🔹 Service Interfaces

```
ASRS.Core/Interfaces/
│
├─ IUserService
│  ├─ LoginAsync(email, password) → bool
│  ├─ RegisterAsync(userDto) → bool
│  ├─ GetUserByIdAsync(id) → UserDto?
│  ├─ UpdateUserAsync(id, userDto) → bool
│  └─ GetAllUsersAsync(search) → IEnumerable<UserDto>
│
├─ IProductService
│  ├─ GetAllProductsAsync(search) → IEnumerable<ProductListDto>
│  ├─ GetProductByIdAsync(id) → ProductListDto?
│  ├─ CreateProductAsync(dto) → bool
│  ├─ UpdateProductAsync(id, dto) → bool
│  └─ DeleteProductAsync(id) → bool
│
├─ IMaterialService (YENİ)
│  ├─ GetAllMaterialsAsync(search) → IEnumerable<MaterialListDto>
│  ├─ GetMaterialByIdAsync(id) → MaterialListDto?
│  ├─ CreateMaterialAsync(dto) → bool
│  ├─ UpdateMaterialAsync(id, dto) → bool
│  └─ DeleteMaterialAsync(id) → bool
│
├─ IBomService
│  ├─ GetBomByProductIdAsync(productId) → IEnumerable<BomItemListDto>
│  ├─ AddBomItemAsync(productId, dto) → bool
│  └─ DeleteBomItemAsync(id) → bool
│
└─ IWorkOrderService
   ├─ GetAllWorkOrdersAsync(status, search) → IEnumerable<WorkOrderDto>
   ├─ GetWorkOrderByIdAsync(id) → WorkOrderDto?
   ├─ CreateWorkOrderAsync(dto) → WorkOrderDto
   ├─ UpdateWorkOrderAsync(id, dto) → bool
   └─ DeleteWorkOrderAsync(id) → bool
```

### 🔹 Service Implementasyonları

#### **UserService**
```csharp
// ASRS.BLL/Services/UserService.cs
public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    
    // Identity tabanlı kimlik doğrulama
    // Kullanıcı CRUD operasyonları
    // Rol atama işlemleri
}
```

#### **ProductService**
```csharp
// ASRS.BLL/Services/ProductService.cs
public class ProductService : IProductService
{
    private readonly AppDbContext _context;
    
    // Ürün listesi (search ile)
    // Ürün detayı getir
    // Ürün ekle/güncelle/sil
    // StockQuantity, MinStockLevel validasyonu
}
```

#### **MaterialService** ⭐ **YENİ**

Malzeme (ham madde, bileşen, parça) yönetimini sağlayan service. Tüm CRUD operasyonlarını gerçekleştirir.

**Sağladığı Özellikler:**
- **GetAllMaterialsAsync():** Tüm malzemeleri listeler, arama desteği vardır (kod veya adla arayabilirsin)
- **GetMaterialByIdAsync():** Spesifik bir malzemenin detayını getirir
- **CreateMaterialAsync():** Yeni malzeme ekler (Code, Name, Unit, StockQuantity, MinStockLevel, Description)
- **UpdateMaterialAsync():** Mevcut malzeme bilgilerini günceller
- **DeleteMaterialAsync():** Malzemeyi siler

**Kullanım Amacı:**
Ham maddeleri (M6 Vida, T-Nut, Rulman, vb.) sistemde kaydetmek ve ürün BOM'larında bileşen olarak kullanmak. Stok takibi de yapılır.

#### **BomService** ⭐ **STOCK KONTROL YAPILIYOR**

BomService, bir ürünün Bill of Materials (BOM) listesini getirirken her bileşenin stok durumunu kontrol eder.

**İşlemi:**
1. Ürüne ait tüm BOM satırlarını veritabanından çeker
2. Her bileşen için (ürün veya malzeme) mevcut stok miktarını alır
3. **Stock kontrol:** `IsStockSufficient = Mevcut Stok ≥ Gereken Miktar`
4. Sonuç:
   - **YETERLI** (TRUE): Depo'da gereken miktardan fazla/eşit stok var → ✅ Yeşil badge
   - **YETERSİZ** (FALSE): Depo'da gereken miktardan az stok var → ❌ Kırmızı badge

**Pratik Örnek 1 (YETERLI):**
- BOM'da yazılı: Alüminyum Profil 5 adet gerekli
- Depo'da: 150 adet mevcut
- Kontrol: 150 ≥ 5 → **YETERLI ✅**

**Pratik Örnek 2 (YETERSİZ):**
- BOM'da yazılı: T-Nut 20 adet gerekli
- Depo'da: 15 adet mevcut
- Kontrol: 15 < 20 → **YETERSİZ ❌ (5 adet eksik)**

#### **WorkOrderService**
```csharp
// ASRS.BLL/Services/WorkOrderService.cs
public class WorkOrderService : IWorkOrderService
{
    private readonly AppDbContext _context;
    
    // İş emri listesi, detayı, oluşturma, güncelleme, silme
    // Status güncellemeleri (Draft → Approved → InProgress → Completed)
    // Priority kontrolü
}
```

---

## 🎨 Presentation Layer — Controllers & Views

### 🔹 Controllers

```
ASRS.Web/Controllers/
│
├─ AccountController
│  ├─ [GET]  /Account/Login            → Login.cshtml
│  ├─ [POST] /Account/Login            → Kullanıcı kimlik doğrulama
│  ├─ [POST] /Account/Logout           → Oturum kapatma
│  └─ [GET]  /Account/AccessDenied     → AccessDenied.cshtml
│
├─ DashboardController
│  ├─ [GET]  /Dashboard                → Index.cshtml
│  └─ [Authorize] Role-based views
│
├─ ProductController ⭐
│  ├─ [GET]  /Product                  → Index.cshtml (Ürün listesi)
│  ├─ [POST] /Product/Create           → Yeni ürün ekle
│  ├─ [GET]  /Product/Edit/{id}        → Edit.cshtml (Ürün düzenle)
│  ├─ [POST] /Product/Edit/{id}        → Ürün güncelle
│  ├─ [POST] /Product/Delete/{id}      → Ürün sil
│  ├─ [GET]  /Product/Bom/{id}         → Bom.cshtml (BOM Yönetimi)
│  ├─ [POST] /Product/AddBomItem       → BOM'a bileşen ekle
│  └─ [POST] /Product/DeleteBomItem    → BOM'dan bileşen sil
│
├─ MaterialController (YENİ) ⭐
│  ├─ [GET]  /Material                 → Index.cshtml (Malzeme listesi)
│  ├─ [POST] /Material/Create          → Yeni malzeme ekle
│  ├─ [GET]  /Material/Edit/{id}       → Edit.cshtml (Malzeme düzenle)
│  ├─ [POST] /Material/Edit/{id}       → Malzeme güncelle
│  └─ [POST] /Material/Delete/{id}     → Malzeme sil
│
└─ WorkOrderController
   ├─ [GET]  /WorkOrder                → Index.cshtml (İş emri listesi)
   ├─ [POST] /WorkOrder/Create         → Yeni iş emri ekle
   ├─ [GET]  /WorkOrder/Edit/{id}      → Edit.cshtml
   ├─ [POST] /WorkOrder/Edit/{id}      → İş emri güncelle
   └─ [POST] /WorkOrder/Delete/{id}    → İş emri sil
```

#### **ProductController — Detay** (BOM Desteği Dahil)

ProductController, ürün yönetimi ve özellikle BOM (reçete) yönetimini sağlar. Bom() metodunda Material desteği eklenmiştir.

**Bom() Metodu:**
- ProductId'ye göre ürünü çeker
- Ürüne ait BOM satırlarını getirir (hem ürün hem malzeme bileşenleri)
- Tüm ürünleri dropdown için hazırlar
- **YENİ:** Tüm malzemeleri dropdown için hazırlar
- ViewBag aracılığıyla View'a gönderir

**AddBomItem() Metodu:**
- BOM'a yeni bileşen ekler
- **Önemli:** ComponentProductId ve MaterialId aynı anda seçilemez (XOR kuralı)
- Hata durumunda kullanıcıya mesaj gösterilir
- Başarılı olduğunda Bom.cshtml'e yönlendirilir

**Stock Kontrol Integrasyon:**
BomService otomatik olarak BOM satırlarının stok durumunu kontrol eder ve IsStockSufficient değerini hesaplar.

#### **MaterialController (YENİ)**

Malzeme yönetimi için eksiksiz CRUD işlemleri sunar.

**İçerdiği Metodlar:**
- **Index():** Tüm malzemeleri listeler, arama desteği vardır
- **Create() [POST]:** Form aracılığıyla yeni malzeme ekler
- **Edit() [GET]:** Malzeme düzenleme formunu açar
- **Edit() [POST]:** Malzeme bilgilerini günceller
- **Delete() [POST]:** Malzemeyi siler

**Yetkilendirme:**
Tüm yazma işlemleri (Create, Edit, Delete) "Yönetici" veya "Depo" rolüne ihtiyaç duyar. List işlemi tüm yetkili kullanıcılara açıktır.

### 🔹 Views (Razor .cshtml)

```
ASRS.Web/Views/
│
├─ Shared/
│  ├─ _Layout.cshtml              (Master layout)
│  │  ├─ Sidebar Navigation
│  │  │  ├─ Dashboard
│  │  │  ├─ Ürünler          ← /Product/Index
│  │  │  ├─ Malzemeler       ← /Material/Index (YENİ)
│  │  │  ├─ İş Emirleri      ← /WorkOrder/Index
│  │  │  ├─ Hareketler
│  │  │  ├─ Kullanıcılar
│  │  │  ├─ Raporlar
│  │  │  └─ Ayarlar
│  │  └─ Footer
│  └─ _ErrorPage.cshtml
│
├─ Account/
│  ├─ Login.cshtml
│  ├─ Logout.cshtml
│  └─ AccessDenied.cshtml
│
├─ Dashboard/
│  └─ Index.cshtml
│
├─ Product/
│  ├─ Index.cshtml              ← Ürün listesi
│  │  ├─ Yeni Ürün Form
│  │  └─ Ürün Tablosu
│  │     ├─ Code
│  │     ├─ Name
│  │     ├─ Category
│  │     ├─ Unit
│  │     ├─ StockQuantity (Yeşil/Kırmızı)
│  │     ├─ MinStockLevel
│  │     ├─ Status (Active/Inactive)
│  │     └─ İşlem (Edit, BOM, Delete)
│  │
│  ├─ Edit.cshtml                ← Ürün düzenle
│  │  └─ Ürün Detayları Form
│  │
│  └─ Bom.cshtml                 ← ✨ BOM YÖNETIMI (ÖZEL)
│     ├─ Bileşen Ekle Form
│     │  ├─ Bileşen Tipi (Radio Button)
│     │  │  ├─ Ürün
│     │  │  └─ Malzeme
│     │  ├─ Ürün Dropdown (id="productSelect")
│     │  ├─ Malzeme Dropdown (id="materialSelect")
│     │  ├─ Gereken Miktar
│     │  └─ Not (Opsiyonel)
│     │
│     └─ Mevcut Reçete Tablosu
│        ├─ Tip (Ürün/Malzeme badge)
│        ├─ Kod
│        ├─ Bileşen Adı
│        ├─ Gereken Miktar
│        ├─ Mevcut Stok         ← StockQuantity
│        ├─ Durum               ← ✅ STOCK KONTROL
│        │  ├─ "Yeterli"   (yeşil)  → StockQuantity ≥ RequiredQuantity
│        │  └─ "Yetersiz"  (kırmızı) → StockQuantity < RequiredQuantity
│        ├─ Not
│        └─ İşlem (Delete)
│
├─ Material/ (YENİ)
│  ├─ Index.cshtml               ← Malzeme listesi
│  │  ├─ Yeni Malzeme Form
│  │  └─ Malzeme Tablosu
│  │     ├─ Code
│  │     ├─ Name
│  │     ├─ Unit
│  │     ├─ StockQuantity
│  │     ├─ MinStockLevel
│  │     ├─ Description
│  │     ├─ Status (Active/Inactive)
│  │     └─ İşlem (Edit, Delete)
│  │
│  └─ Edit.cshtml                ← Malzeme düzenle
│     └─ Malzeme Detayları Form
│
└─ WorkOrder/
   ├─ Index.cshtml
   ├─ Create.cshtml
   └─ Edit.cshtml
```

#### **Bom.cshtml — BOM Yönetimi Arayüzü**

Ürüne ait reçeteyi yönetmek için eksiksiz bir arayüz sağlar.

**Bileşen Ekle Bölümü:**
- **Radio Button:** "Ürün" veya "Malzeme" seçimi
- **Dinamik Dropdown:** Seçime göre ürün veya malzeme listesi gösterilir
- **JavaScript Toggle:** Form alanları dinamik olarak gösterilip gizlenir
  - "Ürün" seçilirse: Ürün dropdown aktif, malzeme dropdown gizli
  - "Malzeme" seçilirse: Malzeme dropdown aktif, ürün dropdown gizli
- **Gereken Miktar:** Kaç adet gerekli
- **Not:** Opsiyonel açıklama

**Mevcut Reçete Tablosu:**
- **Tip Sütunu:** Ürün (mavi badge) veya Malzeme (mor badge) göstergesi
- **Kod & Adı:** Bileşenin code ve name'i
- **Gereken Miktar:** BOM'da yazılı miktar
- **Mevcut Stok:** Depo'da o an bulunduğu miktar
- **Durum Sütunu (⭐ STOCK KONTROL):**
  - "✅ Yeterli" (yeşil arka plan): Stok ≥ Gereken → üretim mümkün
  - "❌ Yetersiz" (kırmızı arka plan): Stok < Gereken → eksik var, temini gerekli
- **İşlem:** Sil butonu ile BOM satırı kaldırılabilir

#### **Material/Index.cshtml** (YENİ)

Malzeme listesi ve yönetimini gösteren sayfadır.

**Yeni Malzeme Ekle Formu:**
- Malzeme Kodu: Benzersiz kod (MAT-001 gibi)
- Malzeme Adı: İnsancıl isim (M6 Vida, T-Nut vb.)
- Birim: Ölçü birimi (adet, kg, metre, litre)
- Stok Miktarı: Başlangıç stok
- Min. Stok Seviyesi: Kritik seviye altında uyarı verilir
- Açıklama: Opsiyonel not

**Malzeme Listesi Tablosu:**
- **Kod:** Malzeme kodu (Monospace font, renkli)
- **Adı:** Malzeme adı
- **Birim:** Ölçü birimi
- **Stok:** Kırmızı renkle gösterilir eğer MinStockLevel'in altındaysa
- **Min. Stok:** Belirlenen minimum seviye
- **Açıklama:** Malzemenin detay açıklaması
- **Durum:** Aktif/Pasif badge
- **İşlem:** Düzenle ve Sil butonları (Yönetici/Depo yetkili kullanıcılar için)

---

## 🗄️ Database Schema & Relationships

### Veritabanı Diyagramı

```
┌─────────────────────────────────────────────────────────────────────┐
│                     ASRS_DB Schema (MySQL)                          │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────────────┐
│   Users             │ (ASP.NET Identity)
├──────────────────────┤
│ Id (PK)              │
│ UserName             │
│ Email                │
│ FirstName            │
│ LastName             │
│ DepartmentId (FK)───────────┐
│ IsActive             │       │
│ PasswordHash         │       │
│ CreatedAt            │       │
└──────────────────────┘       │
           ▲                    │
           │ (N:1)              │
           │                    │
┌──────────────────────────┐   │
│   UserRoles             │   │
├──────────────────────────┤   │
│ UserId (FK)    ────────────┐ │
│ RoleId (FK)───────────┐   │ │
└──────────────────────────┘ │ │
                      ▲      │ │
                      │(N:N) │ │
           ┌──────────┴──────┤ │
           │                 │ │
┌──────────▼─────────┐  ┌────▼────────────┐
│   Roles            │  │   Departments   │
├────────────────────┤  ├─────────────────┤
│ Id (PK)            │  │ Id (PK)         │
│ Name               │  │ Name            │
│ Description        │  │ Description     │
│ NormalizedName     │  │ IsActive        │
└────────────────────┘  │ CreatedAt       │
                        └─────────────────┘
                              ▲
                              │(1:N)
                              │
                    ┌─────────┴──────────┐
                    │                    │
         ┌──────────▼───────┐  ┌────────▼──────────┐
         │    Products      │  │    WorkOrders     │
         ├──────────────────┤  ├───────────────────┤
         │ Id (PK)          │  │ Id (PK)           │
         │ Code (unique)    │  │ OrderNumber       │
         │ Name             │  │ Title             │
         │ Category         │  │ ProductId (FK)──————┐
         │ Unit             │  │ Quantity           │
         │ StockQuantity    │  │ Priority (enum)    │
         │ MinStockLevel    │  │ Status (enum)      │
         │ Description      │  │ DepartmentId (FK)──┼──┐
         │ IsActive         │  │ CreatedByUserId ────┘  │
         │ CreatedAt        │  │ PlannedStartDate       │
         └──────────────────┘  │ PlannedEndDate         │
                ▲              │ ActualStartDate        │
                │(1:N)         │ CompletedAt            │
                │              │ Notes                  │
         ┌──────┴──────────────┤ CreatedAt             │
         │                     └───────────────────────┘
         │(ComponentProductId)   ▲
         │                       │(1:N)
    ┌────▼──────────────────┐   │
    │  BillOfMaterials      │   │
    ├───────────────────────┤   │
    │ Id (PK)               │   │
    │ ProductId (FK)────────┘   │
    │ ComponentProductId (FK)───┐(nullable,XOR MaterialId)
    │ MaterialId (FK)───────┐   │
    │ RequiredQuantity      │   │
    │ Notes                 │   │
    │ CreatedAt             │   │
    └───────────────────┬───┘   │
                ▲              │
                │(1:N)         │
                │              │
         ┌──────┴──────────────┐
         │                     │
    ┌────▼──────────────┐  ┌───┴──────────┐
    │    Materials      │  │              │
    ├───────────────────┤  │              │
    │ Id (PK)           │  │              │
    │ Code (unique)     │  │(nullable,alt│
    │ Name              │  │  XOR)       │
    │ Unit              │  │              │
    │ StockQuantity     │  │              │
    │ MinStockLevel     │  │              │
    │ Description       │  │              │
    │ IsActive          │  │              │
    │ CreatedAt         │  │              │
    └───────────────────┘  │              │
                           └──────────────┘

Relationship Rules:
════════════════════════════════════════════════════════════════════════

1. Product → WorkOrder (1:N)
   Bir ürün birçok iş emrine sahip olabilir
   
2. Product → BillOfMaterials (1:N) — Ana Ürün
   Bir ürünün BOM'u olabilir
   
3. Product → BillOfMaterials (1:N) — Bileşen Ürün
   Bir ürün başka ürünlerin BOM'unda bileşen olabilir
   
4. Material → BillOfMaterials (1:N)
   Bir malzeme birçok BOM'da yer alabilir
   
5. BOM Kontrol: ComponentProductId XOR MaterialId
   ⚠️ Bir BOM satırında SADECE ürün VEYA malzeme olabilir, HİÇBİR İKİSİ BİRLİKTE OLAMAZ

6. Department → Users (1:N)
   Bir departmanda birçok kullanıcı olabilir
   
7. Department → WorkOrders (1:N)
   Bir departman birçok iş emri alabilir
   
8. User → WorkOrders (1:N)
   Bir kullanıcı birçok iş emri oluşturabilir
```

### Migrations (EF Core)

```
ASRS.DAL/Migrations/

├─ 20240101000000_Initial.cs
│  └─ Tüm base tables (Users, Roles, Departments, Products, WorkOrders)
│
├─ 20260312000000_AddMaterials.cs (YENİ)
│  ├─ CreateTable: Materials
│  ├─ AddColumn: BillOfMaterials.MaterialId (FK, nullable)
│  └─ AddColumn: BillOfMaterials.ComponentProductId (null yapma)
│
└─ AppDbContextModelSnapshot.cs
   └─ Tüm schema'nın current state'i
```

---

## 🎯 Ürün-Malzeme-BOM Modülü Detayı

### Mantık & Kurallar

```
ÜRÜN vs MALZEME AYRIMI
═════════════════════════════════════════════════════════════

ÜRÜN (Product)
├─ Üretilmiş/montajlanmış ürün
├─ Örnek: Servo Motor, Konveyör Bant, Alüminyum Profil 40x40
├─ Kod formatı: PRD-001, PRD-002, vb.
├─ Tabloda 1 satır = 1 ürün
└─ ASRS'te fiziksel olarak stoklanır

MALZEME (Material)
├─ Ham madde, bileşen, parça
├─ Örnek: M6 Vida, T-Nut, Rulman 608, Kablo
├─ Kod formatı: MAT-001, MAT-002, vb.
├─ Tabloda 1 satır = 1 malzeme
└─ ASRS'te fiziksel olarak stoklanır

─────────────────────────────────────────────────────────────────

BOM (Bill of Materials) — Reçete
├─ Bir ürünün üretimi için gereken bileşenler listesi
├─ Bileşen: Ürün VEYA Malzeme olabilir
│  ├─ Bileşen Ürün örneği: PRD-001 ürünü, PRD-002 için 3 adet gerekli
│  └─ Bileşen Malzeme örneği: MAT-001 (M6 Vida) PRD-001 için 50 adet gerekli
├─ Cross-Assembly desteği: PRD-003, PRD-001 + PRD-002 + MAT-001 + MAT-002'den oluşabilir
└─ Flat kod yapısı: Hiç hiyerarşi yok, her şey doğrudan rekürsif query ile alınır

─────────────────────────────────────────────────────────────────

STOCK KONTROL (Stock Sufficient Check)
├─ BOM satırını gösterirken:
│  "Bu bileşen yeterli miktarda mevcut mu?"
├─ Hesaplama:
│  IsStockSufficient = (Bileşen Stok Miktarı) ≥ (BOM'da Gereken Miktar)
├─ Sonuç:
│  TRUE → ✅ Yeterli (Yeşil badge)
│  FALSE → ❌ Yetersiz (Kırmızı badge)
├─ Amaç:
│  İş emri planlama sırasında hemen eksikleri görülsün
│  Operasyon: "Bu ürünü yapabilir miyiz? Stok yeterli mi?"
└─ Uyarı: Stok miktarı değiştikçe otomatik güncellenir (next page load)

─────────────────────────────────────────────────────────────────

XOR Constraint (Mutual Exclusivity)
├─ BillOfMaterials.ComponentProductId ve MaterialId
├─ Kural: Biri dolu olursa, diğeri NULL olmalıdır
├─ Seçenek 1: ürün
│  ├─ ComponentProductId = 5 (PRD-001's ID)
│  └─ MaterialId = NULL
├─ Seçenek 2: malzeme
│  ├─ ComponentProductId = NULL
│  └─ MaterialId = 3 (MAT-001's ID)
├─ ❌ HATA: İKİSİ BİRLİKTE
│  ├─ ComponentProductId = 5
│  └─ MaterialId = 3
└─ Application tarafından kontrol edilir:
   if (dto.ComponentProductId.HasValue && dto.MaterialId.HasValue)
       throw new ValidationException();
```

### Data Flow Example

```
Senaryo: "Konveyör Bant" (PRD-003) ürünü için BOM oluşturma
════════════════════════════════════════════════════════════════════

Step 1: Ürün Seçimi
┌─────────────────────────────────────────┐
│ Ürünler / PRD-003 / Bom Sayfası         │
│ [BOM Yönetimi] — Konveyör Bant         │
│                                         │
│ Bileşen Ekle:                           │
│ ────────────────────────────────────────│
│ Bileşen Tipi: ◯ Ürün  ◯ Malzeme        │
│ Seç: [PRD-001: Alüminyum Profil...]     │
│ Gereken Miktar: [5] adet                │
│ Not: Taşıyıcı profil                    │
│ [+ EKLE]                                │
└─────────────────────────────────────────┘

Step 2: Backend İşlemi (BomService)
┌────────────────────────────────────────────────┐
│ BomService.AddBomItemAsync()                   │
│ ├─ Yeni BillOfMaterial oluştur:                │
│ │  ├─ ProductId = 3 (PRD-003)                  │
│ │  ├─ ComponentProductId = 1 (PRD-001)         │
│ │  ├─ MaterialId = NULL (malzeme seçilmedi)    │
│ │  ├─ RequiredQuantity = 5                     │
│ │  └─ Notes = "Taşıyıcı profil"                │
│ │                                              │
│ ├─ db.BillOfMaterials.Add(item)                │
│ └─ await db.SaveChangesAsync()                 │
└────────────────────────────────────────────────┘

Step 3: Veritabanına Kaydedildi
┌─────────────────────────────────────────────────────────────┐
│ BillOfMaterials Table                                       │
├─────────────────────────────────────────────────────────────┤
│ Id│ProductId│ComponentProductId│MaterialId│RequiredQuantity│
│──┼─────────┼──────────────────┼──────────┼────────────────┤
│1 │   3     │        1         │  NULL    │       5        │
│2 │   3     │        2         │  NULL    │       1        │
│3 │   3     │      NULL        │    1     │      10        │ ← MAT-001
└─────────────────────────────────────────────────────────────┘

Step 4: Display (BomService.GetBomByProductIdAsync)
┌────────────────────────────────────────────────────────────────┐
│ BOM Listesi: Page Refresh                                      │
├────────────────────────────────────────────────────────────────┤
│ Tip │ Kod    │ Bileşen Adı        │Gereken│Stok  │Durum       │
│────┼────────┼────────────────────┼───────┼──────┼────────────┤
│Ürün│PRD-001 │Alüminyum Profil..  │   5   │ 150  │✅ Yeterli  │
│    │        │                    │       │      │(150≥5)     │
│────┼────────┼────────────────────┼───────┼──────┼────────────┤
│Ürün│PRD-002 │Servo Motor 24V     │   1   │  30  │✅ Yeterli  │
│    │        │                    │       │      │(30≥1)      │
│────┼────────┼────────────────────┼───────┼──────┼────────────┤
│Mal.│MAT-001 │M6 Vida             │  10   │ 500  │✅ Yeterli  │
│    │        │                    │       │      │(500≥10)    │
└────────────────────────────────────────────────────────────────┘

Sonuç: "Konveyör Bant üretimi için tüm bileşenlerin stoku yeterli"
```

---

## 🔍 Stock Kontrol Mekanizması

### Kod Seviyesinde İmplementasyon

BomService sınıfında, `GetBomByProductIdAsync()` metodu şu işleri yapar:

**Adım 1:** Veritabanından ürüne ait tüm BOM satırlarını ve ilişkili ürünleri/malzemeleri çeker.

**Adım 2:** Her BOM satırı için bileşen türünü belirler:
- Eğer `ComponentProduct` varsa: Ürün, onun stok miktarını al
- Eğer `Material` varsa: Malzeme, onun stok miktarını al

**Adım 3 (ÖNEMLİ - Stock Kontrol):** 
`IsStockSufficient = stock >= RequiredQuantity` hesaplaması yapılır.

**Adım 4:** Hesaplanan sonuç DTO'ya eklenerek View'a gönderilir.

**Mantık Özeti:**
- Depo'daki mevcut stok ≥ BOM'da gereken miktar → **YETERLI ✅**
- Depo'daki mevcut stok < BOM'da gereken miktar → **YETERSİZ ❌**

### UI Seviyesinde Gösterim

Bom.cshtml'de, `IsStockSufficient` değerine göre badge gösterilir:

- **TRUE:** Yeşil arka plan, "✅ Yeterli" yazısı
- **FALSE:** Kırmızı arka plan, "❌ Yetersiz" yazısı

Türün stok miktarını ve gereken miktarı gören kullanıcı, hemen eksikleri farkeder.

### Gerçek Dünya Örneği

**Senaryo:** Servo Motor (PRD-002) üretilecek.

**BOM'da belirtilen bileşenler:**
- PRD-001 (Alüminyum Profil): 1 adet gerekli → Depo'da 150 adet var → ✅ YETERLI
- MAT-001 (M6 Vida): 20 adet gerekli → Depo'da 500 adet var → ✅ YETERLI
- MAT-002 (T-Nut): 20 adet gerekli → Depo'da 15 adet var → ❌ YETERSİZ (5 eksik!)
- MAT-003 (Rulman): 2 adet gerekli → Depo'da 50 adet var → ✅ YETERLI

**Sonuç:** Üretim başlanamaz çünkü T-Nut stok yetersizdir. 5 adet daha temin edilmesi gerekir.

---

## 📡 API Integrasyon Akışı

### Program.cs — Dependency Injection & Seed

**Veritabanı Yapılandırması:**
MySQL 8.0 bağlantısı, connection string'den yapılandırılır. Entity Framework Core ORM olarak kullanılır.

**Identity Yapılandırması:**
ASP.NET Identity kullanılıyor. Kullanıcı kimlik doğrulama, rol atama, şifre yönetimi burada yapılandırılır.

**Service Registration (Dependency Injection):**
BLL'deki tüm service'ler burada Container'a kaydedilir:
- IUserService → UserService
- IProductService → ProductService
- IMaterialService → MaterialService (YENİ)
- IBomService → BomService
- IWorkOrderService → WorkOrderService

Bu sayede Controller'lar service'lere parametreler aracılığıyla erişebilir.

**Material Seed Data:**
Uygulama başlangıcında, Materials tablosu boşsa otomatik olarak 5 örnek malzeme eklenir:
- MAT-001: M6 Vida (500 adet)
- MAT-002: T-Nut (400 adet)
- MAT-003: Rulman 608 (50 adet)
- MAT-004: Kablo 1mm² (200 metre)
- MAT-005: Somun M6 (600 adet)

Bu sayede test edebilmek için hemen malzeme verisi hazır olur.

---

## 📈 Bugüne Kadar Tamamlanan İşler — Özet

### ✅ Tamamlanan Özellikler

**ASRS.Core Katmanı** — Entity, DTO ve Interface tanımlamaları:
- Material entity oluşturuldu; Code, Name, Unit, StockQuantity, MinStockLevel, Description, IsActive ve CreatedAt alanları içerir
- BillOfMaterial entity güncellendi; Material navigation property ve MaterialId FK (nullable) eklendi; ComponentProductId XOR MaterialId ilişkisi sağlandı
- MaterialListDto, CreateMaterialDto, BomItemDto ve BomItemListDto DTOs oluşturuldu ve güncellendi
- IMaterialService interface tanımlandı; GetAllMaterialsAsync, GetMaterialByIdAsync, CreateMaterialAsync, UpdateMaterialAsync ve DeleteMaterialAsync metotları belirtildi

**ASRS.DAL Katmanı** — Veritabanı bağlamı ve migration:
- AppDbContext.cs güncellenmiş; DbSet<Material> eklendi, OnModelCreating içinde Materials tablosu ve BillOfMaterials.MaterialId FK yapılandırıldı
- 20260312_AddMaterials migration oluşturuldu ve uygulandı; Materials tablosu ve BillOfMaterials.MaterialId kolonu başarıyla database'e eklendi

**ASRS.BLL Katmanı** — İş mantığı servisleri:
- MaterialService.cs tamamen tamamlandı; tüm 5 CRUD metodu (GetAll, GetById, Create, Update, Delete) implement edildi ve test edildi
- BomService.cs güncellendi; GetBomByProductIdAsync metodu Product ve Material bileşenlerini almayı destekler, stock control (IsStockSufficient) hesaplaması yapılır

**ASRS.Web Katmanı** — Kontrolörler ve görünümler:
- MaterialController.cs tam CRUD desteğiyle oluşturuldu; Index (lista), Create (yeni ekleme), Edit (düzenleme) ve Delete (silme) eylemleri var
- ProductController.cs güncellenmiş; Bom metoduna IMaterialService enjeksiyonu eklendi, ViewBag.AllMaterials dolduruldu
- Material/Index.cshtml oluşturuldu; malzeme listesi ve yeni malzeme ekleme formu gösterilir
- Material/Edit.cshtml oluşturuldu; malzeme bilgileri düzenleme formu
- Product/Bom.cshtml güncellendi; ürün veya malzeme seçimi için radio butonlar, dinamik dropdown'lar, stock durumu göstergesi (✅ Yeterli / ❌ Yetersiz), JavaScript toggle fonksiyonu
- Shared/_Layout.cshtml güncellendi; "Malzemeler" menü öğesi ana navigasyona eklendi

**Program.cs Yapılandırması**:
- MaterialService dependency injection olarak Container'a kaydedildi
- Material seed data tanımlandı; uygulama başlangıcında 5 örnek malzeme otomatik eklenir (M6 Vida, T-Nut, Rulman 608, Kablo, Somun)

### 📊 Veritabanı Tabloları (Mevcut Durum)

**Users Tablosu** — ASP.NET Identity tarafından yönetilir. Kullanıcı kimlik bilgileri (UserName, Email, FirstName, LastName), DepartmentId ilişkisi, IsActive durumu ve CreatedAt timestamp'i saklanır.

**Roles Tablosu** — ASP.NET Identity tarafından yönetilir. Rol tanımlamaları (Yönetici, Depo, Lojistik, Üretim, Kalite) ve açıklapmaları saklanır.

**UserRoles Tablosu** — ASP.NET Identity tarafından yönetilir. Kullanıcılar ve rollar arasında N:N ilişkisini oluşturur.

**Departments Tablosu** — Bölüm bilgileri; Id, Name, Description, IsActive, CreatedAt.

**Products Tablosu** — Üretilmiş ürünler; Code (unique), Name, Category, Unit, StockQuantity, MinStockLevel, Description, IsActive, CreatedAt. Bu tablo nihai ürünleri temsil eder.

**Materials Tablosu (YENİ)** — Ham maddeler ve bileşenler; Code (unique), Name, Unit, StockQuantity, MinStockLevel, Description, IsActive, CreatedAt. Üretim için gerekli olan ürün olmayan bileşenleri (vida, rulmandı, kablo vb.) temsil eder.

**BillOfMaterials Tablosu (GÜNCELLENME)** — Ürün reçeteleri; ProductId (FK), ComponentProductId (FK, nullable), MaterialId (FK, nullable), RequiredQuantity, Notes, CreatedAt. ComponentProductId XOR MaterialId kısıtlaması sağlanır; bir bileşen SADECE ürün VEYA malzeme olabilir, ikisi birden değil.

**WorkOrders Tablosu** — Üretim siparişleri; OrderNumber (unique), Title, ProductId (FK), Quantity, Priority, Status, DepartmentId (FK), CreatedByUserId (FK), PlannedStartDate, ActualStartDate, CompletedAt, CreatedAt.

---

## 🚀 Sonraki Aşamalar

**Aşama 1: TAMAMLANDI**
Material Entity ve DTO'lar oluşturuldu, MaterialService ve Controller uygulandı, Material görünümleri (Index, Edit) tamamlandı, BomService güncellendi, stock kontrol UI'si entegre edildi.

**Aşama 2: TO-DO**
WorkOrder ile BOM entegrasyonu yapılacak; malzeme depo geçişlerini takip eden stok history tablosu oluşturulacak (kim/ne zaman değiştirdi); bina/raf/konum vb. warehouse yapıları için yeni entity'ler tasarlanacak; RFID okuyucu entegrasyonu yapılacak; gerçek zamanlı stok uyarıları sistem entegre edilecek.

**Aşama 3: TO-DO**
Gelişmiş raporlama özelliği geliştirilecek; Excel/PDF export fonksiyonları eklendra; multi-warehouse (çok depo) desteği sağlanacak; stok tahmini ve tüketim analizi modeli kurulacak.

---

**Uygulamayı Çalıştırmak:**
`dotnet build` komutu ile proje derlenebilir, `dotnet run --project ASRS.Web` komutu ile geliştirme sunucusu başlatılabilir.

---

**Son Güncelleme:** March 12, 2026