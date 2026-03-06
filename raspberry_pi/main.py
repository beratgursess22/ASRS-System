"""
main.py
-------
AS/RS Raspberry Pi Giriş Noktası

Çalıştırma:
    python3 main.py [--port /dev/ttyUSB0] [--log DEBUG]

Komut menüsü (konsol):
    1 → Paket Depola      (RFID okut → boş rafa yerleştir)
    2 → Paket Al          (RFID okut → raftan geri getir)
    3 → Raf Durumu        (veritabanı haritasını göster)
    4 → Arduino Durumu    (konum ve hazır bilgisi)
    5 → Home (Referansla) (eksenleri 0 noktasına götür)
    0 → Çıkış
"""

import argparse
import logging
import sys

from asrs_controller import ASRSController
from serial_comm     import ArduinoComm


# ─── LOGLAMA YAPISILANDIRMASI ─────────────────────────────────────────────────

def setup_logging(level_str: str):
    level = getattr(logging, level_str.upper(), logging.INFO)
    logging.basicConfig(
        level   = level,
        format  = "%(asctime)s [%(levelname)s] %(name)s: %(message)s",
        datefmt = "%H:%M:%S",
        handlers=[
            logging.StreamHandler(sys.stdout),
            logging.FileHandler("asrs.log", encoding="utf-8"),
        ]
    )


# ─── ARGÜMAN AYRIŞTIRICISI ────────────────────────────────────────────────────

def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="AS/RS Otomatik Depolama ve Geri Alma Sistemi"
    )
    parser.add_argument(
        "--port",
        default="/dev/ttyUSB0",
        help="Arduino'nun bağlı olduğu seri port (varsayılan: /dev/ttyUSB0)"
    )
    parser.add_argument(
        "--list-ports",
        action="store_true",
        help="Mevcut seri portları listele ve çık"
    )
    parser.add_argument(
        "--log",
        default="INFO",
        choices=["DEBUG", "INFO", "WARNING", "ERROR"],
        help="Log seviyesi (varsayılan: INFO)"
    )
    return parser.parse_args()


# ─── MENÜ ────────────────────────────────────────────────────────────────────

def print_menu():
    print("\n" + "=" * 40)
    print("   AS/RS KONTROL PANELI")
    print("=" * 40)
    print("  1. Paket Depola")
    print("  2. Paket Geri Al")
    print("  3. Raf Durumunu Goster")
    print("  4. Arduino Durumu")
    print("  5. Home (Eksen Referansla)")
    print("  0. Cikis")
    print("=" * 40)
    print("Secim: ", end="", flush=True)


# ─── ANA DÖNGÜ ────────────────────────────────────────────────────────────────

def main():
    args = parse_args()
    setup_logging(args.log)

    # Sadece port listesi isteniyorsa göster ve çık
    if args.list_ports:
        ports = ArduinoComm.list_ports()
        if ports:
            print("Mevcut seri portlar:")
            for p in ports:
                print(f"  {p}")
        else:
            print("Hicbir seri port bulunamadi.")
        sys.exit(0)

    # ─── Sistem Başlatma ──────────────────────────────────────────────────────
    print(f"\n[MAIN] Arduino portu: {args.port}")
    controller = ASRSController(port=args.port)

    if not controller.initialize():
        print("[HATA] Sistem baslatılamadi. Lütfen bağlantıyı kontrol edin.")
        sys.exit(1)

    # ─── Ana Menü Döngüsü ─────────────────────────────────────────────────────
    try:
        while True:
            print_menu()
            try:
                choice = input().strip()
            except (EOFError, KeyboardInterrupt):
                print("\n[MAIN] Cikis.")
                break

            if choice == "1":
                # ── Depolama ──────────────────────────────────────────────────
                result = controller.run_store()
                if not result:
                    print("[HATA] Depolama islemi basarisiz.")

            elif choice == "2":
                # ── Geri Alma ─────────────────────────────────────────────────
                result = controller.run_retrieve()
                if not result:
                    print("[HATA] Geri alma islemi basarisiz.")

            elif choice == "3":
                # ── Raf Durumu ────────────────────────────────────────────────
                controller.get_db_status()

            elif choice == "4":
                # ── Arduino Durumu ────────────────────────────────────────────
                status = controller.get_status()
                print(f"[Arduino]: {status}")

            elif choice == "5":
                # ── Home ──────────────────────────────────────────────────────
                print("[MAIN] Homlanis basliyor, lutfen bekleyin...")
                if controller.run_home():
                    print("[MAIN] Tum eksenler referanslatildi.")
                else:
                    print("[HATA] Homlama basarisiz.")

            elif choice == "0":
                print("[MAIN] Cikis yapiliyor...")
                break

            else:
                print(f"[UYARI] Gecersiz secim: '{choice}'")

    finally:
        controller.shutdown()
        print("[MAIN] Sistem kapatildi.")


# ─── BAŞLATMA ─────────────────────────────────────────────────────────────────

if __name__ == "__main__":
    main()
