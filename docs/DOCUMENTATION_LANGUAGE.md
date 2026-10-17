# English documentation and cleanup

The user requested English documentation and tracked notes, release icons for
Windows/macOS, removal of completed tasks and outdated documents, and commits and
push to both repositories. Conversation may continue in the user's language.

## Retained documentation

Current documentation, active task descriptions, notes, templates and generated
reports use English. README, AGENTS, REQUIREMENTS, workflow and key architecture
and rules sections were reviewed manually. The remaining corpus used a local
Polish–English engine with terminology and structural review. The initial inventory
covered 859 text files, including 592 Markdown files, before deletion.

R06/D09, both AGENTS files, the task template, validator and report generators now
agree on English. Technical identifiers, source text, numbers, hashes and original
publications were preserved; old heading anchors remain where relevant. Git
history was not rewritten.

## Removed material

- 405 completed task specifications and full registry entries, including this
  documentation task after completion; earlier descriptions and evidence remain in
  Git history. Since 2026-10-17 the whole task registry, the roadmap and the per-task
  testing log are local working files ignored by Git; the tracked
  `docs/rules/content-ledger.json` keeps the card and character coverage metadata.
- The obsolete client UX_LEGACY, WIREFRAMES, UI02-REVIEW and UI05-REVIEW documents,
  replaced by current UX specifications and scenarios.
- The server MONOREPO_DEPENDENCIES plan, superseded by REPOSITORY_SPLIT.
- Superseded task-by-task rollout sections in ROADMAP and WORKFLOW. The roadmap
  now lists remaining work; the workflow states the current maintenance policy.

Current rule specifications and source/acceptance evidence remain useful even
when their implementation task is complete. Existing unfinished task states,
assignees and dependency blockers were retained. Links to removed task files were
removed; links to superseded documents now target their current replacements.

## Verification

- Server task validation: active graph, gates, requirement and content coverage,
  English sections, links, history, generated index and source PDF SHA-256.
- Client documentation validation: local and pinned server references.
- Four completion-ledger regression tests: resolved and missing dependencies,
  unfinished prerequisites, duplicate identifiers and missing content packs.
- Before/after comparison: every existing active task has identical blockers.
- All five card/character coverage reports pass freshness checks; Fate acceptance
  and base/Lords/Kings source validators pass with the compact ledger.
- Release-tool tests: server 18/18, client 34/34; dependency package tests: 6/6.
- Changed Python scripts compile, changed shell scripts pass `bash -n`, and
  both repositories pass `git diff --check`.
- Technical-value audits compare retained files against the original inventory.

The client button inventory is current (223 controls). Its strict check reports
the existing “×” observer-removal button in WaitingRoomScreen; the pre-change
script reports the same issue. The standalone Fate image-source audit could not
run because its ignored local image cache is absent. Fate metadata, coverage and
acceptance checks passed. No full Unity build or installed-app replacement was
performed for this documentation/artwork change.

## Release icon

Client artwork commit `3fc8f07e` contains the original generated master, exact
prompt/provenance, PNG, Windows ICO, macOS ICNS and Unity default icon reference.
The next normal desktop build consumes that icon.

Checks passed for the 1024×1024 RGBA PNG, seven RGBA32 ICO sizes, ten standard/Retina
ICNS variants, matching release/imported PNG bytes and the ProjectSettings texture
GUID. The master and small preview were visually inspected; the master is unchanged
from the built-in imagegen output. Asset paths and the full prompt are documented
in the client's `graphicalassets/branding/README.md`; exports are in `release/icons/`.
