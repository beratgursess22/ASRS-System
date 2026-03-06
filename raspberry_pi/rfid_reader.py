"""
rfid_reader.py
--------------
Raspberry Pi üzerindeki RFID okuyucuya (RC522 modülü) erişim katmanı.

Donanım bağlantısı (RC522 ↔ Raspberry Pi GPIO):
    SDA  → GPIO 24  (CE0 / Pin 8  → SPI CS0 kullanılabilir, veya GPIO 24)
    SCK  → GPIO 11  (SCLK / Pin 23)
    MOSI → GPIO 10  (Pin 19)
    MISO → GPIO 9   (Pin 21)
    GND  → GND
    RST  → GPIO 25  (Pin 22)
    3.3V → 3.3V

Gerekli kütüphane: mfrc522
    pip install mfrc522
"""

import time
import logging
from typing import Optional

try:
    from mfrc522 import SimpleMFRC522
    import RPi.GPIO as GPIO
    _HW_AVAILABLE = True
except ImportError:
    # Gerçek donanım yoksa (geliştirme/test ortamı) simüle edilir
    _HW_AVAILABLE = False
    logging.warning("[RFID] mfrc522 / RPi.GPIO bulunamadi. Simülasyon modu etkin.")

logger = logging.getLogger(__name__)


class RFIDReader:
    """
    RC522 RFID okuyucu soyutlama sınıfı.

    Kullanım:
        reader = RFIDReader()
        uid = reader.read_uid(timeout=10.0)
        if uid:
            print(f"Kart ID: {uid}")
        reader.cleanup()
    """

    def __init__(self):
        if _HW_AVAILABLE:
            self._reader = SimpleMFRC522()
            logger.info("[RFID] RC522 okuyucu baslatildi.")
        else:
            self._reader = None
            logger.info("[RFID] Simülasyon modu. Kart UID elle girilecek.")

    # ─── Okuma ────────────────────────────────────────────────────────────────

    def read_uid(self, timeout: float = 30.0) -> Optional[str]:
        """
        RFID kartı okur ve UID'yi string olarak döndürür.

        Kart okuma, gerçek donanımda bloklar (SimpleMFRC522.read());
        biz döngüyle timeout ekleyerek non-blocking benzeri davranış sağlarız.

        :param timeout: Maksimum bekleme süresi (saniye). 0 = sonsuz.
        :return: UID string veya None (timeout / hata durumunda)
        """
        start = time.time()
        logger.info("[RFID] Kart bekleniyor... (timeout=%.1fs)", timeout)

        if not _HW_AVAILABLE:
            return self._simulate_read(timeout)

        while True:
            try:
                uid, _ = self._reader.read_no_block()
                if uid is not None:
                    uid_str = str(uid).strip()
                    logger.info("[RFID] Kart okundu: %s", uid_str)
                    return uid_str
            except Exception as exc:
                logger.error("[RFID] Okuma hatası: %s", exc)
                return None

            if timeout > 0 and (time.time() - start) > timeout:
                logger.warning("[RFID] Kart okuma timeout.")
                return None

            time.sleep(0.1)  # CPU'yu meşgul etmemek için kısa bekleme

    def read_uid_blocking(self) -> str:
        """
        Kart okunana kadar bloklar. Timeout yoktur.

        :return: UID string
        """
        logger.info("[RFID] Kart okunana kadar bekleniyor...")

        if not _HW_AVAILABLE:
            return self._simulate_read(timeout=0.0) or "SIM-00000"

        try:
            uid, _ = self._reader.read()   # Bloklar
            uid_str = str(uid).strip()
            logger.info("[RFID] Kart okundu (blocking): %s", uid_str)
            return uid_str
        except Exception as exc:
            logger.error("[RFID] Okuma hatası: %s", exc)
            raise

    # ─── Simülasyon ───────────────────────────────────────────────────────────

    def _simulate_read(self, timeout: float) -> Optional[str]:
        """
        Gerçek donanım yokken konsol girişiyle RFID simüle eder.
        """
        prompt = f"[SIM-RFID] Kart UID girin (timeout={timeout:.0f}s): "
        try:
            uid = input(prompt).strip()
            return uid if uid else None
        except (EOFError, KeyboardInterrupt):
            return None

    # ─── Temizlik ─────────────────────────────────────────────────────────────

    def cleanup(self):
        """GPIO pinlerini serbest bırakır."""
        if _HW_AVAILABLE:
            try:
                GPIO.cleanup()
                logger.info("[RFID] GPIO temizlendi.")
            except Exception:
                pass
