# Bot authority and player API

BOT-16 implements the server boundary for the private service described in
[STRATEGIC-BOTS](STRATEGIC-BOTS.md). CPU room admission (BOT-12), assignment
claim/lease delivery (BOT-17) and the remote worker/journal (BOT-18) remain separate.
There is no public endpoint that creates a grant or accepts a participant ID.

## Identities and credentials

The operator enables `Wotr:Bots:Enabled` and configures `ServiceId`,
`ControlTokenHash` (SHA-256 of a randomly generated `wbc1_` token followed by
43 base64url characters), `MinimumVersion` (default `0.1.0`),
`SeatLifetimeSeconds` (5–900, default 300), and `Features` (comma-separated,
default `base`). Disabled is the default. Only the hash is stored in game-server
configuration; the actual control token belongs in the bot service's secret store.

The control identity has no player side or game authority. It can read shared
runtime metadata and will use explicitly marked assignment operations in BOT-17.
A distinct random `wbs1_` seat token resolves to one persisted grant: service,
non-login technical participant, room and/or match, side, generation and lease.
The participant cannot log in as a human or administer the server. Only token
hashes are persisted. No credential is returned in a human room, match or history
response. Credential/authority string representations redact secrets.

`BotCredentialStore.Register` is a trusted lifecycle seam, including an overload
using the caller's PostgreSQL transaction. BOT-12/BOT-17 must create the reserved
seat and grant atomically and bind the started match under the room lock. Issuance
invalidates previous tokens for that grant; reassignment increments generation;
revocation permanently closes the grant. Neither renewal nor issuance can revive
expired/revoked authority. An authenticated token must still map to its current
side in the room/match. One grant cannot represent a human participant.

## Explicit operation allowlist

All unmarked methods/routes are forbidden for both bot identities. Ordinary
player handlers, projections and rule validation handle the allowed operations.

| Method and route | Identity/scope |
| --- | --- |
| `GET /api/runtime` | Any authenticated human, control or seat identity; identical nonsecret response |
| `GET /api/rooms/{id}` | Assigned room |
| `PUT /api/rooms/{id}/ready` | Assigned room and own side; ordinary expected-version validation |
| `GET /api/matches/{id}` | Assigned match; own projection and options |
| `POST /api/matches/{id}/preview` | Assigned match; ordinary side-safe preview |
| `POST /api/matches/{id}/commands` | Assigned match; ordinary command and receipt semantics |
| `GET /api/matches/{id}/receipts/{commandId}` | Assigned match; own receipts |
| `GET /api/matches/{id}/actions` and `/actions/{actionId}` | Assigned match; ordinary visible history and pagination |
| `GET /api/matches/{id}/battles/{battleId}` | Assigned match; ordinary report |
| `GET /api/matches/{id}/updates` | Assigned match; ordinary WebSocket revision/private-version notifications |
| `POST /api/bot/seat/renew` | Seat identity; extends the current token, never beyond its grant lease |

Renewal returns `{ "expiresAt": "<UTC timestamp>" }`, with `Cache-Control:
no-store`. Control and human identities cannot renew a seat. Observer routes,
perspective/side query overrides, foreign matches/rooms, replay/full-save export,
account/admin operations, participant removal, room start and match deletion are
forbidden (`403`). Missing/expired/revoked credentials return `401`. Gameplay
rejections retain the ordinary player's receipt/status/error semantics.
Accepted moves preserve receipts; ordinary retention can remove obsolete
rejection receipts after revision advances. Workers must follow that existing
contract when reconciling their private journal.

Bot room reads and readiness do not refresh the human presence timestamp. They
cannot keep an abandoned room alive. Readiness also rechecks configuration
coverage under the room lock, including a configuration changed while it waited.

## Fencing and streams

Each request authenticates against the current grant, credential expiry and lease.
Command execution locks the match first, then the grant and credential in that
order, and rechecks participant, side, scope, generation, revocation and both
deadlines **before receipt lookup or mutation**. Room readiness follows the same
order starting with the room lock. PostgreSQL `clock_timestamp()` is sampled
after the locks: transaction-start `now()` cannot authorize a request delayed
beyond expiry. The authority locks remain held through commit. A revocation or
generation change completed before this check prevents the queued mutation; a
mutation that already holds authority commits before revocation takes effect.
Future assignment code must preserve this lock order.

Every stream notification, including the five-second heartbeat, resolves the
token again and compares its captured grant/generation/side/match. Lost authority
closes the stream with WebSocket status `1008`. A missed notification never grants
permission to assume a command result; workers recover through the player view,
visible history and ordinary receipt endpoint. Game state, receipts and RNG remain
owned by the game server; private plans and outgoing journals stay in the bot store.

## Independent compatibility

Bot credentials are authenticated **before** the server selects bot compatibility.
Bot-looking headers alone never select it or bypass human/Unity gates. Bot
requests and WebSocket handshakes supply exactly one value for each header:

| Header | Supported value |
| --- | --- |
| `X-Wotr-Bot-Version` | Valid application version at least `Wotr:Bots:MinimumVersion` |
| `X-Wotr-Bot-Protocol` | `1` |
| `X-Wotr-Protocol-Version` | Current `Protocol.Version` (`2`) |
| `X-Wotr-Rules-Version` | Current `Protocol.RulesVersion` |
| `X-Wotr-Content-Version` | Current `Protocol.ContentVersion` |

Missing, duplicate or incompatible declarations return `409 BotUpdateRequired`
with `Cache-Control: no-store`. Bot application versions are independent of the
Unity minimum. Trusted operator-configured feature coverage, not a caller header,
gates the assigned configuration (`409 BotConfigurationUnsupported`). Supported
feature names are `base`, `lords`, `kings`, `fate`, `council`, `dismay`,
`visible-corruption`, `sequential-corruption`, `removing-ruler-tokens`, `new-cities`.
Enabling a name is an operator assertion of accepted bot coverage, not evidence
that a strategy exists or permission to bypass room release gates.

Shared `GET /api/runtime` returns `protocolVersion`, `rulesVersion`,
`contentVersion`, `botProtocolVersion`, and `implementedFeatures` (game-server
features, not bot readiness). Values are nonsecret and identical for humans and
bots. It follows each identity's normal compatibility gate; incompatible clients
must update from their pinned deployment metadata. Existing player DTOs and their
package version are unchanged.

## Integration fixtures

`BotSeatAuthorizationTests`, `BotPlayerApiParityTests`,
`BotCredentialRevocationTests` and `BotCompatibilityTests` exercise the ordinary
HTTP/WebSocket handlers with isolated PostgreSQL. Their technical participant's
human session is created by trusted test seeding solely to compare identical
same-side serialized responses; there is no production login path for it.
BOT-12/BOT-17/BOT-18 should reuse these scope/denial/race fixtures when adding
admission, assignment delivery and the private worker.
