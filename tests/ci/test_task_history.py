import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "scripts"))
from task_registry import matching_start


class TaskHistoryTests(unittest.TestCase):
    def test_preserves_planning_history_before_start(self):
        history = [{"event": "plan", "at": "earlier"}, {"event": "start", "at": "now"}]
        self.assertTrue(matching_start(history, "now"))
        self.assertFalse(matching_start(history, "different"))

    def test_does_not_accept_unstarted_execution(self):
        self.assertFalse(matching_start([], "now"))
        self.assertFalse(matching_start([{"event": "plan", "at": "now"}], "now"))
        self.assertFalse(matching_start([{"event": "note", "at": "now"}, {"event": "start", "at": "now"}], "now"))
