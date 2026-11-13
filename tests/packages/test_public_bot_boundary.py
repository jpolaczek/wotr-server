import io
from pathlib import Path
import sys
import tarfile
import tempfile
import unittest
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "scripts"))
from check_public_artifacts import inspect, require_public
from package_deps import archive, PackageError


class PublicBotBoundaryTests(unittest.TestCase):
    def test_player_and_server_names_remain_allowed(self):
        require_public(["Wotr.Server.dll", "Wotr.Contracts.dll", "Wotr.Domain.pdb", "Runtime/BotMetadata.cs"])

    def test_private_libraries_symbols_sources_and_fixture_never_enter_any_public_archive(self):
        for name in ["Wotr.Bot.Runtime.dll", "Wotr.Bot.Policy.pdb", "src/Wotr.Bot.Core/Memory.cs",
                     "Wotr.RuntimeFixture.dll", "wotr-bot-runtime-0.2.1.zip", "runtime-manifest.json"]:
            with self.subTest(name=name):
                with self.assertRaises(PackageError):
                    archive({"package/" + name: b"private"})

    def test_installer_tree_and_zip_and_tar_inventory_are_inspected_without_extraction(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            player = root / "player"
            player.mkdir()
            (player / "Wotr.Contracts.dll").write_bytes(b"ordinary")
            self.assertEqual(1, inspect(player))
            (player / "Wotr.Bot.Policy.dll").write_bytes(b"private")
            with self.assertRaises(ValueError):
                inspect(player)
            for extension in ("zip", "tgz"):
                path = root / ("public." + extension)
                if extension == "zip":
                    with zipfile.ZipFile(path, "w") as bundle:
                        bundle.writestr("Data/Managed/WOTR.BOT.Runtime.DLL", b"private")
                else:
                    with tarfile.open(path, "w:gz") as bundle:
                        entry = tarfile.TarInfo("Data/Managed/Wotr.Bot.Runtime.pdb")
                        entry.size = 7
                        bundle.addfile(entry, io.BytesIO(b"private"))
                with self.assertRaises(ValueError):
                    inspect(path)


if __name__ == "__main__":
    unittest.main()
