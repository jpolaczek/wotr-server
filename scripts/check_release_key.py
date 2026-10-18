#!/usr/bin/env python3
"""SEC-03: check that the published release-key fingerprint matches the key the game pins.

Reads the public key and key id from the client's ReleasePublisher.cs and compares them, with the SHA-256 of the raw
key, against SECURITY.md of both repositories. Python standard library only.

Usage: python3 scripts/check_release_key.py [--client ../wotr-client]
"""
import argparse
import base64
import hashlib
import re
import sys
from pathlib import Path

SERVER = Path(__file__).resolve().parent.parent
PUBLISHER = "unity/Wotr.Client/Assets/Wotr/Runtime/Networking/ReleasePublisher.cs"


def pinned_key(client):
    source = (client / PUBLISHER).read_text(encoding="utf-8")
    public_key = re.search(r'PublicKey = "([A-Za-z0-9+/=]+)"', source).group(1)
    key_id = re.search(r'KeyId = "([0-9a-f]+)"', source).group(1)
    return public_key, key_id


def expected_values(public_key, key_id):
    raw = base64.b64decode(public_key)
    if len(raw) != 32:
        raise SystemExit(f"FAIL: the pinned key has {len(raw)} bytes, not 32.")
    fingerprint = hashlib.sha256(raw).hexdigest()
    if fingerprint[:16] != key_id:
        raise SystemExit(f"FAIL: key id {key_id} is not the first 8 bytes of SHA-256 {fingerprint}.")
    return {"key id": key_id, "public key": public_key, "fingerprint": fingerprint}


def check_document(path, values):
    text = path.read_text(encoding="utf-8")
    missing = [name for name, value in values.items() if f"`{value}`" not in text]
    return [f"{path}: {name} missing or different" for name in missing]


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--client", type=Path, default=SERVER.parent / "wotr-client")
    arguments = parser.parse_args(argv)
    values = expected_values(*pinned_key(arguments.client))
    problems = check_document(SERVER / "SECURITY.md", values) + check_document(arguments.client / "SECURITY.md", values)
    if problems:
        print("\n".join("FAIL: " + problem for problem in problems))
        return 1
    print(f"PASS: release key {values['key id']} (SHA-256 {values['fingerprint']}) matches both SECURITY.md files.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
