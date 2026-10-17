<a id="podział-monorepo-na-wotr-server-i-wotr-client--dist-03"></a>
# Split monorepo into wotr-server and wotr-client — DIST-03

Source: DIST-03, [DISTRIBUTION_PLAN](DISTRIBUTION_PLAN.md) and [MONOREPO_DEPENDENCIES](REPOSITORY_SPLIT.md). User's consent with 2026-10-12: private repositories `jpolaczek/wotr-server` and `jpolaczek/wotr-client`, push after tests; the existing `jpolaczek/WotR` remains unchanged.

<a id="granice"></a>
## Boundaries

The classification of each path — present and historic — includes `scripts/split_repositories.py`. The first matching rule wins:

| Page | Paths |
| --- | --- |
| both | `README.md`, `AGENTS.md`, `.gitignore`, `.editorconfig`, `.gitattributes`, `.github/`, `scripts/crossrepo.py`, `scripts/dotnet.sh`, `scripts/postgres.sh` |
| server (exceptions) | `graphicalassets/region-map/` and `graphicalassets/highedfmap.jpeg` (sources of geometry and audit MAP-13), `docs/ui/interaction-inventory.json` (decision window from rule directory, checked by `check_catalog.py`) |
| client | `unity/`, `graphicalassets/`, `release/`, `docs/ui/`, `docs/release/PLAYER-GUIDE.md`, `docs/sources/UI-102-MAP-CANVAS.json` and `MAP-14-TERRAIN.json` maps, `tests/release/` (issue and bootstrap release tests), `wotr-deps.lock.json` and build scripts, PlayMode, profiling, maps and release |
| server | Everything else: `src/` , `tests/Wotr.Tests` , `tests/Wotr.Scenarios` , `tests/fixtures` , `tests/deployment` , `tests/packages` , `content/` , `infra/` , central documentation, register, PDF sources |

`README.md` and `AGENTS.md` have their own versions in each repository, and CI is separated: the server launches validator, parcel tests, cover scripts and .NET; client — document control, Python tests and optional Unity job.

<a id="przebieg"></a>
## Run

```bash
python3 -m venv <tmp>/filter-repo && <tmp>/filter-repo/bin/pip install git-filter-repo==2.47.0
python3 scripts/split_repositories.py plan --revision 614c898
python3 scripts/split_repositories.py split --revision 614c898 --output <katalog> \
  --filter-repo <tmp>/filter-repo/bin/git-filter-repo
python3 scripts/split_repositories.py rewrite --repository <katalog>/wotr-client --side client --peer <katalog>/wotr-server
python3 scripts/split_repositories.py rewrite --repository <katalog>/wotr-server --side server --peer <katalog>/wotr-client
```

`split` clones monorepo twice (`git clone --no-local`), leaves the branch `main` on a given committee and removes the remaining references, after which `git filter-repo --paths-from-file` retains the path of the page from the whole history. Active checkout monorepo, its `master` and remote `WotR` are not rewritten.

<a id="wynik-podziału"></a>
## Breakdown result

Database: monorepo `claude/dist-03` `614c898` (428 commits, no tags).

| Repository | Commit after split | Commity | Files | Tree = monorepo selection |
| --- | --- | --- | --- | --- |
| wotr-server | `64492f3` | 421 (7OZONOWOLZOWOLZOWOLZOWOWED) | 1472 | yes |
| wotr-client | `a6c1b90` | 343 (85OZONOWOLZOWOLZOWOLZOWOWED) | 2103 | yes |

Maps of old commits on new (`0000…` means commit empty after filter), lists of saved paths and report are in [history](history/split-report.json): `wotr-server-commit-map.txt`, `wotr-client-commit-map.txt`, `wotr-server-paths.txt`, `wotr-client-paths.txt`.

<a id="odnośniki-między-repozytoriami"></a>
## References between repositories

`docs/repositories/<alias>.json` in each repository contains the name of the second repository, pinned commit and list of its tracked files (`scripts/crossrepo.py`). The link always has a repository, commit and path:

- server registry: `wotr-client:<path>` in `read_before`, `update_docs`, `documentation` (1377
  from the positions assigned); `tasks.py validate` checks them against the commit's list of files
  attached;
- Markdown: `https://github.com/<repository>/blob/<commit>/<path>` (128 server documents, 29
  client documents); validators reject a link to a commit other than pinned and to a file that is
  not there.

`python3 scripts/tasks.py pin-repo wotr-client --checkout … --repository jpolaczek/wotr-client` (server) and `python3 scripts/check_docs.py pin --checkout …` (client) move the pin and all links to the new commit; validation after moving shows links to files removed in the second repository.

<a id="zależności-i-testy-klienta"></a>
## Client Dependency and Tests

The client does not include `src/`, `content/` or the seeder project. `wotr-deps.lock.json` is a contract, public content and test archives of `wotr-server` and `wotr-seeder`, published by `scripts/package_deps.py --test-backend` as a release of `<name>-<version>` repository of `jpolaczek/wotr-server`. `scripts/test-client.sh` runs this server and seeder on an isolated base; the test archives do not enter the production image nor the player packages.
