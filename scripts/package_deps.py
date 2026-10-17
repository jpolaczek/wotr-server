#!/usr/bin/env python3
"""Build the client's server-owned dependencies from one clean commit (DIST-02).

Both archives use the UPM tarball layout (package/...). The Contracts archive is
resolved by Unity; the public content archive is read by scripts/bootstrap_deps.py,
which checks the tracked Resources copies against it. Archives and manifests are
reproducible: unchanged sources give the same bytes on the same toolchain.

With --test-backend it also publishes the server and the PlayMode seeder as two
separate test-only archives (DIST-03), versioned by the server commit. Client
integration runs them instead of compiling the backend; neither is a player artifact.
"""
import argparse
import gzip
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CHANNEL = ROOT / ".artifacts/deps-channel"
CONTRACTS = "com.wotr.contracts"
CONTENT = "com.wotr.public-content"
CONTRACTS_SOURCE = "src/Wotr.Contracts"
ALLOWLIST = "content/unity-public-content.json"
# .NET build files stay with the server; Unity only needs the package and its runtime sources.
CONTRACTS_EXCLUDED = {"Wotr.Contracts.csproj", "Wotr.Contracts.csproj.meta",
                      "packages.lock.json", "packages.lock.json.meta"}
SERVER_NAMESPACES = re.compile(r"\bWotr\.(Domain|Server|Persistence)\b")
SEMVER = re.compile(r"\d+\.\d+\.\d+")
PUBLIC_NAME = re.compile(r"[a-z0-9-]+\.json")
TEST_BACKEND = {"wotr-server": ("src/Wotr.Server", "Wotr.Server.dll"),
                "wotr-seeder": ("tests/Wotr.Scenarios", "Wotr.Scenarios.dll")}
TEST_SOURCES = ["src", "tests/Wotr.Scenarios", "content", "Directory.Build.props", "global.json"]
# Fixed entry time, as npm pack does: unchanged sources give the same bytes in any later commit.
ARCHIVE_TIME = 499162500


class PackageError(Exception):
    pass


def git(root, *args):
    return subprocess.run(["git", *args], cwd=root, check=True, stdout=subprocess.PIPE).stdout


def clean_commit(root, paths):
    if git(root, "status", "--porcelain", "--untracked-files=no", "--", *paths).strip():
        raise PackageError("Commit changes under " + ", ".join(paths) + " before packaging.")
    return git(root, "rev-parse", "HEAD").decode().strip()


def committed(root, commit, path):
    return git(root, "show", f"{commit}:{path}")


def tracked(root, commit, folder):
    names = git(root, "ls-tree", "-r", "--name-only", commit, "--", folder).decode().splitlines()
    return sorted(names)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def fingerprint(files):
    lines = "".join(f"{name}:{files[name]}\n" for name in sorted(files))
    return digest(lines.encode())


def archive(entries):
    """A gzip tar with sorted entries and fixed owners, modes and times."""
    raw = io.BytesIO()
    with gzip.GzipFile(filename="", mode="wb", fileobj=raw, mtime=ARCHIVE_TIME, compresslevel=9) as compressed:
        with tarfile.open(fileobj=compressed, mode="w", format=tarfile.USTAR_FORMAT) as tar:
            for name in sorted(entries):
                info = tarfile.TarInfo(name)
                info.size = len(entries[name])
                info.mtime = ARCHIVE_TIME
                info.mode = 0o644
                info.uid = info.gid = 0
                info.uname = info.gname = ""
                tar.addfile(info, io.BytesIO(entries[name]))
    return raw.getvalue()


def protocol(root, commit):
    source = committed(root, commit, CONTRACTS_SOURCE + "/Runtime/Protocol.cs").decode()
    values = {}
    for key, pattern in [("version", r"int Version = (\d+);"), ("rulesVersion", r'RulesVersion = "([^"]+)";'),
                         ("contentVersion", r'ContentVersion = "([^"]+)";')]:
        match = re.search(pattern, source)
        if not match:
            raise PackageError("Protocol.cs does not declare " + key + ".")
        values[key] = int(match.group(1)) if key == "version" else match.group(1)
    return values


def contracts_version(root, commit):
    package = json.loads(committed(root, commit, CONTRACTS_SOURCE + "/package.json"))
    if package.get("name") != CONTRACTS or not SEMVER.fullmatch(str(package.get("version"))):
        raise PackageError("Contracts package.json needs name " + CONTRACTS + " and a plain x.y.z version.")
    return package["version"]


def check_contracts(root, commit):
    project = committed(root, commit, CONTRACTS_SOURCE + "/Wotr.Contracts.csproj").decode()
    if "<TargetFramework>netstandard2.1</TargetFramework>" not in project or "ProjectReference" in project:
        raise PackageError("Contracts must target netstandard2.1 without project references.")
    for path in tracked(root, commit, CONTRACTS_SOURCE):
        if path.endswith(".cs") and SERVER_NAMESPACES.search(committed(root, commit, path).decode()):
            raise PackageError(path + " refers to server code.")


def contracts_package(root, commit):
    check_contracts(root, commit)
    version = contracts_version(root, commit)
    prefix = CONTRACTS_SOURCE + "/"
    entries = {"package/" + path[len(prefix):]: committed(root, commit, path)
               for path in tracked(root, commit, CONTRACTS_SOURCE)
               if path[len(prefix):] not in CONTRACTS_EXCLUDED}
    extra = {"protocol": protocol(root, commit)}
    return CONTRACTS, version, archive(entries), entries, extra


def allowlist(root, commit):
    data = json.loads(committed(root, commit, ALLOWLIST))
    files = data.get("files")
    if not SEMVER.fullmatch(str(data.get("version"))):
        raise PackageError(ALLOWLIST + " needs a plain x.y.z version.")
    if not files or len(files) != len(set(files)) or not all(isinstance(n, str) and PUBLIC_NAME.fullmatch(n) for n in files):
        raise PackageError(ALLOWLIST + " must list distinct plain JSON filenames.")
    return data["version"], sorted(files)


def content_package(root, commit):
    version, names = allowlist(root, commit)
    files = {name: committed(root, commit, "content/" + name) for name in names}
    hashes = {name: digest(files[name]) for name in names}
    requires = {CONTRACTS: contracts_version(root, commit)}
    content_version = protocol(root, commit)["contentVersion"]
    metadata = {"name": CONTENT, "version": version, "displayName": "WoTR public content",
                "description": "Allowlisted public catalogs and map data for the client.",
                "unity": "6000.6", "wotr": {"files": names, "requires": requires, "contentVersion": content_version}}
    entries = {"package/package.json": (json.dumps(metadata, indent=2, sort_keys=True) + "\n").encode()}
    entries.update({"package/Resources/" + name: files[name] for name in names})
    extra = {"requires": requires, "contentVersion": content_version, "fingerprint": fingerprint(hashes),
             "public": [{"name": name, "sha256": hashes[name]} for name in names]}
    return CONTENT, version, archive(entries), entries, extra


def manifest(name, version, commit, data, entries, extra):
    value = {"schema": 1, "name": name, "version": version, "commit": commit,
             "archive": f"{name}-{version}.tgz", "size": len(data), "sha256": digest(data),
             "files": {path: digest(entries[path]) for path in sorted(entries)}}
    value.update(extra)
    return (json.dumps(value, indent=2, sort_keys=True) + "\n").encode()


def store(archive_path, data, manifest_path, text, keep_existing=False):
    """Publish a new version, or keep an identical one with the manifest of the commit that first made it.

    A test build is versioned by its commit; a later rebuild of that commit keeps the published archive.
    """
    if archive_path.exists():
        if archive_path.read_bytes() != data and not keep_existing:
            raise PackageError(f"{archive_path.name} already exists with other bytes. Bump the version instead of replacing it.")
        return False
    if manifest_path.exists():
        raise PackageError(f"{manifest_path.name} exists without its archive; inspect the channel.")
    archive_path.write_bytes(data)
    manifest_path.write_bytes(text)
    return True


def require_clean_sources(root, paths):
    # dotnet compiles untracked sources too, so they block a test build as well.
    if git(root, "status", "--porcelain", "--untracked-files=normal", "--", *paths).strip():
        raise PackageError("Commit or remove changes under " + ", ".join(paths) + " before publishing the test backend.")


def publish(root, project):
    with tempfile.TemporaryDirectory() as output:
        subprocess.run(["bash", "scripts/dotnet.sh", "publish", project, "-c", "Release", "-o", output, "--nologo",
                        "-p:ContinuousIntegrationBuild=true"], cwd=root, check=True,
                       stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
        folder = Path(output)
        return {str(path.relative_to(folder)): path.read_bytes() for path in sorted(folder.rglob("*")) if path.is_file()}


def test_backend_packages(root, commit):
    require_clean_sources(root, TEST_SOURCES)
    for name, (project, entry) in TEST_BACKEND.items():
        files = publish(root, project)
        if entry not in files:
            raise PackageError(f"dotnet publish of {project} did not produce {entry}.")
        entries = {f"{name}/{path}": data for path, data in files.items()}
        extra = {"entry": f"{name}/{entry}", "framework": "net10.0", "testOnly": True}
        yield name, commit[:12], archive(entries), entries, extra


def build(root, channel, test_backend=False):
    commit = clean_commit(root, [CONTRACTS_SOURCE, "content"])
    channel.mkdir(parents=True, exist_ok=True)
    packages = [make(root, commit) for make in (contracts_package, content_package)]
    results = [publish_package(channel, commit, package, False) for package in packages]
    if test_backend:
        results += [publish_package(channel, commit, package, True) for package in test_backend_packages(root, commit)]
    return commit, results


def publish_package(channel, commit, package, keep_existing):
    name, version, data, entries, extra = package
    stem = f"{name}-{version}"
    text = manifest(name, version, commit, data, entries, extra)
    written = store(channel / (stem + ".tgz"), data, channel / (stem + ".manifest.json"), text, keep_existing)
    return stem, digest(data), written


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--channel", type=Path, default=DEFAULT_CHANNEL,
                        help="Directory that receives <name>-<version>.tgz and .manifest.json.")
    parser.add_argument("--test-backend", action="store_true",
                        help="Also publish the server and the PlayMode seeder for client integration (needs dotnet).")
    args = parser.parse_args()
    try:
        commit, results = build(ROOT, args.channel.resolve(), args.test_backend)
    except PackageError as error:
        raise SystemExit("package_deps: " + str(error))
    print("commit " + commit)
    for stem, sha, written in results:
        print(f"{stem} {sha} {'written' if written else 'unchanged'}")


if __name__ == "__main__":
    main()
