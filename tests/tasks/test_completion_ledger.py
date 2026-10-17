"""Regression checks for task pruning without weakening dependency/coverage gates."""
import copy
import sys
import unittest
from pathlib import Path
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'scripts'))
import task_registry
import tasks


@unittest.skipUnless(tasks.REGISTRY.exists(), 'The task registry is local-only (docs/tasks, ignored by Git).')
class CompletionLedgerTests(unittest.TestCase):
    def setUp(self):
        self.data = tasks.read_json(tasks.REGISTRY)
        self.completed = task_registry.completed_records()

    def test_completed_dependency_is_satisfied_but_missing_one_blocks(self):
        completed_id = self.completed[0]['id']
        candidate = {'depends_on': [completed_id], 'source_gates': []}
        lookup = {entry['id']: entry for entry in task_registry.records(self.data)}
        self.assertEqual([], tasks.blockers(candidate, lookup, {}))
        del lookup[completed_id]
        self.assertEqual([completed_id], tasks.blockers(candidate, lookup, {}))

    def test_active_unfinished_dependency_still_blocks(self):
        unfinished = next(t for t in self.data['tasks'] if t['status'] != 'done')
        lookup = {entry['id']: entry for entry in task_registry.records(self.data)}
        candidate = {'depends_on': [unfinished['id']], 'source_gates': []}
        self.assertEqual([unfinished['id']], tasks.blockers(candidate, lookup, {}))

    def test_duplicate_completion_identifier_is_rejected(self):
        with patch.object(task_registry, 'completed_records', return_value=self.completed + [self.completed[0]]):
            self.assertIn('Duplicate task identifier', tasks.validate(self.data))

    def test_removed_pack_does_not_silently_pass_coverage(self):
        completed = copy.deepcopy(self.completed)
        pack = next(t for t in completed if t.get('content_count'))
        completed.remove(pack)
        with patch.object(task_registry, 'completed_records', return_value=completed):
            self.assertTrue(any('Incomplete content packs' in e for e in tasks.validate(self.data)))


if __name__ == '__main__':
    unittest.main()
