"""Backup orchestration boundaries; these tests do not claim a PostgreSQL restore."""
import errno
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock, patch

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "scripts"))
from deploy import Deployment, digest, export_image


class RunningServerTests(unittest.TestCase):
    def setUp(self):
        self.deployment = Deployment("unused-environment-file")
        self.container = "a" * 64
        self.deployment.compose = Mock(return_value=subprocess.CompletedProcess([], 0, self.container.encode()))

    def test_metadata_comes_from_running_container_instead_of_future_environment_file(self):
        image = "sha256:" + "b" * 64
        with patch("deploy.docker_output", side_effect=[b"wotr_restore_active\n", image.encode()]) as docker:
            details = self.deployment.server_details()
        self.assertEqual("wotr_restore_active", details["database"])
        self.assertEqual(image, details["image"])
        self.assertEqual(self.container, details["container"])
        self.assertEqual(("exec", self.container, "printenv", "WOTR_DATABASE"), docker.call_args_list[0].args)

    def test_ambiguous_container_selection_is_rejected_before_reading_metadata(self):
        self.deployment.compose.return_value.stdout = (self.container + "\n" + "c" * 64).encode()
        with patch("deploy.docker_output") as docker, self.assertRaises(ValueError):
            self.deployment.server_details()
        docker.assert_not_called()

    def test_missing_database_declaration_is_not_replaced_with_a_default(self):
        with patch("deploy.docker_output", return_value=b"") as docker, self.assertRaises(ValueError):
            self.deployment.server_details()
        self.assertEqual(1, docker.call_count)


class BackupTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.folder = Path(self.temporary.name)
        self.deployment = Deployment(self.folder / ".env")
        self.server = {"container": "a" * 64, "image": "sha256:" + "b" * 64,
                       "database": "wotr_restore_current"}
        self.deployment.server_details = Mock(return_value=self.server)
        self.deployment.sql = Mock(return_value="1,2,3")
        self.deployment.compose = Mock(side_effect=self.dump)

    @staticmethod
    def dump(*args, **kwargs):
        database = args[args.index("-d") + 1]
        kwargs["stdout"].write(("dump-of-" + database).encode())

    def backup(self):
        def archive(image, path):
            path.write_text(image)
        with patch("deploy.export_image", side_effect=archive), \
             patch("deploy.docker_output", return_value=b"verified-content-hashes\n"):
            return self.deployment.backup(self.folder / "backup")

    def test_backup_uses_running_database_after_restore(self):
        backup = self.backup()
        self.assertEqual("dump-of-wotr_restore_current", (backup / "database.dump").read_text())
        manifest = json.loads((backup / "manifest.json").read_text())
        self.assertEqual("wotr_restore_current", manifest["database"])
        self.assertEqual(self.server["image"], manifest["server_image"])
        self.assertEqual("wotr_restore_current", self.deployment.sql.call_args.args[1])
        self.assertEqual(digest(backup / "database.dump"), manifest["files"]["database.dump"])

    def test_container_replacement_leaves_backup_incomplete(self):
        self.deployment.server_details.side_effect = [self.server, dict(self.server, container="c" * 64)]
        with self.assertRaisesRegex(ValueError, "Server changed"):
            self.backup()
        self.assertFalse((self.folder / "backup/manifest.json").exists())

    def test_failed_image_export_leaves_backup_incomplete(self):
        with patch("deploy.export_image", side_effect=OSError(errno.ENOSPC, "No space")):
            with self.assertRaises(OSError):
                self.deployment.backup(self.folder / "backup")
        self.assertFalse((self.folder / "backup/manifest.json").exists())

    def test_restore_refuses_a_corrupted_archive_before_database_access(self):
        backup = self.backup()
        (backup / "database.dump").write_bytes(b"corrupted")
        self.deployment.sql.reset_mock()
        self.deployment.compose.reset_mock()
        with self.assertRaisesRegex(ValueError, "checksum mismatch"):
            self.deployment.restore(backup, "wotr_restore_new")
        self.deployment.sql.assert_not_called()
        self.deployment.compose.assert_not_called()

    def test_restore_never_overwrites_existing_database(self):
        backup = self.backup()
        self.deployment.sql.return_value = "1"
        self.deployment.compose.reset_mock()
        with self.assertRaisesRegex(ValueError, "already exists"):
            self.deployment.restore(backup, "wotr_restore_existing")
        self.deployment.compose.assert_not_called()

    def test_restore_rejects_unsafe_targets_before_database_access(self):
        for target in ["wotr", "postgres", "wotr_restore_x;DROP DATABASE wotr", "wotr_restore_"]:
            with self.subTest(target=target), self.assertRaises(ValueError):
                self.deployment.restore(self.folder, target)
        self.deployment.sql.assert_not_called()
        self.deployment.compose.assert_not_called()

    def test_write_failure_terminates_the_exporter(self):
        # A real blocked child exercises cleanup without filling the disk or requiring Docker.
        child = subprocess.Popen([sys.executable, "-c", "import time; time.sleep(60)"],
                                 stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
        self.addCleanup(lambda: child.poll() is None and child.kill())
        with patch("deploy.subprocess.Popen", return_value=child), \
             patch("deploy.shutil.copyfileobj", side_effect=OSError(errno.ENOSPC, "No space")):
            with self.assertRaises(OSError):
                export_image("unused-test-image", self.folder / "image.gz")
        self.assertIsNotNone(child.poll())


if __name__ == "__main__":
    unittest.main()
