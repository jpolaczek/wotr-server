<a id="wymagania-mvp"></a>
# MVP requirements

These requirements apply to the engine, server, client and acceptance testing. Artwork or usability changes must not alter the rules. [DECISIONS](DECISIONS.md) records technical details adopted as project decisions.

<a id="produkt-i-zakres"></a>
## Product and scope

| ID | Requirement and acceptance condition |
| --- | --- |
| R01 | A complete 1v1 game of War of the Ring Second Edition, with all four victory conditions and no manual rule corrections by players. |
| R02 | Lords of Middle-earth, Kings of Middle-earth and Fate of Erebor can be enabled independently; all eight expansion combinations work with the base game. Enabling an expansion includes all of its mandatory rules and components. |
| R03 | Support the optional 1v1 variants from these rulebooks: Council of Rivendell, Dismay, the three Kings tournament variants and New Cities from Fate. |
| R04 | Self-service registration with an email address in the browser, unique account name and email address, login by account name only (AUTH-05, 2026-10-17), and password reset through a link sent to a verified address (D16); public or private rooms for two players, mutually accepted configuration and a host-controlled start. Private rooms require an invitation code. Identity and side come from the server session. |
| R05 | Live and correspondence play, multiple saved games per account; persist every committed decision, including decisions during battles, the Hunt and card effects. |
| R06 | A Unity client for Windows and macOS. All UI, game text, documentation and tracked project notes are in English (DOC-01). |
| R07 | Notifications about pending decisions appear only in the app. No dependency on an external messenger or email. D16 exception: email is used only for account matters (address verification and password reset). |
| R08 | The current priority is smooth interaction, quality and readability. Use neutral placeholder artwork; final illustrations are a separate stage. Replacing assets must not change rule identifiers, data or interaction geometry. |

<a id="poprawność-trwałość-i-poufność"></a>
## Correctness, persistence and confidentiality

| ID | Requirement and acceptance condition |
| --- | --- |
| R09 | The server validates permissions, the current decision, costs, targets and effect order for every command. The client never commits a resolution independently. |
| R10 | A command identifier and revision prevent double spending, rerolling and conflicts between two devices. A committed command and its response remain recoverable after connection loss. |
| R11 | Hidden hands, deck order, private decisions and face-down tiles do not reach the opponent through payloads, options, history, errors or notifications. Information is revealed exactly as the rules require. |
| R12 | Every implemented rule has a source, location and verification scenario. Unconfirmed data blocks the dependent scope; do not replace missing evidence with guesses. |
| R13 | Games recover after a server restart. Rules, content and save-schema versions are stored with each game; an update does not reset existing games. |
| R14 | The server performs random draws and rolls; tests use a controlled random source. Resolution does not depend on client animation order or frames. |

<a id="ergonomia"></a>
## Usability

| ID | Requirement and acceptance condition |
| --- | --- |
| R15 | The map dominates the screen. The phase, decision owner, available and spent dice, and current action cost are visible without opening history. |
| R16 | Every action supports clicks. Drag and drop is an equivalent shortcut. A complete legal drop or final target click sends the command without another Confirm; an incomplete gesture prepares a selection, and an illegal one spends no resources. The server always determines the result. |
| R17 | Dice guide selection, and legal objects and targets are highlighted on the map. A permanent tray shows the active die and remaining budget; details and choices sit beside their objects. Grouped casualties/rerolls have one ✓; excess hand cards are discarded individually by a single click, without confirmation chains or action-name menus. |
| R18 | No mandatory numeric entry for unit counts or scrolling in the main decision panels. Select units, dice, hits and targets visually; limit repeated text and unnecessary numbers. |
| R19 | Show the precise information needed for a decision: army composition, legal targets, costs and known effects. A noninteractive text tooltip appears after 200 ms of hovering or immediately on focus and disappears on exit; it reads only safe projection metadata, does not intercept gestures and does not query the server. Interactive previews, cards and option panels open on an object click or Enter/Space; they remain after the cursor leaves and close with ×/Esc (user-approved exception, 2026-10-15, UI-189: the region window closes after the cursor leaves both the region and the window). Hovering only highlights region terrain, including regions with figures; the region description requires a click. Tooltips disappear on drag/pan/cancel/focus loss and do not intercept Esc before the active window. A legal action retains priority without another confirmation; a new decision belonging to the player may still open its panel automatically. Complete public text and explanations remain available, including for the player's unavailable actions, without exposing the opponent's data. |
| R20 | The UI does not choose between nonequivalent dice, routes, casualties, retreats or valuable resources for the player. The only initial shortcut for repeated decisions is voluntarily skipping combat cards in the current battle. |
| R21 | Waiting for the opponent and reconnecting do not prevent browsing the map, the player's cards or history. Returning shows the current state and decision without mandatory animation playback. |
| R22 | Reinforce color with shape and symbols. The UI supports 1366×768 and 1920×1080, scaling, a mouse and a trackpad. Small labels remain readable when the map is zoomed out. |
| R23 | Local selection feedback within 100 ms without waiting for the network; target 60 FPS at 1080p on a recorded test configuration. Animations are short and skippable; reduced motion is available. |

<a id="proces-i-wydanie"></a>
## Process and release

| ID | Requirement and acceptance condition |
| --- | --- |
| R24 | Before every task, the agent reads the context, records the start, maintains documentation and ends with recorded checks or an explicit blocker. |
| R25 | Tests verify behavior against sources, state boundaries, privacy, retries and recovery. Compilation or a complete base-game playthrough does not replace checking expansion exceptions. |
| R26 | The private backend has TLS, migrations, backups and tested restoration. The Windows/macOS release passes a shared game and save recovery. |
| R27 | Client and server have separate repositories and independent builds/CI. The client uses pinned contracts and permitted public data without a server-source checkout; history and the central task registry retain provenance. |
| R28 | Players download a Windows/macOS installer from the website, install and launch the game without developer tools. macOS covers Intel/ARM64. Until further notice (D13), installers do not have Authenticode/Developer ID signatures; the website and instructions explain the operating-system warning on first installation. Installation/update preserves local data and application identity. |
| R29 | The client detects releases announced by the server. It downloads an optional version only with consent and allows deferral. It automatically downloads a required version in the background, including one scheduled with a deadline, and allows continued play until that deadline (D13). Players can repair the current installation. Replacement happens after a safe shutdown and a player click, preserving the journal/commandId, data and ability to resume games. The server enforces compatibility; packages and manifests are verified with an Ed25519 signature. |
| R30 | The server, database, public website and packages are hosted on a personal Linux PC. The game and website use Cloudflare Tunnel/nginx; large packages use direct HTTPS to nginx through the provider's public IP. Acceptance covers access outside the LAN, autostart, certificate renewal, resumed transfers and restore. |
| R31 | Test-scenario mode: named situations (e.g. “test Will of the West”) launched from the menu on a server with this option enabled. After setup the game proceeds normally, one client plays both sides, the result does not enter saved games, and restarting recreates the same situation. |
| R32 | Demo mode (user decision, 2026-10-15): from the lobby, a player starts a base game against a simple CPU that selects legal moves for the other side through the same server rules engine. One demo per account: a new one replaces the previous one; normal games remain unlimited and have no CPU. |

<a id="obserwowanie-i-replaye--plan-z-2026-10-16"></a>
## Observing and replays — plan of 2026-10-16

These new user requirements are planned and have not passed implementation acceptance.
Breakdown and scenarios: [OBSERVERS-REPLAYS](server/OBSERVERS-REPLAYS.md);
states: registry. This clarifies R04/R11: an authorized
observer's role and selected perspective are independent of a player's side;
a player still does not receive the opponent's hidden data.

| ID | Requirement and acceptance condition |
| --- | --- |
| R33 | Every normal room holds two players and up to 16 observers. Before the game starts, participants can switch roles if the destination has a free place; player seats are fixed after the start. An observer switches between both sides' perspectives, sees their hands and browses the map, descriptions, tabs, activity log and reports, but cannot perform operations that change gameplay. The server authorizes the role independently of the perspective and does not expose future decks or RNG. |
| R34 | A newly completed base game, including a demo, has a complete replay that players and currently present observers can save locally. The Replays library works before login and offline, with file import and export. The player provides both perspectives, automatic playback without thinking time and with speed control, and manual steps through decisions and whole turns in either direction, with historically correct hands, activity log and reports. A saved file does not depend on the game continuing to exist on the server. |

<a id="konfiguracja-nowej-partii"></a>
## New-game configuration

| Option | Default | Condition |
| --- | --- | --- |
| Base Second Edition | always | Cannot be disabled. |
| Lords / Kings / Fate | disabled | Independent toggles; no arbitrary disabling of an expansion's mandatory components. |
| Council of Rivendell | disabled | Requires Lords; the private companion-version choice is a separate FP decision. |
| Dismay | enabled with Lords + Kings | Optional in this combination; otherwise inactive in the MVP. |
| Visible Corruption Tiles | disabled | Requires Kings. |
| Sequential Corruption Tile Draw | disabled | Requires Kings. |
| Removing Ruler Tokens | disabled | In the MVP, requires Lords + Kings and the Dismay mechanic; the shared configuration shows the dependency. |
| New Cities | disabled | Requires Fate. |

**Live MVP release — user decision, 2026-10-11 (UI-96):** until further notice,
new live tables use only the base game. In Production, the server rejects
expansion modules and variants (`ExpansionsUnavailable`), and the client hides
their selection. R02/R03 code and tests remain; setting
`Wotr__Release__Expansions=true` (or using an environment other than Production)
restores the full configuration. Existing expansion games still load.

**Release per module — owner decision, 2026-10-19 (EXP-01):** the expansions
are released one by one, in the order Fate, Lords, Kings.
`Wotr__Release__Modules` names the released modules (`fate`, `lords`, `kings`);
the older `Wotr__Release__Expansions` stays as an alias (`true` all, `false`
none), and the module list wins when both are set. Production without either
releases none. A variant comes with its module (the dependencies above). Any
room, a named one or a challenge's table too, may use the released modules; an
unreleased one is refused with `ExpansionsUnavailable`, and a waiting table set
up before the release narrowed cannot start until its host changes it. The
room view lists the released modules, and the Open games list shows each
table's configuration.

Changing configuration invalidates the other player's earlier acceptance. Starting
requires both players to accept the same version. Configuration is immutable after
start. Private setup decisions do not enter the shared summary.

<a id="poza-mvp"></a>
## Outside the MVP

Warriors of Middle-earth, other expansions and promos, 3–4 players, AI opponents
in normal games (exception: the simple demo CPU in R32), public matchmaking,
rankings, mobile and browser apps, and notifications outside the app. Observing
and local replays were added to the plan by the user's decision of 2026-10-16
(R33/R34); this does not claim implementation. Original artwork does not block
a playable version.

<a id="priorytet-przy-konflikcie"></a>
## Priority when requirements conflict

Rules correctness and confidentiality come first, followed by decision readability,
smooth interaction and finally decoration. Usability means removing unnecessary
interaction, not shortening a decision sequence required by the game.

<a id="stan-implementacji-kont-i-konfiguracji"></a>
## Account and configuration implementation status

T08 implements R04: invitations, sessions and two seats; [description](server/T08-LOBBY.md).
T34 supports starting all 93 legal R02/R03 configurations after both players accept
the same version. Each uses its own setup and complete rules; the server does not
substitute a base game for an expansion configuration. Changing settings invalidates
both acceptances; a started game's configuration is immutable and readable in the
Rules panel. [Expansion acceptance](https://github.com/jpolaczek/wotr-client/blob/d2b997d406fed708403ebf3aca7e30be65e6ef5d/docs/ui/T34-ACCEPTANCE.md)
distinguishes server tests, client tests and release limitations.

<a id="granica-odbioru-wydania-010-rc1"></a>
## Release 0.1.0-rc1 acceptance boundary

R01–R05 and R09–R25 have evidence documented in base-game, expansion, client,
correspondence and T36–T38 tasks. The [release report](release/T39-RELEASE.md)
collects actual results; test counts do not prove every possible game sequence.
R08 still permits placeholder artwork.

On 2026-10-06, the user requested a Windows build without Windows testing and server
configuration without a target host. R06 therefore distinguishes compilation from
execution: both non-Development builds were produced; the unpacked macOS client
started on Apple Silicon with clean data and its login screen was inspected.
R26 remains partly unverified: local TLS and two restores passed acceptance;
the target host/public certificate and a shared Windows ↔ macOS game were not
tested. Manual user acceptance and Intel macOS hardware are also not claimed.

<a id="zmiana-interakcji-ui-14ui-24"></a>
## UI-14–UI-24 interaction changes

R08 and R16–R19 describe the new interface agreed on 2026-10-06. Only the task
registry determines implementation status. Historical T39 and UI-01–UI-13 results
do not prove support for the new gestures or persistent action steps.

UI-24 documents client **0.2.0-rc1** acceptance on 2026-10-07:
3550 .NET tests, 245 EditMode and 309 PlayMode PASS, covering all cards/expansions,
complete games and 28 Player measurements on recorded hardware. Dice, direct
gestures, persistent steps, interactive previews and short Player tooltips have
new evidence. Both non-Development builds and the macOS smoke passed. The Windows
runtime/Intel/host/manual-testing boundary remains as instructed above. This is a
local package of clients requiring protocol 2, with no claim of a new backend deployment.

<a id="plan-dystrybucji--dist"></a>
## Distribution plan — DIST

R27–R30 come from the user's agreements of 2026-10-12 and define future
[DIST-01–DIST-08](release/DISTRIBUTION_PLAN.md) work. Creating tasks does not
constitute installer or deployment acceptance. The public download page does not
require login; the game retains authentication and the current base-game scope.
The Windows/runtime/host deferrals in T38/T39 remain historical; new DIST-08
acceptance requires actual signed installations and updates on both platforms.

<a id="scenariusze-testowe--test"></a>
## Test scenarios — TEST

R31 comes from the user's request of 2026-10-13. TEST-01–TEST-04
implement it: the server mechanism, base-game dice scenarios, client support and
expansion dice. Since 2026-10-14 (user decision, TEST-05), the
MVP is temporarily limited to the base game: the catalog on `main` contains only
base-game scenarios, while expansion scenarios live on `expansion-test-scenarios`.
Test games are not saved games as defined by R05.
