#!/usr/bin/env python3
"""DIST-20: every file of the AlphaWOTR clone against its RELEASE_MANIFEST.tsv (path, sha256). Files the manifest lists
that the git tree lacks (the release zip's bundled Python) are counted, not failed; the bundled WOTR-Gandalf client is not
used by the protocol and is removed from the image."""
import hashlib
import os
import sys

root = sys.argv[1]
bad = missing = ok = 0
for line in open(os.path.join(root, "RELEASE_MANIFEST.tsv"), encoding="utf-8"):
    parts = line.rstrip("\n").split("\t")
    if len(parts) < 2 or parts[0] == "RELEASE_MANIFEST.tsv" or parts[0].startswith("wotr_bridge/play_client/"):
        continue
    path = os.path.join(root, parts[0])
    if not os.path.isfile(path):
        missing += 1
        continue
    with open(path, "rb") as f:
        digest = hashlib.sha256(f.read()).hexdigest()
    if digest == parts[1]:
        ok += 1
    else:
        bad += 1
        print("MISMATCH", parts[0])
print(f"manifest: {ok} files match, {bad} differ, {missing} listed but not in the clone")
sys.exit(1 if bad or ok < 1000 else 0)
