<a id="decyzje-projektowe"></a>
# Project decisions

Base date: 2026-10-04. The following decisions perpetuate the plan. Technical decisions can be corrected on the basis of implementation evidence, describing the impact on contracts, records and tasks. They are not evidence that implementation exists.

| ID | Decision | Reason and consequence |
| --- | --- | --- |
| D01 | Private 1v1 on Windows/macOS, live and by correspondence. | Durable decisions and several saved parties are the foundation, not later expansions. |
| D02 | Base + Lords + Kings + Fate and indicated variants. | The dependencies and secrets of expansions affect the model from the start; Warriors remain out of scope. |
| D03 | Unity 6000.6.0f1, map 2D, UI Toolkit. | We use the current version of the editor; ergonomics are checked on fixtures before completing all cards. |
| D04 | Clean C# + ASP.NET Core .NET 10 LTS + PostgreSQL. | Verified logic, durability and lack of server Unity; pinned SDK only on bootstrap. |
| D05 | HTTPS to command, WSS to update; snapshot + events + receptions. | Retry and reboot do not depend on maintaining one connection. Full event sourcing is not mandatory. |
| D06 | Clicks as full service, drag as shortcut; from UI-14 the final gesture approves the step, and the fixed tray shows die and the budget. | Precise operation without number forms and confirmation chain. |
| D07 | Optimization for a player who knows the rules; explanations on demand. | The map and decision occupy the screen. Full text card remains available. |
| D08 | The only initial shortcut for automatic decisions: omitting cards in one battle. | It reduces repetitive clicks, with limited, referenceable scope. It does not make strategic decisions. |
| D09 | Documentation, tracked project notes, code and game text are in English (DOC-01, user instruction of 2026-10-16); placeholder assets are allowed. | This supersedes the former Polish-documentation policy. Replacing artwork or later localization does not change content IDs or logic. |
| D10 | One task registry , public start/End , gate sources. | Another agent reads the actual state; the ready-made plan does not succeed in performing the function. |

<a id="otwarte-decyzje-techniczne-do-rozstrzygnięcia-w-zadaniach"></a>
## Open technical decisions to be resolved in tasks

- T02 settled: SDK 10.0.401, lockfiles NuGet/Unity, CI .NET on GitHub Actions and optional own Unity
  runner; instructions in [DEVELOPMENT](DEVELOPMENT.md).
- T08 settled: one-time invitations to 14 days, passwords by ASP.NET Identity PasswordHasher (600
  000 iteration), random sessions on 30 days, cancellation of logout; details in [Accounts and rooms
  contract](server/T08-LOBBY.md).
- UI-05/UI-13: ratio HUD, scaling and target sizes click after attempts in extreme states.
- T38: host and domain remain unindicated. User 2026-10-06 recommended configuration and
  instructions without implementation. [Package](../infra/README.md) specifies Caddy/Compose, public
  trust of proxy, private secrets, copy to new database and retention 14 daily/4 weekly as an
  operational setting for starting on the target host. No schedule or storage outside the host has
  been set. TLS public and external accessibility tests are deferred; [evidence
  local](server/T38-DEPLOYMENT.md) are separate. The active base name is a separate
  `WOTR_DATABASE` in the start configuration of the Compose. It allows switching to the restore
  without editing the password and prevents copying of the old database after the update. Backup
  binds this value to one running container; it does not use the default name or future `.env`.

The new entry describes the problem, the choice, the rejected important options, proof and tasks that need to be updated. We do not write a routine variable name change as an architectural decision.

T21-SH-CHAR-04 (2026-10-05): political responses to movement/attack/acquisition are recorded at the event and resolved only after the end of the entire action, according to the new answer of the author of the rules. This allows the correct expiry of Wormtongue after Saruman's death in the same battle. The retreat itself is not an entry activating the nation. Forced omission of card by Grond is due solely to the public composition of the army; we do not use private content to automate the enemy's window. Rereading the obverses corrects the Strider icon to the north and secures the previous records; it does not reverse historical activation.

<a id="d11--kości-i-interaktywne-podglądy-2026-10-06"></a>
## D11 — dice and interactive previews (2026-10-06)

Source: agreed user plan and order of execution. First, die, for Army/Muster and Will, select the symbol, then the available objects. The region's Hover shows together figures/Nazgûle/characters and allows you to choose it; opening 200 ms, closing 150 ms after leaving the region and panel. Clicks serve strategic choices. Priority: fluidity, quality, readability with simple graphics. Rejected: action name menu, required Inspect and clicked composition tabs, additional Confirm after complete drap.

The movement of the first army is saved before the player chooses the second, with the cost of once and final limits only after all action This requires a new domain continuity (UI-16), not blocking the local miniature. casualties/rerolls/discards They keep one ✓ Implementation UI-15 – UI-24 and her tests are separate acceptance from previous reports; accurate gestures include UX_SPEC.

<a id="d12--repozytoria-i-dystrybucja-za-zgodą-gracza-2026-10-12"></a>
## D12 — Repository and distribution with player approval (2026-10-12)

Source: User's arrangements and order to set them up for the task. Decisions are a project of implementation, and their acceptance belongs to DIST-01-DIST-08. [Full specifications and sources](release/DISTRIBUTION_PLAN.md).

Two repositories, votr-client and votr-server; canonical contracts/public content publishes the server, the client clips the packages. The central task registry stays at the server. The client starts without a separate launcher window, asks before downloading and allows you to postpone the update of the compatible version. The signed installers Windows/macOS and helper are based on Velopack; compliance with Unity and both macOS architectures requires tests, not the SDK declaration itself.

The download page is public. All data and packages are on its own PC with Linux. game/page use Cloudflare Tunnel → nginx; installers have separate direct HTTPS to nginx, by the public IP from the operator, with DNS-only and refurbishment of the DNS-01 certificate. The user rejected R2 to store everything on PC; after recognizing the limitations of the usual CDN chose to obtain the public IP. We do not assume that the shut down of the cache in the tunnel provides the agreed route of distribution of large packages.

The first stable channel uses full packages. The client version is separate from the server, protocol and writing; the publication of a complete signed directory announces the release without automatically updating the backend. The application replaces the local journal/commandId and does not finish the batch. The lack of tools or environment required by acceptance gives an open lock, without replacing the signed release with an unsigned release candidate.

<a id="d13--aktualizacje-wymagane-naprawa-i-wydania-z-ci-2026-10-14"></a>
## D13 — updates required, repair and release with CI (2026-10-14)

Source: user's plan with 2026-10-14 and order its execution. It replaces in D12 the principle of consent before each download and the requirement of signed installers at the first release. [Specification](release/DISTRIBUTION_PLAN.md#d13--zmiany-z-2026-10-14).

The version **required** (below the applicable or planned minimum) the client downloads automatically in the background, without asking. Installation is still followed only by a secure local record and click “Restart and update”. The **optional version**remains with permission (Download/Later, zero bytes before consent). The operator can announce a minimum **with the deadline**: until then, the clients download the package and play further; from the deadline the server rejects older versions. From the Settings the player can run **repair** — redownload and install the current version. Once the required version is in effect, the player can always reject it: “Decline and quit” ends the game safely and closes the game without installation; at the next start the selection returns and the downloaded package remains ready.

Releases are built in GitHub Actions. Until further notice, Windows and macOS installers do not have the signature of Authenticode or Developer ID/notarization; the first installation requires bypassing the system warning according to the player's instructions. The integrity of the update in the game is provided only by the signature of the Ed25519 manifest, whose private key is stored on Mac publisher, except CI and server. The system signature remains the deferred task of DIST-12. acceptance Windows performs an external tester and automatic dragons in CI.

<a id="d14--mvp-09-najpierw-na-macos-2026-10-15"></a>
## D14 — MVP 0.9 first on macOS (2026-10-15)

Source: User's decisions with 2026-10-15 (let's continue to work on the pipette for now only maca like mvp 0.9); no Windows tester). [Specification](release/DISTRIBUTION_PLAN.md#d14--mvp-09-najpierw-na-macos-2026-10-15).

The first release for players has the number **0.9.0** and goes to macOS players. Pipeline continues to build, package and test in CI both platforms, and the release directory (without changing the scheme) announces both manifestos; the game side and Guide player show only the macOS installer until the external tester receives Windows. DIST-06 ends with acceptance macOS (DS14), the CI dragons of both systems and EditMode; the DS13 criterion (the Windows tester) passes to the new DIST-13 task.

<a id="d15--hosting-na-omarchy-wspólny-tunel-i-pobieranie-na-porcie-50443-2026-10-15"></a>
## D15 — hosting on Omarchy: common tunnel and download on port 50443 (2026-10-15)

Source: DIST-04 implementation agreed with the 2026-10-15 user and portfolio integration plan (`docs/WOTR-INTEGRATION.md` in the portfolio repository).

Game ( `wotr.example.test` ) uses a common tunnel Cloudflare portfolio, without its own cloudflared in the stack of games; the tunnel occurs only to nginx game on the network `portfolio-ingress-wotr` . Release Files ( `downloads.example.test` They're going without Cloudflare proxy from public IP Omarchy. Connection operator blocks incoming port 443 (also checked at the low router dam), therefore the download address has a port: `https://downloads.example.test:50443/` ; router steers WAN 50443 per port 443 host. Certificate Let's Encrypt ( DNS-01 ) and record DNS ( DDNS ) keep stack containers with token limited to edit DNS zones. Switch to port 443 does not require changes in the code, only `WOTR_DOWNLOADS_URL` and router rules, but changes addresses in signed manifests — applies only to new releases.

<a id="d16--konta-z-e-mailem-bez-zaproszeń-do-kont-2026-10-15"></a>
## D16 — E-mail accounts without invitations to accounts (2026-10-15)

Source: user order from 2026-10-15 ("we delete registration behind invit... ordinary registration for email and Resend password reset possibility) and his choices in questions of that day. Replaces invitations to accounts with T08/MENU-18; table codes remain.

- The account is assumed in the browser, on the `wotr.<domena>/account/…` game page; Unity client
  only opens this page (AUTH-03). Registration requires an email address; the player's name stays
  and is visible to the opponent.
- The account works immediately. The confirmation link comes by mail; reset the password we send
  only to the confirmed address (the letter in the address does not return the account to the
  stranger).
- Log in name or email address.
- Existing accounts without an address are given to the administrator.
- The mail is sent by Resend. Messages only apply to the account (confirmation, reset); batch
  notifications remain only in the application (R07).

Specification: AUTH-02, AUTH-03.

## D17 — public open source in new repositories (2026-10-16)

Source: the user's order of 2026-10-16 ("before the first installer release I want to publish the
repositories as public open source") and their answers of that day.

- Publication uses new public repositories `wotr-server` and `wotr-client` with one fresh commit
  each. The earlier private repositories are `wotr-server-archive` and `wotr-client-archive`; they
  stay private and keep the full history.
- The project is published under the MIT license, `Copyright (c) 2026 jpolaczek`.
- Reference documents — rulebook and FAQ PDFs, photos and scans of the board and images drawn on
  them — stay local and untracked; `docs/sources/LOCAL-ONLY.json` lists them and the tools and
  validators work without them.
- The README credits the board game's designers, artists and publisher.

## D18 — first release on macOS, Linux and Windows; Steam Deck through the Linux build (2026-10-17)

Source: the user's order of 2026-10-17 ("let's start the release of the first installer ... macOS,
Windows but also Linux and the Steam Deck console") and their answers of that day. Extends D14.

- Version 0.9.0 waits for a Linux x64 client: platform `linux-x64`, an AppImage built and packed by
  Velopack in CI, installed by downloading and starting it, updated in place like the other
  platforms.
- The Steam Deck uses the same Linux build: run directly in desktop mode or added to Steam as a
  non-Steam game. Its 1280×800 screen, trackpads, touch and on-screen keyboard get their own task;
  until the game is checked on a device, the game page calls the Steam Deck experimental.
- Testing: the owner's Mac, the owner's Linux PC and external Windows testers (DIST-13). The game
  page offers a platform once its installer passed acceptance.
- Before the release, production runs the newest server, so the installed client connects to it.

Specification: [LNX-01](tasks/LNX-01.md), [LNX-02](tasks/LNX-02.md), [DECK-01](tasks/DECK-01.md).
