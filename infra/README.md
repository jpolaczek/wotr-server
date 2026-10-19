<a id="prywatny-serwer"></a>
# Private server

State acceptance and limitations: [T38](../docs/server/T38-DEPLOYMENT.md). There is no target host yet. The configuration below listens only to the local computer by default; the secret files do not go to Git.

<a id="pierwszy-start-lokalny"></a>
## First local start

Required: Docker Engine with Compose v2 and Python 3 The images were also attached with diggestions; SDK 10.0.401 , ASP . NET 10.0.9 , PostgreSQL 17.11 , Caddy 2.11.2 On Linux, choose host ARM64 or x86_ 64 conforming to the image being built.

```sh
python3 scripts/deploy-init.py
docker compose --env-file infra/.env -f infra/compose.yaml config --quiet
docker compose --env-file infra/.env -f infra/compose.yaml build server
docker compose --env-file infra/.env -f infra/compose.yaml up -d --wait
```

The initializer refuses to overwrite existing secrets. The `secrets` directory has 0700 mode, 0444 files: the directory protects them on the host, and the mounted files can be read by the `app` user from the container. The `wotr` account is the owner of its database, without SUPERUSER/CREATEDB/CREATEROLE. Administrator PostgreSQL has another password, available only to the database service.

Local address: `https://localhost:8443`. Caddy uses local CA. Export the public certificate yourself to the customer for the CLI tests:

```sh
docker compose --env-file infra/.env -f infra/compose.yaml exec -T proxy cat /data/caddy/pki/authorities/local/root.crt > .artifacts/wotr-local-ca.crt
curl --cacert .artifacts/wotr-local-ca.crt https://localhost:8443/health/ready
```

Do not disable certificate check. The player's client should ultimately receive the public address HTTPS with the normally trusted certificate. The local CA is not a public release certificate.

<a id="docelowy-host-i-domena--po-ich-dostarczeniu"></a>
## Target host and domain — upon delivery

1. Install Docker/Compose and move the release code. Provide space for images, database and at least
   two complete copies.
2. Generate new secrets on the host. In `infra/.env` set release version,
   `WOTR_DOMAIN=gra.twoja-domena.pl`, `WOTR_BIND=0.0.0.0`, ports 80/443 and
   `WOTR_CADDYFILE=./Caddyfile`. DNS A/AAAA must lead to host; 80/443 ports are to be accessible to
   certificate publishers and players.
3. Run the above build and Compose. Backend and PostgreSQL do not publish their own ports. Do not
   open 8080/5432. When subnet conflicts are started, change the WOTR_PROXY_SUBNET, WOTR_PROXY_IP
   and WOTR_SERVER_IP together.
4. Check with another computer `/health/ready`, the string and certificate name, registration of two
   invited accounts, room, command and WSS, reboot and reopening the same batch. These target
   attempts have not yet been made.

Caddy manages certificate renewal; `certificates` volume must not be deleted during upgrades. Notifications remain only in the game.

<a id="panel-admina--admin-01"></a>
## Admin panel — ADMIN-01

`https://<WOTR_DOMAIN>/admin` shows the players and the numbers of created and played games to the administrators named
in `infra/.env`: `WOTR_ADMIN_ACCOUNTS=<account name>[,<account name>…]` (passed to the server as
`Wotr__Admin__Accounts`). Empty, the default, keeps the panel closed. After a change restart `server`. Details and the
security model: [ADMIN-PANEL](../docs/server/ADMIN-PANEL.md).

<a id="zakres-wydania-ui-96"></a>

Optional second lock (SEC-05): Cloudflare Access in front of `/admin` and `/api/admin` — steps in
[ADMIN-PANEL](../docs/server/ADMIN-PANEL.md#cloudflare-access).

## Scope of release (UI-96)

The server in Production offers only new tables base game. `WOTR_EXPANSIONS=false` in `infra/.env` (default) transfers `Wotr__Release__Expansions` to the container; `true` also provides expansions and their variants. The change requires the restart of the `server` service; the started parts retain their configuration.

EXP-01 (2026-10-19): the expansions are released one by one, Fate, then Lords, then Kings.
`WOTR_RELEASE_MODULES` in `infra/.env` is passed as `Wotr__Release__Modules` and names the released modules,
separated by commas (`fate`, then `fate,lords`, then `fate,lords,kings`; any case). Each module brings its variants
(New Cities with Fate, Council with Lords, the Kings tiles with Kings, Dismay and Removing Ruler Tokens with Lords and
Kings). When it is set it wins over `WOTR_EXPANSIONS`, which stays as an alias (`true`: all three, `false`: none);
empty, the default, leaves the choice to `WOTR_EXPANSIONS`. An unknown name stops the server at start. Restart
`server` after a change. A waiting table configured with a module the release no longer offers keeps its settings but
cannot start until its host changes them; started games are not affected.

<a id="katalog-wydań-klienta--dist-05"></a>
## Customer release directory — DIST-05

Server from DIST-05 checks the client version in each game request. In Production client without headers `X-Wotr-Client-Version` / `X-Wotr-Protocol-Version` gets 409   `ClientUpdateRequired`. This applies to the previous ZIP - and builds before DIST-05, also local build game. For the time you go, set in `infra/.env`   `WOTR_ACCEPT_UNVERSIONED_CLIENTS=true`  and reboot  `server`; when players install a new client, go back to `false`. The first installation of the new client the player performs alone; the server will not add an update to the old application.

Release directory (`WOTR_DISTRIBUTION_DIR`, in `/distribution` container, read-only):

```text
incoming/<version>/<platform>/ upload from publisher's machine, not published
public/releases/<version>/... unchanged files; nginx from DIST-04 only serves public/
catalog.json announced releases and minimum, atomicly replaced
```

`WOTR_RELEASES_ROOT=/distribution`, `WOTR_DOWNLOADS_URL=https://downloads.<domena>/` and `WOTR_RELEASE_PUBLIC_KEY` (base64 of Ed25519 key) turn on the directory. The empty `WOTR_RELEASES_ROOT` disables the directory and the minimum; the incomplete configuration stops the server start. The change of `catalog.json` works at most seconds without rebooting.

On the publisher's machine (private key outside repositories and download host):

```sh
dotnet run --project src/Wotr.ReleaseTool -- keygen --private-key ~/.config/wotr/release-signing/wotr-release-ed25519.key --public-key ~/.config/wotr/release-signing/wotr-release-ed25519.pub
dotnet run --project src/Wotr.ReleaseTool -- sign --build-info <build>/build-info.json --package <paczka> --notes <opis.md> \
  --downloads-url https://downloads.<domena>/ --private-key <klucz> --output <upload>
```

`sign` refuses to build a developer, dangerous file name and existing version folder. `<upload>/<version>` folder with all three platforms (LNX-01: Windows, macOS, Linux) copy to `incoming/` on host. Operator commands (`--public-key` accepts base64 or `@plik`):

```sh
python3 scripts/release_host.py publish --version 0.3.0 [--minimum]
```

`release_host.py` (DIST-11) runs `ReleaseTool` from the `wotr-server:$WOTR_VERSION` image by `docker run` without network, as operator, with distribution directory under `/release-root`. `docker compose run server` is not suitable for this: on the running server it takes over its permanent address IP.

The tool saves only the `/release-root` mounted to write as an operator; the running server sees the same directory read only.

This is why the prefix is used by `minimum --version`, `withdraw --version` and `verify`. `publish` checks all three platforms, signatures, URL paths, sizes, SHA-256, consistent protocol and commit and the scope of server protocols, and then transfers the folder to `public/` and saves the directory. Any refusal (`REFUSED`, 2 code) changes nothing. The discontinued publication after the transfer ends with a re-`publish`. The minimum has to be announced with complete packages; the issue cannot be withdrawn until it is a minimum. The withdrawal leaves the files.

<a id="minimum-z-terminem-i-instalatory--dist-09"></a>
## Minimum term and installers — DIST-09

`sign` requires DIST-09 also `--installer <file>` (Setup.exe or .pkg); the manifest has a 2 diagram and the platform folder contains a manifest, signature, package and installer. Before sending to host, the publisher checks the folder locally:

```sh
dotnet run --project src/Wotr.ReleaseTool -- verify-upload --upload <upload> \
  --downloads-url https://downloads.<domena>/ --public-key @<klucz.pub> --version 0.4.0
```

On host (the same prefix `docker compose … run` as `publish`):

| Command | Effect |
| --- | --- |
| `publish --version v --minimum --at 2026-10-20T20:00+02:00` | Announces the issue and plans it as a minimum |
| `minimum --version v --at <time with Z or an offset>` | plans a minimum; at least 5 minutes ahead, higher than the current |
| `minimum --version v` | minimum immediately; deletes the term for the same or lower version |
| `minimum --cancel-scheduled` | cancels the planned minimum |
| `status` | revision, minimum, planned minimum and announced editions |

Time without a zone (`2026-10-20T20:00`) is rejected. By the deadline, customers download the version in the background and play further; from the deadline the server rejects older versions without rebooting. The planned minimum cannot be withdrawn (`withdraw`) before canceling the deadline. The first save after the deadline moves the version to `minimumVersion`.

<a id="wydanie-klienta-jednym-poleceniem--dist-11"></a>
## Customer release with one command — DIST-11

The release starts with `v<version>` tag in votr-client; workflow Release (DIST-10) creates draft GitHub Release with packages, installers, `build-info-<platform>.json`, `RELEASE-NOTES.md` and `SHA256SUMS`. The rest is done on Mac publisher, in checkout votr-server:

```sh
python3 scripts/release_client.py v0.9.0 --dry-run              # Check and signature, Nothing comes out of Mac.
python3 scripts/release_client.py v0.9.0                        # optional edition
python3 scripts/release_client.py v0.9.1 --required-at 2026-10-20T20:00+02:00
python3 scripts/release_client.py v0.9.2 --required-now
```

Next: Download draft ed `~/WotrReleases/<version>/assets` control `SHA256SUMS` Both `build-info` (log version, commit tag, build release, one protocol), `.nuspec` packages and notes; `ReleaseTool sign` both platforms key with `~/.config/wotr/release-signing/` and `verify-upload` ; confirmation of the version; `rsync --partial` ed `incoming/<version>/` host; by `ssh` `scripts/release_host.py publish` (from `--minimum [--at]` ); check `/api/client/releases` both platforms (newest version, minimum or term), bytes published `manifest.json` and `.sig` with local signatures, `/api/client/downloads` ( SHA-256 and installer size) and response 206 on the Range for parcels and installers; at the end draft au GitHubie becomes a published edition, and `~/WotrReleases/history.jsonl` I get the entry.

Any refusal ( `REFUSED` ) before confirmation does not change anything on the host. Restart continues: downloads and signed files are in `~/WotrReleases/<version>/` and the version announced by the host does not send a second time, it just checks it. draft the same version (refused release) replaces the local copy until the host has announced it; then the higher version remains.

On Mac required: `gh`  (zalogowane),. NET   10   SDK  (`ReleaseTool`),  `ssh` and `rsync` to host without asking for a password, a key pair (`.key` with rights 0600,  `.pub` equal `WOTR_RELEASE_PUBLIC_KEY`  hosta). Host and checkout:  `~/.config/wotr/release-client.json`, for example `{"host": "omarchy", "hostCheckout": "/srv/wotr/wotr-server"}` (or `--host` / `--host-checkout`);  `hostCheckout` is a production checkout with `infra/.env`.  SSH ed Omarchy (2026-10-15):  `sshd` on, `ufw` allows port 22 only with `192.0.2.0/24` (without redirecting on the router), logging in only with key (`/etc/ssh/sshd_config.d/10-wotr.conf`:  `PasswordAuthentication no`); on Macu `Host omarchy`   →   `192.0.2.85`.

On the host `scripts/release_host.py` reads `infra/.env` (`WOTR_DISTRIBUTION_DIR`, `WOTR_DOWNLOADS_URL`, `WOTR_RELEASE_PUBLIC_KEY`, `WOTR_DOMAIN`) and runs `ReleaseTool` through Compose. Manual support:

| Command on Host | Effect |
| --- | --- |
| `python3 scripts/release_host.py state` | JSON: incoming, addresses, key, announced versions, minimum |
| `python3 scripts/release_host.py status` | directory status according to ReleaseTool |
| `python3 scripts/release_host.py minimum --cancel-scheduled` | cancellation of the time limit |
| `python3 scripts/release_host.py minimum --version v [--at T]` | minimum now or by deadline |
| `python3 scripts/release_host.py withdraw --version v` | withdrawal (first remove minimum or deadline) |
| `python3 scripts/release_host.py verify` | control of files and signatures of the entire directory |

Runbook:

- **Unable release:** Cancel the deadline (`minimum --cancel-scheduled`), withdraw the version
  (`withdraw`) and release the amendment only higher version; players who have already installed the
  defective will get the amendment as any update. Downgrade does not exist. When the version does
  not start, the player installs the current version from the installer on the game page (the
  installer overwrites the installation, the data remain).
- **The publisher key** (key id `95ffc109e1bb9b15`) is only on the publisher's Mac. You need to have
  its encrypted offline copy (e.g. a password image in the password manager) outside Mac, CI and
  host; no version will update players without the key. Loss or leak: new `keygen`, customer release
  pinning old and new public key, signed old yet; after its adoption by the host players passes to
  the new `WOTR_RELEASE_PUBLIC_KEY`. When the leak without trusted update, players install the new
  client manually.
- **Host or Internet not available:** The command stops at `ssh`/`rsync` or at the public address
  check; after repairing it, you just have to repeat it. The game continues on the current version.

<a id="produkcja-na-omarchy--dist-04"></a>
## Production for Omarchy — DIST-04

The game stack works on Omarchy next to the portfolio and the pen, with its own checkout release `/srv/wotr/wotr-server` and data in `/srv/wotr/data`. All Compose commands go through `infra/production.sh` (`compose.yaml` + `compose.production.yaml`)
+ `infra/.env`); Caddy with `compose.yaml` stays for the local profile only.

```text
wotr.example.test      Cloudflare → tunnel portfolio → web (nginx, alias wotr-web, network portfolio-ingress-wotr)
→ game page (infra/site) or server:8080 (API, WSS); IP guest with CF-Connecting-IP
downloads.example.test DNS-only (ddns) → router WAN :50443 → host :443 → downloads (nginx :8443) → /srv/wotr/distribution/public
(read only; Range/206; 8 MB/s on connection, 4 on address, 8 on total); certificate of certbot DNS-01
```

| Catalog | Owner | Content |
| --- | --- | --- |
| `/srv/wotr/wotr-server` | kuba | checkout Issues, `infra/.env` (0600), `infra/secrets/` (0700) |
| `/srv/wotr/data/postgres` | postgres container | production base |
| `/srv/wotr/distribution` | kuba | `incoming/`, `public/`, `catalog.json` (DIST-05/11) |
| `/srv/wotr/backups` | kuba | copies `scripts/deploy.py backup` |

<a id="pierwsze-uruchomienie-etap-b"></a>
### First start (stage B)

```sh
sudo install -d -O kuba -g kuba -m 0750 /srv/wotr
sudo install -d -O kuba -g kuba -m 0700 /srv/wotr/data /srv/wotr/backups
sudo install -d -O kuba -g kuba -m 0755 /srv/wotr/distribution /srv/wotr/distribution/incoming /srv/wotr/distribution/public
git clone git@github.com:jpolaczek/wotr-server.git /srv/wotr/wotr-server && cd /srv/wotr/wotr-server
git checkout <commit wydania serwera>
python3 scripts/deploy-init.py                      # database secrets; He's not writing anything out.
cp infra/production.env.example infra/.env && chmod 600 infra/.env   # WOTR_VERSION = Shortcut commitu
infra/production.sh config --quiet && infra/production.sh build server
install -m 0444 /dev/null infra/secrets/resend_api_key   # AUTH-02: empty, until account mail is disabled
infra/production.sh up -d --wait                    # database, server, web; without external exposure
```

The first account is established on the `/account/register` website (AUTH-02).

The empty database is created at first start (`init-database.sql`); the existing PostgreSQL data directory does not initialize again.

<a id="trasa-gry-w-tunelu-etap-c"></a>
### Game route in the tunnel (step C)

In `/etc/portfolio/platform.env` set `PORTFOLIO_ROUTE_WOTR=true`, then in the portfolio `cloudflared tunnel route dns portfolio wotr.example.test` and `infra/scripts/tunnel.sh up`. The network `portfolio-ingress-wotr` creates a stack of games, so it must operate before the tunnel with this route. Disconnection: the flag `false` and `tunnel.sh up`; the game data stays.

<a id="serwer-plików-etap-d"></a>
### File Server (Step D)

1. Token Cloudflare: My Profile → API Tokens → template **Edit zone DNS**, zone `example.test` only.
   Save without showing it in the terminal: `( umask 377; read -rsp 'Cloudflare token: ' t; echo;
   printf '%s\n' "$t" > infra/secrets/cloudflare_token )`.
2. Router (Livebox, Advanced settings → Network configuration): booking DHCP `192.0.2.85` for Omar
   and rule NAT/PAT TCP ed WAN **50443** on `192.0.2.85:443` Operator blocks incoming port 443
   (checked also at the low router dam), therefore the file address has a port:
   `WOTR_DOWNLOADS_URL=https://downloads.example.test:50443/` It's on Omary. `ufw-docker` : only
   private networks pass to containers, so you need a transfer rule to the container port (ordinary
   `ufw allow 443` not working: `sudo ufw route allow proto tcp from any to any port 8443 comment
   'WoTR downloads container'` .
3. W `infra/.env` `COMPOSE_PROFILES=public` (first of all, `WOTR_CERTBOT_STAGING=1` for the test
   certificate test, then without), and `infra/production.sh up -d --wait` . `ddns` a 5 minutes
   improves the A record `downloads` (without Cloudflare proxy), `certbot` obtains a certificate
   DNS-01 and check the renewal of what 12 h, `downloads` reloads the certificate every 6 h.

The 50443 port (WAN) leads only to release files: other names and the same address IP do not get a certificate, and `incoming/`, `catalog.json`, copies and secrets do not lie in the given directory.

<a id="obsługa"></a>
### Support

- Server update: `git fetch && git checkout <commit>`, new `WOTR_VERSION`, `infra/production.sh
  build server && infra/production.sh up -d --wait`. Before changing the scheme: copy (`python3
  scripts/deploy.py backup /srv/wotr/backups/<data>`).
- One-time running of the server image next to the running one, e.g. migrations on the restored
  copy: service `server` has on the network `frontend` fixed address ( `WOTR_SERVER_IP` ), so `run`
  The free address of this subnet must be invoked, otherwise Docker refuses to ‘Address already in
  use’: `WOTR_SERVER_IP=172.30.93.250 infra/production.sh run --rm --no-deps -e
  WOTR_DATABASE=wotr_restore_<…> server --migrate`.
- Restart PC: services have `restart: unless-stopped` and return with Docker.
- Link or tunnel loss: game is unavailable, the parties stay in the base; the client reconnects upon
  return. Public change IP: `ddns` improves the record within 5 minutes.
- Certificate: `infra/production.sh exec certbot certbot renew --dry-run` checks renewal;
  `infra/production.sh logs certbot downloads ddns` logs.
- External control: GitHub   →  Actions  →  **Public endpoints**  → Run workflow (page, API,  TLS
  files, HEAD, Range, resume, call limit, 404 outside `/releases/` Cloudflare address call tools
  must send their own `User-Agent`: default `Python-urllib` blocks Browser Integrity Check 1010).

<a id="konta-i-codzienna-obsługa"></a>
## Accounts and daily handling

From AUTH-02 ([D16](../docs/DECISIONS.md)) the player establishes an account on the game page (`https://<domena>/account/register`): name, email address and password (12–128 characters). The account runs immediately; the link from the message confirms the address and only confirmed the address gets the password reset link (`/account/forgot`). Log in the game name or address. Invitations to accounts are not (`--create-invite` and `/api/invitations` deleted). Sessions are saved in PostgreSQL; restart does not invalidate them, reset the password cancels all account sessions.

An account before AUTH-02 gets an address with the operator's command (a confirmation link exits immediately):

```sh
infra/production.sh exec -T server dotnet Wotr.Server.dll --set-email <name> <adres>
infra/production.sh exec -T server dotnet Wotr.Server.dll --rename <current name> <new name>   # AUTH-06
```

<a id="poczta-kont-resend"></a>
### Account Mail (Resend)

The messages are only about the account (address confirmation, password reset). As long as `WOTR_EMAIL_PROVIDER=none`, the server does not send anything (log says missing message, no address and link) and registration and login are working.

1. Resend → Domains → add sender domain (game page domain without `wotr.`) and enter in Cloudflare
   (DNS, without proxy) the following records: DKIM (`resend._domainkey`, TXT), SPF and MX for the
   return subdomain (`send`). Wait for "Verified".
2. Resend → API Keys → key with permission **Sending access** only for this domain. Save without
   showing it in the terminal: `rm -f infra/secrets/resend_api_key; ( umask 333; read -rsp 'Resend
   API key: ' k; echo; printf '%s\n' "$k" > infra/secrets/resend_api_key )`. 0444 rights like
   database secrets: the server in the container works as `app` and the `secrets/` directory (0700)
   protects the file from other host users. Until then, an empty file (`install -m 0444 /dev/null
   infra/secrets/resend_api_key`) is located here because Compose requires a secret file.
3. In `infra/.env`: `WOTR_EMAIL_PROVIDER=resend` and `WOTR_EMAIL_FROM="War of the Ring Online
   <no-reply@<domena>>"` (address from the domain verified in the 1 step), then `infra/production.sh
   up -d --wait`. Bad configuration (no key, sender or https address) stops the server start with
   the message.
4. Attempt: registration of the test account on `/account/register`, message in the mailbox,
   confirmation, `/account/forgot`, new password.

Links lead to `https://WOTR_DOMAIN/account/…` with token in a fragment (`#token=`), so they do not reach nginx logs. Limits: 5 messages on 10 minutes from one IP address, to one type account on 2 minutes and 10 daily, including 90 daily (free Resend plan allows 100). The crossing does not change the site's response; server log warns without addresses.

```sh
docker compose --env-file infra/.env -f infra/compose.yaml ps
docker compose --env-file infra/.env -f infra/compose.yaml logs --tail 100 server proxy
```

Liveness `/health` identifies the protocol, readiness `/health/ready` checks the database with a time limit. Only local probe can use HTTP; other production requests require HTTPS and openly trusted proxy. Logs rotate with 10 MB, up to three files per service. Do not enable the login of the request bodies, SQL with data or headers Authorization. Built-in warning ASP.NET with ephemeric keys Data Protection does not apply to WoTR session: authentication uses random tokens and their hashys in the database, without cookies encrypted with this mechanism.

<a id="kopia-i-odtworzenie"></a>
## Copy and reproduction

```sh
python3 scripts/deploy.py backup infra/backups/2026-10-06-before-update
python3 scripts/deploy.py restore infra/backups/2026-10-06-before-update --database wotr_restore_20261006
```

The copy contains a consistent dump of all the tables, a compressed image of the backend with built-in content, total content/migration and uns secretive configuration. The SHA-256 manifesto is only created after all the files; the directory is incomplete without it. Do not update images/migrations in the process of the copy. The active database is determined by `WOTR_DATABASE` of the container running, by default `wotr`. Backup reads this value from the container, rather than from the modified `.env` disk. Manifesto does not export the database name and the actual ID of the image; the replacement of the container during copying interrupts the acceptance of the copy. Keep separate private secrets, own `.env` and the volume of the certificate; the backup does not export the infrastructure or CA key to the game package.

Restore checks hashe, creates a new base with owner `wotr` and plays in one transaction. Refuses to overwrite the existing database. Error leaves an empty target base for diagnosis; does not switch the running backend. Use a compatible image, if necessary `docker load --input
<kopia>/server-image.tar.gz` and an indicative tag ID from the manifest.

After copy control stop accepting the game by `compose stop server` Keep up to date `.env` privately and set in it `WOTR_DATABASE=wotr_restore_20261006` No secret editing needed: the server only replaces the database name at startup, keeping the authentication data. Allows `wotr` or `wotr_restore_` and 1–40 characters `a-z` , `0-9` , `_` ; the setting is only for the role `wotr` on Compose `database:5432` Start `compose up -d --force-recreate --wait server` Full prefix `docker compose --env-file infra/.env -f infra/compose.yaml` the shortcuts are also valid. Verify the readiness, both accounts, the decision, card Receipt and new command Keep the old base to the end acceptance . Another copy is already to include a restored database and new decisions; check the field `database` In her manifesto. `down -v` for update.

Proposed private group retention: daily copy, 14 daily and 4 weekly; additionally before each update. Copy to encrypted location outside the host, with the key stored separately. Gzip script does not encrypt the data. No schedule or shipment has been launched; host and storage location not yet indicated. Regularly execute a restor to a separate database, not only control the existence of a file.

<a id="aktualizacja-i-cofnięcie"></a>
## Update and Undo

First backup and try a new release on its restored copy. Save the previous image and manifesto; change WOTR_VERSION, build and run the backend. Migrations are performed atomicly under the lock and can be repeated after the error. The `--migrate` option allows you to perform them with a separate one-time run.

Undoing the image itself is only allowed if it reads the current database, snapshot, rules and content. Releases before T37 do not read snapshot v2. If you do not have a match, restore the copy and image; this means losing the decisions approved after this copy, so it requires an informed decision of the administrator. Never initialize a new batch under ID unsupported record.

<a id="automatyczna-próba-lokalna"></a>
## Automatic local test

```sh
python3 -m venv .artifacts/deployment-venv
.artifacts/deployment-venv/bin/pip install -r tests/deployment/requirements.txt
python3 -m unittest discover -s tests/deployment -p 'test_*.py' -v
.artifacts/deployment-venv/bin/python scripts/test-deployment.py
```

The script refuses to operate on a domain other than localhost or publication outside of loopback. Creates test accounts and fixture battle, executes backup/restore, temporarily switches the local server to a restored database and continues battle. Then copies this database to the second archive and plays it by checking the behavior of new decisions. At the end, it returns to the original database. It requires space for both archives and temporary Docker export. The report does not contain passwords, tokens or full party status. The `unittest` tests check the limits of the script without Docker; they do not count the actual backup/restore.

<a id="scenariusze-testowe--test-01"></a>
## Test scenarios — TEST-01

`WOTR_TEST_SCENARIOS=true` in `infra/.env` provides each logged account with a test mode (`Wotr__Testing__Scenarios`). Test parties do not get to the list of games and start, restart or exit deletes them. By default, `false`. Production enables it for every account since D19 (TEST-08, 2026-10-17); starting a scenario counts against the account's room budget (`WOTR_LIMIT_ROOMS`, 429 when exceeded). The change requires the service `server` to restart.
