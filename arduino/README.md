# Arduino ASRS Kontrol Yazilimi

Bu klasor, ASRS sisteminin fiziksel hareketlerini yoneten Arduino yazilimini icerir. Arduino, karar veren katman degildir; seri porttan gelen komutlari uygular ve step motorlari kontrol ederek urunu rafa yerlestirir veya raftan geri alir.

## Kullanilan Donanim ve Teknolojiler

- Arduino Mega
- RAMPS pin yapisi
- Step motorlar
- DRV8825/A4988 tarzi step suruculer
- X ve Z limit switch
- GT2 kayis/kasnak mantigina gore step/mm hesabi
- Arduino C/C++
- USB/seri haberlesme

## Klasor Yapisi

```text
arduino/
|-- ASRS_Main/
|   |-- ASRS_Main_Single.ino              # Arduino IDE icin tek dosyalik surum
|   |-- Code/
|   |   |-- main.cpp                      # Ana loop ve komut isleme akisi
|   |   |-- serial_protocol.cpp           # Seri komut parser ve cevap fonksiyonlari
|   |   |-- operations.cpp                # STORE/RETRIEVE operasyonlari
|   |   |-- axes.cpp                      # X/Y/Z eksen hareketleri ve homing
|   |   `-- stepper.cpp                   # Step motor temel surme fonksiyonlari
|   |-- library/
|   |   |-- config.h                      # Pinler, hizlar, raf konumlari, kalibrasyon
|   |   |-- serial_protocol.h
|   |   |-- operations.h
|   |   |-- axes.h
|   |   `-- stepper.h
|   `-- STORE_RETRIEVE_KOMUT_REHBERI.txt  # Komut ve entegrasyon rehberi
`-- raspberyy_pi.txt
```

## Sistem Icindeki Gorevi

Arduino su isleri yapar:

- Seri porttan komut okur.
- Komutu parse eder.
- X ve Z eksenlerinde limit switch ile referans alir.
- X ekseninde hedef raf sutununa gider.
- Z ekseninde hedef raf katina gider.
- Y ekseniyle paketi rafa iter veya raftan alir.
- Islem sonucunu seri porttan bildirir.

Karar mekanizmasi ERP/API tarafindadir. Arduino sadece `STORE`, `RETRIEVE`, `HOME` ve `STATUS` komutlarini uygular.

## Desteklenen Komutlar

Komutlar satir bazli metin protokolu ile gonderilir. Her komut sonunda newline olmalidir.

```text
STORE:<col>:<row>
RETRIEVE:<col>:<row>
HOME
STATUS
```

Ornekler:

```text
STORE:0:2
RETRIEVE:1:0
HOME
STATUS
```

## Raf Indeksleme

Kod tarafinda raf indeksleri 0-based tutulur.

```text
col: 0..3
row: 0..2
```

Yani fiziksel olarak 4 sutun ve 3 kat vardir. UI tarafinda kullaniciya 1-based gosterim yapiliyorsa donusum gerekir:

```text
UI col=1 -> Arduino col=0
UI row=3 -> Arduino row=2
```

## Seri Cevaplar

Arduino islem durumunu su cevaplarla bildirir:

```text
READY
BUSY
OK:STORE_DONE
OK:RETRIEVE_DONE
ERR:<hata_mesaji>
```

API tarafindaki `AsrsSerialWorker`, bu cevaplara gore `AsrsCommand` durumunu gunceller.

## Kalibrasyon ve Konfigurasyon

Ana konfigurasyon dosyasi:

```text
ASRS_Main/library/config.h
```

Bu dosyada:

- RAMPS pin tanimlari
- X, Y, Z step/dir/enable pinleri
- X ve Z limit switch pinleri
- Step/mm hesabi
- Hareket hizlari
- Maksimum eksen mesafeleri
- Raf sutun ve kat konumlari
- Y ekseni hareket mesafesi
- Giris ve cikis hedef Z seviyeleri
- Seri baud rate

tanimlanir.

Guncel raf konumu sabitleri:

```text
SHELF_COLS = 4
SHELF_ROWS = 3
SHELF_X_POS = 160, 320, 480, 640 mm
SHELF_Z_POS = 250, 500, 750 mm
SERIAL_BAUD_RATE = 9600
STEPS_PER_MM = 160
```

Sahada ozellikle su degerler mekanige gore test edilmelidir:

- `SHELF_X_POS`
- `SHELF_Z_POS`
- `Y_TRAVEL_MM`
- `Z_APPROACH_OFFSET_MM`
- `ENTRY_PICK_TARGET_Z_MM`
- `EXIT_DROP_TARGET_Z_MM`

## STORE Akisi

`STORE:<col>:<row>` komutu geldiginde:

1. Komut ve raf araligi dogrulanir.
2. Arduino `BUSY` cevabi verir.
3. Sistem gerekli referans/hareket adimlarini calistirir.
4. Paket giris noktasindan alinir.
5. X ekseni hedef sutuna gider.
6. Z ekseni hedef kata gider.
7. Y ekseni paketi rafa birakir.
8. Basariliysa `OK:STORE_DONE`, hata varsa `ERR:*` doner.

## RETRIEVE Akisi

`RETRIEVE:<col>:<row>` komutu geldiginde:

1. Komut ve raf araligi dogrulanir.
2. Arduino `BUSY` cevabi verir.
3. X/Z eksenleri hedef raf hucresine gider.
4. Y ekseni paketi raftan alir.
5. Paket cikis/teslim noktasina tasinir.
6. Basariliysa `OK:RETRIEVE_DONE`, hata varsa `ERR:*` doner.

## Gelistirme Notlari

- `ASRS_Main_Single.ino`, Arduino IDE ile hizli yukleme icin tutulur.
- `Code/` ve `library/` altindaki moduler yapi, kodun okunabilirligini ve bakimini kolaylastirir.
- ERP/API tarafindaki komut formatlari ile Arduino parser ayni kalmalidir.
- Arduino, RFID UID okumaz; RFID okuma Raspberry Pi tarafindadir.
