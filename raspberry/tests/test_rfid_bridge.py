import unittest

from raspberry.rfid_bridge import CardPresenceGate


class CardPresenceGateTests(unittest.TestCase):
    def test_first_card_is_accepted(self):
        gate = CardPresenceGate(release_after_sec=2.5)

        self.assertEqual("AA BB CC DD", gate.observe("AA BB CC DD", 10.0))

    def test_held_card_is_not_repeated_after_cooldown(self):
        gate = CardPresenceGate(release_after_sec=2.5)

        gate.observe("AA BB CC DD", 10.0)

        self.assertIsNone(gate.observe("AA BB CC DD", 20.0))

    def test_same_card_requires_sustained_removal_before_rearming(self):
        gate = CardPresenceGate(release_after_sec=2.5)

        gate.observe("AA BB CC DD", 10.0)
        self.assertIsNone(gate.observe(None, 11.0))
        self.assertIsNone(gate.observe("AA BB CC DD", 11.1))
        self.assertIsNone(gate.observe(None, 12.0))
        self.assertIsNone(gate.observe(None, 14.5))

        self.assertEqual("AA BB CC DD", gate.observe("AA BB CC DD", 14.6))

    def test_different_card_is_accepted_immediately(self):
        gate = CardPresenceGate(release_after_sec=2.5)

        gate.observe("AA BB CC DD", 10.0)

        self.assertEqual("11 22 33 44", gate.observe("11 22 33 44", 10.1))


if __name__ == "__main__":
    unittest.main()
