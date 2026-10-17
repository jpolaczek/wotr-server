"""The publisher's release command (DIST-11): checks of the draft, refusals, confirmation and public checks."""
from functools import partial
import hashlib
import http.server
import importlib.util
import json
from pathlib import Path
import tempfile
import threading
import unittest
from unittest.mock import patch
import zipfile


SCRIPTS = Path(__file__).resolve().parents[2] / "scripts"
SPEC = importlib.util.spec_from_file_location("wotr_release_client", SCRIPTS / "release_client.py")
CLIENT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(CLIENT)
VERSION, COMMIT = "0.9.0", "c" * 40


def write_draft(folder, version=VERSION, commit=COMMIT, **identity_changes):
    """The flat files of a DIST-10 draft with their SHA256SUMS."""
    folder.mkdir(parents=True)
    for platform, (system, _, _) in CLIENT.PLATFORMS.items():
        package, installer, build_info = CLIENT.names(version, platform)
        with zipfile.ZipFile(folder / package, "w") as archive:
            archive.writestr("WotrOnline.nuspec", "<package xmlns='http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd'>"
                             f"<metadata><id>WotrOnline</id><version>{version}</version><os>{system}</os></metadata></package>")
            archive.writestr("lib/app/data.bin", platform.encode() * 400)
        (folder / installer).write_bytes(b"installer " + platform.encode() * 600)
        identity = {"version": version, "platform": platform, "development": False, "sourceCommit": commit,
                    "protocolVersion": 2}
        identity.update(identity_changes)
        (folder / build_info).write_text(json.dumps(identity))
    (folder / "RELEASE-NOTES.md").write_text("First release for macOS players.\n")
    lines = [f"{CLIENT.digest(path)}  {path.name}\n" for path in sorted(folder.iterdir())]
    (folder / "SHA256SUMS").write_text("".join(lines))


def resum(folder):
    lines = [f"{CLIENT.digest(path)}  {path.name}\n" for path in sorted(folder.iterdir()) if path.name != "SHA256SUMS"]
    (folder / "SHA256SUMS").write_text("".join(lines))


class DraftChecksTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.assets = Path(self.temporary.name) / "assets"

    def tearDown(self):
        self.temporary.cleanup()

    def test_complete_draft_passes(self):
        write_draft(self.assets)
        files = CLIENT.check_assets(self.assets, VERSION, COMMIT)
        self.assertEqual(set(CLIENT.PLATFORMS), set(files))

    def test_draft_refusals(self):
        cases = {
            "changed file": lambda: (self.assets / CLIENT.names(VERSION, "macos-universal")[1]).write_bytes(b"other"),
            "missing platform": lambda: [(self.assets / name).unlink() for name in CLIENT.names(VERSION, "windows-x64")]
                                        and resum(self.assets),
            "extra file": lambda: ((self.assets / "notes.txt").write_text("x"), resum(self.assets)),
            "empty notes": lambda: ((self.assets / "RELEASE-NOTES.md").write_text(" \n"), resum(self.assets)),
        }
        for case, damage in cases.items():
            with self.subTest(case=case):
                write_draft(self.assets)
                damage()
                with self.assertRaises(CLIENT.Refused):
                    CLIENT.check_assets(self.assets, VERSION, COMMIT)
                self.temporary.cleanup()
                self.temporary = tempfile.TemporaryDirectory()
                self.assets = Path(self.temporary.name) / "assets"

    def test_identity_refusals(self):
        for changes in [{"sourceCommit": "d" * 40}, {"development": True}, {"protocolVersion": 0}, {"version": "0.8.9"}]:
            with self.subTest(changes=changes), tempfile.TemporaryDirectory() as temporary:
                assets = Path(temporary) / "assets"
                write_draft(assets, **changes)
                with self.assertRaises(CLIENT.Refused):
                    CLIENT.check_assets(assets, VERSION, COMMIT)

    def test_package_of_another_version_is_refused(self):
        write_draft(self.assets)
        package = self.assets / CLIENT.names(VERSION, "windows-x64")[0]
        with zipfile.ZipFile(package, "w") as archive:
            archive.writestr("WotrOnline.nuspec", "<package><metadata><id>WotrOnline</id><version>0.8.0</version>"
                             "<os>win</os></metadata></package>")
        resum(self.assets)
        with self.assertRaises(CLIENT.Refused):
            CLIENT.check_assets(self.assets, VERSION, COMMIT)


def settings(work, **changes):
    values = dict(version=VERSION, repository="owner/wotr-client", host="omarchy", host_checkout="~/wotr-server",
                  key_dir=work / "keys", work=work, dry_run=False, assume_yes=False, required_at=None, required_now=False)
    values.update(changes)
    return CLIENT.Settings(**values)


STATE = {"incoming": "/srv/wotr/dist/incoming", "downloadsUrl": "https://downloads.example/", "apiUrl": "https://wotr.example",
         "publicKey": "cHVibGlj", "announced": [], "minimumVersion": "", "scheduledMinimum": None, "uploaded": []}


class FlowTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.work = Path(self.temporary.name)
        write_draft(self.work / VERSION / "assets")
        self.calls = []

    def tearDown(self):
        self.temporary.cleanup()

    def release(self, state=STATE, **changes):
        def sign(files, notes, downloads_url, key, upload):
            self.calls.append("sign")
            (upload / VERSION).mkdir(parents=True)
        with patch.object(CLIENT, "host_state", return_value=state), \
                patch.object(CLIENT, "check_key", return_value=(Path("k"), Path("p"))), \
                patch.object(CLIENT, "tag_commit", return_value=COMMIT), \
                patch.object(CLIENT, "download"), patch.object(CLIENT, "sign", side_effect=sign), \
                patch.object(CLIENT, "verify_upload", return_value="PASS"), \
                patch.object(CLIENT, "upload_and_publish", side_effect=lambda *a: self.calls.append("publish")), \
                patch.object(CLIENT, "check_public", side_effect=lambda *a: self.calls.append("check")), \
                patch.object(CLIENT, "run", side_effect=lambda *a, **k: self.calls.append(a[:3])), \
                patch.object(CLIENT, "record", side_effect=lambda *a: self.calls.append("record")):
            CLIENT.release(settings(self.work, **changes))

    def test_dry_run_signs_locally_and_sends_nothing(self):
        self.release(dry_run=True)
        self.assertEqual(["sign"], self.calls)

    def test_publishing_needs_the_typed_version(self):
        with patch("builtins.input", return_value="0.9"), self.assertRaises(CLIENT.Refused):
            self.release()
        self.assertEqual(["sign"], self.calls)

    def test_confirmed_release_is_published_checked_and_recorded(self):
        with patch("builtins.input", return_value=VERSION):
            self.release()
        self.assertEqual(["sign", "publish", "check", ("gh", "release", "edit"), "record"], self.calls)

    def test_a_rerun_after_publishing_only_checks(self):
        (self.work / VERSION / "upload" / VERSION).mkdir(parents=True)
        self.release(state=dict(STATE, announced=[VERSION]))
        self.assertEqual(["check", ("gh", "release", "edit"), "record"], self.calls)

    def test_announced_version_without_local_signed_upload_is_refused(self):
        with self.assertRaises(CLIENT.Refused):
            self.release(state=dict(STATE, announced=[VERSION]))
        self.assertEqual([], self.calls)


class KeyAndRemoteTests(unittest.TestCase):
    def test_key_pair_must_be_private_and_match_the_host(self):
        with tempfile.TemporaryDirectory() as temporary:
            work = Path(temporary)
            keys = work / "keys"
            keys.mkdir()
            private, public = keys / "wotr-release-ed25519.key", keys / "wotr-release-ed25519.pub"
            private.write_text("secret")
            public.write_text("cHVibGlj\n")
            private.chmod(0o600)
            self.assertEqual((private, public), CLIENT.check_key(settings(work), STATE))
            with self.assertRaises(CLIENT.Refused):
                CLIENT.check_key(settings(work), dict(STATE, publicKey="b3RoZXI="))
            private.chmod(0o644)
            with self.assertRaises(CLIENT.Refused):
                CLIENT.check_key(settings(work), STATE)

    def test_remote_commands_run_in_the_host_checkout(self):
        with patch.object(CLIENT, "run") as run:
            CLIENT.remote(settings(Path("/tmp"), host_checkout="~/Work/wotr server"), "python3 scripts/release_host.py state")
        self.assertEqual(("ssh", "omarchy", "cd ~/'Work/wotr server' && python3 scripts/release_host.py state"),
                         run.call_args.args)


class RangeHandler(http.server.SimpleHTTPRequestHandler):
    """Static files with single Range requests (206), as nginx serves downloads; JSON for the API paths."""
    api = {}
    ranges = True

    def log_message(self, *args):
        pass

    def do_GET(self):
        path = self.path.split("?")[0]
        if path in self.api:
            body = json.dumps(self.api[self.path] if self.path in self.api else self.api[path]).encode()
            self.send_response(200)
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        file = Path(self.translate_path(path))
        data = file.read_bytes() if file.is_file() else None
        if data is None:
            self.send_error(404)
            return
        start, end = 0, len(data) - 1
        ranged = self.ranges and self.headers.get("Range", "").startswith("bytes=")
        if ranged:
            first, last = self.headers["Range"][6:].split("-")
            start, end = int(first), min(int(last), len(data) - 1)
        self.send_response(206 if ranged else 200)
        if ranged:
            self.send_header("Content-Range", f"bytes {start}-{end}/{len(data)}")
        self.send_header("Content-Length", str(end - start + 1))
        self.end_headers()
        self.wfile.write(data[start:end + 1])


class PublicChecksTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.work = Path(self.temporary.name)
        assets, self.upload, public = self.work / "assets", self.work / "upload", self.work / "public"
        write_draft(assets)
        self.files = CLIENT.check_assets(assets, VERSION, COMMIT)
        handler = partial(RangeHandler, directory=str(public))
        self.server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), handler)
        threading.Thread(target=self.server.serve_forever, daemon=True).start()
        base = f"http://127.0.0.1:{self.server.server_port}"
        self.state = dict(STATE, apiUrl=base, downloadsUrl=base + "/")
        installers = []
        for platform, files in self.files.items():
            signed, served = self.upload / VERSION / platform, public / "releases" / VERSION / platform
            for folder in (signed, served):
                folder.mkdir(parents=True)
                (folder / "manifest.json").write_text(json.dumps({"version": VERSION, "platform": platform}))
                (folder / "manifest.json.sig").write_bytes(b"signature " + platform.encode())
                for name in (files.package, files.installer):
                    (folder / name.name).write_bytes(name.read_bytes())
            url = f"{base}/releases/{VERSION}/{platform}/"
            RangeHandler.api[f"/api/client/releases?platform={platform}"] = {
                "latest": {"version": VERSION, "manifestUrl": url + "manifest.json", "signatureUrl": url + "manifest.json.sig"},
                "minimumVersion": "", "scheduledMinimum": None}
            installers.append({"platform": platform, "url": url + files.installer.name, "size": files.installer.stat().st_size,
                               "sha256": CLIENT.digest(files.installer)})
        RangeHandler.api["/api/client/releases"] = {}
        RangeHandler.api["/api/client/downloads"] = {"latest": {"version": VERSION, "installers": installers}}
        self.public = public

    def tearDown(self):
        self.server.shutdown()
        self.server.server_close()
        RangeHandler.api = {}
        self.temporary.cleanup()

    def check(self, **changes):
        CLIENT.check_public(settings(self.work, **changes), self.state, self.files, self.upload)

    def test_published_release_passes(self):
        self.check()

    def test_other_manifest_or_missing_minimum_is_refused(self):
        with self.assertRaises(CLIENT.Refused):
            self.check(required_now=True)
        served = self.public / "releases" / VERSION / "macos-universal" / "manifest.json"
        served.write_text("{}")
        with self.assertRaises(CLIENT.Refused):
            self.check()

    def test_download_without_range_support_is_refused(self):
        with patch.object(RangeHandler, "ranges", False), self.assertRaises(CLIENT.Refused):
            self.check()


if __name__ == "__main__":
    unittest.main()
