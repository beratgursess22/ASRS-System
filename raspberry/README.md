# Raspberry Pi RFID Bridge

Bu klasor, MFRC522 RFID okuyucu ile ERP-ASRS API arasinda calisan Python bridge uygulamasini icerir. Raspberry Pi'nin temel gorevi RFID kart UID bilgisini okumak ve API tarafina bildirmektir.

## Kullanilan Teknolojiler

- Python 3
- MFRC522 RFID okuyucu
- Raspberry Pi GPIO
- `requests`
- `mfrc522`
- `RPi.GPIO`
- systemd servis yapisi

## Klasor Yapisi

```text
raspberry/
|-- rfid_bridge.py              # RFID okuma ve API'ye POST gonderme uygulamasi
|-- requirements.txt            # Python bagimliliklari
`-- asrs-rfid-bridge.service    # systemd servis tanimi
```

## Sistem Icindeki Gorevi

Raspberry Pi:

- MFRC522 okuyucuyu baslatir.
- RFID kart okutulunca UID bilgisini okur.
- 5 byte UID donen kartlarda BCC byte'ini ayiklar.
- UID bilgisini hex formatinda API'ye gonderir.
- Ayni kartin cok kisa surede tekrar gonderilmesini engeller.
- Hata ve basari loglarini stdout uzerinden yazar.
- Servis kapanirken GPIO temizligi yapar.

Karar mekanizmasi API/ERP tarafindadir. Raspberry Pi mevcut kodda sadece RFID okuma ve HTTP POST gorevini yapar.

## Ana Uygulama

`rfid_bridge.py`, surekli calisan bir donguyle kart okur.

Varsayilan API endpoint:

```text
http://localhost:5217/api/asrs/rfid-scan
```

Gonderilen JSON:

```json
{
  "cardUid": "AA BB CC DD"
}
```

API cevabi loglanir. Cevap govdesi cok uzunsa log icin kisaltilir.

## Ortam Degiskenleri

Uygulama su environment variable'lari destekler:

```text
ASRS_API_URL
ASRS_HTTP_TIMEOUT_SEC
ASRS_RFID_POLL_INTERVAL_SEC
ASRS_RFID_SAME_CARD_COOLDOWN_SEC
```

Varsayilanlar:

```text
ASRS_API_URL=http://localhost:5217/api/asrs/rfid-scan
ASRS_HTTP_TIMEOUT_SEC=8
ASRS_RFID_POLL_INTERVAL_SEC=0.2
ASRS_RFID_SAME_CARD_COOLDOWN_SEC=2.5
```

## Bagimliliklar

`requirements.txt`:

```text
requests==2.32.3
mfrc522==0.0.7
```

Kurulum:

```bash
pip install -r requirements.txt
```

## Calistirma

Manuel calistirma:

```bash
python3 rfid_bridge.py
```

API baska bir adreste calisiyorsa:

```bash
ASRS_API_URL=http://<api-host>:5217/api/asrs/rfid-scan python3 rfid_bridge.py
```

## systemd Servisi

Servis dosyasi:

```text
asrs-rfid-bridge.service
```

Mevcut servis tanimi su yolu kullanir:

```text
WorkingDirectory=/home/isu/Desktop/ASRS-System/raspberry
ExecStart=/usr/bin/python3 /home/isu/Desktop/ASRS-System/raspberry/rfid_bridge.py
```

Farkli bir kullanici veya farkli proje yolu kullaniliyorsa servis dosyasindaki `User`, `WorkingDirectory` ve `ExecStart` alanlari guncellenmelidir.

Servis kurulumu:

```bash
sudo cp asrs-rfid-bridge.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable asrs-rfid-bridge
sudo systemctl start asrs-rfid-bridge
```

Log takibi:

```bash
journalctl -u asrs-rfid-bridge -f
```

Servis durumu:

```bash
systemctl status asrs-rfid-bridge
```

## API ile Iliski

Raspberry Pi, UID bilgisini `POST /api/asrs/rfid-scan` endpoint'ine yollar. API tarafinda:

1. UID normalize edilir.
2. Aktif `RfidRackMap` kaydi aranir.
3. Eslesen raf hucresi bos ise `Store` komutu kuyruga alinir.
4. Olay `RfidEvent` olarak kaydedilir.

## Gelistirme Notlari

- Mevcut Python bridge Arduino'ya dogrudan seri komut gondermez.
- Arduino seri haberlesmesi API icindeki `AsrsSerialWorker` ile yapilabilir.
- Alternatif mimaride Raspberry Pi, API'den `/api/asrs/commands/next` ile komut cekip Arduino'ya seri porttan iletecek sekilde genisletilebilir.
- Fiziksel Raspberry Pi uzerinde SPI ve GPIO izinlerinin dogru ayarlanmis olmasi gerekir.
