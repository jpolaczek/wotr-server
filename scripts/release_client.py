#!/usr/bin/env python3
"""DIST-11, on the publisher's Mac: take a draft GitHub Release of wotr-client (DIST-10) to the players.

  python3 scripts/release_client.py v0.9.0 [--required-at 2026-10-20T20:00+02:00 | --required-now] [--dry-run]

1. Download the draft and check it: SHA256SUMS, every build-info.json (version of the tag, commit of the tag, release
   build, one protocol), the packages' nuspec and the release notes.
2. Sign every platform (Windows, macOS, Linux) with the publisher's Ed25519 key (ReleaseTool sign) and verify the signed upload.
3. Ask for the version as confirmation. --dry-run stops here: nothing has left the Mac.
4. rsync the upload into the host's incoming folder and publish it over ssh (scripts/release_host.py), optionally
   as the minimum now or from a time.
5. Check the public catalog, downloads, Range requests and the published manifest bytes, mark the GitHub release
   published and append the release history.

A re-run continues: the downloaded and signed files stay in <work>/<version>, and a version the host already
announces is not uploaded again. Needs gh (logged in), the .NET 10 SDK, ssh and rsync to the host and the key pair
in ~/.config/wotr/release-signing. Host and checkout: --host/--host-checkout or ~/.config/wotr/release-client.json.
"""
import argparse
from dataclasses import dataclass
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import shlex
import shutil
import subprocess
import sys
from typing import Optional
import urllib.error
import urllib.request
import zipfile
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parents[1]
CONFIG = Path.home() / ".config/wotr/release-client.json"
PACK_ID = "WotrOnline"
NOTES_LIMIT = 4000  # ReleaseManifest.MaximumNotesLength
# ClientBuildInfo platform → (nuspec os, package suffix, installer suffix) of DIST-06's packaging.
PLATFORMS = {"windows-x64": ("win", "-full.nupkg", "-win-Setup.exe"),
             "macos-universal": ("osx", "-osx-full.nupkg", "-osx-Setup.pkg"),
             "linux-x64": ("linux", "-linux-full.nupkg", "-linux.AppImage")}


class Refused(Exception):
    pass


@dataclass
class Settings:
    version: str
    repository: str
    host: str
    host_checkout: str
    key_dir: Path
    work: Path
    dry_run: bool
    assume_yes: bool
    required_at: Optional[str]
    required_now: bool
    state_file: Optional[Path] = None

    @property
    def tag(self):
        return "v" + self.version

    @property
    def folder(self):
        return self.work / self.version


@dataclass
class PlatformFiles:
    package: Path
    installer: Path
    build_info: Path
    identity: dict


def run(*args, capture=False, check=True):
    result = subprocess.run(args, check=False, text=True, capture_output=capture)
    if check and result.returncode != 0:
        detail = (result.stderr or result.stdout or "").strip() if capture else ""
        raise Refused(f"{' '.join(args[:3])} … failed (exit {result.returncode}). {detail}".strip())
    return result


def digest(path):
    value = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1 << 20), b""):
            value.update(block)
    return value.hexdigest()


def names(version, platform):
    _, package, installer = PLATFORMS[platform]
    return f"{PACK_ID}-{version}{package}", f"{PACK_ID}-{version}{installer}", f"build-info-{platform}.json"


# --- 1. The draft and its checks -------------------------------------------------------------------------------

def tag_commit(settings):
    ref = json.loads(run("gh", "api", f"repos/{settings.repository}/git/ref/tags/{settings.tag}", capture=True).stdout)
    target = ref["object"]
    while target["type"] == "tag":
        target = json.loads(run("gh", "api", target["url"].split("api.github.com/", 1)[1], capture=True).stdout)["object"]
    return target["sha"]


def download(settings, assets, upload, published):
    """The draft's files in <work>/<version>/assets. A copy left by an earlier run is reused while it matches the
    release's SHA256SUMS; a rebuilt draft (Release re-run) replaces it and the signed upload, unless the host
    already announces the older build."""
    view = run("gh", "release", "view", settings.tag, "-R", settings.repository, "--json", "isDraft", capture=True,
               check=False)
    if view.returncode != 0:
        raise Refused(f"No GitHub release {settings.tag} in {settings.repository}; tag it and wait for Release (DIST-10).")
    sums = settings.folder / "SHA256SUMS.remote"
    sums.unlink(missing_ok=True)
    run("gh", "release", "download", settings.tag, "-R", settings.repository, "-p", "SHA256SUMS", "-O", str(sums))
    if (assets / "SHA256SUMS").is_file() and (assets / "SHA256SUMS").read_bytes() == sums.read_bytes():
        return
    if assets.exists():
        if published:
            raise Refused(f"The host announces another build of {settings.version}; publish a higher version.")
        shutil.rmtree(assets)
        shutil.rmtree(upload, ignore_errors=True)
    elif not json.loads(view.stdout)["isDraft"] and published:
        raise Refused(f"{settings.tag} is published on GitHub and on the host, with no local copy to check it against.")
    partial = assets.with_name(assets.name + ".partial")
    shutil.rmtree(partial, ignore_errors=True)
    run("gh", "release", "download", settings.tag, "-R", settings.repository, "-D", str(partial))
    partial.rename(assets)


def check_sums(assets, version):
    expected = {name for platform in PLATFORMS for name in names(version, platform)} | {"RELEASE-NOTES.md"}
    listed = {}
    for line in (assets / "SHA256SUMS").read_text().splitlines():
        value, name = line.split("  ", 1)
        listed[name] = value
    present = {path.name for path in assets.iterdir() if path.is_file()} - {"SHA256SUMS"}
    if set(listed) != expected or present != expected:
        missing = sorted(expected - set(listed) | expected - present)
        extra = sorted(set(listed) - expected | present - expected)
        raise Refused(f"The draft must hold exactly every platform's files; missing {missing}, unexpected {extra}.")
    for name, value in listed.items():
        if digest(assets / name) != value:
            raise Refused(f"{name} does not match SHA256SUMS.")


def nuspec(package):
    with zipfile.ZipFile(package) as archive:
        specs = [name for name in archive.namelist() if "/" not in name and name.endswith(".nuspec")]
        if len(specs) != 1:
            raise Refused(f"{package.name} has no single .nuspec.")
        metadata = ElementTree.fromstring(archive.read(specs[0])).find("{*}metadata")
    return tuple(metadata.findtext("{*}" + field) for field in ("id", "version", "os"))


def check_identity(identity, platform, version, commit):
    if identity.get("platform") != platform or identity.get("version") != version:
        raise Refused(f"build-info-{platform}.json names {identity.get('platform')} {identity.get('version')}.")
    if identity.get("development") is not False:
        raise Refused(f"build-info-{platform}.json is a development build.")
    if identity.get("sourceCommit") != commit:
        raise Refused(f"build-info-{platform}.json was built from {identity.get('sourceCommit')}, not the tag commit {commit}.")
    if not isinstance(identity.get("protocolVersion"), int) or identity["protocolVersion"] <= 0:
        raise Refused(f"build-info-{platform}.json lacks its protocol.")


def check_assets(assets, version, commit):
    check_sums(assets, version)
    notes = (assets / "RELEASE-NOTES.md").read_text(encoding="utf-8").strip()
    if not 0 < len(notes) <= NOTES_LIMIT:
        raise Refused(f"RELEASE-NOTES.md needs 1-{NOTES_LIMIT} characters after trimming.")
    files = {}
    for platform, (system, _, _) in PLATFORMS.items():
        package, installer, build_info = (assets / name for name in names(version, platform))
        identity = json.loads(build_info.read_text())
        check_identity(identity, platform, version, commit)
        if nuspec(package) != (PACK_ID, version, system):
            raise Refused(f"{package.name} is not {PACK_ID} {version} for {system}.")
        files[platform] = PlatformFiles(package, installer, build_info, identity)
    if len({files[platform].identity["protocolVersion"] for platform in files}) != 1:
        raise Refused("The platforms speak different protocols.")
    return files


# --- 2. Host state, key and signing ----------------------------------------------------------------------------

def remote(settings, command):
    checkout = settings.host_checkout
    folder = "~/" + shlex.quote(checkout[2:]) if checkout.startswith("~/") else shlex.quote(checkout)
    return run("ssh", settings.host, f"cd {folder} && {command}", capture=True)


def host_state(settings):
    if settings.state_file is not None:
        return json.loads(settings.state_file.read_text())
    return json.loads(remote(settings, "python3 scripts/release_host.py state").stdout)


def check_key(settings, state):
    private, public = settings.key_dir / "wotr-release-ed25519.key", settings.key_dir / "wotr-release-ed25519.pub"
    if not private.is_file() or not public.is_file():
        raise Refused(f"No key pair in {settings.key_dir} (wotr-release-ed25519.key and .pub).")
    if private.stat().st_mode & 0o077:
        raise Refused(f"{private} is readable by others; chmod 600 it.")
    if public.read_text().strip() != state["publicKey"].strip():
        raise Refused("The local public key differs from WOTR_RELEASE_PUBLIC_KEY on the host.")
    return private, public


def release_tool(*arguments):
    return run("dotnet", "run", "--project", str(ROOT / "src/Wotr.ReleaseTool"), "-c", "Release", "--", *arguments,
               capture=True)


def sign(files, notes, downloads_url, private_key, upload):
    for platform, platform_files in files.items():
        release_tool("sign", "--build-info", str(platform_files.build_info), "--package", str(platform_files.package),
                     "--installer", str(platform_files.installer), "--notes", str(notes),
                     "--downloads-url", downloads_url, "--private-key", str(private_key), "--output", str(upload))


def verify_upload(upload, version, downloads_url, public_key):
    return release_tool("verify-upload", "--upload", str(upload), "--downloads-url", downloads_url,
                        "--public-key", "@" + str(public_key), "--version", version).stdout.strip()


# --- 3. Confirmation -------------------------------------------------------------------------------------------

def minimum_plan(settings):
    if settings.required_now:
        return "required now (minimum from the moment of publishing)"
    if settings.required_at:
        return f"required from {settings.required_at} (players download it before)"
    return "optional (no minimum)"


def summary(settings, commit, files, state, published):
    lines = [f"Release {settings.version} from {settings.repository}@{commit[:12]}, protocol "
             f"{next(iter(files.values())).identity['protocolVersion']}, {minimum_plan(settings)}."]
    for platform, platform_files in files.items():
        lines.append(f"  {platform}: {platform_files.installer.name} {platform_files.installer.stat().st_size:,} B "
                     f"sha256 {digest(platform_files.installer)[:16]}…")
    lines.append(f"Host {settings.host}: {state['incoming']} → {state['downloadsUrl']}, catalog {state['apiUrl']}"
                 + (" (already announced there)" if published else ""))
    return "\n".join(lines)


def confirm(settings):
    if settings.assume_yes:
        return
    answer = input(f"Type {settings.version} to publish it to the players: ").strip()
    if answer != settings.version:
        raise Refused("Not confirmed; nothing was sent.")


# --- 4. Upload and publish -------------------------------------------------------------------------------------

def upload_and_publish(settings, upload, state):
    incoming = state["incoming"].rstrip("/") + "/" + settings.version
    remote(settings, f"mkdir -p {shlex.quote(incoming)}")
    run("rsync", "-a", "--partial", f"{upload / settings.version}/", f"{settings.host}:{incoming}/")
    command = f"python3 scripts/release_host.py publish --version {shlex.quote(settings.version)}"
    if settings.required_now or settings.required_at:
        command += " --minimum"
    if settings.required_at:
        command += f" --at {shlex.quote(settings.required_at)}"
    print(remote(settings, command).stdout.strip())


# --- 5. Public checks, GitHub and history ----------------------------------------------------------------------

def fetch(url, headers=None, limit=None):
    request = urllib.request.Request(url, headers={"User-Agent": "wotr-release-client", **(headers or {})})
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            return response.status, response.headers, response.read(limit) if limit else response.read()
    except (urllib.error.URLError, TimeoutError) as error:
        raise Refused(f"{url} is not reachable: {error}") from error


def fetch_json(url):
    return json.loads(fetch(url)[2])


def check_range(url):
    status, headers, body = fetch(url, {"Range": "bytes=0-1023"}, limit=1024)
    if status != 206 or not headers.get("Content-Range", "").startswith("bytes 0-1023/") or len(body) != 1024:
        raise Refused(f"{url} does not answer a Range request with 206 Partial Content.")


def check_public(settings, state, files, upload):
    api, downloads = state["apiUrl"].rstrip("/"), state["downloadsUrl"].rstrip("/") + "/"
    for platform, platform_files in files.items():
        view = fetch_json(f"{api}/api/client/releases?platform={platform}")
        if (view.get("latest") or {}).get("version") != settings.version:
            raise Refused(f"The catalog offers {platform} {(view.get('latest') or {}).get('version')}, not {settings.version}.")
        if settings.required_now and view.get("minimumVersion") != settings.version:
            raise Refused(f"The minimum is {view.get('minimumVersion')}, not {settings.version}.")
        scheduled = (view.get("scheduledMinimum") or {}).get("version")
        if settings.required_at and settings.version not in (scheduled, view.get("minimumVersion")):
            raise Refused(f"No minimum {settings.version} scheduled in the catalog.")
        signed = upload / settings.version / platform
        for url, name in ((view["latest"]["manifestUrl"], "manifest.json"), (view["latest"]["signatureUrl"], "manifest.json.sig")):
            if fetch(url)[2] != (signed / name).read_bytes():
                raise Refused(f"{url} differs from the signed {name}.")
        check_range(f"{downloads}releases/{settings.version}/{platform}/{platform_files.package.name}")
    offer = fetch_json(f"{api}/api/client/downloads").get("latest") or {}
    if offer.get("version") != settings.version:
        raise Refused(f"The game page offers {offer.get('version')}, not {settings.version}.")
    installers = {item["platform"]: item for item in offer.get("installers", [])}
    for platform, platform_files in files.items():
        item = installers.get(platform) or {}
        if item.get("sha256") != digest(platform_files.installer) or item.get("size") != platform_files.installer.stat().st_size:
            raise Refused(f"The game page offers another {platform} installer than the release.")
        check_range(item["url"])


def record(settings, commit, files, upload):
    entry = {"version": settings.version, "commit": commit, "host": settings.host,
             "publishedAt": datetime.now(timezone.utc).isoformat(timespec="seconds"), "minimum": minimum_plan(settings),
             "installers": {platform: digest(f.installer) for platform, f in files.items()},
             "manifests": {platform: digest(upload / settings.version / platform / "manifest.json") for platform in files}}
    with (settings.work / "history.jsonl").open("a") as history:
        history.write(json.dumps(entry) + "\n")


# --- Command ---------------------------------------------------------------------------------------------------

def release(settings):
    settings.folder.mkdir(parents=True, exist_ok=True)
    assets, upload = settings.folder / "assets", settings.folder / "upload"
    state = host_state(settings)
    published = settings.version in state["announced"]
    private_key, public_key = check_key(settings, state)
    commit = tag_commit(settings)
    download(settings, assets, upload, published)
    files = check_assets(assets, settings.version, commit)
    if not (upload / settings.version).is_dir():
        if published:
            raise Refused(f"The host announces {settings.version}, but this Mac has no signed upload of it to check.")
        sign(files, assets / "RELEASE-NOTES.md", state["downloadsUrl"], private_key, upload)
    print(verify_upload(upload, settings.version, state["downloadsUrl"], public_key))
    print(summary(settings, commit, files, state, published))
    if settings.dry_run:
        print(f"DRY RUN: checked and signed in {settings.folder}; nothing left the Mac.")
        return
    if not published:
        confirm(settings)
        upload_and_publish(settings, upload, state)
    check_public(settings, state, files, upload)
    run("gh", "release", "edit", settings.tag, "-R", settings.repository, "--draft=false")
    record(settings, commit, files, upload)
    print(f"PASS: {settings.version} is published; catalog, downloads, Range and manifests checked.")


def settings_from(argv):
    config = json.loads(CONFIG.read_text()) if CONFIG.is_file() else {}
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("tag", help="v<version>, the tag of the draft release")
    when = parser.add_mutually_exclusive_group()
    when.add_argument("--required-at", help="ISO 8601 time with an offset, at least 5 minutes ahead")
    when.add_argument("--required-now", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--yes", action="store_true", help="skip the typed confirmation")
    parser.add_argument("--repository", default=config.get("repository", "jpolaczek/wotr-client"))
    parser.add_argument("--host", default=config.get("host"))
    parser.add_argument("--host-checkout", default=config.get("hostCheckout"))
    parser.add_argument("--key-dir", default=config.get("keyDir", "~/.config/wotr/release-signing"))
    parser.add_argument("--work", default=config.get("work", "~/WotrReleases"))
    parser.add_argument("--state-file", help=argparse.SUPPRESS)
    args = parser.parse_args(argv)
    if not re.fullmatch(r"v\d+\.\d+\.\d+(?:-[a-z0-9.]+)?", args.tag):
        parser.error("the tag is v<version>, e.g. v0.9.0")
    if args.state_file is None and (not args.host or not args.host_checkout):
        parser.error(f"set --host and --host-checkout (or host and hostCheckout in {CONFIG})")
    if args.required_at and not re.search(r"(Z|[+-]\d\d:\d\d)$", args.required_at):
        parser.error("--required-at needs a time zone, e.g. 2026-10-20T20:00+02:00")
    return Settings(version=args.tag[1:], repository=args.repository, host=args.host or "", host_checkout=args.host_checkout or "",
                    key_dir=Path(args.key_dir).expanduser(), work=Path(args.work).expanduser(), dry_run=args.dry_run,
                    assume_yes=args.yes, required_at=args.required_at, required_now=args.required_now,
                    state_file=Path(args.state_file) if args.state_file else None)


def main(argv=None):
    settings = settings_from(argv)
    # The signed files are public: nginx on the downloads host reads them as another user.
    os.umask(0o022)
    try:
        release(settings)
    except Refused as error:
        sys.exit("REFUSED: " + str(error))
    return 0


if __name__ == "__main__":
    sys.exit(main())
