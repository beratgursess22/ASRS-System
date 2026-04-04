# ASRS-ERP Sistem Durum Dokumani

Bu dokuman, projede bugune kadar yapilmis tum temel yapilari gercek kod durumuna gore ozetlemek icin bastan yazilmistir.

Son guncelleme: 4 Nisan 2026
Durum: Aktif gelistirme

## 1) Proje Ozeti

ASRS-System iki ana parcadan olusuyor:

- ERP-ASRS: .NET 9 tabanli ERP ve operasyon yonetimi (MVC + katmanli mimari)
- arduino: Arduino Mega icin ASRS hareket kontrol yazilimi (Stepper + seri protokol)

Temel hedef:

- Urun, malzeme, BOM, is emri, satin alma talebi/siparisi ve tedarikci sureclerini tek merkezde yonetmek
- Fiziksel depolama mekanizmasina (Arduino/Raspberry Pi) komut akisini hazirlamak

## 2) Katmanli Mimari (ERP-ASRS)

ERP-ASRS cozumunde 5 proje var:

- ASRS.Core
  - Entity, DTO, Enum ve servis interface tanimlari
- ASRS.DAL
  - AppDbContext, Entity Framework Core konfiglari, migration dosyalari
- ASRS.BLL
  - Is kurallari ve servis implementasyonlari
- ASRS.Web
  - MVC controller ve Razor view katmani
- ASRS.API
  - Su an template asamasinda (yalnizca ornek weather endpoint'i var)

## 3) Bugune Kadar Yapilanlar

### 3.1 Tamamlanan Ana Moduller

- Kimlik dogrulama ve yetkilendirme altyapisi
  - ASP.NET Identity entegrasyonu (AppUser/AppRole)
  - Cookie tabanli giris/cikis ve AccessDenied yonlendirmeleri
  - Rol bazli yetki kontrolleri (Controller seviyesinde Authorize)

- Urun yonetimi
  - Urun CRUD
  - Stok ve minimum stok alanlari
  - Varsayilan fiyat ve para birimi alanlari

- Malzeme yonetimi
  - Malzeme CRUD
  - Urunden bagimsiz ham madde/bilesen takibi

- BOM (Bill of Materials) yonetimi
  - Urun-malzeme karmasi bilesen tanimi
  - BOM satirlarinda stok yeterlilik bilgisi
  - Ic ice BOM gereksinim hesabi (servis katmaninda)

- Is emri yonetimi
  - Is emri olusturma/listeleme/detay/silme
  - Durum gecisleri ve durum sonuc enum yapisi
  - Stok tuketim bayraklari (migration ile eklenmis)

- Satin alma sureci
  - Purchase Request modulu
  - Purchase Order modulu
  - Siparis kalem bazli miktar/fiyat/teslim alma islemleri
  - PO kalem bolme (SplitItem)
  - PO kaleminde kalan miktari kismi iptal etme (CancelRemainingItemQuantity)
  - PR kalemlerini Pending durumundayken revize etme (UpdateItem)
  - PR durumunu manuel "Received" yapma kapatildi; teslim alma PO uzerinden stok artisiyla ilerliyor

- Tedarikci ve fiyat listesi yonetimi
  - Tedarikci CRUD
  - Tedarikci-urun/malzeme fiyat kayitlari
  - PO basliginda tedarikci secildiginde uygun fiyatlarin kalemlere otomatik yansimasi

### 3.2 Son Kod Degisikligi Ozeti (27 Mart 2026)

HEAD commit: improve PR PO side (69c655a)

- Yeni DTO'lar:
  - SplitPurchaseOrderItemDto
  - CancelRemainingPurchaseOrderItemDto
- PurchaseOrder tarafinda:
  - Kalem bolme akisi eklendi
  - Kalan miktar kismi iptal akisi eklendi
  - Kalan miktar ve alinan miktar uyumlulugu icin ek dogrulamalar eklendi
- PurchaseRequest tarafinda:
  - Pending durumunda kalem eksik miktar/not duzenleme eklendi
  - PR durumunu manuel Received yapma ve PR uzerinden dogrudan stok yazma kaldirildi
- Web arayuzu:
  - PurchaseOrder detay ekranina kalem bolme ve kismi iptal formlari eklendi
  - PurchaseRequest detay ekranina kalem revizyon islemleri eklendi

### 3.3 Arayuz (ASRS.Web) Durumu

Controller yapisi:

- Account
- Dashboard
- User
- Product
- Material
- WorkOrder
- PurchaseRequest
- PurchaseOrder
- Supplier

Razor sayfa gruplari:

- Account, Dashboard, User
- Product (BOM dahil)
- Material
- WorkOrder
- PurchaseRequest
- PurchaseOrder
- Supplier
- Shared layout/error/validation partial

### 3.4 Veri Modeli Durumu

Aktif entity setleri (DbSet):

- Departments
- Products
- Materials
- BillOfMaterials
- WorkOrders
- PurchaseRequests
- PurchaseRequestItems
- PurchaseOrders
- PurchaseOrderItems
- Suppliers
- SupplierItemPrices

Identity tablolari da ayni context altinda yonetiliyor.

### 3.5 Migration Gecmisi (Uygulanan Gelisim Adimlari)

Kodda bulunan migrationlar:

1. 20260307154124_InitialCreate
2. 20260309171505_AddProduct
3. 20260311111147_AddWorkOrder
4. 20260311121943_AddBillOfMaterials
5. 20260312183241_AddMaterials
6. 20260314115218_AddWorkOrderStockConsumptionFlags
7. 20260318184605_AddPurchaseRequestModule
8. 20260319172558_AddPurchaseOrderModule
9. 20260319182535_AddDefaultPricingToProductAndMaterial
10. 20260320125637_AddSupplierModule
11. 20260320170719_AddSupplierItemPricing

Bu siralama, projenin urun/malzeme temelinden satin alma ve tedarikci modullerine genisledigini gosteriyor.

## 4) Mevcut Durum Analizi

### 4.1 Uretimde Kullanilabilir Olgun Moduller

- Web tarafinda temel ERP akislari (urun, malzeme, BOM, is emri, satin alma, tedarikci)
- Rol bazli erisim kontrolu
- EF Core + MySQL veri modeli

### 4.2 Hala Gelisim Gerektiren Alanlar

- ASRS.API
  - Su an yalnizca minimal template (GET /weatherforecast)
  - Cihaz/RFID/komut endpointleri daha yazilmamis

- Arayuz menu tutarliligi
  - _Layout icinde Kalite/Lojistik menu linkleri var
  - Bu controller/view ciftleri su an projede tanimli degil

- Seed verileri
  - Program.cs icinde kapsamli seed bloklari mevcut
  - Ancak su an yorum satiri durumunda
  - Yorum satiri kaldirilmadan sifirdan kurulumda varsayilan admin kullanicisi olusmaz

- Test ve gozlemlenebilirlik
  - Otomatik test katmani ve detayli merkezi loglama guclendirilmeli

## 5) Arduino Tarafi (Fiziksel Hareket Katmani)

arduino/ASRS_Main altinda step motor ve seri komut tabanli bir kontrol yapisi var.

Temel ozellikler:

- Komut protokolu
  - STORE:col:row
  - RETRIEVE:col:row
  - HOME
  - STATUS

- Durum mesajlari
  - READY, BUSY, OK, ERROR

- Eksen ve raf tanimlari
  - X/Z homing
  - Raf kolon/kat pozisyonlari config uzerinden tanimli

- Operasyon akislar
  - Paketi giristen alip rafa birakma
  - Raftan alip cikisa getirme

Not: Arduino ve ERP-ASRS API entegrasyonu bir sonraki asama olarak gorunuyor.

## 6) Teknoloji Yigini

- .NET 9
- ASP.NET Core MVC
- ASP.NET Identity
- Entity Framework Core
- MySQL (Pomelo)
- Arduino C++ (Mega 2560 odakli)

## 7) Kurulum ve Calistirma

### 7.1 Gereksinimler

- .NET 9 SDK
- MySQL 8+
- (Opsiyonel) Arduino IDE

### 7.2 Veritabani Baglantisi

ASRS.Web/appsettings.json icindeki DefaultConnection ayarini ortamina gore duzenle.

Ornek mevcut baglanti:

Server=localhost;Database=asrs_db;User=root;Password=123456;

### 7.3 Migration Uygulama

ERP-ASRS klasorunde:

dotnet ef database update --project ASRS.DAL --startup-project ASRS.Web

### 7.4 Web Uygulamasini Calistirma

ERP-ASRS klasorunde:

dotnet run --project ASRS.Web

### 7.5 API Projesini Calistirma (Gelistirme/Test)

ERP-ASRS klasorunde:

dotnet run --project ASRS.API

Not: API su an template seviyesinde oldugu icin islevsel ERP endpointlerini icermez.

### 7.6 Ilk Giris ve Seed Notu (Kritik)

- Web uygulamasinda varsayilan acilis rotasi `/Account/Login` oldugu icin en az bir aktif kullanici gerekir.
- Program.cs icindeki seed blogu yorum satirinda oldugundan sifir veritabaninda hazir admin kullanicisi yoktur.
- Bu nedenle ilk kurulumda:
  - ya seed bloklari kontrollu sekilde acilmali,
  - ya da SQL/Identity uzerinden manuel ilk yonetici kullanicisi olusturulmalidir.

### 7.7 Rol Notu

- Kod tarafinda aktif yetkilerde su roller kullaniliyor: `Yonetici`, `Depo`, `Uretim`, `Satin Alma`.
- Seed acilirsa rol listesinin bu yetkilerle uyumlu oldugunu kontrol etmek gerekir.

## 8) Rol ve Yetki Ozet Tablosu

Kodda kullanilan roller:

- Yonetici
- Depo
- Lojistik
- Uretim
- Kalite
- Montaj
- Satin Alma

Aktif kullanilan ana yetki desenleri:

- Yonetici: Tum modullere tam erisim
- Depo: Urun/malzeme/BOM tarafinda yazma islemleri
- Uretim: Is emri yonetimi
- Satin Alma: Talep/siparis/tedarikci yonetimi

## 9) Yol Haritasi (Bir Sonraki Mantikli Adimlar)

1. ASRS.API icine gercek endpointlerin eklenmesi
   - RFID okutma
   - Komut olusturma
   - Durum geri bildirimi

2. Seed mekanizmasinin kontrollu acilmasi
   - Development ortamina ozel
   - Tekrar calistirmaya dayanikli

3. Test altyapisinin genisletilmesi
   - Servis katmani birim testleri
   - Kritik is akislarina entegrasyon testleri

4. Donanim entegrasyon testleri
   - Raspberry Pi -> API -> Arduino uc uca akisin dogrulanmasi

## 10) Kisa Sonuc

Proje, ERP tarafinda beklenenden daha olgun bir noktaya gelmis durumda:

- Temel domain modelleri tamam
- Operasyonel modullerin buyuk bolumu calisiyor
- Satin alma ve tedarikci modulleri sisteme eklenmis

Ana eksik halka, API ve donanimla uc uca canli entegrasyonun tamamlanmasi.
