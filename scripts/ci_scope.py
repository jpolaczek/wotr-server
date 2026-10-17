#!/usr/bin/env python3
"""BUILD-03: decide whether a push needs the .NET build and tests or only the documentation checks.

Prints `code=true` or `code=false` for $GITHUB_OUTPUT. Documentation is docs/** and the Markdown files at the
repository root, except what the .NET tests read: the data files of docs/sources (JSON indexes of the cards) and
docs/TEST_SCENARIOS.md. Pinning wotr-client rewrites links in many Markdown files, docs/sources included, and stays
documentation. Without a base commit to compare with (a new branch, a tag, a shallow history) the answer is always
code=true.
"""
import subprocess
import sys

READ_BY_TESTS = "docs/TEST_SCENARIOS.md"
EMPTY = "0" * 40


def documentation(path):
    if path == READ_BY_TESTS or path.startswith("docs/sources/") and not path.endswith(".md"):
        return False
    return path.startswith("docs/") or ("/" not in path and path.endswith(".md"))


def needs_code(paths):
    return not paths or any(not documentation(path) for path in paths)


def changed(base, head):
    """The paths changed between two commits, or None when the base is unknown."""
    if not base or base == EMPTY:
        return None
    known = subprocess.run(["git", "cat-file", "-e", base + "^{commit}"], capture_output=True)
    if known.returncode != 0:
        return None
    diff = subprocess.run(["git", "diff", "--name-only", base, head], capture_output=True, text=True, check=True)
    return [line for line in diff.stdout.splitlines() if line]


def main(argv):
    base = argv[1] if len(argv) > 1 else ""
    head = argv[2] if len(argv) > 2 else "HEAD"
    paths = changed(base, head)
    code = paths is None or needs_code(paths)
    print("code=" + ("true" if code else "false"))
    if paths is not None:
        print(f"{len(paths)} changed paths; " + ("full checks" if code else "documentation only"), file=sys.stderr)


if __name__ == "__main__":
    main(sys.argv)
