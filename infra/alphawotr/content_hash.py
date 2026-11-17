#!/usr/bin/env python3
"""DIST-20: one hash of what the image runs (AlphaWOTR's files, the sidecar, the installed packages and their versions),
so two builds can be compared: `docker run --rm --entrypoint python3 wotr-alphawotr:0.2.0 /opt/sidecar/content_hash.py`."""
import hashlib
import importlib.metadata
import os
import sysconfig

digest = hashlib.sha256()
for top in ("/opt/alphawotr", "/opt/sidecar", sysconfig.get_paths()["purelib"]):
    for folder, dirs, files in sorted(os.walk(top)):
        dirs.sort()
        if "__pycache__" in folder:
            continue
        for name in sorted(files):
            if name.endswith(".pyc") or name == "RECORD" or name == "INSTALLER" or name == "direct_url.json":
                continue
            path = os.path.join(folder, name)
            digest.update(path.encode() + b"\0")
            with open(path, "rb") as f:
                digest.update(hashlib.sha256(f.read()).digest())
packages = sorted(f"{d.metadata['Name'].lower()}=={d.version}" for d in importlib.metadata.distributions())
digest.update("\n".join(packages).encode())
print(digest.hexdigest(), len(packages), "packages")
