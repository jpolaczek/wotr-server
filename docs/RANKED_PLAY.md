# Ranked play

Owner's decision of 2026-11-05 ([R35](REQUIREMENTS.md#ranked-play)). Status: planned, not implemented. This document
holds the rules of the mode, which bind the server and the client. The client's interface is described in
`wotr-client:docs/ui/RANKED_UX.md`.

A player searches for an opponent with a few choices. The server pairs two compatible players and starts the game at
once. The game cannot be left, every decision runs a clock, and the result changes both players' ratings. One rating
belongs to each account, whatever the expansions and faction.

## The search

A search names three kinds of choice. The window offers nothing else: no name, visibility or variant.

| Choice | Values | Default |
|---|---|---|
| Each released expansion (Lords, Kings, Fate) | Off, Allow, Enforce | Allow |
| Faction | None, Free Peoples, Shadow; with a faction, Allow or Enforce | None |
| Each time preset | Off, Allow, Enforce | Standard Allow, the others Off |

- **Expansions.** Off means it must not be in the game, Allow means it may be, Enforce means it must be. Only modules
  the server has released (`ReleaseScope`) are offered. The base game is always in.
- **Faction.** A click on the chosen faction releases it. With a faction chosen, Allow is a preference and Enforce a
  requirement. With none, the switch is disabled. The faction always starts unselected.
- **Time presets.** At least one preset is not Off. Enforcing one lowers any other enforced preset to Allow.

| Preset | Big decision | Small decision |
|---|---|---|
| Fast | 1:30 | 0:20 |
| Standard | 3:00 | 0:30 |
| Long | 5:00 | 1:00 |

## Compatibility and the resulting game

Two searches are compatible when all three of these hold.

1. **Expansions.** No expansion is enforced by one player and off for the other. An expansion is in the game if
   either player enforces it, or both allow it. Otherwise it is out.
2. **Faction.** The two players do not both enforce the same faction. A player who enforces a faction gets it. Then
   preferences are met. If both prefer the same faction, or neither chose one, the server's random source decides.
3. **Time.** Both players must accept at least one preset (neither has it Off), and two different enforced presets
   are incompatible. An enforced preset is used if the other player accepts it. Otherwise the longest shared preset
   is used, because running out of time loses the game.

The game is the default game of its expansions: `ConfigurationRules.Normalize` of the chosen modules. Council of
Rivendell, Visible Corruption Tiles, Sequential Corruption Tile Draw, Removing Ruler Tokens and New Cities are off.
Dismay follows its existing default (on with Lords and Kings).

## Pairing

- One search per account. The server holds it while the client polls every 2 s, and it stays eligible for 6 s after
  the last poll, so a closed client is never paired.
- Among compatible searches, those that meet both players' faction preferences come first. Then the closest rating
  wins, within a window of 150 points plus 50 per 10 s of the longer wait, unlimited after 2 minutes. W3Champions
  loosens its range the same way so that a player always finds a match. Compatibility is never loosened.
- There is no accept step: a pair enters the game at once. Each client presents the opponent's avatar, name and
  rounded rating for 3 seconds before the board opens (owner, 2026-11-05), within the 15 s the first decision gains.
  The search's answer reveals nothing else about the opponent.
- A player with an active ranked match cannot search; the answer names the match. Normal games may continue
  alongside.
- Both clients must satisfy the module gate (`ModuleClientPolicy`) for the resulting game, as for any table.

## The ranked match

- It is a normal match with its own ledger kind, started by the server, with an unlisted room so that game chat
  works. It has no waiting room: this is the one exception to R04's host-controlled start.
- One active ranked match per account.
- No observers (they see both hands, R33), no CPU seats. A test or demo match is never ranked.

## Leaving

- Leaving is an explicit concession. Concede in the board menu, and Quit game during a ranked match, ask for a
  confirmation and then end the game at once as a loss.
- Deleting the match and leaving its room are refused for ranked matches, so neither can escape the loss.
- A finished ranked game stands in My games' Finished tab as a result only.
- A lost connection is not leaving (see Reconnecting).

## Clocks

The server alone keeps the clocks and decides. The engine holds no time.

- Every new decision, meaning a change of decision id or owner, starts the clock of that decision's owner, with no
  credit carried over. A command that leaves the decision unchanged does not restart it.
- Running out of time loses the game for the decision's owner.
- **Big limit:** FellowshipPhase, AllocateHunt, Action, ActionSequence, ArmyEvent, CharacterEvent, CouncilVersions,
  CouncilHomelands, CouncilToken.
- **Small limit:** DiscardCards, Reaction, CombatCard, Losses, HuntDamage, CorruptionTile, RemoveExcess,
  EffectChoice, FellowshipRoute, Guide, CompanionSeparation, HuntReroll, BattleReroll, BattleContinue,
  BattleRetreat, BattleAdvance, StrongholdDefense, SiegeContinuation, SpecialDieChoice, KeepActionDie, RulerDiscard,
  Dismay.
- A new decision kind must be classified before it can be used in a ranked match.
- The server's deadline is 3 s beyond the limit the client shows. This absorbs latency and the opponent's
  presentation, so a player never loses while their clock still shows time.
- The first decision of the match gains 15 s for loading.
- Downtime of the server shifts every running deadline by its length, so a deployment loses nobody a game.
- A command arriving after the deadline is refused (`TimeExpired`). A retried command that was already applied returns
  its receipt even after the deadline or during a pause.

**Warning.** 10 s before the limit, once per decision, the decision's owner hears a warning: with the 30 s limit that
is 20 s in. It passes the background-window filter and respects mute. The clock also turns red, so a muted player has
the same warning.

## Pause

- Each player may pause 3 times per match. A pause is spent when it starts.
- Either player may unpause, the one who paused too. Pausing an already paused match is refused and spends nothing.
- While paused, the clock is frozen, game commands are refused (`MatchPaused`), and chat and concession still work.
  Unpausing restores the exact remaining time.
- A pause ends by itself after 10 minutes, so a match whose players are both gone cannot stay frozen.
- A pause is not a rules event: it changes no game state and does not appear in the replay.

## Reconnecting

A lost connection costs only the time it lasts. Nothing on the server locks a seated player out of an active ranked
match. Normal matches keep today's behaviour.

| Today, for correspondence | In a ranked match |
|---|---|
| The client waits for a message with no timeout, although the server sends a notice at least every 5 s. | No message for 8 s aborts the socket and starts reconnecting. |
| Nothing tracks whether a seated player is connected. | The client sends a heartbeat every 2 s. A seat with no socket heard within 8 s is offline, and the opponent sees it. Presence is held in memory and changes no clock, revision or replay. |
| Retries back off from 1 s to 15 s, `Retry-After` may hold them for 60 s, and an HTTP request may take 15 s. | A retry every second with jitter; each attempt and each command bounded at 5 s; `Retry-After` capped at 5 s. |
| A dead socket keeps one of the account's 4 update sockets, and the return gets 429. | A seated player's socket for this match always gets a slot, evicting that account's oldest. A silent socket closes after 8 s. |
| Sessions last a fixed 30 days. | Starting a ranked match extends both sessions to at least 7 days ahead. |
| A client below the catalog's minimum version is refused everywhere. | During an active match its seated players are exempt from the minimum for that match, for finding it and for signing in. The protocol minimum still applies. The search applies the minimum as usual. |
| The board plays the opponent's missed steps on return. | The return applies the view quietly; the action log holds what happened. |

These already work and are kept: the unsent draft stays and can be edited offline with commit disabled, and an
uncertain commit is recovered through its receipt by command id, so it applies exactly once.

- The clock keeps counting locally while offline, and the warning still sounds. On return it re-syncs. The server
  neither pauses nor extends; the opponent may spend a pause of their own as a courtesy.
- A game that ended while the player was away shows its end screen, reason and rating on return. After a relaunch it
  stands in the Finished tab.
- After a crash the client asks for the password, then opens the active match directly. It stores no sign-in, so a
  relaunch during a short decision will probably lose the game; the owner accepted this for the first version.
- A pairing survives a dropped connection: the next poll, or signing in, finds the match. A search that lapsed tells
  the player it stopped.
- A second device opens the same match. A seat is online while either client is heard.
- A client below the minimum can still read how its match ended for an hour after it ended, since the seat goes with
  the last command and the end screen still has to load. After that, as for any other request, it must update.
- After a server restart, presence starts empty and gives seats a short grace before showing anyone offline.
- A player who never reaches the board after pairing takes an ordinary loss when the clock runs out. A void game
  would let a player dodge an opponent by killing the client.

## Rating

W3Champions states that its MMR is "based on the Glicko2 rating system with some improvements" and keeps the exact
method private. This mode uses published Glicko-2 (Glickman), one game as one rating period, both players updated
from each other's values before the game.

| Parameter | Value |
|---|---|
| Starting rating | 1500 |
| Starting deviation | 350 |
| Starting volatility | 0.06 |
| Tau | 0.5 |
| Lowest deviation | 45 |

- Ratings are shown rounded. There are no seasons, no growth of deviation with inactivity, and no Ranking Points.
- A concession and a timeout count as an ordinary loss. War of the Ring has no draws.
- A finished game updates both ratings exactly once. Normal, test and demo games never change a rating.
- Checked against Glickman's example (1464.05, deviation 151.52). For a new player alternating wins and losses
  against an established 1500 (deviation 80), one game moves the rating by about 175 points at game 1, 35 at game 10,
  20 at game 20 and 13 at game 40.

## Leaderboard

The top 25 by rating, among accounts with at least one finished ranked game. Ties go to the player with more games,
then by account id. Below the list the player sees their own rank and rating. There is no minimum number of games.

## The owner's answers of 2026-11-05

| Question | Answer |
|---|---|
| When both players allow an expansion, is it in? Default state? | In; default Allow. |
| Time presets | Fast, Standard, Long, as above. |
| Warning | 10 s before the limit. |
| A dropped connection | A loss only when the clock runs out; reconnecting is designed above. |
| Big and small decisions | The lists above. |
| A pause's limit | 10 minutes, ending by itself. |
| A minimum of games on the leaderboard | None for now. |
| Stay signed in after a crash | Not in the first version. |
| A player who never reaches the board | An ordinary loss. |

## Sources

- [W3Champions Stack FAQ](https://w3champions.atlassian.net/wiki/spaces/AV/pages/34635777/Stack+FAQ), read on
  2026-11-05: Glicko-2 with private improvements, rating deviation shrinking within about 10 games, a range that
  loosens with waiting.
- [Glickman, Example of the Glicko-2 system](http://www.glicko.net/glicko/glicko2.pdf): the algorithm and the worked
  example.
