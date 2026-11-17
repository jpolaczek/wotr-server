"""The CI filters must isolate scheduler tests without omitting or duplicating tests."""
import itertools
from pathlib import Path
import re
import subprocess
import unittest


class TestBackendPartitions(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        root = Path(__file__).resolve().parents[2]
        source = (root / 'scripts/test_parts.sh').read_text()
        declarations = source.split('dotnet build', 1)[0]
        cls.filters = subprocess.check_output(
            ['bash', '-c', declarations + '\nprintf "%s\\n" "${PARTS[@]}"'],
            cwd=root, text=True).splitlines()

    def matches(self, expression, name):
        def predicate(match):
            present = match[2] in name
            return str(not present if match[1] == '!~' else present)
        boolean = re.sub(r'FullyQualifiedName(!?~)([^&|()]+)', predicate, expression)
        boolean = boolean.replace('&', ' and ').replace('|', ' or ')
        return eval(boolean, {'__builtins__': {}}, {})

    def test_every_name_combination_belongs_to_exactly_one_host(self):
        categories = ('Bot', 'Persistence', 'Expansion', 'Kings', 'Lords', 'Fate', 'Council', 'Release', 'ModuleIndependence')
        for flags in itertools.product((False, True), repeat=len(categories)):
            name = 'Wotr.Tests.' + ''.join(c for c, enabled in zip(categories, flags) if enabled) + 'Tests'
            with self.subTest(name=name):
                self.assertEqual(1, sum(self.matches(f, name) for f in self.filters))

    def test_scheduler_and_avatar_tests_run_in_different_processes(self):
        def hosts(name):
            return [i for i, f in enumerate(self.filters) if self.matches(f, 'Wotr.Tests.' + name)]
        resource = hosts('BotRuntimeResourceTests.EightConcurrentMatchesObeyWorkerAndWorkBounds')
        thinking = hosts('BotThinkingModeTests.TheHostGivesThePacesWorkAndDeadlineToTheRuntime')
        avatar = hosts('AvatarTests.RegistrationsDrawEverySoldierAndNothingElse')
        self.assertEqual([0], resource)
        self.assertEqual(resource, thinking)
        self.assertNotEqual(resource, avatar)
