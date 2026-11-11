#!/usr/bin/env python3
"""Reject operator-only bot files in public publishes, archives and unpacked players."""
import argparse
from pathlib import Path
import tarfile
import zipfile


def require_public(names):
    forbidden = ("wotr.bot.", "wotr.runtimefixture", "wotr.runtimeupgradefixture", "wotr-bot-runtime-", "wotr-bot-symbols-")
    for name in names:
        parts = str(name).replace("\\", "/").lower().split("/")
        if any(part.startswith(forbidden) or part in ("runtime-manifest.json", "private-symbols.json") for part in parts):
            raise ValueError("Operator-only bot component in public artifact: " + str(name))


def inspect_archive(path):
    if zipfile.is_zipfile(path):
        with zipfile.ZipFile(path) as archive:
            names = archive.namelist()
    elif tarfile.is_tarfile(path):
        with tarfile.open(path) as archive:
            names = archive.getnames()
    else:
        raise ValueError("Inspect an unpacked installer directory, ZIP or tar archive: " + str(path))
    require_public(names)
    return len(names)


def inspect(path):
    require_public([path.name])
    if not path.is_dir():
        return inspect_archive(path)
    files = [item for item in path.rglob("*") if item.is_file()]
    require_public(str(item.relative_to(path)) for item in files)
    count = len(files)
    for item in files:
        if item.name.lower().endswith((".zip", ".tgz", ".tar.gz", ".tar", ".nupkg")):
            count += inspect_archive(item)
    return count


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("artifacts", type=Path, nargs="+")
    args = parser.parse_args()
    try:
        for artifact in args.artifacts:
            print(f"PASS: {inspect(artifact)} public entries inspected in {artifact}")
    except (ValueError, OSError, tarfile.TarError, zipfile.BadZipFile) as error:
        parser.exit(1, str(error) + "\n")


if __name__ == "__main__":
    main()
