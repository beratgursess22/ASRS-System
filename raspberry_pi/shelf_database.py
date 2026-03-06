"""
shelf_database.py
-----------------
Raf durum yönetimi ve RFID–raf eşleştirme veritabanı.

Sorumluluklar:
  - Her raf gözünün dolu/boş durumunu takip etmek
  - RFID UID'sini hangi raf gözünde sakladığını kayıt altında tutmak
  - Yeni paket için boş raf gözü bulmak (sütun 0'dan başlayarak, en alt kattan)
  - Verinin JSON dosyasında kalıcı olarak saklanması

Raf düzeni:
    4 Sütun (col: 0–3) × 3 Kat (row: 0–2) = 12 raf gözü
    (col, row) koordinat sistemi kullanılır.

JSON dosya formatı (shelf_state.json):
    {
        "slots": {
            "0,0": {"uid": "123456789", "stored_at": "2026-03-06T10:30:00"},
            "1,2": {"uid": "987654321", "stored_at": "2026-03-06T11:00:00"},
            ...
        },
        "uid_map": {
            "123456789": [0, 0],
            "987654321": [1, 2]
        }
    }
"""

import json
import logging
import os
from datetime import datetime
from typing import Optional, Tuple, Dict

logger = logging.getLogger(__name__)

# Raf boyutları (config.h ile tutarlı olmalı)
SHELF_COLS = 4
SHELF_ROWS = 3

# Kalıcı depolama dosyası (modülle aynı dizin)
DEFAULT_STATE_FILE = os.path.join(os.path.dirname(__file__), "shelf_state.json")


class ShelfDatabase:
    """
    Raf durum veritabanı.

    Kullanım:
        db = ShelfDatabase()
        col, row = db.find_empty_slot()
        db.store(uid="ABCD1234", col=col, row=row)
        ...
        pos = db.find_uid("ABCD1234")   # → (col, row)
        db.remove(col=col, row=row)
    """

    def __init__(self, state_file: str = DEFAULT_STATE_FILE):
        self._state_file = state_file
        # slots: {(col, row): {"uid": str, "stored_at": str}}
        self._slots: Dict[Tuple[int, int], dict] = {}
        # uid_map: {uid_str: (col, row)}
        self._uid_map: Dict[str, Tuple[int, int]] = {}

        self._load()

    # ─── Kalıcı Depolama ──────────────────────────────────────────────────────

    def _load(self):
        """JSON dosyasından durum verilerini yükler."""
        if not os.path.exists(self._state_file):
            logger.info("[DB] Durum dosyasi bulunamadi, bos veritabani olusturuluyor.")
            return

        try:
            with open(self._state_file, "r", encoding="utf-8") as f:
                data = json.load(f)

            self._slots.clear()
            self._uid_map.clear()

            for key, val in data.get("slots", {}).items():
                col_str, row_str = key.split(",")
                pos = (int(col_str), int(row_str))
                self._slots[pos] = val

            for uid, pos_list in data.get("uid_map", {}).items():
                self._uid_map[uid] = tuple(pos_list)

            logger.info("[DB] %d kayit yuklendi: %s", len(self._slots), self._state_file)

        except (json.JSONDecodeError, KeyError, ValueError) as exc:
            logger.error("[DB] Durum dosyasi okunamadi: %s", exc)

    def _save(self):
        """Mevcut durumu JSON dosyasına yazar."""
        data = {
            "slots": {
                f"{col},{row}": val
                for (col, row), val in self._slots.items()
            },
            "uid_map": {
                uid: list(pos)
                for uid, pos in self._uid_map.items()
            }
        }
        try:
            with open(self._state_file, "w", encoding="utf-8") as f:
                json.dump(data, f, indent=2, ensure_ascii=False)
            logger.debug("[DB] Durum dosyasina kaydedildi.")
        except OSError as exc:
            logger.error("[DB] Kaydetme hatasi: %s", exc)

    # ─── Slot Sorguları ───────────────────────────────────────────────────────

    def is_slot_empty(self, col: int, row: int) -> bool:
        """Belirtilen raf gözü boş mu?"""
        return (col, row) not in self._slots

    def is_slot_valid(self, col: int, row: int) -> bool:
        """Koordinatlar raf sınırları içinde mi?"""
        return 0 <= col < SHELF_COLS and 0 <= row < SHELF_ROWS

    def find_empty_slot(self) -> Optional[Tuple[int, int]]:
        """
        Tarama sırasıyla (önce kat, sonra sütun) ilk boş raf gözünü bulur.
        Tarama sırası: Kat 0 → Kat 2, her katta Sütun 0 → Sütun 3.
        :return: (col, row) veya None (tüm raflar dolu)
        """
        for row in range(SHELF_ROWS):
            for col in range(SHELF_COLS):
                if self.is_slot_empty(col, row):
                    logger.info("[DB] Bos slot bulundu: sutun=%d kat=%d", col, row)
                    return (col, row)
        logger.warning("[DB] Tum raf gozleri dolu!")
        return None

    def find_uid(self, uid: str) -> Optional[Tuple[int, int]]:
        """
        Verilen UID'nin hangi raf gözünde saklandığını döndürür.
        :return: (col, row) veya None
        """
        pos = self._uid_map.get(uid)
        if pos:
            logger.info("[DB] UID %s -> sutun=%d kat=%d", uid, pos[0], pos[1])
        else:
            logger.info("[DB] UID %s bulunamadi.", uid)
        return pos

    def get_uid_at(self, col: int, row: int) -> Optional[str]:
        """
        Belirtilen raf gözündeki paketin UID'sini döndürür.
        :return: UID string veya None
        """
        entry = self._slots.get((col, row))
        return entry["uid"] if entry else None

    # ─── Depolama / Silme ─────────────────────────────────────────────────────

    def store(self, uid: str, col: int, row: int) -> bool:
        """
        Paketi veritabanına kaydeder.
        :return: True başarılı, False slot dolu veya geçersiz
        """
        if not self.is_slot_valid(col, row):
            logger.error("[DB] Gecersiz konum: sutun=%d kat=%d", col, row)
            return False

        if not self.is_slot_empty(col, row):
            logger.error("[DB] Slot zaten dolu: sutun=%d kat=%d", col, row)
            return False

        timestamp = datetime.now().isoformat(timespec="seconds")
        self._slots[(col, row)] = {"uid": uid, "stored_at": timestamp}
        self._uid_map[uid] = (col, row)
        self._save()

        logger.info("[DB] Paket kaydedildi: UID=%s sutun=%d kat=%d", uid, col, row)
        return True

    def remove(self, col: int, row: int) -> Optional[str]:
        """
        Raf gözündeki paketi veritabanından siler.
        :return: Silinen paketin UID'si veya None
        """
        if not self.is_slot_valid(col, row):
            logger.error("[DB] Gecersiz konum: sutun=%d kat=%d", col, row)
            return None

        entry = self._slots.pop((col, row), None)
        if entry is None:
            logger.warning("[DB] Slot zaten bos: sutun=%d kat=%d", col, row)
            return None

        uid = entry["uid"]
        self._uid_map.pop(uid, None)
        self._save()

        logger.info("[DB] Paket silindi: UID=%s sutun=%d kat=%d", uid, col, row)
        return uid

    # ─── Durum Görüntüleme ────────────────────────────────────────────────────

    def print_status(self):
        """Konsola raf durum haritasını yazdırır."""
        print("\n=== RAF DURUMU (Kat \\ Sutun) ===")
        header = "     " + "  ".join(f"S{c+1}" for c in range(SHELF_COLS))
        print(header)
        print("     " + "─" * (SHELF_COLS * 4))

        for row in range(SHELF_ROWS - 1, -1, -1):  # En üstten alta
            row_str = f"K{row+1} │ "
            for col in range(SHELF_COLS):
                if self.is_slot_empty(col, row):
                    row_str += "[ ] "
                else:
                    row_str += "[X] "
            print(row_str)

        stored = len(self._slots)
        total  = SHELF_COLS * SHELF_ROWS
        print(f"\nDoluluk: {stored}/{total}")
        print("================================\n")

    def get_all_stored(self) -> Dict[Tuple[int, int], dict]:
        """Tüm dolu raf gözlerini döndürür."""
        return dict(self._slots)
