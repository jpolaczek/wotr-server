import importlib.util
import pathlib
import subprocess
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("ci_scope", ROOT / "scripts" / "ci_scope.py")
SCOPE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(SCOPE)


class CiScopeTests(unittest.TestCase):
    def test_documentation_alone_skips_the_dotnet_checks(self):
        self.assertFalse(SCOPE.needs_code(["docs/tasks/tasks.json", "docs/tasks/UI-181.md", "AGENTS.md", "README.md",
                                           "docs/sources/MAP-14-RENDERER.md"]), "A pin of wotr-client stays documentation.")

    def test_code_or_files_the_tests_read_need_the_full_run(self):
        self.assertTrue(SCOPE.needs_code(["docs/tasks/tasks.json", "src/Wotr.Server/Program.cs"]))
        self.assertTrue(SCOPE.needs_code(["docs/sources/fate-index.json"]))
        self.assertTrue(SCOPE.needs_code(["docs/TEST_SCENARIOS.md"]))
        self.assertTrue(SCOPE.needs_code([".github/workflows/ci.yml"]))
        self.assertTrue(SCOPE.needs_code(["infra/README.md"]), "Only Markdown at the root counts as documentation.")

    def test_an_empty_change_runs_everything(self):
        self.assertTrue(SCOPE.needs_code([]))

    def test_an_unknown_base_runs_everything(self):
        self.assertIsNone(SCOPE.changed("", "HEAD"))
        self.assertIsNone(SCOPE.changed("0" * 40, "HEAD"))
        self.assertIsNone(SCOPE.changed("1" * 40, "HEAD"))

    def test_a_real_documentation_commit_is_documentation_only(self):
        with tempfile.TemporaryDirectory() as folder:
            def git(*args):
                return subprocess.run(["git", "-C", folder, *args], capture_output=True, text=True, check=True).stdout.strip()
            git("init", "-q")
            git("config", "user.email", "ci@example.invalid")
            git("config", "user.name", "CI")
            (pathlib.Path(folder) / "docs").mkdir()
            (pathlib.Path(folder) / "docs" / "notes.md").write_text("one\n")
            git("add", ".")
            git("commit", "-q", "-m", "base")
            base = git("rev-parse", "HEAD")
            (pathlib.Path(folder) / "docs" / "notes.md").write_text("two\n")
            git("commit", "-q", "-am", "docs")
            head = git("rev-parse", "HEAD")
            result = subprocess.run(["python3", str(ROOT / "scripts" / "ci_scope.py"), base, head],
                                    cwd=folder, capture_output=True, text=True, check=True)
            self.assertEqual("code=false", result.stdout.strip())


if __name__ == "__main__":
    unittest.main()
