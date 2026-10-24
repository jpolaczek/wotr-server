import io
from pathlib import Path
import sys
import tarfile
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "scripts"))
import package_rules
from package_deps import digest


class RulesPackageTests(unittest.TestCase):
    def test_archive_is_reproducible_and_contains_only_reviewed_sources(self):
        bundle, manifest = package_rules.build()
        self.assertEqual((bundle, manifest), package_rules.build())
        self.assertEqual(manifest["sha256"], digest(bundle))
        with tarfile.open(fileobj=io.BytesIO(bundle), mode="r:gz") as archive:
            names = archive.getnames()
            self.assertEqual(sorted(manifest["files"]), names)
            self.assertIn("LICENSE", names)
            self.assertIn("content/base-board.json", names)
            self.assertIn("content/fate-cards.json", names)
            for item in archive.getmembers():
                self.assertTrue(item.isfile())
                self.assertEqual(manifest["files"][item.name], digest(archive.extractfile(item).read()))
                self.assertNotIn("/Demo/", item.name)
                self.assertNotIn("Wotr.Server", item.name)
                self.assertNotIn("Wotr.Bot", item.name)
                self.assertNotIn("/tasks/", item.name)


if __name__ == "__main__":
    unittest.main()
