<a id="t08--konta-i-prywatne-pokoje"></a>
# T08 — Accounts and Private Rooms

`Authentication` is responsible for passwords and sessions, `Rooms` for room transaction and acceptance, `Domain/Configuration` for variant dependencies. `Endpoints` translates HTTP for operations; it does not contain game rules. Start creates snapshot and assigns it to a room in one PostgreSQL transaction. Blockage applies to a single room.

<a id="konta"></a>
## Accounts

From AUTH-02 ( [D16](../DECISIONS.md) ) account is set up on the game side `/account/register` Name ASCII 3–32 characters (visible to the opponent), email address and password 12–128 characters. Invitations to accounts do not exist (table `invitations` is not used to enable the older image of the server to work on this basis. Registration creates an account with an unconfirmed address, confirmation link and session in one transaction; the account works immediately. Name and address are unique without case distinction. Login takes the name or address. Hashing by ASP . NET Identity PasswordHasher ed 600 000 Iteration; update the older hash after correct login. The incorrect login has one code; registration distinguishes the wrong format, the busy name and the occupied address. Login: 20 requests/minute/address IP, and 10 failed sign-ins of one name within 15 minutes lock that name for 15 minutes (`429 SignInLocked`, SEC-02, [T37](T37-HARDENING.md#sign-in-lock)); registration and request for reset: 5 au 10 minutes to address IP , without queue. Before using reverse proxy, you should consciously configure trusted forwarded headers (T38).

Client from AUTH-03 only log in: the “User or email” field and password. The “Create account” and “Forgot password?” buttons open in the browser `<adres serwera>/account/register` and `/account/forgot` Tests PlayMode log in with the seeder accounts ( `AccountA` , `AccountB` ) or establish accounts by API with address in domain `example.test` ; `scripts/test-client.sh` client sets `Wotr__Email__PermitsPerAddress=10000` Because the registration counts for the mail limit.

Address confirmation and password reset: one-time links ( 256 bits, in the database only SHA-256 ) on the pages `/account/verify` and `/account/reset` , token in the address fragment. Confirmed link is valid 7 days, reset link 60 the new reset link cancels the older one. Reset goes only to confirmed address; an account with unconfirmed address gets a confirm link again. The answer to the reset request does not reveal whether the account exists. The new password cancels all account sessions. To the account at most one message of the given type on 2 minutes (confirmation link does not stop reset) and 10 daily, for all 90 daily. The dispatch is supported by the Resend ( infra/README.md Account before AUTH-02 gets the address by the operator’s command `--set-email <name> <adres>` .

Session: Separate 256 -bit random tokens, validity 30 days, the server saves only SHA-256 Token. `Authorization: Bearer …` Never parameter URL Logout cancels a specific session; does not log off other devices. WSS checks the validity of the lowest of what 5 s; HTTP On every demand, forced HTTPS beyond Development. The administration panel can be developed later.

| Method and path | Entry → result |
| --- | --- |
| POST `/api/auth/register` | username, email, password → SessionView; 400 `InvalidRegistration`, 409 `UsernameTaken`/`EmailTaken` (name and address each unique, case-insensitively) |
| POST `/api/auth/login` | username (the account name only, any case; AUTH-05), password → SessionView; 401 otherwise; 429 `SignInLocked` with Retry-After while the name is locked (SEC-02) |
| POST `/api/auth/logout` | active session → 204 |
| POST `/api/account/verify-email` | token → 204; 400 `InvalidToken` |
| POST `/api/account/password-reset` | login (name or email) → always 202 |
| POST `/api/account/password-reset/confirm` | token, password → 204; 400 `InvalidToken`/`InvalidPassword` |
| GET/POST `/api/rooms` | list of your own rooms / creation with a choice of sides |
| POST `/api/rooms/join` | random code invitation to room → free seat |
| GET `/api/rooms/{id}` | members only → RoomView |
| PUT `/api/rooms/{id}/configuration` | ExpectedVersion, configuration → new version, both acceptances removed |
| POST `/api/rooms/{id}/confirm` | ExpectedVersion → confirmation of your own website |
| POST `/api/rooms/{id}/start` | ExpectedVersion → one permanent matchId |

The room code is 256-bit and visible to its members to hand over to the other player. The identity from the session; payoad does not select the account. The same account does not occupy both sides. Foreign ID gives 404, the third account with a full room code gets `RoomFull` without view. The renewed join and start are idmpotent. No change to pages/lowering room in this area.

<a id="format-wejścia-i-komunikaty-auth-01"></a>
## Input format and messages AUTH-01

`Wotr.Contracts.AccountInput` shares the format limitations with the server and Unity form. The name only allows ASCII, digits and `._-`; code has 43 characters base64url. Password retains all characters including initial/final spaces. The form only trims external white names and invitations. Password requirements remain 12–128 characters.

Invalid format stops approval locally, indicates the field and sets focus. An empty invitation in registration mode does not send login. The server does not rely on this validation; it still checks each request itself. The incorrect password, busy name and the wrong/used/outline invitation retain a common 401 `InvalidCredentialsOrInvitation` refusal. The client explains it in accordance with form mode, without requesting which account exists.

<a id="konfiguracja-i-granice"></a>
## Configuration and boundaries

The default Dismay is calculated only when the value is omitted; the apparent false remains false. Council requires Lords; Dismay and Renovating Ruler Tokens require Lords + Kings (Removing also Dismay); two variants of tiles require Kings; New Cities requires Fate. Sources: REQUIREMENTS and the L03/X02/F01. Test rule checks all 512 settings: 93 legal, 419 rejected. They do not contain private versions of Companion.

The acceptance T08 only had a base module available for launch, and expansions had `StartUnavailableReason=ContentNotImplemented`. T34 opens all 93 legal configurations after acceptance engine and client windows. The dependencies are validated, two consents of one version, atomic start and invariability of configuration of the started batch; [report T34](https://github.com/jpolaczek/wotr-client/blob/40f84df7c2f006983895c7b56826fdecc224865b/docs/ui/T34-ACCEPTANCE.md).

From UI-96 `ReleaseScope` (`Wotr:Release:Expansions`, default excluded in Production) limits new tables to base game: configuration with module or variant expansions and start of the table preset give 409 `ExpansionsUnavailable`; `RoomView.ExpansionsAvailable` tells the client whether to show switches. Started parties and other environments unchanged. EXP-01 releases the expansions one module at a time ([below](#release-per-module--exp-01-2026-10-19)).

<a id="weryfikacja"></a>
## Verification

`bash scripts/test.sh`: actual PostgreSQL 17, 31 tests, without omission. Includes two registrations with one invitation, the termination of invitation and session, logout, HTTP with a real session, no invitation permission for administrator, race for second place, start-up race, reset acceptance, old confirmation, other people's room and matchId, illegal configuration and lockout of unimplemented expansions. The previous tests commands/WSS performed again after the split of methods and optimization of the read-over.

The matrix test detected an erroneous manual expectation of the number of legal combinations (90); an independent conversion gives `(1+4+2+24) × 3 = 93`. The rules have not been changed to match the result.

<a id="menu-02--samodzielna-rejestracja"></a>
## MENU-02 — self-registration

Historical ( AUTH-02 removes invitations to accounts and requires an email address: no Invitation in register meant to register an ordinary account open. Account and session are created in one transaction; registration instantly log on to menu. validation/wa It's a high-performance, and it's nuclear-powered, and we don't replace the incorrect code with an open registration. 600000 Iteration, name normalization and request limits remain. Messages of the new menu do not suggest a mandatory invitation to the account.

<a id="wynik-menu-03--2026-10-11"></a>
## Result MENU-03 — 2026-10-11

`./scripts/test-menu.sh --no-restore --filter 'FullyQualifiedName~PublicRoomTests|FullyQualifiedName~MenuMigrationTests|FullyQualifiedName~LobbyTests' --logger 'trx;LogFileName=menu03-final.trx' --results-directory .artifacts/results` : **12/12 PASS , 0 skip**. Migration 006 preserves old private rooms, places/codes/Versions , designates a historical host and perpetuates the name in the party header. The public list has a separate indexed projection and pagination ID ; does not serialize RoomView Parallel joint allows one guest, retry preserves the version/place. The first attempt did not compile fixture with ambiguous new(); the second 10/12 revealed no enum converter in fixture HTTP Fixed open type and use BoardCatalog .Json; runtime of the protocol did not require a change.

<a id="wynik-menu-04--2026-10-11"></a>
## Result MENU-04 — 2026-10-11

`./scripts/test-menu.sh --no-restore --filter 'FullyQualifiedName~MenuLifecycleTests|FullyQualifiedName~PublicRoomTests|FullyQualifiedName~LobbyTests' --logger 'trx;LogFileName=menu04.trx' --results-directory .artifacts/results`: **14/14 PASS, 0 skip**. One transaction and room lock protect zaproszenia/Ready/leave/start. New rooms accept only the base also in Development (lifted by EXP-01: named rooms take the released modules); host-only start, unready, code invalidation, idmpotent leave and rebooted readings received. Eight races host/guest leave with start have retained all participants of each started batch. Old tables without name retain common configuration/start.

<a id="menu-09--lokalny-serwer-i-kody-publicznych-stolików"></a>
## MENU-09 — local server and public table codes

Notification 2026-10-12 revealed that the active local image `ui83-7df29a1` did not contain API MENU-02 – 07 The new client itself was not enough: the old Create ignored the name/visibility, and `/leave` and `/invitation` returned 404 Local server received `menu09-fix` image `9cbcd02471d6` After the copy, re-create to the new database and check migration 006 All previous fields have been retained 9 accounts, 52 session, 5 rooms, 4 lot, 96 Receipts, 212 events and 2 read position. Old rooms remain Private according to migration; public visibility is selected when creating a new table.

Host can now publicly share the code also with the public free table. The code still only sees host, not public list or guest. Rules full of room, invalidations after leave and no after-start entry change. Back supports 404 as a return from the inaccessible screen; it does not declare release of the location that the server has not confirmed. With other errors it allows you to repeat.

`MENU-09` saves commands and reports: server tests **18/18**, frontend **7/7**, regression recovery/Settings **11/11**, without skips. An attempt of two new accounts on the restored copy includes a public list, invitation and leave; active HTTPS/readiness checked with local certificate CA. No commands were executed in actual batches. Without external publication and without database reset.

<a id="usuwanie-gracza-i-czat-poczekalni--menu-24-2026-10-15"></a>
## Remove player and chat waiting room — MENU-24 (2026-10-15)

User order with 2026-10-15 (citation and layout: wotr-client:docs/ui/WAITING_ROOM_UX.md).

- `POST /api/rooms/{id}/remove` (host). The guest loses place, the readiness of both sides zeros,
  the room version grows, the invitation code is new and disabled (as after leave). The account goes
  to the `Removed` list: reconnecting the code or from the public list gives 409 `RemovedFromRoom`.
  - Errors: 404 for a foreign or closed room; 409 for `HostRequired`, `MatchAlreadyStarted` or
    `SeatEmpty`.
- `GET /api/rooms/{id}` returns 410 `RemovedFromRoom` for the deleted account, so the client shows
  the reason instead of the general “not available”.
- Chat (`RoomChat`, process memory, no database):
  - `POST /api/rooms/{id}/chat` with `RoomChatRequest { Text }`. The text after the space has 1–200
    characters, without control characters; otherwise 400 `InvalidMessage`. Only an open room member
    (404) can write. Answer: `RoomChatView`;
  - `GET /api/rooms/{id}/chat?after=N` returns `RoomChatView { Messages, Latest }`. Message
    (`RoomChatMessage`) is `Sequence`, `Author` (account name), `Side` and `Text`. When `after` is
    greater than `Latest` (after server reboot), the client gets everything that is;
  - at most 50 messages per room; rooms without messages by 2 h are deleted on the next save.
    Restart the server cleans the chat;
  - limit of requests as for rooms (`GameRateLimits.Rooms`).
- Contracts 0.6.0: `RoomChatMessage`, `RoomChatView`, `RoomChatRequest`. Public content 1.0.8
  requires this version.

<a id="puste-nierozpoczęte-poczekalnie-znikają--menu-27-2026-10-15"></a>
## Empty, uninitiated waiting rooms disappear — MENU-27 (2026-10-15)

User decision with 2026-10-15: games that have not started are not saved and an empty waiting room is removed.
- Each room reading by a member and every room change saves `rooms.seen_at`. The waiting room reads
  the room as 2 s, so it is a presence signal.
- `EmptyRoomSweeper` what 10 s removes rooms without a party that nobody has used since 30 s,
  together with their chat in memory. This includes the room from which everyone came out (also for
  demo), and the room closed by the host. Previously closed rooms remained in the base with the
  `closed` flag.
- The first review is only performed with 30 s after the process starts. Rooms with the started
  batch (`match_id`) are not moved.
- Configuration: `Wotr:Rooms:EmptySeconds`, `Wotr:Rooms:SweepSeconds`.

<a id="wyjście-hosta-z-pustego-pokoju--menu-32-2026-10-16"></a>
## Host exit from an empty room — MENU-32 (2026-10-16)

The user's request from 2026-10-16: exit from an empty, unopened room removes this room. `RoomStore.Leave` host in a room without a guest and without `match_id` closes the room in the transaction and then removes his poem (`DELETE FROM rooms WHERE id=$1 AND match_id IS NULL`). `POST /api/rooms/{id}/leave` forgets the chat of the room closed or removed by the host. When the guest is in the room, the host only closes it (MENU-23). Abandoned rooms still deletes `EmptyRoomSweeper`. Test: `EmptyRoomSweeperTests.TheHostLeavingAnEmptyRoomDeletesItAtOnce`.

<a id="obserwatorzy-pokoju--obs-01-2026-10-16"></a>
## Room Observers — OBS-01 (2026-10-16)

User order with 2026-10-16: "implement replay and watcherow" (project: [OBSERVERS-REPLAYS](OBSERVERS-REPLAYS.md)).

- The room has two player seats and to 16 observers. Viewers are in the `room_observers` table (012
  migration), not in `RoomState`: `room_id`, `account_id`, `username`, `joined_at`, `lease_until`.
  The key `(room_id, account_id)` makes several windows of one account take one seat. Removing the
  room removes its viewers (`ON DELETE CASCADE`).
- Each connection, role change, output and removal of the participant blocks the room line (`FOR
  UPDATE`) and runs through `LockedRoom`: first removes expired leases, then counts viewers.
  Therefore, simultaneous entries do not exceed 2+16. Refuse withdraws the entire transaction.
- The 012 migration removes the limit of “at least one player”: the host can go to the audience and
  leave both spaces empty. The old rooms work unchanged because `RoomState` did not get new fields.
- The viewer's lease: 60 s on the base clock (`Wotr:Rooms:ObserverLeaseSeconds`). It is renewed by
  every room reading (`GET /api/rooms/{id}`, chat) and `POST /api/rooms/{id}/presence` (client
  every 15 s). The expired lease gives 404; the return requires re-connection and can hit the full
  audience. The viewer's public `leave` slows the seat immediately.
- The viewer's connection:
  - code (`POST /api/rooms/join` from `Role = Observer`) or from the public list (`POST
    /api/rooms/{id}/join?role=Observer`);
  - works in Waiting and during the party, also at the occupied player's seats. After the end of the
    game, new viewers get 409 `MatchFinished` and are present;
  - full audience gives 409 `ObserversFull`. Renewing by the current contestant changes nothing and
    the player remains a player;
  - the viewer's entry and exit does not change the version of the room or the readiness.
- `POST /api/rooms/{id}/role` with `RoomRoleRequest { Role, Side }` only works before start (409
  `RolesLocked`):
  - player → spectators at a free place in the audience (`ObserversFull`);
  - spectator or second player → free seat `Side` (`SeatTaken`);
  - any change of cast will zero the readiness of both parties and raise the version; the invitation
    code shall remain.
- Host remains the host in the audience: configures, shows the code and starts the game. Start
  requires two ready-to-use players. `Ready`/`Confirm` are only for players (409 `PlayerRequired`),
  also demo from the waiting room.
- `POST /api/rooms/{id}/remove` with optional
  `RemoveParticipantRequest { Name }`:
  - an empty name removes the second player as in MENU-24;
  - the name indicates the player or viewer. Unknown name or host itself is 409
    `ParticipantNotFound`;
  - deleted account goes to `Removed`, so it gets 410 at reading and 409 `RemovedFromRoom` at
    return.
- Output:
  - the viewer is slowing down the seat;
  - the host closes the room before the start, including from the audience. A room where there is no
    one but him is immediately removed;
  - host-visitors after takeoff just stop watching.
- The invitation code of the host can always be enabled, including after filling the seats and after
  the start, because it allows viewers in. The viewer does not see the room code with the name.
- `GET /api/rooms` returns open rooms where the account is playing or watching.
- The public list shows open rooms of other hosts in Waiting and in an ongoing party (without
  finished) with status, number of players and viewers and `CanPlay`/`CanObserve`. The code does not
  enter the list.
- The chat includes viewers. The message has `Author` (account name) and `Role`; `Side` is of
  relevance only to the player.
- `EmptyRoomSweeper` does not remove the open waiting room that the viewer sees with valid lease.
  The closed room removes so far.

## Changing the account name — AUTH-06 (2026-10-18)

AUTH-07 (2026-10-18, the owner's request): Change name is removed from the client's code; the released 0.9.0-rc.9
and rc.10 still show it, later releases will not. The endpoint below and the operator's `--rename` stay on the server.

A signed-in player changes their account name with `POST /api/account/name` and `{ "username": <new name>,
"password": <current password> }` (game session, the login budget). The answer is the session's account with its new
name (`SessionView.Username`; the token stays valid), or 400 `InvalidName` / `WrongPassword` (never 401, so the client
stays signed in), or 409 `NameTaken`. The operator renames any account without a password:
`dotnet Wotr.Server.dll --rename <current name> <new name>` (infra/README.md).

- The new name follows the registration rules (`AccountInput`: 3–32 letters a–z, digits or `. _ -`, never an email
  address) and is unique in any case (AUTH-05); a change of case alone is allowed.
- A name on the administrators' list (`WOTR_ADMIN_ACCOUNTS`, ADMIN-01) is never given to another account, by rename
  or by registration; it reads as taken, so the list is not revealed. Renaming an administrator therefore needs the
  new name on the list (the owner's account: operator rename, then the list changed to the new name).
- One transaction renames the account and the copies of the name in rooms (`hostName`, `freePeoplesName`,
  `shadowName`, each room's version moves on) and in the audience (`room_observers`). Game lists, replays and the admin
  panel read the name from the account; chat lines keep the name they were written under. Sessions stay valid.
- Every rename is logged with the old and the new name.

Tests: `AccountRenameTests` (rename and its rooms, refusals, reserved names, operator).

## Lobby chat, players online and challenges — LOBBY-01 (2026-10-18)

Release status: the first client with the lobby, 0.9.0-rc.10, was withdrawn from the release catalog the same day at
the owner's request (`release_host.py withdraw --version 0.9.0-rc.10`, catalog revision 8); 0.9.0-rc.9 is the newest
offered again. The server endpoints below stay deployed and the client code stays for a later release.

The Open games screen polls `GET /api/lobby?after=<sequence>` every `LobbyLimits.PollSeconds` (3 s). Every endpoint
needs the game session (401 without it) and stands behind the client version gate (DIST-05). The poll uses the reads
budget; posts and challenge answers use the rooms budget. One poll marks the caller online and returns `LobbyView`:

- `messages`: the messages after `after`, oldest first. A first read (`after` ≤ 0) or a sequence the server never gave
  out gets none (LOBBY-03). `latest` is the newest sequence; the client passes it back as `after`.
- `online`: accounts that polled within `LobbyLimits.OnlineSeconds` (20 s), technical accounts (`demo-cpu`) never,
  sorted by name, with `you` on the caller's own line.
- `incoming`: the pending, unexpired challenges to the caller. `outgoing`: the caller's challenges of the last
  5 minutes with their status, so the outcome reaches the challenger.

| Endpoint | Answer | Refusals |
| --- | --- | --- |
| `POST /api/lobby/messages` `{text}` | 200 `LobbyMessage` | 400 `InvalidMessage` |
| `POST /api/lobby/challenges` `{name}` | 200 `ChallengeView` (`Pending`) | 404 `PlayerNotFound`; 409 `SelfChallenge`, `PlayerOffline`, `AlreadyChallenged`, `TooManyChallenges` |
| `POST /api/lobby/challenges/{id}/accept` | 200 `RoomView` | 404 `ChallengeNotFound`; 409 `ChallengeClosed`, `RoomUnavailable` |
| `POST /api/lobby/challenges/{id}/decline` | 204 | 404 `ChallengeNotFound`; 409 `ChallengeClosed` |
| `POST /api/lobby/challenges/{id}/cancel` | 204 | 404 `ChallengeNotFound`; 409 `ChallengeClosed` |

- Chat text follows the room chat (`RoomChat.Clean`): 1–200 characters after trimming, no control characters. A
  message keeps its author's name as written (AUTH-06).
- **The chat is relayed, never stored (LOBBY-03, 2026-10-18, the owner's decision).** A message waits in the server's
  memory only for `LobbyChat.RelayWindow` (1 minute) and at most `LobbyLimits.MessagesKept` (200), long enough for the
  menus polling every 3 s to fetch it; a restart forgets it, and nothing reaches the database or the logs. Sequences
  start from the clock at start-up, so they keep rising across restarts. Each client keeps the messages of its own
  sign-in (LOBBY-02). Migration `016_lobby_chat_relay.sql` drops `lobby_messages` with the messages stored before.
- A challenge names an online, non-technical player (any case). One pending challenge between two players at a time,
  either way (a lock on the pair keeps two simultaneous ones out), and at most 3 pending from one player. It waits
  `LobbyLimits.ChallengeSeconds` (120 s); past that it reads `Expired` and cannot be accepted.
- Only the challenged player accepts or declines, and only the challenger cancels; someone else's id, or an unknown one,
  is 404. A challenge that is no longer pending is 409 `ChallengeClosed`.
- Accepting claims the challenge in one update, so a second accept finds it closed. The server then sets a private
  table like any other: the challenger hosts it on the Free Peoples seat under the name `<challenger> vs <challenged>`
  (cut to 64 characters) and opens its invitation, and the challenged player takes the Shadow seat through the code.
  The challenge names its room only then: until that moment it still reads `Pending`, then `Accepted` with `roomId`,
  which tells the challenger which table to open. If the table cannot be set, it is removed (both leave), the challenge
  goes back to `Pending` and the answer is 409 `RoomUnavailable` (or 500 for a database fault).
- The table is an ordinary waiting room: both players open it within MENU-27's 30 seconds, as after a join by code;
  the sides can be swapped there before the start.
- Names in `online` and challenges come from the accounts, so a renamed player shows their new name at once.
- Configuration (tests): `Wotr:Lobby:OnlineSeconds`, `Wotr:Lobby:ChallengeSeconds`.

Persistence: migration `015_lobby.sql` (`lobby_presence`, `lobby_challenges`; an account's rows go with the account; its
`lobby_messages` was dropped by 016). Presence and challenges are in the database; the chat relay lives in the one
server process.

Tests: `LobbyChatTests` (presence and its expiry, chat text, the in-memory relay and first read, challenge refusals, acceptance
and its table, a failed acceptance leaving no table, decline, cancel and expiry, 401 without a session);
`MigrationRecoveryTests` (migration 15).

## Release per module — EXP-01 (2026-10-19)

Owner decision 2026-10-19: the expansions are released one by one, in the order Fate, Lords, Kings. `ReleaseScope`
holds the set of released modules instead of one switch.

- `Wotr:Release:Modules` (`Wotr__Release__Modules`, `WOTR_RELEASE_MODULES` in infra/.env) lists the released modules
  by id, `fate`, `lords` or `kings`, separated by commas, semicolons or spaces, in any case. An unknown name stops the
  server at start (`InvalidOperationException` naming the key and the name); repeats count once.
- `Wotr:Release:Expansions` stays as an alias: `true` releases all three modules, `false` none. When both are set,
  `Modules` wins. An empty or blank `Modules` counts as unset, because compose passes it empty by default.
- Without either key, Production releases no module and every other environment all of them (as in UI-96).
- `ReleaseScope.Offers` accepts a configuration when it is valid (`ConfigurationRules.Conflict`) and every module it
  enables is released. The base game is always offered. A variant comes with its module: Conflict already refuses a
  variant without the module it needs (New Cities needs Fate, Council needs Lords, the two tile variants need Kings,
  Dismay and Removing Ruler Tokens need Lords and Kings), so no separate list of variants is kept.
- Any room may use the released modules: the earlier rule that named rooms (`Managed`) play only the base game is
  gone, and so is the limit for a challenge's table (LOBBY-01), which is an ordinary named private room configured in
  the waiting room. `CreateRoomRequest` is unchanged.
- `PUT /api/rooms/{id}/configuration` with an unreleased module answers 409 `ExpansionsUnavailable` and changes
  nothing. A table configured before the release narrowed keeps its settings and shows `StartUnavailableReason =
  ExpansionsUnavailable`; its start is refused with the same code until the host picks released modules. Started games
  are not affected.
- `RoomView.OfferedModules` lists the released modules (lower-case ids in release order) and
  `RoomView.ExpansionsAvailable` stays true when any module is released, for clients from before EXP-01.
  `PublicRoomSummary.Configuration` carries the room's configuration, so the Open games list can show the expansion
  crests of each table (read from `rooms.state`, normalized; a room saved without one reads as the base game).
- Contracts 0.12.0 (`ExpansionModules`, `RoomView.OfferedModules`, `PublicRoomSummary.Configuration`); public content
  1.0.13 requires them.

Tests: `ReleaseModuleTests` (the module list, the alias, both set, an empty list, an unknown name in `Read` and at the
start of the server, the defaults per environment, `Offers`, a named room taking Fate and refusing Kings, a table that
cannot start after the release narrowed, and over HTTP a named table with `Wotr__Release__Modules=fate` and its
public summary); `LobbyChatTests.AcceptingSetsThePrivateTableOfTheChallengerWithBothSeated` (a challenge's table takes
Fate); `MenuLifecycleTests.ReadinessCanBeWithdrawnAndOnlyHostStartsExactlyOneNamedBaseGame` (a named room takes
Lords); `LobbyTests` (the UI-96 alias, `OfferedModules` of a full and a base-only release).

## Client compatibility per module — EXP-02 (2026-10-19)

A module goes live with the first client release that plays it, so an older client must never sit at a table it
cannot show. `ModuleClientPolicy` pairs each module with that first version.

- `Wotr:Release:ModuleClients` (`Wotr__Release__ModuleClients`, `WOTR_MODULE_CLIENTS` in infra/.env) lists
  `module=version` pairs separated by commas, semicolons or spaces, e.g. `fate=0.10.0` and later
  `fate=0.10.0,lords=0.11.0`; no spaces around `=`. Module names are case-insensitive; versions are SemVer
  (`ClientVersion`, a pre-release is older than its release). An unknown module, a malformed version or a repeated
  module stops the server at start. A module without a version is open to every client.
- In Production every released module (`ReleaseScope.Modules`) needs its version; a missing one stops the server at
  start with the module's name. Other environments may leave the setting empty.
- `ModuleCompatibility` is an endpoint filter. A client (by `X-Wotr-Client-Version`, already checked by the DS11 gate)
  older than a restricted module, or one without a version, is refused with 409 `ClientUpdateRequired` (`no-store`)
  before the endpoint runs when the table uses that module. A client that plays every restricted module passes
  without a query; only an older one reads the table's `state->'configuration'`.
- Rooms: `GET /api/rooms/{id}`, `POST /api/rooms/{id}/join` (player or observer), `POST /api/rooms/join` (by code),
  `confirm`, `ready`, `start` and `presence` check the room's configuration; `PUT .../configuration` checks the
  configuration the host asks for. Leaving, removing a participant, changing a role and the invitation stay open, so
  an older client can still leave. The lists (`GET /api/rooms`, `/public`) still show such tables; their join is the
  refusal.
- Matches: `GET /api/matches/{id}`, `commands`, `preview`, the `updates` socket, `GET /api/matches/{id}/replay`, the
  whole `/api/observe/{id}` group and its replay check the match's configuration. Deleting a game stays open.
- The client's existing DIST-07 path turns this 409 into the required update (`UpdateCoordinator.Refused`), and the
  updater installs the newest announced release; base tables stay open to it until then. Set a module's version only
  to a release the catalog announces.
- A configuration change resets both confirmations and bumps the room's version, and every confirmation, Ready and
  start carries that version, so a confirmation checked against the old configuration cannot confirm a new one.

Tests: `ModuleClientTests` (parsing, broken settings, which versions play which modules, the Production rule, and over
HTTP an older client refused at a Fate table's join, read, confirm, start, match, updates, replay and observe while a
current client plays it and the older one keeps base tables; without the setting nothing is restricted).
