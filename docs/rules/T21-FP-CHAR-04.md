<a id="t21-fp-char-04--postacie-wolnych-ludów-karty-1924"></a>
# T21-FP-CHAR-04 — characters Free Peoples, card 19–24

<a id="źródła-i-interpretacje"></a>
## Sources and interpretations

Verified Avers Second Edition 19–24 and transcriptions: `docs/sources/base-card-index.json`, `content/base-cards.json`. Basic instruction p. 24–26 (events, figure and recruitment), 29–32 (hits, losses, retreat and siege), 34 (politics). [Pack 03](T21-FP-CHAR-03.md) defines common Sudden Strike and Brave Stand.

Kevin Chapman's clarification: [House also chooses card for impossible recruitment](https://boardgamegeek.com/thread/2423409/article/34758782#34758782) , [Grey Company lists the unit atomicly, Regular goes to loss](https://boardgamegeek.com/thread/3275066/article/44043544#44043544) , [one army of the party concerned in the region](https://boardgamegeek.com/thread/3723845/article/47815179#47815179) , [final correction of Mûmakil time after consultation of designers](https://boardgamegeek.com/thread/2111275/article/35365716#35365716) The latter replaces the previous responses of the same thread: with the initiative 5 still considering the defender as the first; additional hits Nameless Wood count for Mûmakil when FP weapon. Dead Men recruitment interpretation without reserve: [Francesco Nepitello's response from the moderator](https://boardgamegeek.com/thread/74839/article/586333#586333) Almanach v98 p. 15–16 , 39–40 helps to find these sources; overriding remains the obverses card And the final answer of designers.

<a id="wydarzenia"></a>
## Events

Standard cost of Character/Event or legal replacement, charged once. All card rejected. No executive target does not match the condition of play. The loss selection belongs to the owner of the attacked army.

| ID | Condition and order | Boundaries |
| --- | --- | --- |
| 19–21 The Ents Awake | Gandalf White on board and any Companion in Fangorn. 3D6 throw, 4+ hits the Shadow army in Orthanc. After losses, if White is in Fangorn or Rohan region, optionally play one more card Character without dice. | Saruman is not a condition. In the absence of an army, only Saruman present in Orthanc, other Nazgûle/services remain. The destruction of the army removes its Nazgûli and its minions. Hits the garrison or besieging FP. The destruction of the garrison at FP outside immediately returns Stronghold FP. Three dices according to the obverses; in the absence of an army, the results do not result in loss or policy. |
| 22 Dead Men of Dunharrow | Strider/Aragorn in Rohan region, also in siege. Move it from any Companion of the same region to Erech, Lamedon or Pelargir. If there is an army of Shadow: D6 hits, losses, mandatory retreat of survivors. Then optional recruitment of 0–3 Regular Gondor from reserve. | Fangorn/Orthanc is not Rohan. It does not move units or army commanders. Without the army Shadow does not draw, it does not remove the single Nazgûli/Minion. No retreat destroys an army with its commanders. A retreat to the neighboring free region, without empty sediments controlled by FP. Recruitment can recover the occupied settlement; control changes only when the unit is actually recruited. No reserves block ruchu/ataku. |
| 23 House of the Stewards | Boromir in the Gondor region. Recruit one Regular or Elite Gondor in his armii/regionie. Then select two Strategy. | Osgiliath is not Gondor. Free region without settlement allowed. The garrison in siege and FP besieging Gondor captured is allowed. The empty settlement controlled by Shadow and the region with enemy field army do not allow recruitment outside its own besieged garrison. The lack of legal recruitment does not block selection. |
| 24 The Grey Company | Strider/Aragorn with the army FP. Name one Regular of this army on Elite of the same nation with reserve; Regular goes to loss. Then select two Strategy. | Mandatory replacement, if possible, also in siege and outside the regions of the nation. Lack of adequate Elite means to skip the exchange without killing Regular. Atomic replacement: commander and Companion do not die when the replaced unit was last. |

Ents and Dead Men attacks trigger political aftermath of the attack once, before losses, also when Ents fail. Companion Dead Men move trigger normal consequences of arrival according to the region's control at the moment of movement; the very subsequent recovery of settlement is not a re-entry. After the withdrawal of Shadow solves any excess, and then the event returns to the FP recruitment. The FP army limits considered after the event.

House/Grey select both card before rejecting up to six. The Guide Grey option, if appropriate, appears only after the event has been completed and the first rejection; its selection may require further rejection.

<a id="bezpłatna-kontynuacja-entów"></a>
## Free Ends Continuation

The card is the same as normal. The window is based on the public position of the White, also with empty hands or without legal cards; available card are private. The player chooses card from hand or skips. The cost does not include dice, the original die remains considered until the end of the chain. The new Ent can open its own sequel: three Ents and one other card at most. card free does not run Grey's ability for paying it dice Event. The nested sequels do not finish the additional action.

<a id="walka"></a>
## Fight

| ID | Condition | Initiative and effect |
| --- | --- | --- |
| 19 Ents’ Rage | Defending army in Rohan region, Fangorn or Orthanc. | 3: +2 to Combat Roll FP, without bonus for Leader Re-roll. Natural 1 misses, 6 hits. |
| 20 Huorn-dark | How 19. | 3: maximum of two dice Combat Roll Shadow. Does not change special attacks. |
| 21 Nameless Wood | How 19. | 5: if FP has received at least one hit in Combat Roll/Leader Re-roll, add two hits. There is no limit to five hits. Hits of the previous special attack are not enough. |
| 22 Sudden Strike | Commander FP or Companion in battle. | 2: a common program of the 03 package. |
| 23–24 Brave Stand | Companion in battle. | 3: a common program of the 03 package. |

The condition of 19–21 concerns the region of the defender regardless of which party is attacking.

<a id="kontynuacje-widoczność-i-ui"></a>
## Continuation, visibility and UI

Losses of events benefit from the same rule degradation/elimination and the same choice figures a battle , but have a separate state frame: region, garrison, owner, figure , source, hits and results. They do not create an apparent battle neither windows cards fight. Play does not repeat the throw. Server rejects figure outside the target, excess/niedob loss, foreign command And illegal targets.

The Dead Men objective and the optional Companion create one motion project with one approval. Strider/Aragorn is mandatory. The map is used to select the location and retreat. Recruitment to three Regular uses figures chips, with the option of leaving empty selection; without the number field. House chooses the type of figure, Grey unit to replace. The preview does not draw, shows the cost, movement/exchanges and the public effects; the choices available and private footprint are given only to the right player.

<a id="plan-sprawdzeń"></a>
## Verification plan

Each half of all six cards: legality, cost, limits and moment. Enty: zero/multiple hits, garrison/field, single Saruman, others Minion, politics, gaining, free chain and privacy. Dead Men: both versions of the king, besieged beginning, group/targets, casualties/degradation, legal and impossible retreat, overflow, 0–3 recruitment and recovery settlement. House/Grey: reserves, nations, control, atomicity, two card, hand limits and Grey. Fight: Region of Defender, threshold, initiative, special/normal hits and limits.

Save/restart/retry each new decision family and projections of both players. Regression of shared service casualties/recruitment , the real Unity with the server, control of the lack of scrolling and cutting trays. Results added after execution; this specification is not proof of credit.
