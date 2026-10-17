<a id="t19--twierdze-oblężenia-wypady-i-odsiecz"></a>
# T19 — Fortress, siege, Outings and Rescue

Source: BASE, printed p. 28–32, especially p. 31–32; [official text p. 32](https://www.aresgames.eu/download/20980/?tmstv=1770553617). This document develops the common procedure [T18](T18-BATTLES.md), does not create a second engine battle.

<a id="tryby-i-rozpoczęcie"></a>
## Modes and Start

`Field` is a regular battle between neighbors. If the defender controls Stronghold in the region, before the first and each next round can fight in the field or shelter. Stronghold alone does not raise the threshold of a hit in battle in the field. `Relief` is an attack from the neighbouring region on the besieging: the garrison does not participate, does not accept losses and does not make Leadership available. `Siege` is an attack on the garrison in the same region. `Sortie` is an attack on the garrison obliging in the same region.

All modes have the cost of Army or Character with participating commander; At War positions and rear guard still apply. The fall may leave the rear guard inside Stronghold. The garrison does not leave her ordinary Army movement. The besieged may leave or be reinforced by a simple movement; the defender cannot be saved by free movement by an enemy army.

<a id="schronienie-i-kontrola"></a>
## Refuge and control

The shelter moves the defenders to a separate garrison group and gives the attacker optional progress to the participants. Only actual progress is established by siege: if the garrison exceeds five units, the defender points the excess to the reserve. Refusal of progress ends battle and restores the defenders to the region without limiting them to five. There is no additional assault within the same action.

settlement still belongs to the defender and gives it the right points. After destroying the entire garrison, when there is at least one enemy unit in the region, Stronghold changes control; the besieging already stands there, so it does not require new progress. Mutual destruction of both armies without a living army in the region does not change control. The preserved rear guard counts when checking the actual presence of the army after battle, but does not participate in throws or losses.

After the departure or death of all the besieging remaining garrison returns to the region. The control cannot be settled in half of the usual losses between the choice of the attacker and the defender. Other regions can end siege by event regardless of the ongoing battle.

<a id="szturm-i-przedłużenie"></a>
## Storm and extension

In `Siege`, the attacker hits 6+, the defender on 5+, in all rounds. The garrison cannot retreat to the neighboring region. After one round, the assault ends unless the attacker openly points out the participating elite to degradation; he does not sacrifice the rear guard. Each additional round requires another elite. The choice of elite and listed figure are saved, and the next round retains the same cost of action.

The interpretation of a joint reduction operation: p. 32 limits the extension of the availability of participating elites; the exchange of figure uses p. 30 (regular of the same nation: failed, then reserve; in the absence of regular removal of the elite). The absence of figure does not create a regular. If the last participating unit disappears, battle ends and the participating commanders die. The test also covers this coastal case.

<a id="wypad-i-odsiecz"></a>
## Out and out

In `Sortie`, both sides hit 5+, so the garrison does not obtain wall cover during its own attack. At least the first round is mandatory; later the attacker may interrupt and return to Stronghold. The besieged defender may withdraw to the free neighboring region. The winning garrison does not go beyond its region; after the abolition of siege passes to the field with the remaining rear guard.

The `Relief` is attended only by the selected external army. After the elimination or retreat of the besieging garrison returns to the region, and the winning rescue can optionally enter. The limit of the works after the merger of the two friendly armies. Progress is not available when the besieging one is still standing. The city and fortification retain the rule of the first round with the T18; Stronghold uses only the described mode.

<a id="weryfikacja-i-przekazanie"></a>
## Verification and transmission

V08: initial and subsequent shelter, refusal of progress at six units, actual limit of five, assault and multiple extension, lack of regular, lack of elite, ban on retreat, both results of the departure and its rear guard, relief without garrison, connection above the, mutual destruction and reflection Stronghold. HTTP/PostgreSQL resumes the selection of elite after reboot, and the retry retains figure and cost. UI-07 receives a clear mode, election and preview without hidden exceptions in the client.
