#!/usr/bin/env python3
"""Validate Kings sovereign states, Dark Chieftains, tokens and source bounds."""
import task_registry
import argparse

from source_lords_cards import fetch, read

INDEX_PATH = 'docs/sources/kings-character-index.json'
EXPECTED_SOVEREIGNS = {
    'brand': (1, 'north', 1, 2, 'Dale', 'Muster'),
    'dain': (3, 'dwarves', 1, 4, 'Erebor', 'Muster'),
    'denethor': (5, 'gondor', 1, 3, 'Minas Tirith', 'Event'),
    'theoden': (7, 'rohan', 2, 3, 'Edoras', 'Muster'),
    'thranduil': (9, 'elves', 2, 4, 'Woodland Realm', 'Character'),
}
EXPECTED_CHIEFTAINS = {
    'black-serpent': (1, 'southrons-easterlings', 2, 2),
    'shadow-of-mirkwood': (2, 'sauron', 3, 1),
    'ugluk': (3, 'isengard', 2, 1),
}


def validate_card(card, sources):
    assert card['verification'] == 'second_edition_front_reviewed'
    assert card['effectsVerified'] and card['reviewedAt'] and card['edition']
    assert card['entry'] and card['abilities'] and card['removal']
    assert card['verificationCases'] and card['rulebookPages']
    assert card['title'] and card['subtitle'] and card['identity']
    assert card['printedNumber'] == f"KM {card['number']}/{card['numberOfCards']}"
    assert card['extraActionDice'] == 0, 'Ruler is shared, not an extra standard die per figure'
    source = sources[card['source']['sheet']]
    left, top, right, bottom = card['source']['bounds']
    assert 0 <= left < right <= source['width']
    assert 0 <= top < bottom <= source['height']
    assert right - left == 332 and bottom - top == 600
    assert source['sha256'] == 'e4139bd403a61853ad0c50a6bf3aa16aef7171d26143c6e8df315632e2e718c0'


def validate_sovereigns(cards):
    by_id = {card['id']: card for card in cards}
    for identity, expected in EXPECTED_SOVEREIGNS.items():
        number, nation, level, resistance, region, result = expected
        awakened = by_id[f'kings.character.{identity}.awakened']
        corrupted = by_id[f'kings.character.{identity}.corrupted']
        for card in [awakened, corrupted]:
            assert card['identity'] == identity and card['nationIcon'] == nation
            assert card['initialRegion'] == region and card['side'] == 'FreePeoples'
            assert card['numberOfCards'] == 10
        assert awakened['number'] == number and corrupted['number'] == number + 1
        assert awakened['alternateState'] == corrupted['id']
        assert corrupted['alternateState'] == awakened['id']
        assert awakened['initialRole'] == 'Leader'
        assert awakened['role'] == 'AwakenedSovereign' and awakened['controller'] == 'FreePeoples'
        assert (awakened['level'], awakened['leadership']) == (level, 1)
        assert awakened['shadowResistance'] == resistance
        assert awakened['rulerDieContribution'] == 'FreePeoples'
        assert awakened['rulerAbilityResult'] == result
        assert corrupted['role'] == 'CorruptedSovereign' and corrupted['controller'] == 'Shadow'
        assert (corrupted['level'], corrupted['leadership']) == (0, 0)
        assert corrupted['shadowResistance'] is None and corrupted['rulerDieContribution'] is None
        assert not any(corrupted[key] for key in ['countsAsCompanion', 'countsAsCharacter', 'countsAsMinion'])


def validate_chieftains(cards):
    chieftains = [card for card in cards if card['role'] == 'DarkChieftain']
    assert len(chieftains) == 3
    for card in chieftains:
        number, nation, level, leadership = EXPECTED_CHIEFTAINS[card['identity']]
        assert card['id'] == f"kings.character.{card['identity']}"
        assert (card['number'], card['numberOfCards']) == (number, 3)
        assert (card['nationIcon'], card['level'], card['leadership']) == (nation, level, leadership)
        assert card['side'] == card['controller'] == card['rulerDieContribution'] == 'Shadow'
        assert not card['countsForGandalfWhiteEntry']
        assert card['shadowResistance'] is None


def validate_tokens(index):
    tokens = index['tokens']
    assert len(tokens) == len({token['id'] for token in tokens}) == 5
    for token in tokens:
        identity = token['identity']
        assert token['id'] == f'kings.token.{identity}'
        assert token['awakenedCard'] == f'kings.character.{identity}.awakened'
        assert token['abilityResult'] == EXPECTED_SOVEREIGNS[identity][-1]
        assert (token['front'], token['back']) == ('Ruler', 'Dismay')
        assert token['granted'] == 'Awakening' and token['usesPerGame'] == 1
        assert token['rulebookPages'] == [5, 10, 17, 18]


def validate_groups(index):
    cards = index['cards']
    tasks = {task['id']: task for task in task_registry.records()}
    expected = {
        'T28': [card['id'] for card in cards if card['role'] != 'DarkChieftain'],
        'T29': [card['id'] for card in cards if card['role'] == 'DarkChieftain'],
    }
    assigned = []
    assert len(index['implementationGroups']) == 2
    for group in index['implementationGroups']:
        task_id = group['task']
        ids = group['character_ids']
        assert ids == expected[task_id] == tasks[task_id]['character_ids']
        assert len(ids) == tasks[task_id]['character_count']
        assigned.extend(ids)
    assert len(assigned) == len(set(assigned)) == 13


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--fetch', action='store_true', help='Verify pinned source sheet in ignored cache')
    args = parser.parse_args()
    index = read(INDEX_PATH)
    cards = index['cards']
    sources = {source['id']: source for source in read('docs/sources/manifest.json')['sources']}
    assert len(cards) == len({card['id'] for card in cards}) == 13
    assert len({tuple(card['source']['bounds']) for card in cards}) == 13
    assert index['composition'] == dict(cards=13, awakened=5, corrupted=5, darkChieftains=3,
                                         sovereignIdentities=5, chieftainIdentities=3,
                                         rulerTokens=5, kingsHuntTiles=2)
    for card in cards:
        validate_card(card, sources)
    validate_sovereigns(cards)
    validate_chieftains(cards)
    validate_tokens(index)
    validate_groups(index)
    print('PASS: 13 Kings character fronts, 5 sovereign state pairs, 3 Chieftains, 5 tokens, T28/T29 groups 10+3.')
    if args.fetch:
        fetch(cards, sources)


if __name__ == '__main__':
    main()
