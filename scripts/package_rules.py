#!/usr/bin/env python3
"""Build a pinned, deterministic source package of the pure engine for independent consumers."""
import argparse
import fnmatch
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from package_deps import (ROOT, PackageError, SEMVER, archive, check_contracts, clean_commit, committed,
                          contracts_version, digest, fingerprint, protocol, tracked)

SOURCES = ["src/Wotr.Contracts", "src/Wotr.Domain", "content", "Directory.Build.props", "global.json",
           "LICENSE", "docs/sources/materials/map-data-MIT-LICENSE.txt", "scripts/package_rules.py", "scripts/package_deps.py"]


def selected(root, commit):
    check_contracts(root, commit)
    paths = {"Directory.Build.props", "global.json", "LICENSE", "docs/sources/materials/map-data-MIT-LICENSE.txt"}
    for folder in ("src/Wotr.Contracts", "src/Wotr.Domain"):
        for path in tracked(root, commit, folder):
            if "/Demo/" not in path and (path.endswith((".cs", ".csproj")) or path.endswith("/packages.lock.json")):
                paths.add(path)
    project = ET.fromstring(committed(root, commit, "src/Wotr.Domain/Wotr.Domain.csproj"))
    references = [item.attrib["Include"] for item in project.iter("ProjectReference")]
    if references != ["../Wotr.Contracts/Wotr.Contracts.csproj"] or list(project.iter("PackageReference")):
        raise PackageError("Pure engine must depend only on Contracts; review new dependencies before packaging.")
    content = tracked(root, commit, "content")
    for resource in project.iter("EmbeddedResource"):
        pattern = resource.attrib["Include"]
        if not pattern.startswith("../../content/") or ".." in pattern[6:]:
            raise PackageError("Unexpected embedded resource: " + pattern)
        matches = fnmatch.filter(content, pattern[6:])
        if not matches:
            raise PackageError("Embedded resource has no committed input: " + pattern)
        paths.update(matches)
    return sorted(paths)


def build(root=ROOT):
    commit = clean_commit(root, SOURCES)
    metadata = json.loads(committed(root, commit, "content/rules-package.json"))
    if metadata["name"] != "wotr-rules" or not SEMVER.fullmatch(metadata["version"]):
        raise PackageError("Rules package needs its reviewed name and x.y.z version.")
    files = {name: committed(root, commit, name) for name in selected(root, commit)}
    hashes = {name: digest(value) for name, value in files.items()}
    manifest = {
        "schemaVersion": 1, "name": metadata["name"], "version": metadata["version"],
        "repository": "https://github.com/jpolaczek/wotr-server", "sourceCommit": commit,
        "contractsVersion": contracts_version(root, commit), "protocol": protocol(root, commit),
        "sourceFingerprint": fingerprint(hashes), "files": hashes,
    }
    bundle = archive(files)
    manifest.update(archive=f"wotr-rules-{metadata['version']}.tar.gz", sha256=digest(bundle))
    return bundle, manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / ".artifacts/rules-package")
    args = parser.parse_args()
    try:
        bundle, manifest = build()
        payloads = {manifest["archive"]: bundle,
                    "wotr-rules.lock.json": (json.dumps(manifest, indent=2, sort_keys=True) + "\n").encode()}
        args.output.mkdir(parents=True, exist_ok=True)
        for name, data in payloads.items():
            path = args.output / name
            if path.exists() and path.read_bytes() != data:
                raise PackageError("Refusing to replace a different pinned artifact: " + str(path))
        for name, data in payloads.items():
            (args.output / name).write_bytes(data)
        print(f"PASS: {manifest['archive']}; {len(manifest['files'])} files; source {manifest['sourceCommit']}; sha256 {manifest['sha256']}")
        return 0
    except PackageError as error:
        parser.exit(1, str(error) + "\n")


if __name__ == "__main__":
    raise SystemExit(main())
