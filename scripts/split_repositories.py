#!/usr/bin/env python3
"""Split the WoTR monorepo into wotr-server and wotr-client with their history (DIST-03).

`plan` classifies every path that exists now or ever existed. `split` clones the
monorepo twice (the source checkout is only read), keeps each side's paths with
git filter-repo, saves the old-to-new commit maps and compares the resulting trees
with the monorepo. `rewrite` then turns references to files that moved to the other
repository into pinned cross-repository references (scripts/crossrepo.py).
"""
import argparse
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys

import crossrepo

ROOT = Path(__file__).resolve().parents[1]
BOTH = ["README.md", "AGENTS.md", ".gitignore", ".editorconfig", ".gitattributes", ".github/",
        "scripts/crossrepo.py", "scripts/dotnet.sh", "scripts/postgres.sh"]
# Server-owned files under client folders: map geometry sources and the rule-to-UI decision inventory.
SERVER_EXCEPTIONS = ["graphicalassets/region-map/", "graphicalassets/highedfmap.jpeg",
                     "docs/ui/interaction-inventory.json"]
CLIENT = ["unity/", "graphicalassets/", "release/", "docs/ui/", "docs/release/PLAYER-GUIDE.md",
          "docs/sources/UI-102-MAP-CANVAS.json", "docs/sources/MAP-14-TERRAIN.json",
          "tests/release/test_release.py", "tests/release/window.swift", "tests/release/test_bootstrap_deps.py",
          "wotr-deps.lock.json"] + ["scripts/" + name for name in [
              "accept-menu-builds.py", "audit-menu-play.py", "bootstrap_deps.py", "check_docs.py",
              "expansion_ui_acceptance.py", "export_map_canvas.py", "export_map_terrain.py",
              "merge-motion-profile.py", "profile-client.py", "release.py", "requirements-map-canvas.txt",
              "review-map-client.py", "smoke-macos.py", "test-client-batches.py", "test-client.sh",
              "test_map_profile.py", "test_profile_client.py", "unity.sh"]]
REPOSITORIES = {"server": "jpolaczek/wotr-server", "client": "jpolaczek/wotr-client"}
MARKDOWN_LINK = re.compile(r"(\]\()([^)\s]+)(\))")
REGISTRY_FIELDS = ("read_before", "update_docs", "documentation")


def matches(path, rules):
    return any(path == rule or (rule.endswith("/") and path.startswith(rule)) for rule in rules)


def classify(path):
    if matches(path, BOTH):
        return "both"
    if matches(path, SERVER_EXCEPTIONS):
        return "server"
    return "client" if matches(path, CLIENT) else "server"


def git(cwd, *args, **kwargs):
    return subprocess.run(["git", *args], cwd=cwd, check=True, stdout=subprocess.PIPE, text=True, **kwargs).stdout


def paths(source, revision):
    current = git(source, "ls-tree", "-r", "--name-only", revision).splitlines()
    history = git(source, "log", "--format=", "--name-only", revision).splitlines()
    return sorted(set(current)), sorted(set(history) | set(current))


def plan(source, revision):
    current, every = paths(source, revision)
    report = {"revision": git(source, "rev-parse", revision).strip(), "current": {}, "history": {}}
    for label, items in (("current", current), ("history", every)):
        for side in ("server", "client", "both"):
            report[label][side] = sum(1 for path in items if classify(path) == side)
    return report, current, every


def split(source, revision, output, filter_repo):
    report, current, every = plan(source, revision)
    output.mkdir(parents=True, exist_ok=False)
    for side in ("server", "client"):
        target = output / ("wotr-" + side)
        git(output, "clone", "--quiet", "--no-local", "--no-checkout", str(source), target.name)
        git(target, "checkout", "--quiet", "-B", "main", report["revision"])
        for branch in git(target, "for-each-ref", "--format=%(refname:short)", "refs/heads/").split():
            if branch != "main":
                git(target, "branch", "--quiet", "-D", branch)
        git(target, "remote", "remove", "origin")
        kept = [path for path in every if classify(path) in (side, "both")]
        listing = output / f"paths-{side}.txt"
        listing.write_text("".join(path + "\n" for path in kept))
        subprocess.run([filter_repo, "--paths-from-file", str(listing), "--force", "--quiet"], cwd=target, check=True)
        for name in ("commit-map", "ref-map"):
            shutil.copyfile(target / ".git/filter-repo" / name, output / f"{side}-{name}")
        expected = sorted(path for path in current if classify(path) in (side, "both"))
        actual = sorted(git(target, "ls-tree", "-r", "--name-only", "main").splitlines())
        report[side] = {"head": git(target, "rev-parse", "main").strip(), "files": len(actual),
                        "commits": int(git(target, "rev-list", "--count", "main")),
                        "tree_matches_monorepo": actual == expected,
                        "missing": sorted(set(expected) - set(actual)), "extra": sorted(set(actual) - set(expected))}
        if actual != expected:
            raise SystemExit(f"{side}: tree differs from the monorepo selection; inspect {output}.")
    (output / "split-report.json").write_text(json.dumps(report, indent=2) + "\n")
    return report


def tracked(repository):
    return set(git(repository, "ls-files").splitlines())


def peer_target(own, peer, document, target, repository, commit):
    """The pinned URL for a relative link that now points into the peer repository, else None."""
    if re.match(r"[a-zA-Z][\w+.-]*:", target) or target.startswith("#"):
        return None
    local, _, anchor = target.partition("#")
    resolved = os.path.normpath(str(PurePosixPath(document).parent / local))
    if resolved.startswith(".."):
        return None
    in_own = resolved in own or any(path.startswith(resolved + "/") for path in own)
    if in_own:
        return None
    if resolved in peer:
        kind = "blob"
    elif any(path.startswith(resolved + "/") for path in peer):
        kind = "tree"
    else:
        return None
    return f"https://github.com/{repository}/{kind}/{commit}/{resolved}" + ("#" + anchor if anchor else "")


def rewrite_markdown(repository_root, own, peer, peer_repository, peer_commit):
    changed = 0
    for document in sorted(path for path in own if path.endswith(".md")):
        path = repository_root / document
        text = path.read_text(encoding="utf-8")
        def replace(match):
            url = peer_target(own, peer, document, match.group(2), peer_repository, peer_commit)
            return match.group(1) + url + match.group(3) if url else match.group(0)
        moved = MARKDOWN_LINK.sub(replace, text)
        if moved != text:
            path.write_text(moved, encoding="utf-8")
            changed += 1
    return changed


def rewrite_registry(repository_root, own, peer, alias):
    path = repository_root / "docs/tasks/tasks.json"
    data = json.loads(path.read_text())
    moved = 0
    for task in data["tasks"]:
        for field in REGISTRY_FIELDS:
            values = []
            for value in task[field]:
                if value not in own and value in peer:
                    value, moved = f"{alias}:{value}", moved + 1
                values.append(value)
            task[field] = values
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n")
    return moved


def rewrite(repository_root, side, peer_root, peer_revision):
    peer_side = "client" if side == "server" else "server"
    alias = "wotr-" + peer_side
    own, peer = tracked(repository_root), set(git(peer_root, "ls-tree", "-r", "--name-only", peer_revision).splitlines())
    commit = git(peer_root, "rev-parse", peer_revision).strip()
    report = {"links": rewrite_markdown(repository_root, own, peer, REPOSITORIES[peer_side], commit)}
    if side == "server":
        report["registry"] = rewrite_registry(repository_root, own, peer, alias)
    crossrepo.pin(repository_root, alias, REPOSITORIES[peer_side], peer_root, peer_revision, [])
    report["pinned"] = commit
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    commands = parser.add_subparsers(dest="command", required=True)
    planning = commands.add_parser("plan")
    splitting = commands.add_parser("split")
    for command in (planning, splitting):
        command.add_argument("--source", type=Path, default=ROOT)
        command.add_argument("--revision", default="HEAD")
    splitting.add_argument("--output", type=Path, required=True)
    splitting.add_argument("--filter-repo", default="git-filter-repo")
    rewriting = commands.add_parser("rewrite")
    rewriting.add_argument("--repository", type=Path, required=True)
    rewriting.add_argument("--side", choices=["server", "client"], required=True)
    rewriting.add_argument("--peer", type=Path, required=True)
    rewriting.add_argument("--peer-revision", default="HEAD")
    args = parser.parse_args()
    if args.command == "plan":
        report = plan(args.source, args.revision)[0]
    elif args.command == "split":
        report = split(args.source.resolve(), args.revision, args.output.resolve(), args.filter_repo)
    else:
        report = rewrite(args.repository.resolve(), args.side, args.peer.resolve(), args.peer_revision)
    print(json.dumps(report, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
