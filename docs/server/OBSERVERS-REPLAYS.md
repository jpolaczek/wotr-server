<a id="obserwatorzy-i-lokalne-replaye--projekt-z-2026-10-16"></a>
# Observers and local replays

This document specifies observers and local replay behavior (R33/R34). The server
and client implementation and acceptance tasks have completed; remaining work is
listed in the active registry. The implementation details below
remain the product contract. Previous task evidence is available in Git history.

State of implementation (2026-10-16):
- OBS-01 (rolls, limit 2+16, leases) runs on the server; description of routes in
  [T08-LOBBY](T08-LOBBY.md);
- OBS-02 (view viewer readings `/api/observe/{id}`) runs on the server; description in
  [T07-TRANSACTIONS](T07-TRANSACTIONS.md).
- REPLAY-01 (recording steps in `match_frames`) runs on the server for the batches created after the
  013 migration; description in [T07-TRANSACTIONS](T07-TRANSACTIONS.md).
- REPLAY-02 (the `.wotrreplay` v1 format and download at the end of the batch) runs on the server;
  the specification in [REPLAY-FORMAT](REPLAY-FORMAT.md).
- OBS-04 (list from Play/Observe, Join by code as viewer, waiting room with seats and audience)
  works in the client.
- OBS-03 and OBS-05 (board viewer with the "Observing" bar, page switch, information only clicks and
  live session with presence) operate in the client; description in the votr-client
  `docs/ui/OBSERVER_UX.md`.
- OBS-06: acceptance matrix OB01–OB09 with full room measurement 2+16
  (OBS-06).
- REPLAY-03: local library in the client (validation v1, atomic record, duplicates, collisions,
  export, removal).
- REPLAY-04 and REPLAY-05: "Save play" at player and viewer, "Replays" in menu and before logging in
  and player with steps, turn, pace and perspective.
- REPLAY-06: effects of the viewer and when playing, silence on jumps, page change and reconnect.
- REPLAY-07: acceptance whole (full batch by HTTP with viewer joining in half, file in second
  offline installation) — REPLAY-07.



<a id="źródło-i-ustalenia"></a>
## Source and findings

User: “Every room hosted can welcome two players and to 16 observers”, the observer switches the views of the players and sees them card, does not affect the game, but reviews the tabs and descriptions. After the game is finished, there is to be a “write play” in the “replays” menu with the library and automatic or manual playback both ways.

User answers during planning:

- Player change ↔ observer: **only before start**.
- Storage: **locally on your computer**.
- Navigation: **action and the whole turn**.
- Record: **players and observers**.
- File transfer: **import and export in the first version**.
- Auto: **no time to think**, with a pace adjustment.

We implement base game itself, also recording completed demo parts. We do not extend the rules or AI. Development scripts are not a separate public replay library. Code/UI in English, documentation in Polish.

<a id="pokój-i-role"></a>
## Peace and Roles

The limit is two player accounts and sixteen observer accounts per room. One participant has one role; the other one window/ed The conflict page remains bivalent: Free Peoples / Shadow. The role of an observer is not the third party.

In Waiting, the player can only go to the observers at a free place; the observer selects the free seat Free Peoples or Shadow. The occupied page does not take over, and the full audience does not allow to leave the player's seat by changing the role. The entire operation is atomic: the error leaves the previous role. The change of cast invalidates Ready of both sides. The entry/exit of the observer does not change configuration or readiness. Start requires two ready players.

The host is a separate administrative function. After moving to the audience, he still manages the waiting room and starts the game; a room with the host-observer and two empty player seats is allowed. Removing the participant before the start indicates the account, also the observer; he keeps protection against the immediate reentry of the deleted person. The public way of the host from the waiting room continues to close it. After the start, the player roles are blocked and the observer does not get game management.

Joining as an observer works in Waiting and during the party. The public list shows the status, the space volume and the corresponding Play/Observe. Private room still requires code. The code allows you to watch also after taking the player's seats and after the start; it does not go to the public DTO. After the end the party disappears from the list of open games and does not accept new observers. The current players can watch the score and download the replay before leaving; players also after reopening the completed batch with My games.

The default connection lease: Renewing every 15 s, Expiring after 60 s without renewal, the server clock. Returning before expiration is idmpotent. Closing one of the several connections of this account does not remove the others. Once the return expires requires a re-acceptation and can end with a lack of seats. The expiry of the observer does not receive or give the player a place. The room cleaning mechanism takes into account active observers. The host identity does not disappear with the termination of the connection.

Chat belongs to the waiting room's participants, with the name and role of the author instead of a mandatory page; since CHAT-01 it continues on the board during the game (see below).

<a id="widok-tylko-do-oglądania"></a>
## View only for viewing

The common presentation of board has Play/Observe/Replay modes and a data source separated from the command transport. Default perspective is Free Peoples; Viewing switch: Free Peoples / Shadow updates hand, board journal and reports for the same gameplay position. It retains camera and open bookmarks, and outdated previews refresh or close. Site names replace confusing You/Opponent and personal victory messages.

Map click, preview kart/characters/dice, bookmarks, pan/zoom, history, reports, Settings and output remain active. Game changers are inactive; we do not disable the entire UI tree. Do not create command, preview action, sketches, automatic decisions, confirmations or tags to read player notifications. The decision details may describe who is running and what is being settled, but do not contain feasible options and command tokens.

The server authorizes the role regardless of the page viewed on the reading, history, reports and WebSocket. The parameter perspective does not give the player another hand. The authorized observer gets the knowledge of the selected page, including its hand, not the full MatchState, the future deck order or RNG. The existing endpoints of commands/preview/receipts remain available only to the players. The observer does not remove the party and does not run the demo as a viewer.

Live stream uses notifications and emergency query. The first opening and reconnect take on the current state without catching up on animation. The generation number of the perspective protects against late responses, also when both sides have the same revision. When the party is lost, the party is removed or the session expires, the client ends up observing the easy-to-read status; loaded previews do not give further access to the server.

<a id="rejestrowanie-i-plik-replaya"></a>
## Recording and play file

The current matches.state, match_events and retrieves do not store all historical states. Recording starts when creating a new game: the initial state and one state after each approved decision changing the course, together with its automatic consequences. Also applies to battle, Hunt, the effect of card and the command of CPU. Recording the state, history, retrieve and new step is one transaction. Retry fails to step, reject and change private preference itself also does not.

Step is the accepted decision with consequences, not every animation frame or every log line. The index includes order, revision, turn and phase; the turn index indicates the first saved state of the given full turn game. The initial step is before the first command player, even if the normal start of the server has already settled the automatic start action of the turn.

We record the constant, read projections of both sides and incremental history/report data from their creation position. Without command options, private preferences and multiple copy of the entire story to each frame. Export does not re-execute rules or draws; later engine change does not count the old batch. History and report in the player are created only from entries to the selected step, without future results.

`.wotrreplay` is a printed ZIP containing a manifest, index, portions JSON states and incremental history/raports and a catalog of descriptions cards and characters from the time of the batch. Manifesto identifies the source/match, format and content version, name, players, pages, dates, score, number of tur/kroks and completeness. Graphics use local resources and stable IDs; no illustration has a clear substitute, does not block text and playback. Format does not contain passwords, session, private host addresses, RNGs and future decks. REPLAY-02 sets the specification v1 and its limits before writing the reader.

Downloading only after the game's authoritative end: both players or the current viewer of the completed batch. There is no public library on the server. The register will survive the reboot and late addition of the observer; it is removed with the party, also when replacing the demo. The local saved file remains independent. The parties started before the recording is enabled have an explicitly unavailable replay, without extrapolating missing states.

<a id="biblioteka-nawigacja-i-efekty"></a>
## Library, navigation and effects

Save play on the ending screen has states ready/download/saved/b The file goes atomic to the local Replays directory in the application data, separate from match-recovery. The library is shared for the local installation, does not require an account and is not synchronized with the server. Replays opens the tab of the existing menu; access is also from the screen before logging in, without blocking the server or logging in. List reads metadata rather than the entire gameplay; the latest party dates are the first.

The line shows the name, pages and players, the date, result and number of turn. Play, Import, Export and Delete work by clicking; the file/places export selection is available on Windows/macOS. Import copies the file to the library, export creates a copy. The save/import re-file does not multiply rows; the ID collision with other content does not overwrite the file. The damaged, incomplete, too large or unsupported file has a clear error. We do not unpack any archive paths to the application folders. Disk error or aborted download does not create the correct entry of the library.

The player opens the start in pause. It has Play/Pause, Previous/Next action, Previous/Next turn, running axis, current turn/phase and page switch. Manually scrolls. Previous/Next turn goes to start strictly previous/nast dark turn; at the control limits it is inactive. The perspective change does not change position or resume pause itself. Subsequent readings cancel older requests; the undoing accepts lower revision.

Auto skips the thought: Slow / Normal / Fast rate means 2 / 1 / 0,25 break after animation is finished, default Normal. Focus loss pauses. The last step stops playing and shows the score without multiple open fullscreen endings when scrolling through the final. The status is loaded with limited cache portions, without the entire game in RAM.

Live and auto can play the effects of the course, but not alert the required decision or personal Victory/Defeat. Manual jump, undo, page change, first view and reconconnect are without gameplay effects; the usual gestures of preview still use the settings UI. The new jump cancels the pending sequences. The common move Fellowship and the change Corruption retain both sounds and two animations consecutively: Journey boots → metal, Mordor metal → actual step. Auto awaits at the end of both stages; Reduced motion and Mute do not block progress.

<a id="zadania-i-scenariusze-odbioru"></a>
## Tasks and scenarios acceptance

Each row has the main owner. acceptance all REPLAY-07 checks all the following scenarios after the part is completed. OBS-06 receives functionality/observation protocol; common effects receive REPLAY-06.

| Scenario | Case | Owner |
| --- | --- | --- |
| OB01 | 2 players + 16 viewers; simultaneous requests for last place, no duplicate account. | OBS-01 |
| OB02 | Change role/pages only before start, full audience, host as viewer, Ready and rollback. | OBS-01 |
| OB03 | Public Waiting/Started and private code, Play/Observe, chat and removal of the participant. | OBS-04 |
| OB04 | Only read the server side; the player does not gain someone else's hand by the perspective parameter. | OBS-02 |
| OB05 | Map, card and tabs active; zero commands, preview, draft, automatic decisions and player-seen. | OBS-03 |
| OB06 | Switching both perspectives, history and reports on the same revision; answers in reverse order. | OBS-05 |
| OB07 | 15/60 s lease, multiple account connections, outright exit, site recovery and expiration. | OBS-01 |
| OB08 | WebSocket/polling, late entry, reboot, session loss, batch removal, silent catch-up. | OBS-05 |
| OB09 | Full run for two players and 16 viewers; load control and regular parties. | OBS-06 |
| RP01 | Initial status and any decision including CPU, battle and Hunt; both hands in time. | REPLAY-01 |
| RP02 | Rollback, retry, reboot, no steps for rejections and preferences themselves; old game without a replay. | REPLAY-01 |
| RP03 | Versified complete file, offline descriptive data, no technical secrets and no entire deck. | REPLAY-02 |
| RP04 | Download only after completion for player/observer; denial to foreign and during the game; retention. | REPLAY-02 |
| RP05 | Atomic recording, import/exports, duplicate/collision, damage, limit, interrupt and no space. | REPLAY-03 |
| RP06 | Save play for player and viewer, Retry, metadata list and local removal. | REPLAY-04 |
| RP07 | Library and playback before logging in and at the server's inaccessible. | REPLAY-04 |
| RP08 | Steps/turn in both directions, boundaries, both perspectives, hand/reports without future results. | REPLAY-05 |
| RP09 | No reflection, three paces, pause, focus, end; no lock on Reduced motion/mute. | REPLAY-05 |
| RP10 | Live/auto: the effects of facts; manual jump/reconnect/change pages: no events. | REPLAY-06 |
| RP11 | Both tracks: two animations and both sounds consecutively; cancellation after jumping and no player alerts. | REPLAY-06 |
| RP12 | Full game → file → import on second install → offline playing; demo after replacing. | REPLAY-07 |
| RP13 | Long game: portioning, limited cache, fast subsequent jumps and no late readings. | REPLAY-05 |

When implementing: server/PostgreSQL tests, full EditMode and targeted PlayMode according to WORKFLOW. Without mandatory dumps and local builds. Do not declare checking Windows runtime without the environment available. Backend/migration and the uploaded packages precede a consistent client; implementation and publication require a separate order. The players' current parties and roles remain clear and the lack of historical frames is clear.

<a id="wdrożenie-i-obsługa-replay-07-2026-10-16"></a>
## implementation and support (REPLAY-07, 2026-10-16)

<a id="kolejność-i-zgodne-wersje"></a>
### Order and compatible versions

1. First the server with migration in order (production already has `011_account_email.sql` with
   AUTH-02):
   - `012_room_observers.sql` — viewers and rents, lifting the player's seat requirement, new public
     list index;
   - `013_match_frames.sql` — `matches.recorded` and steps recordings.

Migrations run the server at startup (`Database.Migrate`). Parties before 013 do not have a recording and give 409 `ReplayUnavailable`.
2. The server commit dependencies packages (`scripts/package_deps.py`): Contracts **0.9.0** and
   public content **1.0.10****; the client testback `d1d645354168`.
3. Then the client with these packages. The client before OBS-04 connects to the new server (the
   fields are additional), but there is no Play/Observe, board viewer, "Save replay" or library.
4. implementation, publication and push require separate order; this task does not perform them.

<a id="wdrożenie-produkcyjne-2026-10-16"></a>
### implementation Production (2026-10-16)

On request of the user, after transferring the history and publication of packages (Contracts 0.9.0, public content 1.0.10 and test backend `d1d645354168` in GitHub Releases):

- `/srv/wotr/backups/2026-10-16-before-obs-replays` copies (recreated to `wotr_restore_20261016obs`:
  1–11 migrations, 3 account, 1 party, like production) and `…-before-obs-replays-2` just before
  switching;
- checkout `654b86e9a172` (server code the same as the tested `975d41a4`),
  `WOTR_VERSION=654b86e9a172`, image built; previous image `wotr-server:303744822513` remains until
  possible withdrawal;
- the migration attempt on the copy did not move through the fixed service address (return in
  `infra/README.md`) and was omitted; the migrations of 012 and 013 were performed at server
  startup: the `wotr` database has the migrations of 1–13, all services healthy;
- via Cloudflare: `/healthz` 200 , `/account/register` 200 ; with client version headers
  `/api/observe/{id}` , `/actions` , `/replay` , `/api/matches/{id}/replay` , `/api/rooms/public`
  and POST `/api/rooms/{id}/presence` and `/role` respond 401 (requires login) and unknown route 404
  .

The `wotr_restore_20261016obs` database stays to acceptance; it can be removed later. The installed client builds do not have Play/Observe or the replay library; this requires a new client release (a separate order).

<a id="instrukcja-obsługi"></a>
### User Manual

- Watching the game:
  - in Open games, select “Observe” for the game;
  - either enter the code in “Join by code” and click “Observe” also when the seats are taken or the
    game is in progress.
- In the waiting room, the viewer can take a free seat (Take Seat) and the player goes to the
  audience (Observe). Host removes the viewer with a cross at his name.
- On board, the “Observing” bar switches Free Peoples/Shadow. Clicks only show (regions, card, Log,
  reports); the viewer changes nothing. “Exit match” slows down the audience seat.
- Replay: click “Save play” on the end screen. They can do this to the player and the viewer present
  at the end.
- The "Replays" on the menu or on the login screen (without an account and server) has for each
  write "Play", "Export" and "Delete" and at the bottom of the "Import" file `.wotrreplay`.
- Player:
  - ‘ ⁇ Turn’ and ‘Turn ⁇ ’ pass through the whole turn, ‘ ⁇ Step’ and ‘Step ▶’ by one step;
  - ‘Play’/‘Pause’, step axis and pace of Slow/Normal/Fast;
  - the page switch shows the hand and knowledge of the selected page.
- The files are in `<persistentDataPath>/Replays` (MACOS: `~/Library/Application
  Support/<firma>/<produkt>/Replays`), together for all accounts on this computer.

## Chat on the board — CHAT-01 (2026-10-17)

The room's chat (MENU-24) continues on the board: `GET/POST /api/matches/{id}/chat` resolve the room that started the
game and answer its participants (players and observers) under the rooms' request budget; anyone else, or an
unknown game, gets 404. What a reader sees depends on the reader's role, in the waiting room and on the board alike:
an observer reads every message, a player reads only the players' messages, so the audience never talks into the
game. Messages stay in the server's memory only (the latest 50 per room, 1–200 characters of plain text, no history
after a restart). Tests: `RoomChatTests.ObserversReadEveryoneAndPlayersOnlyThePlayersAlsoThroughTheGame`.

## A player never watches their own game — OBS-07 (2026-10-18)

The owner's requests: "a game we take part in should be closed to us for observation; one cannot be an observer and a
player in one game", and "make sure finished and deleted games disappear from Open games". The open public list
(`RoomStore.ListPublic`) listed every public table of another host, so a guest who joined someone's table saw that
table among Open games with Observe. It now leaves out any table the viewer sits at as a player (they find it in My
games), lists a started table only while its match exists and is not finished, and keeps leaving out closed tables.
Asking to watch one's own table by code or from the list already keeps the seat without a place in the audience
(`Admit`), and the observer endpoints answer players with 404 (OBS-02). A removed match deletes its room
(`MatchRemoval`), so it leaves the list with it. Test: `RoomObserverTests.APlayerNeverSeesTheirOwnTableAmongOpenGamesNorJoinsItsAudience`.
