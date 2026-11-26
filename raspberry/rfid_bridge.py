#!/usr/bin/env python3
import json
import os
import signal
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from typing import Optional

import requests
from mfrc522 import MFRC522
import RPi.GPIO as GPIO


API_URL = os.getenv("ASRS_API_URL", "http://localhost:5217/api/asrs/rfid-scan")
HTTP_TIMEOUT_SEC = float(os.getenv("ASRS_HTTP_TIMEOUT_SEC", "8"))
POLL_INTERVAL_SEC = float(os.getenv("ASRS_RFID_POLL_INTERVAL_SEC", "0.2"))
SAME_CARD_COOLDOWN_SEC = float(os.getenv("ASRS_RFID_SAME_CARD_COOLDOWN_SEC", "2.5"))
ARCHITECTURE = os.getenv("ASRS_ARCHITECTURE", "edge").strip().lower()
EVENTS_PATH = Path(
    os.getenv(
        "ASRS_RFID_EVENTS_PATH",
        str(
            Path.home()
            / ".local"
            / "state"
            / "asrs-gateway"
            / ARCHITECTURE
            / "rfid-events.jsonl"
        ),
    )
)

_running = True

if ARCHITECTURE not in {"edge", "cloud"}:
    raise ValueError("ASRS_ARCHITECTURE must be 'edge' or 'cloud'.")


class CardPresenceGate:
    """Allow one event per card presentation, then wait for card removal."""

    def __init__(self, release_after_sec: float) -> None:
        self.release_after_sec = release_after_sec
        self.active_uid = ""
        self.last_seen_at = 0.0

    def observe(self, uid_hex: Optional[str], now: float) -> Optional[str]:
        if uid_hex is None:
            if self.active_uid and (now - self.last_seen_at) >= self.release_after_sec:
                self.active_uid = ""
            return None

        if uid_hex == self.active_uid:
            self.last_seen_at = now
            return None

        self.active_uid = uid_hex
        self.last_seen_at = now
        return uid_hex


def log(message: str) -> None:
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    print(f"[{ts}] {message}", flush=True)


def record_event(event: str, **fields) -> None:
    payload = {
        "timestamp_utc": datetime.now(timezone.utc).isoformat(timespec="milliseconds"),
        "monotonic_ns": time.monotonic_ns(),
        "architecture": ARCHITECTURE,
        "event": event,
        **fields,
    }
    try:
        EVENTS_PATH.parent.mkdir(parents=True, exist_ok=True)
        with EVENTS_PATH.open("a", encoding="utf-8") as stream:
            stream.write(json.dumps(payload, separators=(",", ":")) + "\n")
    except OSError as ex:
        log(f"EVENT_WRITE_FAILED event={event} error={ex}")


def stop_handler(signum, frame):
    del signum, frame
    global _running
    _running = False


def post_uid(uid_hex: str) -> None:
    payload = {"cardUid": uid_hex}
    request_started_ns = time.monotonic_ns()
    record_event("rfid_request_started", card_uid=uid_hex)
    try:
        response = requests.post(API_URL, json=payload, timeout=HTTP_TIMEOUT_SEC)
        duration_ms = (time.monotonic_ns() - request_started_ns) / 1_000_000
        command_id = None
        try:
            response_payload = response.json()
            if isinstance(response_payload, dict):
                command_id = response_payload.get("commandId")
        except requests.exceptions.JSONDecodeError:
            pass
        record_event(
            "rfid_response_received",
            card_uid=uid_hex,
            command_id=command_id,
            http_status=response.status_code,
            duration_ms=duration_ms,
        )
        body = response.text.strip()
        if len(body) > 300:
            body = body[:300] + "..."
        log(f"POST {API_URL} uid='{uid_hex}' -> {response.status_code} body={body}")
    except requests.RequestException as ex:
        record_event(
            "rfid_request_failed",
            card_uid=uid_hex,
            duration_ms=(time.monotonic_ns() - request_started_ns) / 1_000_000,
            error_type=type(ex).__name__,
        )
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
    presence_gate = CardPresenceGate(SAME_CARD_COOLDOWN_SEC)

    try:
        while _running:
            uid_hex = read_uid_hex(reader)
            accepted_uid = presence_gate.observe(uid_hex, time.monotonic())
            if accepted_uid is None:
                time.sleep(POLL_INTERVAL_SEC)
                continue

            log(f"CARD_DETECTED uid='{accepted_uid}'")
            record_event("rfid_detected", card_uid=accepted_uid)
            post_uid(accepted_uid)
            time.sleep(POLL_INTERVAL_SEC)
    finally:
        GPIO.cleanup()
        log("RFID bridge stopped.")

    return 0


if __name__ == "__main__":
    sys.exit(main())
