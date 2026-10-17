<a id="t18--bitwy-polowe"></a>
# T18 — battle field

Source: BASE , printed s. 10 , 27–32 , 35 ; FAQ 1.2 (the sequence of several initiatives of the same card Each decision below shall be subject to the same verification of the party, revision and ID decision on what the others are action Settlements cards The base is owned by T21 packages; T18 provides the mechanism and tests it with open-air programs `test.*` .

<a id="wejście-i-uczestnicy"></a>
## Entry and participants

`AttackArmy` uses one die Army or Character (commander in the selected group). Exactly one group attacks the neighboring army. All participating units and leaders must belong to the nations of At War; Nazgûle and the characters are treated as At War. The attacker remains in the starting region. If he leaves the rear guard, she also has to contain an army unit. All defenders in the appropriate army participate. identifier participants are recorded; figure rear guard is not hit and cannot follow battle. At the beginning we save the attacked nations also when the entire army is killed. The political effect occurs once on battle, at the end of the entire action (correct T21-SH-CHAR-04 based on the decision of the Chapman of 2021).

<a id="runda-i-informacje"></a>
## Round and Information

The attacker chooses card or resigns, then defender. Before the second approval, the opponent sees only reverse/talia and the play. Both faces become public at the same time. Each card may have several programs with separate initiatives; the lower initiative first, with the first draw. Cancellation disables the unexecuted programs of the given card. The dedication of Leadership checks available points of each figure; cancelled or already dedicated points must not be sacrificed again.

Windows: Initiatives 0–3 , joint fight, initiative 4 , selection and joint deployment of commanders, initiative 5 , initiative 6 , the attacker's loss, the defender's loss, the initiative 7 , end of round. Leadership is the maximum number of reroll ( BASE p. 29–30 ); the player points from zero to the available limit of failed dice The selection of the two sides is saved before any reroll is carried out, so that the second selector does not know the new results. The zero limit does not create unnecessary decision. Natural 1 always misses, 6 always hits; maximum five dice The result, the modifier and the hit threshold are kept separately.

A special attack by card settles losses immediately and resumes the program card. By default, it uses the threshold of a normal attack, but not the metastasies or modifiers of card. Allows you to set your own threshold and number of dice. Hits of both regular throws are set before losses; the death of the attacker does not remove the hits of the due defender.

<a id="straty"></a>
## Casualties

The selection includes ordered operations on figure: removal of the regular for 1, degradation of the elite for 1 or removal of the elite for 2. One elite can receive two hits by removal. The regular replacement elite comes from the same nation: first they have fallen, then the reserve. If it is not, degradation removes the elite for one hit. Earlier loss operation can release figure regular for subsequent degradation. New figure enters the saved list of participants. FP goes to the fallen, Shadow to the reserve. After counting the total selection, the lack of the last participating unit removes all participating leaders/Nazgûle/post, also when the region still has rear guard. Overtime hits expires only after the removal of the entire army.

<a id="odwrót-postęp-i-granice-etapu"></a>
## Retreat, progress and phase limits

After the cards is rejected, the attacker ends or continues. Only the continuation gives the defender the choice to stay or retreat to the neighboring free region: without the enemy army and without the settlement controlled by the enemy. The retreat ignores the national limit on border crossing. The 0 level character remains in the region. The crossing of the stack limit is a separate decision before the battle resumes. The absence of legal retreat automatically leads to the next round.

After the back or the damage, the defender is selected to advance all/au The strength of the contestants or remain. Without an army unit there is no progress. FP It still requires an individual, but the character can be alone. Only actual progress changes the control of the region and records the activation of the right nations to the end action Just a simple attack on city/fortyfikacj s uses the threshold 6 In the first round. acceptance T18 did not include attacks on fortresses; [T19](T19-SIEGES.md) adds shelter, garrison, rescue and getaway. Cost action closes once and for all battle ; battle started by the effect returns to the master program.

<a id="weryfikacja"></a>
## Verification

T18 tests include V05–V07, group and rear guard validation, privacy cards and reroll, modifiers, initiatives and their tie, immediate special losses, death of both armies, origin figures at degradation, legality of retreat, progress and control. The HTTP/PostgreSQL test launches a new server after secret selection of card and after losses of the attacker and renews the saved command without repeating randomness.
