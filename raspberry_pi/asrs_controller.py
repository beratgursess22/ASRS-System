"""
asrs_controller.py
------------------
AS/RS sisteminin Raspberry Pi tarafındaki ana kontrol katmanı.

Bu modül; rfid_reader, shelf_database ve serial_comm modüllerini
bir araya getirerek yüksek seviyeli depolama ve geri alma iş akışlarını
yönetir.

İş akışı:
  Depolama (store):
    1. RFID kart okutulur → UID alınır
    2. Kart daha önce depolandı mı kontrol edilir
    3. Boş raf gözü bulunur
    4. Arduino'ya STORE komutu gönderilir
    5. Veritabanı güncellenir

  Geri Alma (retrieve):
    1. RFID kart okutulur → UID alınır
    2. Kartın hangi raf gözünde olduğu sorgulanır
    3. Arduino'ya RETRIEVE komutu gönderilir
    4. Veritabanından kaydı silinir
"""

import logging
from enum import Enum, auto
from typing import Optional, Tuple

from rfid_reader    import RFIDReader
from shelf_database import ShelfDatabase
from serial_comm    import ArduinoComm

logger = logging.getLogger(__name__)


# ─── DURUM ENUM ───────────────────────────────────────────────────────────────

class SystemState(Enum):
    IDLE        = auto()   # Komut bekliyor
    READING_RFID = auto()  # RFID okunuyor
    BUSY        = auto()   # Arduino hareket ediyor
    ERROR       = auto()   # Hata durumu


# ─── KONTROL SINIFI ──────────────────────────────────────────────────────────

class ASRSController:
    """
    Raspberry Pi tarafı merkezi kontrolcü.

    Kullanım:
        ctrl = ASRSController(port="/dev/ttyUSB0")
        ctrl.initialize()   # Arduino bağlantısı + READY bekleme
        ctrl.run_store()    # RFID oku → depolama
        ctrl.run_retrieve() # RFID oku → geri alma
        ctrl.shutdown()
    """

    def __init__(self, port: str = "/dev/ttyUSB0"):
        self._rfid   = RFIDReader()
        self._db     = ShelfDatabase()
        self._comm   = ArduinoComm(port=port)
        self._state  = SystemState.IDLE

    # ─── Başlatma / Kapatma ───────────────────────────────────────────────────

    def initialize(self) -> bool:
        """
        Arduino'ya bağlanır ve homing tamamlanana kadar bekler.
        :return: True başarılı, False hata
        """
        logger.info("[CTRL] Sistem baslatiliyor...")

        if not self._comm.connect():
            logger.error("[CTRL] Arduino baglantisi kurulamadi.")
            self._state = SystemState.ERROR
            return False

        logger.info("[CTRL] Arduino homing tamamlanana dek bekleniyor...")
        if not self._comm.wait_for_ready(timeout=60.0):
            logger.error("[CTRL] Arduino hazir durumuna gelemedi.")
            self._state = SystemState.ERROR
            return False

        self._state = SystemState.IDLE
        logger.info("[CTRL] Sistem hazir.")
        return True

    def shutdown(self):
        """Kaynakları serbest bırakır."""
        self._comm.disconnect()
        self._rfid.cleanup()
        logger.info("[CTRL] Sistem kapatildi.")

    # ─── Depolama İş Akışı ────────────────────────────────────────────────────

    def run_store(self) -> bool:
        """
        Tam depolama senaryosunu çalıştırır:
          RFID oku → boş slot bul → Arduino'ya STORE gönder → DB kaydet.
        :return: True başarılı, False hata
        """
        if self._state != SystemState.IDLE:
            logger.warning("[CTRL] Sistem mesgul, islem reddedildi.")
            return False

        # 1. RFID oku
        self._state = SystemState.READING_RFID
        print("[CTRL] Lutfen RFID kart okutun (DEPOLAMA)...")
        uid = self._rfid.read_uid_blocking()

        if not uid:
            logger.error("[CTRL] RFID okunamadi.")
            self._state = SystemState.IDLE
            return False

        logger.info("[CTRL] UID: %s", uid)

        # 2. Kart zaten depolanmış mı?
        existing_pos = self._db.find_uid(uid)
        if existing_pos:
            col, row = existing_pos
            logger.warning(
                "[CTRL] Bu kart zaten depolanmis: sutun=%d kat=%d", col, row
            )
            print(f"[UYARI] Bu paket zaten rafta! (Sutun {col+1}, Kat {row+1})")
            self._state = SystemState.IDLE
            return False

        # 3. Boş raf gözü bul
        slot = self._db.find_empty_slot()
        if slot is None:
            logger.error("[CTRL] Bos raf gozu yok!")
            print("[HATA] Tum raf gozleri dolu, islem iptal.")
            self._state = SystemState.IDLE
            return False

        col, row = slot
        print(f"[CTRL] Hedef: Sutun {col+1}, Kat {row+1}")

        # 4. Arduino'ya gönder
        self._state = SystemState.BUSY
        success = self._comm.send_store(col, row)

        if not success:
            logger.error("[CTRL] Arduino STORE komutu basarisiz.")
            self._state = SystemState.ERROR
            return False

        # 5. Veritabanını güncelle
        self._db.store(uid=uid, col=col, row=row)
        self._db.print_status()

        self._state = SystemState.IDLE
        print(f"[CTRL] Depolama tamamlandi: UID={uid} → Sutun {col+1}, Kat {row+1}")
        return True

    # ─── Geri Alma İş Akışı ───────────────────────────────────────────────────

    def run_retrieve(self) -> bool:
        """
        Tam geri alma senaryosunu çalıştırır:
          RFID oku → DB'den konum bul → Arduino'ya RETRIEVE gönder → DB sil.
        :return: True başarılı, False hata
        """
        if self._state != SystemState.IDLE:
            logger.warning("[CTRL] Sistem mesgul, islem reddedildi.")
            return False

        # 1. RFID oku
        self._state = SystemState.READING_RFID
        print("[CTRL] Lutfen RFID kart okutun (GERI ALMA)...")
        uid = self._rfid.read_uid_blocking()

        if not uid:
            logger.error("[CTRL] RFID okunamadi.")
            self._state = SystemState.IDLE
            return False

        logger.info("[CTRL] UID: %s", uid)

        # 2. Kartın konumunu bul
        pos = self._db.find_uid(uid)
        if pos is None:
            logger.warning("[CTRL] Bu UID veritabaninda yok: %s", uid)
            print(f"[UYARI] Bu kart ({uid}) sistemde kayitli degil!")
            self._state = SystemState.IDLE
            return False

        col, row = pos
        print(f"[CTRL] Paket konumu: Sutun {col+1}, Kat {row+1}")

        # 3. Arduino'ya gönder
        self._state = SystemState.BUSY
        success = self._comm.send_retrieve(col, row)

        if not success:
            logger.error("[CTRL] Arduino RETRIEVE komutu basarisiz.")
            self._state = SystemState.ERROR
            return False

        # 4. Veritabanından sil
        self._db.remove(col, row)
        self._db.print_status()

        self._state = SystemState.IDLE
        print(f"[CTRL] Geri alma tamamlandi: UID={uid} ← Sutun {col+1}, Kat {row+1}")
        return True

    # ─── Yardımcı Komutlar ────────────────────────────────────────────────────

    def run_home(self) -> bool:
        """Tüm eksenleri referans noktasına götürür."""
        logger.info("[CTRL] HOME komutu gonderiliyor...")
        return self._comm.send_home()

    def get_status(self) -> Optional[str]:
        """Arduino'dan durum sorgular."""
        return self._comm.send_status()

    def get_db_status(self):
        """Raf veritabanı durumunu konsola yazdırır."""
        self._db.print_status()

    @property
    def state(self) -> SystemState:
        return self._state
