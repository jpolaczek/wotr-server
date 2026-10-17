<a id="t12--ruch-armii"></a>
# T12 — Army movement

Sources: BASE p. 8 , 20 , 24 , 26–28 , 31–32 , 34–35 Full capacity of characters and their entry into the game remain in T20; T12 defines participation data and checks mechanics on explicit fixtures. The movement of single characters is different action I'm not the army.

`MoveArmies` includes die, the selected cost category Army/Character and one or two `ArmyMoveChoice`: source, target region, ID units and ID included characters. Army/ArmyMuster allows two movements, Character to one with command/Nazgûl or moving figure. Will of the West pays the indicated category. Each group must have at least one regular/elite unit. It is possible to separate figure from one region into different groups, but no ID can occur twice in the same action.

All figure must belong to the player, stand in the given region and not be locked in the garrison. The 0 level prevents movement of the character also with the army. The normal commander of FP cannot remain without units; Nazgûle and characters can stay. die Character requires the presence of the figure in the moving group, not only in the source region.

The target must be adjacent to the verified graph. The unit and commander of a nation not being At War may enter the territory of their own nation or neutral but not other nation, also allies. The nazgûle/postaty do not inherit political constraints. The army does not enter the enemy field army. It can enter the empty settlement opponent or strengthen its army besieging the enemy Stronghold; it does not merge with the garrison. The attack is not a shortcut to ordinary movement and is formed in T18.

The plan is checked step by step on the copy of the board data itself. The second group can move from the region into which the first one entered, but must contain other figure. We do not reject the temporary excess at the point of connection. After both movements, the correct limit creates `RemoveExcess` from T11; the cost remains Resolving to answer.

Entering the nation's region triggers T11 activation and empty occupation settlement changes control and the right political trigger. Points result from the current control, military victory continues to check the phase 6 . Abandoning siege by the last field unit to remove the defenders from the garrison; the new site of the second group is already checked for such condition. Companion the ending movement can activate the right nation according to the icon.

settlement to Town, City or Stronghold (BASE p.10, 32). Fortifications of Osgiliath and Fords of Isen are not scored with settlement or regular recruitment points. Example: Shadow entering Fords of Isen activates Rohan, but does not move it for gaining settlement. `EnteringAFortificationActivatesWithoutSettlementCaptureOrPoliticalAdvance` and `FortificationsAreNotRecruitmentSettlements` protect both effects of the same definition.

Preview and Apply use one planer without access to RNG. Preview returns the positions of figures, changes of control/politics and the required excess, without modification of the record. Options specify armies, candidates and targets with allowed figure and acceptable costs; they do not calculate all of the Army subsets. The client uses them to select, and from UI-127 also to an uninteractive army spirit for the purpose and signature of the attack mode; the arrows are not from UI-73.
