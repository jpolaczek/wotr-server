"""The host side of the release command (DIST-11): environment, catalog state and ReleaseTool commands."""
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


SCRIPTS = Path(__file__).resolve().parents[2] / "scripts"
SPEC = importlib.util.spec_from_file_location("wotr_release_host", SCRIPTS / "release_host.py")
HOST = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(HOST)


class HostTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.folder = Path(self.temporary.name)
        self.environment_file = self.folder / ".env"
        self.environment_file.write_text(
            "# deployment\nWOTR_VERSION=0.9.0-host\nWOTR_DOMAIN=wotr.example\nWOTR_DOWNLOADS_URL='https://downloads.example/'\n"
            f"WOTR_RELEASE_PUBLIC_KEY=\"cHVibGlj\"\nWOTR_DISTRIBUTION_DIR={self.folder / 'dist'}\n")

    def tearDown(self):
        self.temporary.cleanup()

    def test_environment_values_and_missing_configuration(self):
        environment = HOST.read_environment(self.environment_file)
        self.assertEqual("https://downloads.example/", environment["WOTR_DOWNLOADS_URL"])
        self.assertEqual("cHVibGlj", environment["WOTR_RELEASE_PUBLIC_KEY"])
        self.environment_file.write_text("WOTR_DOMAIN=wotr.example\n")
        with self.assertRaises(ValueError):
            HOST.read_environment(self.environment_file)

    def test_state_lists_announced_releases_and_uploads(self):
        dist = self.folder / "dist"
        (dist / "incoming/0.9.1").mkdir(parents=True)
        (dist / "catalog.json").write_text(json.dumps({"minimumVersion": "0.9.0", "releases": [
            {"version": "0.9.0", "withdrawn": False}, {"version": "0.8.0", "withdrawn": True}]}))
        state = HOST.state(HOST.read_environment(self.environment_file))
        self.assertEqual(["0.9.0"], state["announced"])
        self.assertEqual(["0.9.1"], state["uploaded"])
        self.assertEqual("https://wotr.example", state["apiUrl"])
        self.assertEqual(str(dist / "incoming"), state["incoming"])

    def test_empty_distribution_has_no_releases(self):
        state = HOST.state(HOST.read_environment(self.environment_file))
        self.assertEqual(([], [], ""), (state["announced"], state["uploaded"], state["minimumVersion"]))

    def test_release_tool_commands(self):
        environment = HOST.read_environment(self.environment_file)
        publish = HOST.parse(["publish", "--version", "0.9.0", "--minimum", "--at", "2026-10-20T20:00+02:00"])
        command = HOST.release_tool(environment, HOST.tool_arguments(publish))
        tool = command[command.index(HOST.RELEASE_TOOL) + 1:]
        self.assertEqual(["publish", "--root", "/release-root", "--downloads-url", "https://downloads.example/",
                          "--public-key", "cHVibGlj", "--version", "0.9.0", "--minimum", "--at",
                          "2026-10-20T20:00+02:00"], tool)
        self.assertIn(f"{self.folder / 'dist'}:/release-root", command)
        self.assertIn("wotr-server:0.9.0-host", command)
        self.assertEqual(["--network", "none"], command[command.index("--network"):command.index("--network") + 2])
        self.assertEqual(["minimum", "--cancel-scheduled"], HOST.tool_arguments(HOST.parse(["minimum", "--cancel-scheduled"])))
        with self.assertRaises(SystemExit):
            HOST.parse(["publish", "--version", "0.9.0", "--at", "2026-10-20T20:00Z"])


if __name__ == "__main__":
    unittest.main()
