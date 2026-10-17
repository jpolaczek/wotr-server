<a id="panel-admina--admin-01"></a>
# Admin panel — ADMIN-01

The owner's request of 2026-10-18: "add an admin panel to the server application: view the users and the numbers of
created and played games". The panel is the page `https://<WOTR_DOMAIN>/admin` (`infra/site/admin.html`,
`infra/site/assets/admin.js`), in the look of the account pages, with its data from the server's `/api/admin`.

<a id="co-pokazuje"></a>
## What it shows

- **Players and games** (`GET /api/admin/summary`): players (accounts that are not technical), confirmed email
  addresses, players signed in during the last 7 days, tables created and tables waiting for players now, games
  started, finished (with a winner) and in progress, games their players removed before the end, Free Peoples and
  Shadow wins, and apart from games between players: test scenario sessions and demo games against the CPU.
- **Accounts** (`GET /api/admin/users?page=0&size=50`, newest first, at most 100 per page): account name, created,
  last sign-in, whether the email is confirmed, tables hosted, games, games finished and wins.

Games come from the match ledger (`match_ledger`, migration 014), written when a match starts (`MatchStore.Insert`,
with its kind: game, test or demo) and when a save first has a winner (`MatchStore` → `MatchLedger.Finished`), so a
match its players remove (`MatchRemoval`) still counts. Tables come from `room_ledger` (`RoomStore.Create`); the empty
room sweeper does not reduce them. Accounts and sessions have `created_at` from migration 014; the rows already there
are dated exactly from their 720-hour session expiry (an account's first session is its registration). Tables created
before the migration count without a time.

<a id="prywatnosc"></a>
## Privacy

The views (`AdminViews.cs`) never carry an email address, password data, a token, an account id or a network
address; `AdminPanelTests.UserListCarriesNoSensitiveField` pins the user fields. Showing email addresses would be a
deliberate later change. The page writes every value with `textContent` and keeps the admin token only in its memory:
a reload or a closed tab signs out.

<a id="dostep-i-bezpieczenstwo"></a>
## Access and security model

- **Administrators** are named by the host's configuration, not by the database: `WOTR_ADMIN_ACCOUNTS` in
  `infra/.env` (comma-separated account names, any case) reaches the server as `Wotr__Admin__Accounts`
  (`Wotr:Admin:Accounts`, `AdminOptions`). Empty (the default) keeps the panel closed to everyone; a malformed name
  stops the server at start. The old `accounts.is_admin` column is not used.
- **Sign-in** (`POST /api/admin/session` with `{ "username", "password" }`) checks the password first (PBKDF2, the same
  cost for a wrong name, a wrong password or an account outside the list) and then issues an **admin session** of
  8 hours (`sessions.purpose = 'admin'`). It has the login rate limit (20 per minute per address).
- **Every data request** is checked afresh (`AdminAccess`): an open, unrevoked admin session of a non-technical account
  still named in the configuration. A game session — even of an administrator — never reads the panel, so a game token
  left on a player's computer cannot. Data requests have the per-account read rate limit.
- **Anyone else gets an empty 404**, as for a path that does not exist: anonymous requests, game sessions, other
  accounts, a refused sign-in and a malformed sign-in body. Admin answers carry `Cache-Control: no-store`.
- **Sign-out** revokes the admin session through `POST /api/auth/logout`.
- The website calls `/api/admin` without client version headers; the client compatibility gate leaves it out like the
  account pages (`ClientCompatibilityGate`).
- **Audit:** the server log records every admin sign-in (`Admin panel sign-in by …`), refused sign-ins without a name,
  and every view (`Admin panel /api/admin/users read by …`).
- The page `/admin` itself is public static HTML (`noindex`, `no-store`) under the site's Content-Security-Policy
  (`infra/nginx/security-headers.conf`: own scripts and styles only, no inline script, no form submission).

<a id="konfiguracja-na-hoscie"></a>
## Configure it on the host

1. In `infra/.env` set `WOTR_ADMIN_ACCOUNTS=<your account name>` (several: `name1,name2`). The account must already
   exist (registered on the site).
2. Deploy the server image with migration 014 and the site files (`infra/site/admin.html`, `assets/admin.js`,
   `assets/site.css`) and `infra/nginx/game.conf` (`location = /admin`), then restart `server` and `web`.
3. Open `https://<WOTR_DOMAIN>/admin` and sign in with that account's name and password.

<a id="testy"></a>
## Tests

`tests/Wotr.Tests/AdminPanelTests.cs`: configuration parsing; only an admin session of a configured account reads the
panel (anonymous, game sessions, another account, a wrong password and a malformed body get the empty 404; sign-out
revokes); the summary and the paged users count games from the ledger on an own database (a finished game, one in
progress, one removed, a test and a demo match, a table, a technical account left out); the user list has exactly its
public fields; migration 014 dates existing accounts and sessions and carries existing matches and tables into the
ledgers.
