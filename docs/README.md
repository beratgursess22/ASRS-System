# SmartRack Proje Raporu

Bu klasor, SmartRack AS/RS projesinin tam final raporunu icerir. Rapor, otomatik depolama ve geri alma sistemi prototipinin mekanik tasarimini, gomulu sistem mimarisini, ERP yazilim katmanini, RFID entegrasyonunu ve endustri muhendisligi analizlerini birlikte aciklar.

## Raporu Goruntule

- [SmartRack Final Report PDF](./SmartRack_FinalReport.pdf)

## Rapor Bilgileri

- Baslik: SmartRack: a Smart Automated Storage and Retrieval System (AS/RS)
- Rapor turu: Capstone Project Final Report
- Sayfa sayisi: 104
- Tarih: Mayis 2026
- Ogrenciler: Asude Hazal Peker, Duygu Kudat, Ibrahim Berat Gurses
- Danismanlar: Assoc. Prof. Saliha Karadayi Usta, Asst. Prof. Husamettin Osmanoglu

## Kisa Ozet

SmartRack, manuel depo operasyonlarindaki is gucu bagimliligi, hatali yerlestirme/geri alma, dusuk izlenebilirlik ve gereksiz malzeme hareketi problemlerini azaltmak icin gelistirilmis dusuk maliyetli bir AS/RS prototipidir.

Sistem; 3x4 raf yapisi, uc eksenli hareket mekanizmasi, RFID tabanli kimliklendirme, Arduino Mega ve RAMPS 1.6 ile motor kontrolu, Raspberry Pi ile RFID/komunikasyon katmani, MySQL veritabani ve ASP.NET Core MVC tabanli ERP arayuzunden olusur.

## Sistem Kapsami

Raporda sistem su ana basliklarla incelenir:

- Manuel depo sistemlerindeki problemler ve AS/RS ihtiyaci
- Literaturde AS/RS, slotting, enerji, otomasyon ve depo yonetimi yaklasimlari
- SmartRack mekanik tasarimi ve 3x4 raf prototipi
- X, Y ve Z eksenli hareket sistemi
- Arduino Mega, RAMPS 1.6, DRV8825, NEMA 17 motorlar ve limit switch yapisi
- Raspberry Pi ve MFRC522 RFID okuyucu ile UID okuma
- ERP, REST API, MySQL veritabani ve komut kuyrugu mimarisi
- STORE ve RETRIEVE operasyon akislari
- Endustri muhendisligi analizleri ve proje sonuclari

## Donanim ve Yazilim Ozeti

Mekanik prototip 130 cm uzunluk, 100 cm yukseklik ve 25 cm derinlikte tasarlanmistir. Raf sistemi 4 sutun ve 3 kat olmak uzere toplam 12 hucreden olusur. Raf araliklari yatay ve dikey olarak 25 cm olacak sekilde planlanmistir.

Hareket sistemi:

- X ekseni: yatay raf konumlandirma
- Z ekseni: dikey kat konumlandirma
- Y ekseni: paketi rafa itme veya raftan alma

Kontrol sistemi:

- Arduino Mega 2560 dusuk seviyeli step motor kontrolunu yapar.
- RAMPS 1.6 ve DRV8825 suruculer motorlari yonetir.
- Raspberry Pi RFID okuma ve ust seviye haberlesme gorevini ustlenir.
- ERP/API katmani komut uretir, veritabani kayitlarini tutar ve sistemi izler.

## Endustri Muhendisligi Analizleri

Rapor sadece teknik prototipi degil, sistemin endustriyel uygulanabilirligini de analiz eder.

### ABC-Pareto Slotting Analizi

ABC analizi, urunlerin geri alma talep frekansina gore raf yerlesimini optimize etmek icin kullanilmistir. Sonuclara gore:

- A sinifi urunler en yuksek operasyonel talebe sahiptir.
- A segmenti toplam 18,875 talep birimi uretmistir.
- B segmenti 5,710 talep birimi, C segmenti 1,100 talep birimi uretmistir.
- Yuksek frekansli A urunleri giris/cikis noktasina daha yakin hucrelere atanmalidir.

Bu yaklasim seyahat mesafesini, islem suresini ve gereksiz enerji tuketimini azaltmayi hedefler.

<img width="321" height="193" alt="Screenshot 2026-05-24 at 18 16 06" src="https://github.com/user-attachments/assets/000b0e53-b6e9-40b8-9d58-3eb68fbf5827" />
<img width="320" height="187" alt="Screenshot 2026-05-24 at 18 16 16" src="https://github.com/user-attachments/assets/2302b0cc-c4c4-4528-ad4c-c58794cf1dde" />
<img width="422" height="128" alt="Screenshot 2026-05-24 at 18 16 10" src="https://github.com/user-attachments/assets/77ff729a-8157-492c-bab6-316b5a112f0b" />


### AHP Performans Degerlendirmesi

AHP yontemi, SmartRack ile forklift tabanli geleneksel depo yaklasimini performans kriterleri uzerinden karsilastirmak icin kullanilmistir.

Degerlendirilen kriterler:

- Retrieval & Storage Speed
- Accuracy
- Safety
- Cost Efficiency
- Energy Efficiency

Rapor sonucuna gore Retrieval & Storage Speed ve Safety en kritik kriterler arasindadir. Tutarlilik orani kabul edilebilir sinir olan 0.10'un altinda kaldigi icin AHP karsilastirmalari tutarli kabul edilmistir.

<img width="150" height="97" alt="Screenshot 2026-05-24 at 18 17 44" src="https://github.com/user-attachments/assets/d81a059a-9f6b-4226-a9f9-bb8c33f69118" />
<img width="168" height="133" alt="Screenshot 2026-05-24 at 18 17 39" src="https://github.com/user-attachments/assets/b13ca69c-8033-46fb-9973-b2d53610de25" />
<img width="417" height="127" alt="Screenshot 2026-05-24 at 18 17 35" src="https://github.com/user-attachments/assets/b837a34c-66c8-40d8-a8b3-19448aad72cd" />


### Maliyet-Fayda ve Geri Odeme Analizi

Cost-benefit ve payback analizinde CAPEX, OPEX, is gucu ihtiyaci, bakim maliyeti, operasyonel tasarruf ve uzun vadeli ekonomik uygunluk birlikte incelenmistir.

One cikan sonuclar:

- SmartRack, forklift tabanli yapiya gore daha dusuk yillik operasyon maliyeti sunar.
- Otomasyon is gucu bagimliligini azaltir.
- Is gucu ihtiyaci 10 operatorden 2 operatore dusurulerek yaklasik %80 is gucu tasarrufu saglanabilecegi hesaplanmistir.
- Benefit-Cost Ratio degeri 1'in uzerindedir.
- Tahmini geri odeme suresi yaklasik 2 yildir.

<img width="700" height="517" alt="Screenshot 2026-05-24 at 18 18 30" src="https://github.com/user-attachments/assets/1a5742b7-621b-4d6d-ae01-ee18ac5906db" />
<img width="819" height="193" alt="Screenshot 2026-05-24 at 18 18 35" src="https://github.com/user-attachments/assets/695dadea-47d8-4ccf-8a64-54691acfddf8" />
<img width="358" height="383" alt="Screenshot 2026-05-24 at 18 18 42" src="https://github.com/user-attachments/assets/aa634cf2-434e-49a6-9c81-a8eae6407ace" />
<img width="819" height="114" alt="Screenshot 2026-05-24 at 18 18 52" src="https://github.com/user-attachments/assets/311e5f04-91b5-4a84-8b67-b68a09f9c69a" />
<img width="478" height="363" alt="Screenshot 2026-05-24 at 18 18 56" src="https://github.com/user-attachments/assets/f6b50f47-78f7-4ae7-a86d-10f173a6a365" />


### Karbon Ayak Izi Analizi

Karbon ayak izi analizi, LPG forklift, elektrikli forklift ve Smart AS/RS alternatiflerini 10 yillik isletim donemi icin karsilastirir.

Sonuclar:

- LPG forklift: 428.56 ton CO2
- Elektrikli forklift basit hesap: 140.24 ton CO2
- Elektrikli forklift batarya/sarj kayiplari dahil: 311.64 ton CO2
- Smart AS/RS dogrudan operasyonel karbon ayak izi: 39.62 ton CO2
- Smart AS/RS isitma etkisi dahil kismi toplam: 189.36 ton CO2

Bu sonuclar, Smart AS/RS yapisinin dogrudan operasyonel emisyon acisindan en dusuk karbon ayak izine sahip alternatif oldugunu gosterir.

<img width="586" height="426" alt="Screenshot 2026-05-24 at 18 20 05" src="https://github.com/user-attachments/assets/84de3327-b1fd-4e98-ac6a-7752f0f9aac9" />
<img width="813" height="417" alt="Screenshot 2026-05-24 at 18 20 12" src="https://github.com/user-attachments/assets/b14cb20f-e3ec-4746-9f20-96b6d23e6513" />

### FMEA Risk Analizi

FMEA, sistemdeki olasi hata modlarini onceliklendirmek icin kullanilmistir. Severity, Occurrence ve Detection degerleri ile RPN hesaplanmistir.

En kritik riskler:

- Nesne yerlestirme sirasinda hatali konumlandirma: RPN 100
- Z ekseni / shuttle hareket problemi: RPN 100
- Controller haberlesme problemi: RPN 54
- Raf yapisal stabilitesi: RPN 50
- X ve Y ekseni hareket kontrol riskleri: RPN 48

Rapor, ozellikle kalibrasyon, limit switch kontrolu, hareket testleri ve duzenli mekanik denetimi oncelikli iyilestirme alanlari olarak belirtir.

<img width="606" height="502" alt="Screenshot 2026-05-24 at 18 20 18" src="https://github.com/user-attachments/assets/25bdc6c6-faf7-48ce-b81b-452576f9edf3" />

## Test ve Sonuclar

Raporun test sonuclari, SmartRack prototipinin otonom depolama ve geri alma islemlerini prototip olceginde basarili sekilde gerceklestirdigini gosterir.

Dogrulanan alanlar:

- Mekanik raf ve shuttle hareketi
- Arduino Mega, RAMPS 1.6 ve DRV8825 tabanli motor kontrolu
- X/Z homing mekanizmasi
- RFID UID okuma ve ERP ile eslestirme
- Limit switch geri bildirimi
- STORE ve RETRIEVE komut akislari
- ERP dashboard ile raf doluluk takibi
- MySQL veritabani ve komut kuyrugu
- REST API uzerinden sistem entegrasyonu

## Sinirlamalar

Rapor, prototipin bazi sinirlarini da belirtir:

- Sistem endustriyel olcekte agir yuk ve surekli calisma kosullarinda test edilmemistir.
- Y ekseninde titresim, Z ekseninde yer yer asagi kayma ve kayis atlama problemleri gozlemlenmistir.
- Sistem yerel ve kontrollu ag ortaminda test edilmistir.
- Yapay zeka destekli rota optimizasyonu, tahmine dayali bakim ve gelismis envanter tahmini bu kapsamda uygulanmamistir.
- Endustri hesaplamalari prototip olcegindeki varsayimlara ve sinirli uzun donem veriye dayanmaktadir.

## Gelecek Gelistirmeler

Raporda onerilen gelecek calismalar:

- Daha guclu mekanik yapi ve endustriyel motorlar
- Daha yuksek tasima kapasiteli raf sistemi
- Ayrilmis giris ve cikis noktalari
- Konveyor destekli urun giris/cikis yapisi
- Y ekseni ve sinir noktalarina ek sensorler
- PLC tabanli endustriyel kontrol mimarisi
- Es zamanli X/Z eksen hareketi
- Kamera ve goruntu isleme ile raf doluluk dogrulama
- Bulut tabanli izleme ve uzaktan erisim
- AI tabanli slotting, rota optimizasyonu ve predictive maintenance

## Sonuc

SmartRack raporu, projenin yalnizca calisan bir teknik prototip olmadigini; ayni zamanda operasyonel, ekonomik, cevresel ve risk yonetimi acisindan olculebilir bir depo otomasyon cozumunu temsil ettigini ortaya koyar. Sistem, Industry 4.0 yaklasimina uygun olarak RFID, gomulu otomasyon, ERP destekli envanter yonetimi ve analitik karar verme yontemlerini tek prototipte birlestirir.
