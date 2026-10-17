"""Normalize audited board facts. Geometry is optional rendering data, never legality."""
import json
from pathlib import Path

root=Path(__file__).resolve().parents[1]
def read(p): return json.loads((root/p).read_text())
def write(p,v):
    f=root/p; f.parent.mkdir(parents=True,exist_ok=True)
    f.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n')
source=read('docs/sources/materials/map-facts-reviewed.json')
nations=[dict(id=k,side=v['side'],active=v['political']['active'],stepsToWar=4-v['political']['startBox']) for k,v in source['nations'].items()]
regions=[];units=[]; counters={}
def add(nation,kind,location,count):
    key=f'{nation}.{kind}'
    for _ in range(count):
        counters[key]=counters.get(key,0)+1
        units.append(dict(id=f'base.{key}.{counters[key]:02}',nation=nation,kind=kind.title(),location=location))
for id,r in sorted(source['regions'].items()):
    nation=r.get('nation')
    regions.append(dict(id=id,name=r['name'],nation=nation,originalOwner=source['nations'][nation]['side'] if nation else None,settlement=r.get('settlement'),victoryPoints=r.get('vp',0),neighbors=sorted(r['adjacency'])))
    for kind,count in (r.get('setup') or {}).items(): add(r.get('setupNation',nation),kind,id,count)
for nation,defn in source['nations'].items():
    for kind,count in defn['reinforcements'].items(): add(nation,kind,'reserve',count)
tiles=[]
for pool,entries in [('standard',[('eye',True,False,4),('3',False,False,3),('2',False,False,2),('2',True,False,1),('1',False,False,2),('1',True,False,2),('0',True,False,2)]),('special-fp',[('0',False,False,2),('-2',False,False,1),('-1',False,False,1)]),('special-sh',[('die',False,True,1),('eye',True,True,1),('3',False,True,1),('1',True,True,1)])]:
    for value,reveal,stop,count in entries:
        for i in range(count):tiles.append(dict(id=f'base.hunt.{pool}.{value}.{int(reveal)}.{i+1}',pool=pool,value=value,reveal=reveal,stop=stop))
cards=[f'base.{side}.{deck}.{n:02}' for side in ['fp','sh'] for deck in ['character','strategy'] for n in range(1,25)]
data=dict(version='base-setup-1',source='BASE pp.4–7,14–21,40; BOARD-2E; T04-DATA-AUDIT',regions=regions,nations=nations,units=units,
    impassableBorders=[dict(a=a,b=b) for a,b in [('rivendell','high-pass'),('old-ford','high-pass'),('dimrill-dale','hollin'),('lorien','dunland'),('gorgoroth','north-ithilien')] if a in source['regions'] and b in source['regions'] and b not in source['regions'][a]['adjacency']],
    dice=[dict(side='FreePeoples',initialCount=4,maximumCount=6,faces=['Character','Character','ArmyMuster','Muster','Event','WillOfTheWest']),dict(side='Shadow',initialCount=7,maximumCount=10,faces=['Character','Army','ArmyMuster','Muster','Event','Eye'])],cards=cards,
    characterDefinitions=['base.character.'+s for s in ['ringbearers','gandalf-grey','gandalf-white','strider','aragorn','boromir','gimli','legolas','merry','pippin','gollum','saruman','witch-king','mouth-of-sauron']],huntTiles=tiles)
write('content/base-board.json',data)
print(f'Normalized {len(regions)} regions, {sum(len(r["neighbors"]) for r in regions)//2} edges, {len(units)} pieces, {len(tiles)} tiles.')
