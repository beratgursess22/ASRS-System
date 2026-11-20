#!/usr/bin/env python3
"""Pull AS/RS commands from an API and dispatch them to Arduino safely.

The same gateway runs in Edge and Cloud modes. Only ASRS_API_BASE_URL and
ASRS_ARCHITECTURE change. Dry-run mode is enabled by default so a deployment
cannot send a physical motor command until explicitly configured to do so.
"""

from __future__ import annotations

import argparse
import json
import os
import signal
import sys
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable, Optional


class GatewayError(RuntimeError):
    """Base error for gateway failures."""


class UnsafeGatewayState(GatewayError):
    """Raised when a previous physical command has an unknown outcome."""


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="milliseconds")


def parse_bool(value: str) -> bool:
    normalized = value.strip().lower()
    if normalized in {"1", "true", "yes", "on"}:
        return True
    if normalized in {"0", "false", "no", "off"}:
        return False
    raise ValueError(f"Invalid boolean value: {value!r}")


@dataclass(frozen=True)
class GatewayConfig:
    api_base_url: str
    architecture: str
    dry_run: bool
    serial_port: str
    serial_baud_rate: int
    serial_timeout_sec: float
    http_timeout_sec: float
    poll_interval_sec: float
    ack_retry_count: int
    ack_retry_delay_sec: float
    state_path: Path
    events_path: Path

    @classmethod
    def from_environment(cls) -> "GatewayConfig":
        architecture = os.getenv("ASRS_ARCHITECTURE", "edge").strip().lower()
        if architecture not in {"edge", "cloud"}:
            raise ValueError("ASRS_ARCHITECTURE must be 'edge' or 'cloud'.")

        state_path = Path(
            os.getenv(
                "ASRS_GATEWAY_STATE_PATH",
                str(
                    Path.home()
                    / ".local"
                    / "state"
                    / "asrs-gateway"
                    / architecture
                    / "state.json"
                ),
            )
        )
        events_path = Path(
            os.getenv(
                "ASRS_GATEWAY_EVENTS_PATH",
                str(state_path.with_name("events.jsonl")),
            )
        )
        return cls(
            api_base_url=os.getenv(
                "ASRS_API_BASE_URL", "http://localhost:5217/api/asrs"
            ).rstrip("/"),
            architecture=architecture,
            dry_run=parse_bool(os.getenv("ASRS_GATEWAY_DRY_RUN", "true")),
            serial_port=os.getenv("ASRS_SERIAL_PORT", "/dev/ttyUSB0"),
            serial_baud_rate=int(os.getenv("ASRS_SERIAL_BAUD_RATE", "9600")),
            serial_timeout_sec=float(os.getenv("ASRS_SERIAL_TIMEOUT_SEC", "180")),
            http_timeout_sec=float(os.getenv("ASRS_HTTP_TIMEOUT_SEC", "8")),
            poll_interval_sec=float(os.getenv("ASRS_COMMAND_POLL_INTERVAL_SEC", "0.4")),
            ack_retry_count=max(1, int(os.getenv("ASRS_ACK_RETRY_COUNT", "3"))),
            ack_retry_delay_sec=float(os.getenv("ASRS_ACK_RETRY_DELAY_SEC", "1")),
            state_path=state_path,
            events_path=events_path,
        )


@dataclass(frozen=True)
class AsrsCommand:
    command_id: int
    command_type: str
    row: Optional[int]
    col: Optional[int]

    @classmethod
    def from_payload(cls, payload: dict[str, Any]) -> "AsrsCommand":
        try:
            command_id = int(payload["id"])
            command_type = str(payload["type"]).strip().upper()
        except (KeyError, TypeError, ValueError) as exc:
            raise GatewayError(f"Invalid command payload: {payload!r}") from exc

        if command_type not in {"STORE", "RETRIEVE", "HOME", "STATUS"}:
            raise GatewayError(f"Unsupported command type: {command_type!r}")

        row = payload.get("row")
        col = payload.get("col")
        if command_type in {"STORE", "RETRIEVE"}:
            if row is None or col is None:
                raise GatewayError(f"{command_type} requires row and col.")
            row = int(row)
            col = int(col)
            if not 0 <= row <= 2 or not 0 <= col <= 3:
                raise GatewayError(
                    f"Rack coordinates out of range: row={row}, col={col}"
                )

        return cls(command_id, command_type, row, col)

    def serial_line(self) -> str:
        if self.command_type in {"STORE", "RETRIEVE"}:
            return f"{self.command_type}:{self.col}:{self.row}"
        return self.command_type

    def to_dict(self) -> dict[str, Any]:
        return {
            "id": self.command_id,
            "type": self.command_type,
            "row": self.row,
            "col": self.col,
            "serial_line": self.serial_line(),
        }


class EventRecorder:
    def __init__(self, path: Path, architecture: str):
        self._path = path
        self._architecture = architecture

    def record(self, event: str, **fields: Any) -> None:
        self._path.parent.mkdir(parents=True, exist_ok=True)
        payload = {
            "timestamp_utc": utc_now_iso(),
            "monotonic_ns": time.monotonic_ns(),
            "architecture": self._architecture,
            "event": event,
            **fields,
        }
        with self._path.open("a", encoding="utf-8") as stream:
            stream.write(json.dumps(payload, separators=(",", ":")) + "\n")


class StateStore:
    """Persist dispatch state atomically to favor safety over availability."""

    def __init__(self, path: Path, max_dispatched_ids: int = 1000):
        self._path = path
        self._max_dispatched_ids = max_dispatched_ids

    def _empty(self) -> dict[str, Any]:
        return {"version": 1, "dispatched_command_ids": [], "last_command": None}

    def load(self) -> dict[str, Any]:
        if not self._path.exists():
            return self._empty()
        try:
            state = json.loads(self._path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            raise GatewayError(f"Cannot read gateway state: {self._path}") from exc
        if state.get("version") != 1:
            raise GatewayError("Unsupported gateway state version.")
        return state

    def _save(self, state: dict[str, Any]) -> None:
        self._path.parent.mkdir(parents=True, exist_ok=True)
        temporary = self._path.with_suffix(self._path.suffix + ".tmp")
        temporary.write_text(
            json.dumps(state, indent=2, sort_keys=True) + "\n", encoding="utf-8"
        )
        os.replace(temporary, self._path)

    def was_dispatched(self, command_id: int) -> bool:
        return command_id in self.load().get("dispatched_command_ids", [])

    def mark_dispatch_started(self, command: AsrsCommand) -> None:
        state = self.load()
        dispatched = list(state.get("dispatched_command_ids", []))
        if command.command_id not in dispatched:
            dispatched.append(command.command_id)
        state["dispatched_command_ids"] = dispatched[-self._max_dispatched_ids :]
        state["last_command"] = {
            "command": command.to_dict(),
            "stage": "dispatch_started",
            "pending_ack": None,
            "updated_at_utc": utc_now_iso(),
        }
        self._save(state)

    def mark_ack_pending(self, signal_value: str, message: Optional[str]) -> None:
        state = self.load()
        last = state.get("last_command")
        if not last:
            raise GatewayError("Cannot store an acknowledgement without a command.")
        last["pending_ack"] = {"signal": signal_value, "message": message}
        last["updated_at_utc"] = utc_now_iso()
        self._save(state)

    def mark_ack_delivered(self, signal_value: str) -> None:
        state = self.load()
        last = state.get("last_command")
        if not last:
            raise GatewayError("Cannot complete an acknowledgement without a command.")
        last["pending_ack"] = None
        last["stage"] = {
            "BUSY": "busy",
            "OK": "done",
            "ERR": "failed",
        }[signal_value]
        last["updated_at_utc"] = utc_now_iso()
        self._save(state)

    def pending_ack(self) -> Optional[dict[str, Any]]:
        last = self.load().get("last_command")
        if not last or not last.get("pending_ack"):
            return None
        return {
            "command_id": int(last["command"]["id"]),
            **last["pending_ack"],
        }

    def unresolved_command(self) -> Optional[dict[str, Any]]:
        last = self.load().get("last_command")
        if not last:
            return None
        if last.get("stage") in {"dispatch_started", "busy"}:
            return last
        return None


class ApiClient:
    def __init__(self, base_url: str, timeout_sec: float):
        try:
            import requests
        except ImportError as exc:
            raise GatewayError("requests is required for API communication.") from exc
        self._requests = requests
        self._base_url = base_url.rstrip("/")
        self._timeout_sec = timeout_sec
        self._session = requests.Session()

    def get_next_command(self) -> Optional[AsrsCommand]:
        try:
            response = self._session.get(
                f"{self._base_url}/commands/next", timeout=self._timeout_sec
            )
        except self._requests.RequestException as exc:
            raise GatewayError(f"Command polling failed: {exc}") from exc
        if response.status_code == 204:
            return None
        try:
            response.raise_for_status()
            return AsrsCommand.from_payload(response.json())
        except (self._requests.RequestException, ValueError) as exc:
            raise GatewayError(f"Invalid command response: {exc}") from exc

    def acknowledge(
        self, command_id: int, signal_value: str, message: Optional[str]
    ) -> None:
        try:
            response = self._session.post(
                f"{self._base_url}/commands/{command_id}/ack",
                json={"signal": signal_value, "message": message},
                timeout=self._timeout_sec,
            )
            response.raise_for_status()
        except self._requests.RequestException as exc:
            raise GatewayError(f"Acknowledgement failed: {exc}") from exc

    def close(self) -> None:
        self._session.close()


class DryRunTransport:
    def execute(
        self, command: AsrsCommand, on_signal: Callable[[str], bool]
    ) -> None:
        on_signal("BUSY")
        on_signal("ERR:DRY_RUN_NO_HARDWARE")

    def close(self) -> None:
        return


class SerialTransport:
    def __init__(self, port: str, baud_rate: int, timeout_sec: float):
        try:
            import serial
        except ImportError as exc:
            raise GatewayError(
                "pyserial is required when ASRS_GATEWAY_DRY_RUN=false."
            ) from exc
        self._serial_module = serial
        self._port = port
        self._baud_rate = baud_rate
        self._timeout_sec = timeout_sec
        self._connection = None

    def _ensure_open(self):
        if self._connection is None or not self._connection.is_open:
            self._connection = self._serial_module.Serial(
                self._port,
                self._baud_rate,
                timeout=1,
                write_timeout=5,
            )
        return self._connection

    def execute(
        self, command: AsrsCommand, on_signal: Callable[[str], bool]
    ) -> None:
        connection = self._ensure_open()
        connection.reset_input_buffer()
        connection.write((command.serial_line() + "\n").encode("ascii"))
        connection.flush()

        deadline = time.monotonic() + self._timeout_sec
        while time.monotonic() < deadline:
            line = connection.readline().decode("ascii", errors="replace").strip()
            if not line or line.upper() == "READY":
                continue
            if on_signal(line):
                return
        raise TimeoutError(
            f"Arduino did not finish command {command.command_id} within "
            f"{self._timeout_sec:g} seconds."
        )

    def close(self) -> None:
        if self._connection is not None and self._connection.is_open:
            self._connection.close()


class AsrsGateway:
    def __init__(
        self,
        config: GatewayConfig,
        api: Any,
        transport: Any,
        state: StateStore,
        events: EventRecorder,
    ):
        self._config = config
        self._api = api
        self._transport = transport
        self._state = state
        self._events = events

    def _ack_with_retries(
        self, command_id: int, signal_value: str, message: Optional[str]
    ) -> bool:
        for attempt in range(1, self._config.ack_retry_count + 1):
            attempt_started_ns = time.monotonic_ns()
            self._events.record(
                "ack_attempt",
                command_id=command_id,
                signal=signal_value,
                attempt=attempt,
            )
            try:
                self._api.acknowledge(command_id, signal_value, message)
            except Exception as exc:  # HTTP and test adapters use different errors.
                self._events.record(
                    "ack_failed",
                    command_id=command_id,
                    signal=signal_value,
                    attempt=attempt,
                    duration_ms=(time.monotonic_ns() - attempt_started_ns) / 1_000_000,
                    error=str(exc),
                )
                if attempt < self._config.ack_retry_count:
                    time.sleep(self._config.ack_retry_delay_sec)
                continue

            self._state.mark_ack_delivered(signal_value)
            self._events.record(
                "ack_delivered",
                command_id=command_id,
                signal=signal_value,
                duration_ms=(time.monotonic_ns() - attempt_started_ns) / 1_000_000,
            )
            return True
        return False

    @staticmethod
    def _is_terminal_ok(command: AsrsCommand, response: str) -> bool:
        if response == "OK":
            return True
        expected_marker = {
            "STORE": "STORE_DONE",
            "RETRIEVE": "RETRIEVE_DONE",
            "HOME": "HOMED",
            "STATUS": "OK",
        }[command.command_type]
        return expected_marker in response

    def _record_and_ack_signal(self, command: AsrsCommand, raw_signal: str) -> bool:
        normalized = raw_signal.strip().upper()
        self._events.record(
            "serial_signal",
            command_id=command.command_id,
            raw_signal=raw_signal.strip(),
        )
        if normalized == "BUSY":
            api_signal = "BUSY"
        elif normalized.startswith("OK"):
            if not self._is_terminal_ok(command, normalized):
                self._events.record(
                    "serial_signal_ignored",
                    command_id=command.command_id,
                    raw_signal=raw_signal.strip(),
                    reason="unexpected_completion_marker",
                )
                return False
            api_signal = "OK"
        elif normalized.startswith("ERR") or normalized.startswith("ERROR"):
            api_signal = "ERR"
        else:
            self._events.record(
                "serial_signal_ignored",
                command_id=command.command_id,
                raw_signal=raw_signal.strip(),
            )
            return False

        self._state.mark_ack_pending(api_signal, raw_signal.strip())
        self._ack_with_retries(
            command.command_id, api_signal, raw_signal.strip()
        )
        return api_signal in {"OK", "ERR"}

    def recover_pending_ack(self) -> bool:
        pending = self._state.pending_ack()
        if pending is None:
            return True
        self._events.record(
            "ack_recovery_started",
            command_id=pending["command_id"],
            signal=pending["signal"],
        )
        return self._ack_with_retries(
            pending["command_id"], pending["signal"], pending.get("message")
        )

    def run_once(self) -> bool:
        if not self.recover_pending_ack():
            return False

        unresolved = self._state.unresolved_command()
        if unresolved is not None:
            command_id = unresolved["command"]["id"]
            self._events.record(
                "gateway_blocked_unresolved_command",
                command_id=command_id,
                stage=unresolved["stage"],
            )
            raise UnsafeGatewayState(
                f"Command {command_id} has unknown physical state "
                f"({unresolved['stage']}); manual reconciliation is required."
            )

        poll_started_ns = time.monotonic_ns()
        self._events.record("api_poll_started")
        try:
            command = self._api.get_next_command()
        except Exception as exc:
            self._events.record(
                "api_poll_failed",
                duration_ms=(time.monotonic_ns() - poll_started_ns) / 1_000_000,
                error=str(exc),
            )
            raise
        self._events.record(
            "api_poll_completed",
            duration_ms=(time.monotonic_ns() - poll_started_ns) / 1_000_000,
            command_available=command is not None,
            command_id=command.command_id if command is not None else None,
        )
        if command is None:
            return False

        if self._state.was_dispatched(command.command_id):
            self._events.record(
                "duplicate_command_ignored", command_id=command.command_id
            )
            return False

        # Persist before serial write. A crash can miss a command, but cannot safely
        # justify dispatching the same physical movement a second time.
        self._state.mark_dispatch_started(command)
        self._events.record(
            "command_received",
            command_id=command.command_id,
            operation=command.command_type,
            row=command.row,
            col=command.col,
        )
        self._events.record(
            "serial_write_started",
            command_id=command.command_id,
            serial_line=command.serial_line(),
            dry_run=self._config.dry_run,
        )

        try:
            self._transport.execute(
                command,
                lambda raw_signal: self._record_and_ack_signal(command, raw_signal),
            )
        except Exception as exc:
            self._events.record(
                "command_execution_error",
                command_id=command.command_id,
                error=str(exc),
            )
            # A transport exception does not prove that the physical command
            # failed. Keep the command unresolved and block future dispatches.
            raise UnsafeGatewayState(
                f"Command {command.command_id} transport outcome is unknown; "
                "manual reconciliation is required."
            ) from exc
        return True

    def close(self) -> None:
        self._transport.close()
        close_api = getattr(self._api, "close", None)
        if close_api:
            close_api()


def log(message: str) -> None:
    print(f"[{utc_now_iso()}] {message}", flush=True)


def build_gateway(config: GatewayConfig) -> AsrsGateway:
    state = StateStore(config.state_path)
    events = EventRecorder(config.events_path, config.architecture)
    api = ApiClient(config.api_base_url, config.http_timeout_sec)
    if config.dry_run:
        transport = DryRunTransport()
    else:
        transport = SerialTransport(
            config.serial_port,
            config.serial_baud_rate,
            config.serial_timeout_sec,
        )
    return AsrsGateway(config, api, transport, state, events)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--once", action="store_true", help="Poll and handle at most one command."
    )
    args = parser.parse_args()

    try:
        config = GatewayConfig.from_environment()
    except (TypeError, ValueError) as exc:
        log(f"CONFIG_ERROR {exc}")
        return 2

    running = True

    def stop_handler(signum, frame):
        del signum, frame
        nonlocal running
        running = False

    signal.signal(signal.SIGINT, stop_handler)
    signal.signal(signal.SIGTERM, stop_handler)

    try:
        gateway = build_gateway(config)
    except GatewayError as exc:
        log(f"STARTUP_ERROR {exc}")
        return 2
    log(
        "Gateway started "
        f"architecture={config.architecture} dry_run={config.dry_run} "
        f"api={config.api_base_url}"
    )
    try:
        if args.once:
            try:
                gateway.run_once()
            except UnsafeGatewayState as exc:
                log(f"SAFETY_BLOCK {exc}")
                return 3
            return 0

        while running:
            try:
                gateway.run_once()
            except UnsafeGatewayState as exc:
                log(f"SAFETY_BLOCK {exc}")
                time.sleep(max(5, config.poll_interval_sec))
                continue
            except GatewayError as exc:
                log(f"GATEWAY_ERROR {exc}")
            time.sleep(config.poll_interval_sec)
    finally:
        gateway.close()
        log("Gateway stopped.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
