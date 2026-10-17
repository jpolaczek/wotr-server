<a id="t20--postacie-bazowe"></a>
# T20 — Base characters

Status of execution: T20, register remains the source of status. The document defines the behavior and interpretation of sources; the results of acceptance are found in [TESTING](../TESTING.md).

<a id="źródła-i-kolejność"></a>
## Sources and order

Statistics and capabilities: [14 obverses SRC-02](../sources/base-character-index.json), [audit](../sources/SRC-02-AUDIT.md), BASE p. 19–21, 24–25, 29–32, 36–41. The instructions define recovery of dice, movement, battle and Hunt; the text of a specific card specifies an exception.

The timing of the selection confirms the answers of Kevin Chapman (Krieghund, developer), read 2026-10-05 by public API BoardGameGeek: [Gandalf: Guide at the end of action](https://boardgamegeek.com/thread/1284330/article/25097856#25097856), [change Guide in this action](https://boardgamegeek.com/thread/1284330/article/25109327#25109327), [End of use cards/kocs](https://boardgamegeek.com/thread/1384018/article/19480575#19480575), [Witch-king must survive and continue to participate](https://boardgamegeek.com/thread/1714473/article/24873256#24873256), [Scouts and canceled card continue to run Sorcerer](https://boardgamegeek.com/thread/1284330/article/28385921#28385921). We do not consider the engine of another implementation as a source of rules.

Hobbits: [publisher explanation for second edition](https://www.aresgames.eu/1647) confirms the separation of the normal loss from the ability of subsequent guides and the immediate return after elimination. The group separated against the same tile uses one move from the position of the figurine and progress before disclosure, according to the rule of group separation (BASE p. 39); a separate additional tile begins a new group. Almanac's auxiliary Almanac indicates these cases, but does not replace obverses and primary sources.

<a id="figura-wersja-i-cykl-życia"></a>
## figure, version and life cycle

`CharacterState.Id` identifies figure, `DefinitionId` version card. Gandalf and Strider retain ID at conversion, and the restored Gandalf uses the same figure. Eliminated Minion remains in the state as removed; cannot enter again. The entry history of any Minion does not disappear after his death.

Entry requires action your own website and a specific available dice Gandalf White and Aragon require the Will of the West result; Minion Muster. Validation and projection use one entry planer. The effect uses version statistics, location/garoni, policy activation and additional number dice the next recovery. The current jackpot does not change at the entry or elimination.

| Version | Condition / Place |
| --- | --- |
| Gandalf White | Grey outside Fellowship or eliminated, any Minion ever entered. Replaces the living Grey in his place; after the death of Fangorn or unoccupied Stronghold Elfs, also besieged. |
| Aragorn | Strider in the unoccupied Minas Tirith, Dol Amroth or Pelargir. Gondor doesn't have to be At War. |
| Saruman | Isengard At War, unoccupied Orthanc; may enter his own besieged garrison. |
| Witch-king | Sauron and at least one FP nation At War. Army Shadow containing Sauron's army unit, also under siege garrison. Activates all FP nations. |
| Mouth of Sauron | Fellowship on the Mordor track or all the FP At War nations. Unoccupied Stronghold Saurona, also besieged; does not require Saurona At War. |

<a id="akcje-i-ruch"></a>
## action and traffic

Gimli, Boromir and Legolas can use any available result dice It's a distinct ability from the activation of the nation by coming. Guide hide Fellowship any result dice.

Shadowfax gives 4 range to Gandalf White alone or with one Hobbit, without changing the level of 3. With a larger group there is a common highest level. Saruman cannot leave Orthanc, also with the Army movement, reverse and progress; he can command an attack with Orthanc. Nazgûle and Witch-king fly to any region outside Stronghold controlled by FP, which he does not obsess of Shadow. They can enter and exit from their own siege. Mouth moves the path to 3 regions, ignoring enemy armies, but cannot exceed its own siege nor enter the unobed Stronghold controlled by FP.

Voice of Saruman requires a living Saruman and a controlled, unobsessed Orthanc. Variants: one regular in available settlement Isengard or the conversion of two regulars to Orthanc into two elites. settlement ; uses real reserves. When swapping, performs a maximum of two pairs, a limited number of regulars in Orthanc and elites in the reserve. This is the application of the recruitment restrictions to available figures ( BASE p. 26 ) for alternative use of Muster card Saruman; not a separate errata nor the possibility of giving up another available pair. Example: two regulars and one available elite gives one replacement; zero elite disables this variant. `VoiceUpgradeUsesOnlyAvailableReplacementPairs` covers both borders and no figures The selected seats at the reserve shortage are the player's decision. The army's excess uses a simple permanent reduction window.

Messenger of the Dark Tower allows once on turn to pay the Army dice score Buster: two Army movements, one Army attack or event. The choice of the capability is an open cost mode in the same command; the server does not transform dice into an independent free action. The number used turn remains in the record.

<a id="bitwa-i-okna-reakcji"></a>
## battle and reaction windows

Captain of the West adds one to the participating army force, still up to 5 dice. It does not count the rear guard and does not claim Gandalf White or Hobbits. Alive Saruman gives all elites of Isengard Leadership for movement and combat; the effect disappears immediately after its elimination.

White Rider is an optional choice once at the beginning of battle, after deciding to shelter, before the first card. Gandalf's resignation from Leadership abolishes Leadership all Nazgùli, including Witch-king, throughout battle. It does not remove their presence or other abilities.

The Sorcerer opens optional selection only after the first round, if Witch-king participates, lives and Shadow used card combat. First, losses, decisions of Continuation/retreat and the reduction of the elite before the next round of siege. Only then will card from the matching deck, before progress after the victory or choosing the next cards. Rejecting card and closing the round cannot occur before these decisions.

Gandalf Grey opens an optional selection from the matching deck after the event has been paid for by Event. The Guide condition works at the end, also when it was during this action. Nested battle or the decision must be completed before the window. The selection respects the limit of the hand, visibility and invalidation of the consent to automatically omit cards.

<a id="polowanie-i-powrót-hobbitów"></a>
## Hunt and Hobbits Return

The capabilities use the public window Guide. Hobbit can separate when remaining injuries; reduces them by one and opens the selection of the next Guide. It does not consume the only ordinary loss from the tile. Eliminated Hobbit outside Mordor track returns as separate, but the simple loss remains used. On the return track it does not work and separated Companion is removed.

The characters leaving Fellowship at one cabl are waiting as one public group for the selection of the place. The state stores its specific ID, the starting point and the range. First the election of Guide/casualties ends, then the group receives one route before the Fellowship disclosure. It is not possible to separate the additional character or move the group from the new position after the disclosure.

Gollum automatically becomes Guide empty Fellowship and is not Companion. Its ability immediately ignores the disclosure of a regular numbered tile, also after changing Guide in the current Hunt. It does not ignore an Eye or a special red tile. An optional reduction by one requires a hidden Fellowship, no other mandatory disclosure and legal disclosure site. With progress of zero in the unoccupied settlement FP selection is unavailable.

<a id="kontrakt-i-odbiór"></a>
## Contract and acceptance

The action options show portrait/variant, a specific cost and legal location; they do not send decks or secret hands. The client prepares the selection by clicking on the map and confirms once. For flight, he does not use a fictional level or requires intermediate regions to click. Automatic capabilities resulting from the rules do not require empty confirmations; voluntary capabilities always remain the choice.

Tests include all entries of the manifesto's ability, legal and illegal entrances, figure After the transition, elimination and dice next turn , boundaries movement/obl The mangers, the reaction moments, both types of Hobbits return, extra tiles, hand limits, Mouth ability reset in the new turn repeat command and reboot PostgreSQL in the middle of the decision. Client tests check the projection/stretch and operation of new windows. The T15–T19 tests so far remain a regression.
