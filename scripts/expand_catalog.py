"""Maintainer input for the structured rule/decision specification, not game logic."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
def write(path, data):
    (ROOT / path).write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n')

# trigger | conditions | choices | cost | ordered effects | next window | edge
ROWS = {
'B01': 'Accepted configuration|Verified enabled content only|Council versions if enabled|None|Compose content; place components; shuffle; draw starting hands|RecoverDraw|Reject unresolved enabled content before any shuffle',
'B02': 'New turn|Game ongoing|Discard excess, Fellowship choices, Hunt dice, actions|Per action|Recover; draw both decks; discard; Fellowship; allocate; roll; alternate; military victory|Next turn or terminal|Mandatory discard interrupts phase advancement',
'B03': 'Action opportunity|Actor has available dice|Act, pass if fewer dice, discard a die|Concrete die for discard/action; none for pass|Validate count; resolve action; change actor|Other side with dice|At equal counts pass is illegal; exhausted side is skipped',
'B04': 'Action selected|Result permits category and targets are legal|Category, objects, targets and concrete cost|One appropriate die unless explicit exception|Validate all selections; pay; resolve; clear action|Reaction or next action|Will of the West and special dice are not silently substituted',
'B05': 'Before normal action|Unused owned ring; no ring used this turn; available die|Die and permitted result|FP transfers ring to SH; SH removes it|Change face; move Eye to Hunt if chosen; keep actor|Same normal action opportunity|Changed die need not be the die subsequently spent',
'B06': 'Draw or card event|Correct deck, timing and verified card effect|Deck; excess discards; effect targets|Die/card according to source|Complete draw procedure; discard to six; resolve effect windows|Prior continuation|Empty deck stays empty; facedown discard hides identity',
'B07': 'Muster or activation trigger|Nation and At War restrictions|Eligible nation for voluntary advance|Muster for voluntary advance|Activate/advance as triggered; re-evaluate legality|Action or effect continuation|Passive FP nation cannot voluntarily enter At War',
'B08': 'Recruit action|At War, friendly eligible settlement, reserves|Units and distinct settlements|Muster; actual reserve pieces|Validate combination; move reserves; finish|Next action|Card recruitment checks its own exceptions, not ordinary muster globally',
'B09': 'Move army|Adjacency, politics, force and leadership|Subsets and up to two moves|Army or eligible Character die|Validate no piece moves twice; move; resolve final excess|Excess removal or next action|Temporary excess is allowed until action ends',
'B10': 'Character action|Verified version/level/entry requirements|Characters and destinations/version|Matching action resource|Validate path/group; move or enter; schedule dice for recovery|Ability or next action|Do not award newly gained character die mid-turn',
'B11': 'Legal battle begins|Participants and mode fixed|Attacker card/none then defender card/none|Cards; battle action already paid|Commit attacker; commit defender; reveal together; order lower initiative first with defender tie|Combat effects and roll|Attacker declining never removes defender choice',
'B12': 'Combat effects resolved|Surviving eligible force and leadership|Casualties, elite downgrade/remove|Hits and actual replacement regulars|Roll up to five; reroll eligible misses; attacker then defender losses|Continue battle|No regular available means elite elimination; natural 1 misses, 6 hits',
'B13': 'Round survived|Eligible retreat destinations|Attacker stop/continue; defender stay/retreat; advance|No extra ordinary action die|Attacker decision; defender if continued; move eligible survivors; update control|Next round or action end|Attacker stop does not grant arbitrary defender retreat',
'B14': 'Stronghold battle or shelter|Distinct field and garrison forces|Shelter, sortie, relief, extend with elite|Elite downgrade for extension|Resolve mode; five-unit garrison limit; single siege round unless extended|Siege extension or battle end|Besieging a stronghold does not award its victory points',
'B15': 'Fellowship phase or move action|Hidden/revealed and reachable destinations|Declare, reveal route, hide, move abstract progress|Character die for move/hide|Progress; Hunt; finalize movement die into Hunt, or declare/reset progress|Hunt intervention or next action|No secret route is preselected by ordinary progress',
'B16': 'Guide/separation opportunity|Eligible companion and level requirements|Guide; separating companions; destination|According to phase/action|Change group; resolve guide change; heal/activate only eligible declaration|Guide ability or prior continuation|Ringbearers and Gollum do not count as ordinary companions',
'B17': 'Hunt allocation then Fellowship movement|At most companions, but one allowed at zero; minimum from recovered movement dice|Specific Shadow dice|Allocated dice unavailable for actions|Allocate; roll action dice and add Eyes; later Hunt roll/rerolls|Action phase or tile draw|Keeper Eye is not a Fellowship movement die for next-turn minimum',
'B18': 'Successful Hunt tile drawn|Tile type and timing window|Legal interventions; guide sacrifice/random companion/corruption|Card/ability and chosen damage payment|Resolve interventions; damage; guide change; reveal; additional tile if required|Nested reaction or action completion|Never choose arbitrary companion instead of required random draw',
'B19': 'Enter/move in Mordor|Entry and hidden state rules|Legal intervention, hide or movement action|Applicable die|Prepare pool; draw/resolve tile; progress as tile permits; check corruption|Next action or immediate end|Final track position does not win before lethal corruption is resolved',
'B20': 'Corruption/ring destruction or phase six|Ongoing game|None (automatic)|None|Corruption >=12; ring destroyed; Shadow >=10 VP; FP >=4 VP|Terminal or next turn|Military VP checked in phase six; simultaneous military thresholds favor Shadow',
'L01': 'Lords setup/roll/recovery|Lords enabled and eligible special sources|Eligible special die, mandatory Eye precedence|Selected die, deferred removal flag|Compose components; roll; select by rule; transfer Eyes; recover removals at proper time|Integration roll window or actions|Multiple Eyes with different provenance require an explicit choice',
'L02': 'Lords character/ring ability|Lords and verified source card|Entry, movement, ring or Smeagol choices|Exact verified card cost|Resolve card-specific windows and source-bound lifetime|Ability continuation|Unconquered checks current control; no generic flying minion assumption',
'L03': 'Council setup/first Fellowship phase|Lords plus Council|Private versions; eligible companions outside; Shadow token if one|Once-per-turn token when later used|Choose private versions; reveal guide; declare outsiders; allocate token(s)|Fellowship or token action|Tokens never count as dice for passing',
'K01': 'Kings recovery/roll/Ruler action|Kings and qualifying character|Discard eligible die; Ruler ability; token|Required discarded die; token mutually exclusive with ring|Recover Ruler; roll; Shadow discard then FP; move Eyes; ability|Next roll window or normal action|Ordinary Shadow Eye cannot be discarded; Ruler Eye can',
'K02': 'Corrupt/awaken/recover sovereign|Kings; eligible Eye provenance; FP unused die; more than one eligible Eye|Sovereign; Eye; tile or sequential stop|Eligible Eye and ability-specific costs|Draw per rules; replace special draws; choose/store tile; return others; transition sovereign|Next action or sequential draw|Private retained tile is not exposed to FP, except enabled visible variant',
'K03': 'Chieftain entry or combat card ability|Kings and verified card|Entry; substitute combat effect with sovereign/chieftain ability|Die and played card as required|Commit played card; resolve replacement at prescribed timing|Combat continuation|Replacing effect does not mean no combat card was played',
'X01': 'Lords and Kings overlap|Both enabled|Eligible provenance-specific Eye/council entry cost|Actual source components|Apply explicit integration; skip Smeagol tile in corruption; exclude Captain Eyes|Kings continuation|Captain Eyes neither pay nor increase corruption draw count',
'X02': 'Configured Kings variant window|Kings and dependencies validated|Dismay reroll; sequential draw/stop|Dismay token or current corruption cost|Apply selected variant only at its window|Roll/corruption continuation|Sequential retains last draw, not best earlier tile',
'F01': 'Fate content composition|Fate enabled; GAP-FATE must be resolved|New Cities if configured|None|Apply verified setup/map/card/Gimli diff before shuffling|Setup|No production diff may be inferred from incomplete photos',
'X03': 'Kings plus Fate content composition|Both enabled; verified Fate content|None (automatic)|None|Remove Dain/Brand sovereigns; suppress two Kings replacements; use Fate versions|Setup validation|Reject conflicting duplicate slots instead of last-file-wins',
}
data = json.loads((ROOT / 'docs/rules/catalog.json').read_text())
for rule in data['rules']:
    fields = ROWS[rule['id']].split('|')
    rule['specification'] = dict(zip(['trigger','conditions','choices','cost','order','next_window','edge_case'], fields))
    rule['specification']['normal_case'] = fields[0] + ' → ' + fields[4] + ' → ' + fields[5]
    rule['specification']['duration'] = 'Until explicit completion/expiry of its resolution frame; paid costs persist across nested decisions.'
    rule['specification']['visibility'] = 'Public board/resources/results; own hand and private commitments only for owner; opponent receives counts/backs. Deck order and future RNG never projected.'
    rule['required_modules'] = {'L':['Lords'], 'K':['Kings'], 'F':['Fate']}.get(rule['id'][0], {'X01':['Lords','Kings'],'X02':['Kings'],'X03':['Kings','Fate']}.get(rule['id'], ['Base']))
    rule['verification_cases'] = [rule['id']+'.normal',rule['id']+'.edge']
    rule['unresolved'] = {'B06':['GAP-BASE-CARDS'],'B10':['GAP-BASE-CHAR'],'L02':['GAP-LORDS-CHAR'],'L03':['GAP-LORDS-CHAR'],'K02':['GAP-KINGS-CHAR'],'K03':['GAP-KINGS-CHAR','GAP-KINGS-CARDS'],'F01':['GAP-FATE'],'X03':['GAP-FATE']}.get(rule['id'], [])
data['schema_version'] = 2
data['note'] = 'Structured requirements, not proof of implementation. verification_cases are planned scenarios; tests lists only executed implementation coverage. Card exceptions remain source-gated.'
write('docs/rules/catalog.json', data)

# id | rules | owner | visibility | presentation | next | scenarios
DECISIONS = '''Configure|B01|Both seats|Public configuration|Configuration|Setup|S19
CouncilVersions|L03|FP|Owner; guide public|PortraitVersions|CouncilOutside|S19
CouncilOutside|L03|FP|Reveal chosen outsiders|PortraitDestination|CouncilToken|S19
CouncilToken|L03|SH|Public selected token|TokenChoice|Fellowship|S17,S19
DiscardCards|B06|Hand owner|Owner identities; public count|CardGrid|Saved continuation|S07
FellowshipPhase|B15,B16|FP|Public declaration; Council versions remain private|MapPortrait|AllocateHunt|S14,S19
AllocateHunt|B17|SH|Public selected dice|DiceTransfer|Roll|S17
SpecialDieChoice|L01,X01|Rule-ordered side|Public|DiceComparison|RulerDiscard|S05,S17
RulerDiscard|K01,X01|SH then FP|Public|DiceComparison|Dismay|S17
Dismay|X02|SH|Public|TokenDie|PostRoll|S17
Action|B03,B04|Alternating actor|Public intentions after commit only|ObjectIntentCost|Effect resolution|S01,S05
Ring|B05,L02|Current actor|Public|RingDieFace|Same Action|S05,S17
MoveArmies|B09|Current actor|Public after commit|MapUnits|Excess|S01,S02,S03,S04
Recruit|B08|Current actor|Public|ReservePlacement|Action|S06
Politics|B07|Current actor|Public|NationTrack|Action|S06
Characters|B10,B16|Owner|Council versions reveal when required|PortraitDestination|Ability or Action|S14,S19
EventTargets|B06|Effect-designated side|Own card; reveal on play|CardMapTargets|Effect continuation|S07,S08
Reaction|B06,B18|Effect-designated side|Own legal cards only; same public prompt|CardOrDecline|Saved frame|S08,S15
CombatCard|B11,K03|Attacker then defender|Owner front; other sees committed back until reveal|CardOrDecline|Reveal|S09,S13
Losses|B12|Attacker then defender|Public|HitsUnits|BattleContinue|S10,S11
Excess|B09,B14|Army owner|Public|UnitsToReserve|Saved frame|S04,S12
BattleContinue|B13,B14|Attacker|Public|ContinueStopElite|DefenderRetreat or end|S12
DefenderRetreat|B13,B14|Defender|Public|MapStayShelter|Next round or end|S12
Advance|B13|Attacker|Public|MapUnits|Action|S01,S12
FellowshipRoute|B15|FP|Public committed location/route required by rule|MapRoute|Hunt or Action|S14
HuntDamage|B18|FP|Guide public; random candidate versions protected|GuideRandomCorruption|Reveal/Guide/Continuation|S15
Guide|B16,B18|FP|Selected guide public|PortraitChoice|Ability/Continuation|S14,S15
MordorIntervention|B19|Effect-designated side|Own cards only|TileCardOrDecline|Damage and progress|S16
CorruptionAttempt|K02,X01|SH|Private candidates and chosen value unless Visible|EyePortraitTile|Return unchosen/Action|S18
SequentialCorruption|K02,X02|SH|Private current tile unless Visible|TileDrawStop|Action|S18
SovereignAbility|K02,K03|Owner|Public except explicit hidden tile data|PortraitTargetCost|Effect continuation|S18
RulerAbility|K01|Current actor|Public|DieTokenTarget|Action|S17'''
decisions=[]
for row in DECISIONS.splitlines():
    id,rules,owner,visibility,presentation,next_window,scenarios=row.split('|')
    decisions.append(dict(id=id,rules=rules.split(','),owner=owner,visibility=visibility,presentation=presentation,next_window=next_window,scenarios=scenarios.split(','),checkpoint='Atomic snapshot + events + receipt after every committed answer, before notification. Store decision ID, owner, paid costs and continuation.',cancel='Draft may be changed before submission; mandatory decisions cannot be skipped; optional decline is a committed answer.'))
write('docs/rules/decisions.json',dict(schema_version=1,decisions=decisions,automatic_steps=['SetupComposition','Shuffle','RecoverDice','DrawBothDecks','RollDice','TransferRolledEyes','RevealCommittedCards','CombatRoll','HuntRoll','ApplyChosenDamage','CheckImmediateVictory','CheckMilitaryVictory']))
