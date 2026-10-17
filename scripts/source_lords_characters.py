#!/usr/bin/env python3
"""Validate reviewed Lords character fronts, version identities and ownership."""
import task_registry
import argparse

from source_lords_cards import fetch, read

INDEX_PATH = 'docs/sources/lords-character-index.json'


def validate_card(card, sources, base):
    assert card['id'] == 'lords.character.' + card['id'].rsplit('.', 1)[1]
    assert card['verification'] == 'second_edition_front_reviewed'
    assert card['effectsVerified'] and card['reviewedAt'] and card['edition']
    assert card['title'] and card['subtitle'] and card['identity']
    assert card['entry'] and card['abilities'] and card['removal']
    assert card['verificationCases'] and card['rulebookPages']
    assert card['printedNumber'] == f"LM {card['number']}/{card['numberOfCards']}"
    assert card['extraActionDice'] in {0, 1}
    assert card['nationIcon'] in {None, 'elves', 'north', 'any', 'sauron'}
    source = sources[card['source']['sheet']]
    left, top, right, bottom = card['source']['bounds']
    assert 0 <= left < right <= source['width']
    assert 0 <= top < bottom <= source['height']
    assert len(source['sha256']) == 64 and source['url'].startswith('https://')
    if card['baseAlternative']:
        assert base[card['baseAlternative']]['identity'] == card['identity']
    if card.get('transformsTo'):
        target = base[card['transformsTo']]
        assert target['identity'] == card['identity'] or card['identity'] == 'smeagol'


def validate_composition(index, cards):
    by_side = {side: [c for c in cards if c['side'] == side]
               for side in ['FreePeoples', 'Shadow']}
    for side, count, denominator in [('FreePeoples', 10, 10), ('Shadow', 4, 5)]:
        assert [c['number'] for c in by_side[side]] == list(range(1, count + 1))
        assert {c['numberOfCards'] for c in by_side[side]} == {denominator}
        assert index['composition']['freePeoples' if side == 'FreePeoples' else 'shadow'] == count
    assert index['composition']['characterCards'] == len(cards) == 14
    assert len({c['identity'] for c in cards}) == 14
    assert sum(c['baseAlternative'] is not None for c in cards) == index['composition']['alternateVersions'] == 9
    assert sum(c['baseAlternative'] is None for c in cards) == index['composition']['newIdentities'] == 5
    special_id = index['composition']['shadowFifthComponent']
    event = next(c for c in read('docs/sources/lords-card-index.json')['cards'] if c['id'] == special_id)
    assert event['printedNumber'] == 'LM 5/5' and event['kind'] == 'SpecialEvent'
    assert special_id not in {c['id'] for c in cards}


def validate_stats(cards):
    by_key = {c['id'].rsplit('.', 1)[1]: c for c in cards}
    for key in ['elrond', 'galadriel']:
        card = by_key[key]
        assert (card['level'], card['movementLevel'], card['combatCardLevel']) == (0, 0, 3)
        assert (card['leadership'], card['nationIcon']) == (2, 'elves')
    for key in ['boromir', 'gimli', 'legolas']:
        card = by_key[key]
        assert (card['level'], card['movementLevel'], card['combatCardLevel']) == (1, 2, 1)
    assert {key for key, c in by_key.items() if c['nationIcon'] is None} == {'smeagol', 'boromir', 'gimli', 'merry', 'pippin'}
    assert by_key['gandalf-grey']['nationIcon'] == 'any'
    assert by_key['strider']['nationIcon'] == 'north'
    assert (by_key['smeagol']['level'], by_key['smeagol']['leadership']) == ('X', 0)
    assert (by_key['balrog']['level'], by_key['balrog']['activeLevel']) == (0, 2)
    assert (by_key['witch-king']['level'], by_key['witch-king']['unlimitedMovement']) == (None, True)
    assert {key for key, c in by_key.items() if c['extraActionDice']} == {'mouth-of-sauron', 'witch-king'}
    assert {c['specialDie'] for c in cards if c['specialDie']} == {'narya', 'nenya', 'vilya', 'balrog', 'gothmog'}
    assert {key for key, c in by_key.items() if c.get('countsForGandalfWhiteEntry') is False} == {'balrog', 'gothmog', 'witch-king'}


def validate_groups(index, cards):
    tasks = {t['id']: t for t in task_registry.records()}
    assigned = []
    for group, task_id, variant, count in zip(index['implementationGroups'], ['T25', 'T26'], [None, 'Council'], [8, 6]):
        expected = [c['id'] for c in cards if c['requiredVariant'] == variant]
        assert group['task'] == task_id
        assert group['content_ids'] == tasks[task_id]['character_ids'] == expected
        assert tasks[task_id]['character_count'] == len(expected) == count
        assigned.extend(expected)
    assert len(index['implementationGroups']) == 2
    assert len(assigned) == len(set(assigned)) == len(cards)
    assert index['composition']['councilOnlyVersions'] == 6
    assert index['composition']['gandalfOptionalWithoutCouncil']
    starters = {c['id']: c['councilStart'] for c in cards if c.get('councilStart')}
    assert set(starters.values()) == {'Minas Tirith', 'Erebor', 'Woodland Realm', 'The Shire'}
    assert len(starters) == 5


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fetch', action='store_true', help='Verify pinned character sheet in ignored cache')
    args = parser.parse_args()
    index = read(INDEX_PATH)
    cards = index['cards']
    sources = {s['id']: s for s in read('docs/sources/manifest.json')['sources']}
    base = {c['id']: c for c in read('docs/sources/base-character-index.json')['cards']}
    assert len(cards) == len({c['id'] for c in cards}) == 14
    for card in cards:
        validate_card(card, sources, base)
    validate_composition(index, cards)
    validate_stats(cards)
    validate_groups(index, cards)
    print('PASS: 14 character fronts, 5 new identities + 9 alternatives, exact stats, disjoint T25/T26 groups 8+6.')
    if args.fetch:
        fetch(cards, sources)


if __name__ == '__main__':
    main()
