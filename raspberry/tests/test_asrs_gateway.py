import tempfile
import unittest
from pathlib import Path

from raspberry.asrs_gateway import (
    AsrsCommand,
    AsrsGateway,
    DryRunTransport,
    EventRecorder,
    GatewayConfig,
    StateStore,
    UnsafeGatewayState,
)


class FakeApi:
    def __init__(self, commands=None, ack_failures=0):
        self.commands = list(commands or [])
        self.ack_failures = ack_failures
        self.acks = []
        self.poll_count = 0

    def get_next_command(self):
        self.poll_count += 1
        return self.commands.pop(0) if self.commands else None

    def acknowledge(self, command_id, signal_value, message):
        if self.ack_failures > 0:
            self.ack_failures -= 1
            raise RuntimeError("simulated acknowledgement failure")
        self.acks.append((command_id, signal_value, message))


class FakeTransport:
    def __init__(self, signals):
        self.signals = signals
        self.commands = []

    def execute(self, command, on_signal):
        self.commands.append(command.serial_line())
        for signal_value in self.signals:
            if on_signal(signal_value):
                return

    def close(self):
        return


class FailingTransport(FakeTransport):
    def execute(self, command, on_signal):
        self.commands.append(command.serial_line())
        raise TimeoutError("simulated serial timeout")


class GatewayTests(unittest.TestCase):
    def setUp(self):
        test_temp_root = Path.cwd() / "tmp"
        test_temp_root.mkdir(parents=True, exist_ok=True)
        self.temporary = tempfile.TemporaryDirectory(dir=test_temp_root)
        root = Path(self.temporary.name)
        self.config = GatewayConfig(
            api_base_url="http://example.test/api/asrs",
            architecture="edge",
            dry_run=True,
            serial_port="unused",
            serial_baud_rate=9600,
            serial_timeout_sec=1,
            http_timeout_sec=1,
            poll_interval_sec=0,
            ack_retry_count=2,
            ack_retry_delay_sec=0,
            state_path=root / "state.json",
            events_path=root / "events.jsonl",
        )
        self.state = StateStore(self.config.state_path)
        self.events = EventRecorder(
            self.config.events_path, self.config.architecture
        )

    def tearDown(self):
        self.temporary.cleanup()

    def gateway(self, api, transport):
        return AsrsGateway(self.config, api, transport, self.state, self.events)

    def test_serial_command_format_uses_col_before_row(self):
        store = AsrsCommand.from_payload(
            {"id": 1, "type": "STORE", "row": 2, "col": 3}
        )
        retrieve = AsrsCommand.from_payload(
            {"id": 2, "type": "RETRIEVE", "row": 0, "col": 1}
        )
        self.assertEqual("STORE:3:2", store.serial_line())
        self.assertEqual("RETRIEVE:1:0", retrieve.serial_line())

    def test_store_dispatch_acknowledges_busy_then_ok(self):
        command = AsrsCommand.from_payload(
            {"id": 10, "type": "STORE", "row": 1, "col": 2}
        )
        api = FakeApi([command])
        transport = FakeTransport(["BUSY", "OK:STORE_DONE"])

        self.assertTrue(self.gateway(api, transport).run_once())

        self.assertEqual(["STORE:2:1"], transport.commands)
        self.assertEqual(["BUSY", "OK"], [item[1] for item in api.acks])
        self.assertEqual("done", self.state.load()["last_command"]["stage"])

    def test_retrieve_error_is_reported(self):
        command = AsrsCommand.from_payload(
            {"id": 11, "type": "RETRIEVE", "row": 0, "col": 1}
        )
        api = FakeApi([command])
        transport = FakeTransport(["BUSY", "ERR:RETRIEVE_FAILED"])

        self.gateway(api, transport).run_once()

        self.assertEqual(["BUSY", "ERR"], [item[1] for item in api.acks])
        self.assertEqual("failed", self.state.load()["last_command"]["stage"])

    def test_wrong_ok_marker_does_not_complete_store(self):
        command = AsrsCommand.from_payload(
            {"id": 16, "type": "STORE", "row": 0, "col": 0}
        )
        api = FakeApi([command])
        transport = FakeTransport(["OK:RETRIEVE_DONE", "OK:STORE_DONE"])

        self.gateway(api, transport).run_once()

        self.assertEqual(["OK"], [item[1] for item in api.acks])
        self.assertEqual("OK:STORE_DONE", api.acks[0][2])
        self.assertEqual("done", self.state.load()["last_command"]["stage"])

    def test_dry_run_never_reports_physical_success(self):
        command = AsrsCommand.from_payload(
            {"id": 17, "type": "STORE", "row": 0, "col": 0}
        )
        api = FakeApi([command])

        self.gateway(api, DryRunTransport()).run_once()

        self.assertEqual(["BUSY", "ERR"], [item[1] for item in api.acks])
        self.assertEqual("ERR:DRY_RUN_NO_HARDWARE", api.acks[-1][2])
        self.assertEqual("failed", self.state.load()["last_command"]["stage"])

    def test_duplicate_command_is_not_dispatched_twice(self):
        command = AsrsCommand.from_payload(
            {"id": 12, "type": "STORE", "row": 0, "col": 0}
        )
        api = FakeApi([command, command])
        transport = FakeTransport(["BUSY", "OK:STORE_DONE"])
        gateway = self.gateway(api, transport)

        gateway.run_once()
        gateway.run_once()

        self.assertEqual(["STORE:0:0"], transport.commands)

    def test_terminal_ack_is_recovered_without_redispatch(self):
        command = AsrsCommand.from_payload(
            {"id": 13, "type": "STORE", "row": 0, "col": 0}
        )
        failing_api = FakeApi([command], ack_failures=4)
        first_transport = FakeTransport(["BUSY", "OK:STORE_DONE"])

        self.gateway(failing_api, first_transport).run_once()
        pending = self.state.pending_ack()
        self.assertEqual("OK", pending["signal"])

        recovered_api = FakeApi()
        second_transport = FakeTransport([])
        self.gateway(recovered_api, second_transport).run_once()

        self.assertEqual([], second_transport.commands)
        self.assertEqual(["OK"], [item[1] for item in recovered_api.acks])
        self.assertEqual("done", self.state.load()["last_command"]["stage"])

    def test_unknown_physical_state_blocks_new_polling(self):
        command = AsrsCommand.from_payload(
            {"id": 14, "type": "STORE", "row": 0, "col": 0}
        )
        self.state.mark_dispatch_started(command)
        api = FakeApi()
        transport = FakeTransport([])

        with self.assertRaises(UnsafeGatewayState):
            self.gateway(api, transport).run_once()

        self.assertEqual(0, api.poll_count)
        self.assertEqual([], transport.commands)

    def test_transport_timeout_remains_unresolved_and_is_not_acked_failed(self):
        command = AsrsCommand.from_payload(
            {"id": 15, "type": "STORE", "row": 0, "col": 0}
        )
        api = FakeApi([command])
        transport = FailingTransport([])

        with self.assertRaises(UnsafeGatewayState):
            self.gateway(api, transport).run_once()

        self.assertEqual(["STORE:0:0"], transport.commands)
        self.assertEqual([], api.acks)
        self.assertEqual(
            "dispatch_started", self.state.load()["last_command"]["stage"]
        )


if __name__ == "__main__":
    unittest.main()
