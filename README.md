# War of the Ring — online 1v1 (wotr-server)

<a id="repozytoria"></a>
## Repositories

The project is open source in two public repositories (D17). They start from one fresh
commit; the earlier history, split from the `WotR` monorepo in DIST-03
([split and commit map](docs/release/REPOSITORY_SPLIT.md)), stays in private archives:

- **wotr-server** (this repository): Domain, Server, Contracts, canonical content,
  database and migrations, infrastructure, server tests, central task registry,
  requirements, rules and sources. It publishes client packages (`scripts/package_deps.py`).
- **[wotr-client](https://github.com/jpolaczek/wotr-client)**: Unity, artwork,
  UI documentation, client tests, builds and player packages. It uses pinned
  archives from this repository without its source checkout.

Client file references use `wotr-client:<path>` in the registry and GitHub links
to the commit in `docs/repositories/wotr-client.json`;
`python3 scripts/tasks.py validate` checks them.

A Unity game project for Windows and macOS. Priorities: faithful rules, an
authoritative server and convenient map-based gameplay. Documentation, tracked
project notes, code, identifiers and the game interface are in English (DOC-01).

The base Second Edition, Lords of Middle-earth, Kings of Middle-earth, Fate of
Erebor and the required variants are implemented. Private accounts and tables,
live or correspondence 1v1 play, persistent decisions, cards, battles and sieges,
the Fellowship, Hunt and four victory conditions are supported. The server
validates every command and sends players only their authorized views. The UI
guides choices on the map and in a permanent tray without mandatory numeric
entry; artwork is provisional.

The local **0.1.0-rc1** release candidate covers Windows x64 and macOS Universal.
The [player guide](https://github.com/jpolaczek/wotr-client/blob/1e81512f4f33819ef08b357776fa221372bf2396/docs/release/PLAYER-GUIDE.md)
explains installation, invitations, tables and save recovery. The
[T39 report](docs/release/T39-RELEASE.md) records actual results and package
locations. There is no target host yet; Windows runtime and cross-platform
play are deferred at the user's request. This is a local private package,
without publication or distribution signatures.

The [backend and deployment](infra/README.md) passed local TLS/WSS acceptance
and an actual backup restore with battle continuation. Full T39 backend:
**3499/3499 tests PASS**. The [expansion matrix](docs/rules/T33-COMPOSITION.md)
covers 93 legal configurations; UI evidence and its limits are in
[TESTING](docs/TESTING.md). Final client regression: **198/198 EditMode and
270/270 PlayMode PASS**, including complete base and expansion games.

[Setup and tests](docs/DEVELOPMENT.md) · [Building packages](https://github.com/jpolaczek/wotr-client/blob/1e81512f4f33819ef08b357776fa221372bf2396/release/README.md)

<a id="bezpieczeństwo"></a>
## Security and official endpoints

The maintainer runs only these endpoints: the game site and API at `https://wotr.vibestudios.org` (admin panel at
`/admin`), installers and update packages at `https://downloads.vibestudios.org:50443/`, and the two GitHub
repositories. Updates are signed with the Ed25519 release key `95ffc109e1bb9b15`
(SHA-256 `95ffc109e1bb9b15710cb7ba298fb959751630ae8d96ab3e9dcab9ddadd8b5d3`). Report vulnerabilities privately as
described in [SECURITY](SECURITY.md); what the service stores is in the [privacy note](docs/PRIVACY.md).

<a id="od-czego-zacząć"></a>
## Where to start

1. Read the [requirements](docs/REQUIREMENTS.md) and [agent working instructions](AGENTS.md).
2. Learn the [UI and interactions](https://github.com/jpolaczek/wotr-client/blob/1e81512f4f33819ef08b357776fa221372bf2396/docs/ui/UX_SPEC.md), [game logic](docs/rules/LOGIC.md) and [architecture](docs/ARCHITECTURE.md).
3. Check [sources and gaps](docs/sources/README.md).
4. Work on a task from the local task registry (not in the repository) within the stage commissioned by the user.

<a id="obsługa-zadań"></a>
## Task management

The task registry (`docs/tasks/`) and the roadmap are local working files, ignored by Git (2026-10-17).
`scripts/tasks.py` manages them on the working machine; it requires Python 3, installs nothing and does not
access the network.

```sh
python3 scripts/tasks.py ready --stage A
python3 scripts/tasks.py start T01 --agent codex --note "Audit sources and record gaps"
python3 scripts/tasks.py note T01 --note "Source found; version verification in progress"
python3 scripts/tasks.py validate
```

The [workflow](docs/WORKFLOW.md) describes `finish`, `block` and result-recording
rules. The local tasks.json stores states, timestamps and history; the index is generated, not edited by hand.
CI reads only the tracked content coverage ledger `docs/rules/content-ledger.json`.

<a id="zakres"></a>
## Scope

The base game, Lords of Middle-earth, Kings of Middle-earth and Fate of Erebor,
in the configurations described by the requirements. Private accounts and rooms,
live and correspondence play with every decision saved. Placeholder artwork
is sufficient for the MVP.

Do not implement rules based on guesses. Unconfirmed card text, map connections
or exceptions go into the [gap registry](docs/sources/gaps.json), which blocks
the relevant task rather than the entire project.

<a id="dokumenty-uzupełniające"></a>
## Supporting documents

- [Client–server contracts](docs/CONTRACTS.md)
- [Test scenarios and gates](docs/TESTING.md)
- [Expansion integration rules](docs/rules/EXPANSIONS.md)
- [Design decisions](docs/DECISIONS.md)
- [UI scenario list](https://github.com/jpolaczek/wotr-client/blob/1e81512f4f33819ef08b357776fa221372bf2396/docs/ui/SCENARIOS.md)
Documents describe required behavior; a description alone does not mean a feature is ready.

## The board game

War of the Ring is a board game designed by **Roberto Di Meglio, Marco Maggi and Francesco
Nepitello**, created, published and distributed worldwide by
[Ares Games](https://www.aresgames.eu/games/war-of-the-ring-line). Credits from the English
rulebooks:

| Role | Second Edition | Lords of Middle-earth | Kings of Middle-earth |
| --- | --- | --- | --- |
| Game design | Roberto Di Meglio, Marco Maggi, Francesco Nepitello | Roberto Di Meglio, Marco Maggi, Francesco Nepitello | Roberto Di Meglio, Marco Maggi, Francesco Nepitello |
| Art | John Howe | John Howe; additional art Jon Hodgson | John Howe |
| Art direction and graphic design | Fabio Maiorana | Fabio Maiorana | Fabio Maiorana |
| Sculptures | Bob Naismith | Bob Naismith and Dust Studio | Bob Naismith |
| Sculpture design | John Howe and Matteo Macchi | Matteo Macchi and Francesco Nepitello | — |
| Photos | Christoph Cianci | Christoph Cianci | Christoph Cianci |
| Production | Roberto Di Meglio and Fabrizio Rolla | Roberto Di Meglio and Fabrizio Rolla | Roberto Di Meglio and Fabrizio Rolla |

The Fate of Erebor expansion is also by Ares Games. This online version exists thanks to their
work and with great respect for it. If you enjoy it, please buy the board game from Ares Games or
your local game store.

## License

MIT ([LICENSE](LICENSE)). The map data reused from John Champaign's project keeps its MIT notice
([docs/sources/materials/map-data-MIT-LICENSE.txt](docs/sources/materials/map-data-MIT-LICENSE.txt)).
