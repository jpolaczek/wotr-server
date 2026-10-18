# Privacy note

War of the Ring Online is run by one person for players who create an account. This note says what the service
keeps and why. It covers the official endpoints listed in [SECURITY](../SECURITY.md).

## What is stored on the server

| Data | Why | How long |
| --- | --- | --- |
| Account name | Signing in; shown to other players in rooms, the lobby, games and replays | Until the account is deleted |
| Email address | Confirming the address and resetting the password; never shown to other players | Until the account is deleted |
| Password | Only as a PBKDF2 hash (HMAC-SHA512, 600 000 iterations, random salt); the password itself is never stored | Until the account is deleted |
| Sessions | Keeping you signed in; stored as SHA-256 hashes of the tokens | Until they expire or you sign out |
| Games, moves, room chat, replays | Playing, resuming and replaying games | While the game exists |
| Lobby chat | Passed on to the players on the Open games screen; held in the server's memory only, never stored | About a minute |
| Technical logs | Errors, sign-in refusals, admin panel use; they can contain network addresses | Rotated after about 30 MB per service |
| Failed sign-in counts | The temporary sign-in lock; kept in memory only | 15 minutes, or until the server restarts |

The game client talks only to the game server and the downloads host. It has no analytics, advertising or third-party
tracking. Settings, local saves and downloaded update packages stay on your computer.

## Services in between

- Traffic to `wotr.vibestudios.org` passes through Cloudflare (Cloudflare Tunnel), which sees it as any proxy does.
- Account emails are sent through Resend.
- The downloads host is reached directly over HTTPS.

## Deleting an account

Open an issue in [wotr-server](https://github.com/jpolaczek/wotr-server/issues) titled "Account deletion" with only your
account name (no email address or other details). The maintainer confirms through the account's email address and
then deletes the account: name, email address, password hash and sessions. Replays that other players saved on their
own computers cannot be recalled.
