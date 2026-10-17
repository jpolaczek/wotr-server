<a id="format-pliku-wotrreplay--wersja-1-replay-02-2026-10-16"></a>
# File format `.wotrreplay` — version 1 (REPLAY-02, 2026-10-16)

The play file is a ZIP archive with entries JSON (UTF-8, field names like in Contracts: cameCase, enumy as text). Types are in `src/Wotr.Contracts/Runtime/ReplayContracts.cs` (`ReplayFormat`, `ReplayManifest`, `ReplayStep`, `ReplayTurn`, `ReplayFrame`, `ReplayBattleReport`). Designs and scenarios RP03/RP04: [OBSERVERS-REPLAYS](OBSERVERS-REPLAYS.md).

<a id="źródło-danych"></a>
## Data source

The file is created only from the recording of the batch (REPLAY-01, the `match_frames` table) and the log (`match_events`). Export does not run rules or draws; later engine change does not change the old play.

<a id="wpisy"></a>
## Entries

The entries names are fixed. The reader rejects any other name, including paths from `..`, `/` at the beginning or `\`.

| Entry | Content |
| --- | --- |
| `manifest.json` | `ReplayManifest`, saved at the end of the archive. |
| `index/steps.json` | `ReplayStep[]`: `step` (from 0), `revision`, `turn`, `phase`, growing. |
| `index/turns.json` | `ReplayTurn[]`: the first step of each turn. |
| `frames/NNNN.json` | To 25 `ReplayFrame` (`step`, `revision`, `freePeoples`, `shadow`); the portion of `k` contains the steps `25k`–`25k+24`. |
| `history/free-peoples/NNNN.json`, `history/shadow/NNNN.json` | `ActionLogPage` pages (no cursors): to 500 log steps in order of events and summaries of them action Each action once in the entire file. |
| `battles.json` | `ReplayBattleReport[]`: battle report and revision, where it was full. |
| `catalog/base-cards.json`, `catalog/base-card-text.json`, `catalog/base-characters.json` | Descriptions of cards and characters from the time of the party, to read offline. |

<a id="klatka-i-historia-w-kroku"></a>
## The frame and story in step

- The frame contains views of both sides of `Projection.Observe`: hand and site knowledge, no
  options, decision tokens, preferences and availability action. `History` frame is empty.
- History in step `n` is the steps of the log of a given page with `revision` no more than a
  revision of the `n` frame. `PlayerView.History` is played with the last 50 such steps (`revision`,
  `kind`, `text`).
- The battle report is shown in step, whose revision is not less than `ReplayBattleReport.revision`.
  Earlier, battle only describes the view in the frame (`battle`, `lastBattleRound`), so there are
  no future results.

## Manifest

- Identity: `format` = `wotr-replay`, `formatVersion` = 1, `replayId` (= `matchId`), `matchId`,
  `source` = `wotr-server`. No host address.
- Party: `name`, `startedAt` and `finishedAt` (first and last frame), `exportedAt`, `players` (page
  and account name), `configuration`, `winner`, `victory`.
- Size: `turns`, `steps`, `frameChunks`, `freePeoplesHistoryChunks`, `shadowHistoryChunks`.
- `complete`: recording from step 0 to end of the batch. Export only gives full recordings.
- `files`: any other entry with size (bytes) and SHA-256 (hex, lower letters). The reader checks
  them before using the entry.

<a id="czego-plik-nie-zawiera"></a>
## What does not contain

Full `MatchState`, deck order, draw status, commands, retrieve, decision tokens, session, passwords, identifier accounts and automatic decision preferences. The `ReplayExportTests` test checks the lack of session tokens, identifier accounts, `passwordHash` and `decks`.

<a id="limity-v1"></a>
## Limits v1

| Limit | Value |
| --- | --- |
| Archive Size | 512 MB |
| Size of one entry after unpacking | 32 MB |
| Number of entries | 4096 |
| Number of steps | 50 000 |
| Chests in serving | 25 |
| Log Steps in serving | 500 |

The reader rejects the file above the limit before unpacking it.

<a id="wersje"></a>
## Versions

- `formatVersion` larger than supported: readable error "newer replay format", without partial
  reading.
- New fields in JSON within v1 are acceptable and ignored by an older reader. Rename entries,
  portions or field meanings requires v2.

<a id="pobieranie"></a>
## Download

| Route | Who | Response |
| --- | --- | --- |
| `GET /api/matches/{id}/replay` | Party player | 200 `application/zip`, name `<name>-<8 id characters>.wotrreplay` |
| `GET /api/observe/{id}/replay` | viewer with valid lease | As above |

- Alien, expired viewer or removed party: 404.
- Party under way: 409 `MatchNotFinished`.
- Party without recording (prefaced REPLAY-01): 409 `ReplayUnavailable`.
- The file can be downloaded multiple times as long as the batch exists.
- The server saves the archive with portions to the temporary file that disappears after closing the
  reply or interrupting the request.
