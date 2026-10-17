#!/usr/bin/env python3
"""DIST-11, on the distribution host (the wotr-server checkout with infra/.env): what the publisher's command needs
to know about the host, and the ReleaseTool operator commands through Compose.

  state                                   JSON: incoming folder, downloads and API URLs, public key, catalog state
  publish --version v [--minimum [--at T]] announce an uploaded release (optionally as the minimum, now or at T)
  minimum (--version v [--at T] | --cancel-scheduled)
  withdraw --version v | verify | status

ReleaseTool runs in the server image without a network, as the operator, with only the distribution folder
mounted for writing. Not through `docker compose run server`: that would take the running server's fixed address.
"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
RELEASE_TOOL = "/app/release-tool/Wotr.ReleaseTool.dll"
REQUIRED = ("WOTR_VERSION", "WOTR_DISTRIBUTION_DIR", "WOTR_DOWNLOADS_URL", "WOTR_RELEASE_PUBLIC_KEY", "WOTR_DOMAIN")


def read_environment(path):
    """KEY=VALUE lines of a Compose env file; quotes around a value are dropped."""
    values = {}
    for line in Path(path).read_text().splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        value = value.strip()
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "'\"":
            value = value[1:-1]
        values[key.strip()] = value
    missing = [key for key in REQUIRED if not values.get(key)]
    if missing:
        raise ValueError(f"{path} lacks {', '.join(missing)}; the release catalog is not configured.")
    return values


def distribution(environment):
    folder = Path(environment["WOTR_DISTRIBUTION_DIR"])
    return folder if folder.is_absolute() else (ROOT / "infra" / folder).resolve()


def state(environment):
    folder = distribution(environment)
    catalog_path = folder / "catalog.json"
    catalog = json.loads(catalog_path.read_text()) if catalog_path.is_file() else {}
    incoming = folder / "incoming"
    return {
        "incoming": str(incoming),
        "downloadsUrl": environment["WOTR_DOWNLOADS_URL"],
        "apiUrl": "https://" + environment["WOTR_DOMAIN"].rstrip("/"),
        "publicKey": environment["WOTR_RELEASE_PUBLIC_KEY"],
        "announced": [release["version"] for release in catalog.get("releases", []) if not release.get("withdrawn")],
        "minimumVersion": catalog.get("minimumVersion", ""),
        "scheduledMinimum": catalog.get("scheduledMinimum"),
        "uploaded": sorted(path.name for path in incoming.iterdir() if path.is_dir()) if incoming.is_dir() else [],
    }


def release_tool(environment, arguments):
    """ReleaseTool in the deployed server image, as the operator, with the distribution folder writable at
    /release-root and nothing else: no network, read-only image."""
    return ["docker", "run", "--rm", "--network", "none", "--read-only", "--tmpfs", "/tmp",
            "--user", f"{os.getuid()}:{os.getgid()}", "-v", f"{distribution(environment)}:/release-root",
            "--entrypoint", "dotnet", f"wotr-server:{environment['WOTR_VERSION']}", RELEASE_TOOL,
            *arguments[:1], "--root", "/release-root", "--downloads-url", environment["WOTR_DOWNLOADS_URL"],
            "--public-key", environment["WOTR_RELEASE_PUBLIC_KEY"], *arguments[1:]]


def tool_arguments(args):
    if args.command == "publish":
        return ["publish", "--version", args.version, *(["--minimum"] if args.minimum else []),
                *(["--at", args.at] if args.at else [])]
    if args.command == "minimum":
        return ["minimum", "--cancel-scheduled"] if args.cancel_scheduled else \
            ["minimum", "--version", args.version, *(["--at", args.at] if args.at else [])]
    if args.command == "withdraw":
        return ["withdraw", "--version", args.version]
    return [args.command]


def parse(argv):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--env-file", default=str(ROOT / "infra/.env"))
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("state")
    publish = commands.add_parser("publish")
    publish.add_argument("--version", required=True)
    publish.add_argument("--minimum", action="store_true")
    publish.add_argument("--at")
    minimum = commands.add_parser("minimum")
    choice = minimum.add_mutually_exclusive_group(required=True)
    choice.add_argument("--version")
    choice.add_argument("--cancel-scheduled", action="store_true")
    minimum.add_argument("--at")
    withdraw = commands.add_parser("withdraw")
    withdraw.add_argument("--version", required=True)
    commands.add_parser("verify")
    commands.add_parser("status")
    args = parser.parse_args(argv)
    if args.command == "publish" and args.at and not args.minimum:
        parser.error("--at needs --minimum")
    if args.command == "minimum" and args.at and args.cancel_scheduled:
        parser.error("--at goes with --version")
    return args


def main(argv=None):
    args = parse(argv)
    try:
        environment = read_environment(args.env_file)
    except (OSError, ValueError) as error:
        sys.exit("ERROR: " + str(error))
    if args.command == "state":
        print(json.dumps(state(environment), indent=2))
        return 0
    return subprocess.run(release_tool(environment, tool_arguments(args)), cwd=ROOT).returncode


if __name__ == "__main__":
    sys.exit(main())
