#!/usr/bin/env python3
import os
import signal
import sys
import time
from datetime import datetime
from typing import Optional

import requests
from mfrc522 import MFRC522
import RPi.GPIO as GPIO


API_URL = os.getenv("ASRS_API_URL", "http://localhost:5217/api/asrs/rfid-scan")
HTTP_TIMEOUT_SEC = float(os.getenv("ASRS_HTTP_TIMEOUT_SEC", "8"))
POLL_INTERVAL_SEC = float(os.getenv("ASRS_RFID_POLL_INTERVAL_SEC", "0.2"))
SAME_CARD_COOLDOWN_SEC = float(os.getenv("ASRS_RFID_SAME_CARD_COOLDOWN_SEC", "2.5"))

_running = True


def log(message: str) -> None:
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    print(f"[{ts}] {message}", flush=True)


def stop_handler(signum, frame):
    del signum, frame
    global _running
    _running = False


def post_uid(uid_hex: str) -> None:
    payload = {"cardUid": uid_hex}
    try:
        response = requests.post(API_URL, json=payload, timeout=HTTP_TIMEOUT_SEC)
        body = response.text.strip()
        if len(body) > 300:
            body = body[:300] + "..."
        log(f"POST {API_URL} uid='{uid_hex}' -> {response.status_code} body={body}")
    except requests.RequestException as ex:
        log(f"POST_FAILED uid='{uid_hex}' error={ex}")


def read_uid_hex(reader: MFRC522) -> Optional[str]:
    status, _ = reader.MFRC522_Request(reader.PICC_REQIDL)
    if status != reader.MI_OK:
        return None

    status, uid = reader.MFRC522_Anticoll()
    if status != reader.MI_OK or not uid:
        return None

    # MFRC522_Anticoll can return 5 bytes for 4-byte UID cards.
    # The 5th byte is BCC (xor of first 4 bytes), not part of UID.
    if len(uid) == 5 and (uid[0] ^ uid[1] ^ uid[2] ^ uid[3]) == uid[4]:
        uid = uid[:4]

    return " ".join(f"{b:02X}" for b in uid)


def main() -> int:
    signal.signal(signal.SIGINT, stop_handler)
    signal.signal(signal.SIGTERM, stop_handler)

    reader = MFRC522()
    log(f"RFID bridge started. API_URL={API_URL}")

    last_uid = ""
    last_sent_at = 0.0

    try:
        while _running:
            uid_hex = read_uid_hex(reader)
            if uid_hex is None:
                time.sleep(POLL_INTERVAL_SEC)
                continue

            now = time.time()
            same_recent = uid_hex == last_uid and (now - last_sent_at) < SAME_CARD_COOLDOWN_SEC
            if same_recent:
                time.sleep(POLL_INTERVAL_SEC)
                continue

            log(f"CARD_DETECTED uid='{uid_hex}'")
            post_uid(uid_hex)
            last_uid = uid_hex
            last_sent_at = now
            time.sleep(POLL_INTERVAL_SEC)
    finally:
        GPIO.cleanup()
        log("RFID bridge stopped.")

    return 0


if __name__ == "__main__":
    sys.exit(main())
