# ASRS-System

ASRS-System, ERP yonetimi ile fiziksel otomatik depolama ve geri alma mekanizmasini birlestiren akilli depo projesidir. Sistem; .NET tabanli ERP/Web/API katmani, Arduino tabanli hareket kontrol yazilimi ve Raspberry Pi uzerinde calisan RFID okuma koprusunden olusur.

## Proje Amaci

Bu proje; urun, malzeme, BOM, is emri, satin alma, tedarikci, kalite kontrol ve ASRS raf operasyonlarini tek merkezden yonetmek icin gelistirildi. ERP tarafi karar ve kayit merkezidir; Arduino fiziksel hareketleri uygular; Raspberry Pi ise RFID okuyucu ile API arasinda kopru gorevi gorur.

## Ana Klasorler

```text
ASRS-System/
|-- ERP-ASRS/      # .NET ERP, MVC web arayuzu, API, EF Core veri katmani
|-- arduino/       # Arduino Mega/RAMPS icin ASRS hareket kontrol kodu
|-- raspberry/     # MFRC522 RFID okuyucu ile API arasindaki Python bridge
`-- README.md      # Genel sistem dokumani
```

## Sistem Bilesenleri

### ERP-ASRS

ERP-ASRS, projenin yazilim ve karar merkezidir. ASP.NET Core MVC web arayuzu, ASP.NET Core Web API, is kurallari, Entity Framework Core veri modeli ve MySQL veritabani bu klasor altindadir.

Baslica moduller:

- Kullanici, rol ve departman yonetimi
- Urun ve malzeme yonetimi
- BOM tanimlari
- Is emri surecleri
- Satin alma talebi ve satin alma siparisi
- Tedarikci ve tedarikci fiyat listesi
- Kalite kontrol, uygunsuzluk ve CAPA aksiyonlari
- ASRS raf hucreleri, RFID eslesmeleri, komut kuyrugu ve sistem durumu

Detay: [ERP-ASRS/README.md](./ERP-ASRS/README.md)

### Arduino

Arduino tarafi, seri porttan gelen metin komutlarini isleyerek step motor hareketlerini yonetir. Sistem 4 sutun ve 3 katli raf yapisi icin `STORE`, `RETRIEVE`, `HOME` ve `STATUS` komutlarini destekler.

Detay: [arduino/README.md](./arduino/README.md)

### Raspberry Pi

Raspberry Pi tarafi, MFRC522 RFID okuyucudan UID okur ve bu bilgiyi ASRS.API tarafina HTTP POST ile gonderir. Ayni kartin cok kisa surede tekrar gonderilmesini engelleyen cooldown mantigi ve systemd servis dosyasi vardir.

Detay: [raspberry/README.md](./raspberry/README.md)

## Proje Raporu

Projenin tam teknik raporu `docs/` klasoru altinda tutulur. Rapor PDF dosyasi eklendiginde asagidaki sayfadan goruntulenebilir:

- [Proje Raporu Sayfasi](./docs/README.md)
- [SmartRack Final Report PDF](./docs/SmartRack_FinalReport.pdf)

PDF rapor dosyasi `docs/SmartRack_FinalReport.pdf` yolu altindadir.

## Calisma Akisi

1. Kullanici veya fiziksel operator RFID karti okutur.
2. `raspberry/rfid_bridge.py`, kart UID bilgisini `ASRS.API` tarafina gonderir.
3. API, UID bilgisini normalize eder ve `RfidRackMaps` tablosunda aktif eslesmeyi arar.
4. Uygun raf hucresi bossa `AsrsCommand` tablosuna `Store` komutu eklenir.
5. API icindeki `AsrsSerialWorker` aktifse komutu dogrudan seri porttan Arduino'ya yollar.
6. Alternatif olarak Raspberry Pi, `/api/asrs/commands/next` endpoint'i ile kuyruktan komut cekip Arduino'ya iletebilir.
7. Arduino komutu uygular ve `BUSY`, `OK:*` veya `ERR:*` cevaplari uretir.
8. API komut durumunu ve raf hucresi doluluk bilgisini gunceller.

## Kullanilan Teknolojiler

- .NET 9
- ASP.NET Core MVC
- ASP.NET Core Web API
- Entity Framework Core 9
- Pomelo EntityFrameworkCore MySQL Provider
- MySQL
- ASP.NET Core Identity
- Bootstrap, CSS, JavaScript, Razor Views
- Python 3
- `requests`, `mfrc522`, `RPi.GPIO`
- Arduino C/C++
- Arduino Mega, RAMPS, DRV8825/A4988 tarzi step suruculer
- MFRC522 RFID okuyucu

## Hizli Baslangic

ERP cozumunu derlemek icin:

```bash
cd ERP-ASRS
dotnet restore
dotnet build
```

Web uygulamasini calistirmak icin:

```bash
cd ERP-ASRS/ASRS.Web
dotnet run
```

API uygulamasini calistirmak icin:

```bash
cd ERP-ASRS/ASRS.API
dotnet run
```

Raspberry Pi bridge icin:

```bash
cd raspberry
pip install -r requirements.txt
python3 rfid_bridge.py
```

Arduino kodu `arduino/ASRS_Main/ASRS_Main_Single.ino` dosyasi uzerinden Arduino IDE ile yuklenebilir. Moduler kaynak dosyalari `Code/` ve `library/` altindadir.

## Dokumantasyon Notu

Bu README genel sistem gorunumunu verir. Her alt klasordeki README dosyasi kendi bileseninin klasor yapisini, kullandigi teknolojileri, gorevini ve entegrasyon noktalarini daha detayli aciklar.
