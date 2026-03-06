"""
serial_comm.py
--------------
Raspberry Pi ↔ Arduino Mega USB seri haberleşme katmanı.

Protokol (serial_protocol.h ile birebir eşleşir):
    Gönderilen komutlar:
        STORE:<col>:<row>\\n
        RETRIEVE:<col>:<row>\\n
        HOME\\n
        STATUS\\n

    Alınan yanıtlar:
        READY
        BUSY
        OK:<mesaj>
        ERR:<mesaj>

Gerekli kütüphane: pyserial
    pip install pyserial
"""

import time
import logging
import serial
import serial.tools.list_ports
from typing import Optional

logger = logging.getLogger(__name__)

# Varsayılan seri port ayarları
DEFAULT_PORT     = "/dev/ttyUSB0"  # Raspberry Pi'de Arduino için tipik port
DEFAULT_BAUDRATE = 9600
DEFAULT_TIMEOUT  = 120.0           # Komut yanıt bekleme süresi (saniye)
                                   # Uzun hareket süreleri için geniş tutuldu

# Arduino yeniden başlama süresi (seri açıldıktan sonra bootloader bekleme)
ARDUINO_RESET_DELAY = 2.5


class ArduinoComm:
    """
    Arduino Mega ile USB seri haberleşme sınıfı.

    Kullanım:
        comm = ArduinoComm()
        comm.connect()
        comm.wait_for_ready()
        comm.send_store(col=1, row=0)
        comm.disconnect()
    """

    def __init__(
        self,
        port: str = DEFAULT_PORT,
        baudrate: int = DEFAULT_BAUDRATE,
        timeout: float = DEFAULT_TIMEOUT,
    ):
        self._port     = port
        self._baudrate = baudrate
        self._timeout  = timeout
        self._serial: Optional[serial.Serial] = None

    # ─── Bağlantı ─────────────────────────────────────────────────────────────

    def connect(self) -> bool:
        """
        Seri portu açar.
        :return: True başarılı, False hata
        """
        try:
            self._serial = serial.Serial(
                port     = self._port,
                baudrate = self._baudrate,
                bytesize = serial.EIGHTBITS,
                parity   = serial.PARITY_NONE,
                stopbits = serial.STOPBITS_ONE,
                timeout  = 1.0,   # read() için kısa timeout (polling loop)
            )
            logger.info("[SERIAL] Port acildi: %s @ %d baud", self._port, self._baudrate)

            # Arduino reset'ten çıkana kadar bekle
            time.sleep(ARDUINO_RESET_DELAY)
            self._serial.reset_input_buffer()
            return True

        except serial.SerialException as exc:
            logger.error("[SERIAL] Port acilamadi: %s", exc)
            return False

    def disconnect(self):
        """Seri portu kapatır."""
        if self._serial and self._serial.is_open:
            self._serial.close()
            logger.info("[SERIAL] Port kapatildi.")

    def is_connected(self) -> bool:
        return self._serial is not None and self._serial.is_open

    # ─── Mevcut Portları Listele ───────────────────────────────────────────────

    @staticmethod
    def list_ports() -> list:
        """Sistemdeki mevcut seri portları listeler."""
        ports = [p.device for p in serial.tools.list_ports.comports()]
        logger.info("[SERIAL] Mevcut portlar: %s", ports)
        return ports

    # ─── Gönderme ─────────────────────────────────────────────────────────────

    def _send_line(self, line: str):
        """Seri porta bir satır gönderir (satır sonu \\n eklenir)."""
        if not self.is_connected():
            raise ConnectionError("Seri port bağlı değil.")
        message = line.strip() + "\n"
        self._serial.write(message.encode("ascii"))
        self._serial.flush()
        logger.debug("[SERIAL] Gonderildi: %r", message)

    # ─── Alma ──────────────────────────────────────────────────────────────────

    def _read_line(self, timeout: float) -> Optional[str]:
        """
        Seri porttan bir satır okur.
        :param timeout: Maksimum bekleme süresi (saniye)
        :return: Okunan satır (\\n kırpılmış) veya None
        """
        end_time = time.time() + timeout
        while time.time() < end_time:
            if self._serial.in_waiting > 0:
                raw = self._serial.readline()
                line = raw.decode("ascii", errors="ignore").strip()
                if line:
                    logger.debug("[SERIAL] Alindi: %r", line)
                    return line
            time.sleep(0.05)
        logger.warning("[SERIAL] Yanit zaman asimi (%.1fs)", timeout)
        return None

    def _wait_for_response(self, expected_prefix: str, timeout: float) -> Optional[str]:
        """
        Belirli bir önek ile başlayan yanıt gelene kadar bekler.
        BUSY yanıtlarını geçer (işlem devam ediyor).
        :return: Tam yanıt satırı veya None
        """
        end_time = time.time() + timeout
        while time.time() < end_time:
            remaining = end_time - time.time()
            line = self._read_line(timeout=min(remaining, 2.0))

            if line is None:
                continue

            if line == "BUSY":
                logger.info("[SERIAL] Arduino mesgul, bekleniyor...")
                continue

            if line.startswith(expected_prefix):
                return line

            # Beklenmeyen yanıt: loglayıp devam et
            logger.warning("[SERIAL] Beklenmeyen yanit: %r", line)

        return None

    # ─── READY Bekleme ────────────────────────────────────────────────────────

    def wait_for_ready(self, timeout: float = 30.0) -> bool:
        """
        Arduino'dan "READY" yanıtı gelene kadar bekler.
        Sistem açıldığında homing bitmeden READY gelmez.
        :return: True: hazır, False: timeout
        """
        logger.info("[SERIAL] Arduino hazir bekleniyor...")
        resp = self._wait_for_response("READY", timeout)
        if resp:
            logger.info("[SERIAL] Arduino HAZIR.")
            return True
        logger.error("[SERIAL] Arduino READY vermedi.")
        return False

    # ─── Komut Gönderme ───────────────────────────────────────────────────────

    def send_store(self, col: int, row: int) -> bool:
        """
        Depolama komutunu gönderir ve OK yanıtını bekler.
        :return: True başarılı, False hata
        """
        logger.info("[SERIAL] STORE komutu: sutun=%d kat=%d", col, row)
        self._send_line(f"STORE:{col}:{row}")
        resp = self._wait_for_response("OK", self._timeout)
        if resp:
            logger.info("[SERIAL] STORE tamamlandi: %s", resp)
            return True
        logger.error("[SERIAL] STORE yaniti alinamadi.")
        return False

    def send_retrieve(self, col: int, row: int) -> bool:
        """
        Geri alma komutunu gönderir ve OK yanıtını bekler.
        :return: True başarılı, False hata
        """
        logger.info("[SERIAL] RETRIEVE komutu: sutun=%d kat=%d", col, row)
        self._send_line(f"RETRIEVE:{col}:{row}")
        resp = self._wait_for_response("OK", self._timeout)
        if resp:
            logger.info("[SERIAL] RETRIEVE tamamlandi: %s", resp)
            return True
        logger.error("[SERIAL] RETRIEVE yaniti alinamadi.")
        return False

    def send_home(self) -> bool:
        """HOME komutunu gönderir."""
        logger.info("[SERIAL] HOME komutu gonderiliyor...")
        self._send_line("HOME")
        resp = self._wait_for_response("OK", self._timeout)
        return resp is not None

    def send_status(self) -> Optional[str]:
        """STATUS komutunu gönderir ve yanıtı döndürür."""
        self._send_line("STATUS")
        return self._read_line(timeout=5.0)
