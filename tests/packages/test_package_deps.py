"""wotr-server packages for the client: reproducible archives that are never replaced (DIST-02)."""
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import shutil
import subprocess
import tarfile
import tempfile
import unittest


SCRIPTS = Path(__file__).resolve().parents[2] / "scripts"


def load(name):
    spec = importlib.util.spec_from_file_location("wotr_" + name, SCRIPTS / (name + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


PACKER = load("package_deps")
CONTRACTS = "com.wotr.contracts"
CONTENT = "com.wotr.public-content"
PROTOCOL = """namespace Wotr.Contracts
{
    public static class Protocol
    {
        public const int Version = 2;
        public const string RulesVersion = "rules-1";
        public const string ContentVersion = "content-1";
    }
}
"""
SERVER_FILES = {
    "src/Wotr.Contracts/package.json": json.dumps({"name": CONTRACTS, "version": "0.1.0"}),
    "src/Wotr.Contracts/package.json.meta": "guid: 1",
    "src/Wotr.Contracts/Runtime.meta": "guid: 2",
    "src/Wotr.Contracts/Runtime/Protocol.cs": PROTOCOL,
    "src/Wotr.Contracts/Runtime/Protocol.cs.meta": "guid: 3",
    "src/Wotr.Contracts/Runtime/Wotr.Contracts.asmdef": '{"name": "Wotr.Contracts"}',
    "src/Wotr.Contracts/Runtime/Wotr.Contracts.asmdef.meta": "guid: 4",
    "src/Wotr.Contracts/Wotr.Contracts.csproj":
        "<Project><PropertyGroup><TargetFramework>netstandard2.1</TargetFramework></PropertyGroup></Project>",
    "src/Wotr.Contracts/Wotr.Contracts.csproj.meta": "guid: 5",
    "src/Wotr.Contracts/packages.lock.json": "{}",
    "src/Wotr.Contracts/packages.lock.json.meta": "guid: 6",
    "content/unity-public-content.json": json.dumps({"version": "1.0.0", "files": ["board.json", "cards.json"]}),
    "content/board.json": '{"regions": 1}',
    "content/cards.json": '{"cards": 2}',
    "content/private-state.json": '{"hidden": true}',
    "content/seed.sql": "insert into secrets values ('x');",
}


def git(root, *args):
    subprocess.run(["git", *args], cwd=root, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def write(root, files):
    for path, text in files.items():
        (root / path).parent.mkdir(parents=True, exist_ok=True)
        (root / path).write_text(text)


def commit(root, files, message="change"):
    write(root, files)
    git(root, "add", "-A")
    git(root, "commit", "-q", "-m", message)


def server_repository(folder):
    root = folder / "server"
    root.mkdir()
    git(root, "init", "-q")
    git(root, "config", "user.email", "test@example.invalid")
    git(root, "config", "user.name", "Test")
    commit(root, SERVER_FILES, "initial")
    return root


def members(data):
    with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as archive:
        return {member.name: archive.extractfile(member).read() for member in archive.getmembers()}


class PackerTests(unittest.TestCase):
    def setUp(self):
        self.folder = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, self.folder)
        self.server = server_repository(self.folder)

    def build(self, name="channel"):
        PACKER.build(self.server, self.folder / name)
        return self.folder / name

    def test_same_commit_gives_identical_archives_and_manifests(self):
        first, second = self.build("first"), self.build("second")
        names = sorted(path.name for path in first.iterdir())
        self.assertEqual(names, sorted(path.name for path in second.iterdir()))
        for name in names:
            self.assertEqual((first / name).read_bytes(), (second / name).read_bytes(), name)
        for stem in (f"{CONTRACTS}-0.1.0", f"{CONTENT}-1.0.0"):
            manifest = json.loads((first / (stem + ".manifest.json")).read_text())
            data = (first / (stem + ".tgz")).read_bytes()
            self.assertEqual(hashlib.sha256(data).hexdigest(), manifest["sha256"])
            self.assertEqual(len(data), manifest["size"])
            self.assertRegex(manifest["commit"], "^[0-9a-f]{40}$")

    def test_contracts_archive_has_the_upm_layout_without_dotnet_build_files(self):
        files = members((self.build() / f"{CONTRACTS}-0.1.0.tgz").read_bytes())
        self.assertIn("package/package.json", files)
        self.assertIn("package/Runtime/Protocol.cs.meta", files)
        self.assertIn("package/Runtime/Wotr.Contracts.asmdef", files)
        self.assertFalse([name for name in files if "csproj" in name or "packages.lock" in name])
        manifest = json.loads((self.folder / "channel" / f"{CONTRACTS}-0.1.0.manifest.json").read_text())
        self.assertEqual({"version": 2, "rulesVersion": "rules-1", "contentVersion": "content-1"}, manifest["protocol"])

    def test_content_archive_holds_only_the_allowlist(self):
        channel = self.build()
        files = members((channel / f"{CONTENT}-1.0.0.tgz").read_bytes())
        self.assertEqual(["package/Resources/board.json", "package/Resources/cards.json", "package/package.json"],
                         sorted(files))
        manifest = json.loads((channel / f"{CONTENT}-1.0.0.manifest.json").read_text())
        self.assertEqual({CONTRACTS: "0.1.0"}, manifest["requires"])
        self.assertEqual("content-1", manifest["contentVersion"])
        self.assertEqual(manifest["fingerprint"], PACKER.fingerprint({e["name"]: e["sha256"] for e in manifest["public"]}))

    def test_a_published_version_is_never_replaced(self):
        channel = self.build()
        before = (channel / f"{CONTENT}-1.0.0.tgz").read_bytes()
        commit(self.server, {"content/board.json": '{"regions": 2}'})
        with self.assertRaisesRegex(PACKER.PackageError, "Bump the version"):
            PACKER.build(self.server, channel)
        self.assertEqual(before, (channel / f"{CONTENT}-1.0.0.tgz").read_bytes())
        self.assertFalse((channel / f"{CONTENT}-1.0.0.tgz.partial").exists())
        commit(self.server, {"content/unity-public-content.json":
                             json.dumps({"version": "1.1.0", "files": ["board.json", "cards.json"]})})
        PACKER.build(self.server, channel)
        self.assertTrue((channel / f"{CONTENT}-1.1.0.tgz").is_file())

    def test_uncommitted_sources_are_refused(self):
        write(self.server, {"content/board.json": '{"regions": 3}'})
        with self.assertRaisesRegex(PACKER.PackageError, "Commit changes"):
            self.build()

    def test_contracts_cannot_refer_to_server_code(self):
        commit(self.server, {"src/Wotr.Contracts/Runtime/Leak.cs": "using Wotr.Domain;"})
        with self.assertRaisesRegex(PACKER.PackageError, "server code"):
            self.build()


if __name__ == "__main__":
    unittest.main()
