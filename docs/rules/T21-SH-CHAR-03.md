<a id="t21-sh-char-03--character-cienia-1318"></a>
# T21-SH-CHAR-03 — Character Shadow 13–18

<a id="źródła-i-kolejność"></a>
## Sources and order

Confirmed Avers Second Edition with [SRC-01](../sources/SRC-01-AUDIT.md) , clippings `base.shadow.character.13..18` In `.artifacts/source-audit` ; both applications, numbers and initiatives. BASE p. 22–23 , 28–29 , 36–43 ; card Hobbits and Whites with SRC-02 Almanach s. 25–28 , 42/45 is used to search for cuts, does not override instructions. Directly read statements by Kevin Chapman: [Breaking a Reductions Hunt](https://boardgamegeek.com/thread/2744340/article/38676702#38676702) , [Balrog on Staying in Moria](https://boardgamegeek.com/thread/2913481/article/40587157#40587157) , [ban on disclosure after declaration in an unreachable sanctuary](https://boardgamegeek.com/thread/2441679/article/35035573#35035573) , [Daring and already dedicated to White Rider](https://boardgamegeek.com/thread/2438936/article/45505470#45505470) . Copy of replies API `sh-char03-thread-*.json` .

BASE p. 43 (also confirmed version of 2.3) explicitly requires the full settlement of the declaration before entering the Mordor track. Therefore, the declaration, Balrog and the called fortresses precede the separate selection of Enter Mordor and the re-creation of the pool. We do not accept with unsupported footnote the sentence of almanac that the self-declaration of the gate previously carries Fellowship onto the track. In addition, the difference in the 02 package is still valid for voluntary reroll: the original BASE has priority. Control of the new version remains in SRC-08/GAP-FAQ.

<a id="wydarzenia"></a>
## Events

All use the usual cost of Character/Event Shadow. The condition checked before the cost, random data only on the server; preview does not draw. Exceptions do not change the prior order of protection, update Guide and victory.

| Card | Condition and full implementation |
| --- | --- |
| 13 Lure of the Ring | Fellowship revealed, also Mordor. Gollum: without draw +1 Corruption. Otherwise, the random Companion, then FP selects Corruption equal to the level or elimination of the indicated character. Hobbit can use Take Them Alive; outside Mordor, a simple return, on the removal track. The composition changes Guide. It is not Hunt, no protection Hunt and Worn. |
| 14 The Breaking of the Fellowship | Fellowship revealed. Gollum: +1 instead of selection. Selection is subject to Staff/Mithril, but not to injury reduction, Gollum or Worm. Eye and special FP ignored after exchange. FP selects exactly the min(kaf, number of Companion); Shelob throws the value. All are sent to Fellowship region only, without movement; on Mordor track they are removed. Change of Guide, without Zepsucia/Reveal/Stop. Tile returns according to the usual procedure after action. |
| 15 Worn with Sorrow and Toil | card on the table. After the actual victim Hunt: optional response Shadow, random Character from the hand FP or selected Character from the table. Protection reducing damage to zero and leaving Guide-Hobbita are not the victim. Randomly hit Hobbit remains the victim despite Take Them Alive. card remains after the reaction and can work again with another kaflu. Expiration immediately at the declaration in the unattainable hometown/Stronghold FP, before Balrog. |
| 16 Flocks of Crebain | card on the table with the same expiration as Worn. Shadow can reject it before the Hunt: +1 to the throw and all its reroll. It concerns normal movement and Nazgûl Strike. Without response at the same selection of tiles and on the Mordor track. Refusal retains card. Results 1 still miss. |
| 17 Balrog of Moria | card on the table. After a complete declaration or disclosure, the route covering Moria (also remains and won Moria) opens an optional response Shadow, before the additional Stronghold. The recoil gives a tile with ignore of the Eye, the rest as effective Hunt. After the declaration in the native city/Stronghold FP ignores the Reveal icon also with the gained settlement; the unattached sanctuary additionally blocks the voluntary disclosure of Gollum. If Balrog reveals after a declaration elsewhere, further fortresses result from the preserved declaration route, not from the final region itself. Alternatively, card from the table is available as one of cards combats in the permitted battle. The use of one application consumes both. |
| 18 The Lidless Eye | Selection from zero to three of your own unreleased dice. The selected die receives Eye and immediately reaches Hunt. die paying card is Resolving and cannot be selected; foreign, released, deleted, present in Hunt and repeated ID are rejected. The left dice retains status and origin. |

Worm does not reveal types cards from hand: the possibility of a random Character depends only on the open presence cards A test without any Character does not remove Strategy. ID rejected card remains private FP , Shadow He sees only a real change in numbers. card The table and their removal are public. Guide is selected after the change in composition, and the Worm settles against the ability of the new Guide .

<a id="walka"></a>
## Fight

- 13/16: Joint They Are Territorial (4); 14: Joint Dread and Despair (3), without changing costs.
- 15/18 Words of Power (1): Nazgûl's presence, including WK under White Rider; the choice of the
  live Companion of the participating opponent in battle. Until the end of the round, no Leadership
  and no ability. Captain does not increase strength; the cancelled White Rider restores Leadership
  Nazgûli, but Gandalf still has its own cancelled. The new round restores the abilities and
  previous White Rider of the entire battle. The presence itself/level of the character does not
  disappear: Blade, Brave Stand and Heroic Death retain the right conditions. Andúril/Sudden Strike
  uses the current Leadership. The gift at the initiative of 0 can cancel card, except for the cost
  impossible after the White Rider.
- 17 Durin’s Bane (2): Army defender within range of no more than two legal edges from Moria,
  including Moria; black boundaries are not edges. Three special dice 4+, immediate losses before a
  normal throw. Without modifiers/overcasts of ordinary Combat Roll. Draw of Sudden Strike/Charge
  settles defender; destruction of the army ends battle before the attack.

Balrog from the table has the same secret choice as card from hand. During the qualifying battle is a common, publicly known additional candidate, but the correct origin of the commitment remains hidden for joint disclosure. The number of available cards does not allow distinguishing Balrog from another card Character. card unpaid fight remains on the table after battle; it does not become at the expense of the discard "from hand" and does not take place in the limit after battle. Sorcererer works after this play. Any excess after selection of Sorcer requires a discard only at the end of battle.

<a id="stan-i-ergonomia"></a>
## Condition and ergonomics

Separate serialized stages before and after the victim; no re-RNG after restart. Selection to separate has a clear purpose other than Hunt injuries. Declaration stores the selected route and returns to the phase to the end of nested reactions. Round stores ID Companion with cancelled capabilities. All new enums added at the end.

Lure is two short effects with portrait in context. Breaking chooses portraits with the shown target region; no number or no existing route is entered. Lidless selects the existing dice and shows the transition to Hunt in draft, one approval also for zero. Permanent reactions cards in a fixed tray, card from the table available in the combat selection. No mandatory scrolling; the map and inspection can be used pending.

<a id="weryfikacja"></a>
## Verification

Conditions, cost, actor, window, Gollum/Hobbits/Mordor , Staff/Mithril/protection , public and private effects of the Worn, extinction before Balrog, order of fortresses, current route and no edges through the mountains. Both applications of all cards , both roles in battle , White Rider/Captain/And úril/ Daring/Charge and return after the round. Two Mordor gate scenarios check the blocked entrance during Balroga and recreate the prepared tile only after a separate entry decision. PostgreSQL : reboot and reboot in each new type of decision. Unity: actual server and both clients, local preview/ undo/recovery and 1366/1920 × 100/125%.

2026-10-05: full regression domains/HTTP/PostgreSQL 1090/1090, final set Shadow03 after adding two 80/80. gate scenarios Full PlayMode 96/96, focused 9/9 including 32 non-window and text fields; eight of the most tight screens were viewed. Test detected a Vorn overflow of 2,4 px at 125%; short titles with inspection removed the problem. The source of the first two timesouts was a click before rewinding the view, corrected by waiting the test for the new panel. Accurate editor and command results: [TESTING](../TESTING.md). No manual user tests or measurement of FPS in this task were performed.
