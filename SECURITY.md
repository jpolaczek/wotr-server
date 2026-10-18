# Security policy

War of the Ring Online is a hobby project run by one person on a home server. Reports are welcome and are
handled as quickly as that allows.

## Reporting a vulnerability

Use **Report a vulnerability** on the Security tab of the affected repository:

- [jpolaczek/wotr-server](https://github.com/jpolaczek/wotr-server/security/advisories/new) — server, API, game site,
  admin panel, database, deployment and the release tooling;
- [jpolaczek/wotr-client](https://github.com/jpolaczek/wotr-client/security/advisories/new) — the Unity game, its
  updater and installers.

The report stays private between you and the maintainer until a fix is published. Please do not open a public issue
for a vulnerability. Expect a first answer within 7 days.

In scope: the official endpoints below, the current release of the game and the `main` branch of both repositories.
Out of scope: denial of service by volume, social engineering, the operator's home network beyond the published ports,
and findings that need an already compromised computer.

## Official endpoints

| Purpose | Address |
| --- | --- |
| Game site, account pages, game API and update catalog | `https://wotr.vibestudios.org` |
| Admin panel (operator only) | `https://wotr.vibestudios.org/admin` |
| Installers, update packages and signed release manifests | `https://downloads.vibestudios.org:50443/` |
| Source code | `https://github.com/jpolaczek/wotr-server`, `https://github.com/jpolaczek/wotr-client` |

The game never asks for your password anywhere else. A copy of the game built from source can point at other
servers; only the addresses above are run by the maintainer.

## Release signing key

Every update package is named, with its size and SHA-256, in a release manifest signed with this Ed25519 key. The
game refuses a manifest signed by any other key and a package whose hash differs.

| | |
| --- | --- |
| Algorithm | Ed25519 |
| Key id | `95ffc109e1bb9b15` |
| Public key (base64) | `assXbu7Zbo494JMal54lWXX3LdodwnzpOiwz40RXDG4=` |
| SHA-256 of the raw public key | `95ffc109e1bb9b15710cb7ba298fb959751630ae8d96ab3e9dcab9ddadd8b5d3` |

The key id is the first 8 bytes of that SHA-256. The private key stays on the release machine, outside both
repositories and the download host. `python3 scripts/check_release_key.py` compares this table with the key the
game pins (`wotr-client:unity/Wotr.Client/Assets/Wotr/Runtime/Networking/ReleasePublisher.cs`).

Installers are not signed with Apple or Microsoft certificates (decision D13, task DIST-12); the
[player guide](https://github.com/jpolaczek/wotr-client/blob/main/docs/release/PLAYER-GUIDE.md) explains the system
warning on first installation.

## Accounts and passwords

- Passwords are stored only as PBKDF2 hashes (ASP.NET Core Identity, HMAC-SHA512, 600 000 iterations, a random salt
  per password). Session tokens are stored as SHA-256 hashes.
- Sign-ins are limited per network address (20 per minute) and per account name: after 10 failed sign-ins within
  15 minutes the name is locked for 15 minutes, from every address, even with the right password
  ([details](docs/server/T37-HARDENING.md#sign-in-lock)).
- Email addresses are used only for confirming the address and resetting the password.

What the service stores and how to ask for deletion: [privacy note](docs/PRIVACY.md).
