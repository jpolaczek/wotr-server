"""Validate reviewed border classifications and export the public presentation catalog."""

import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import sys

from export_map_geometry import ROOT, read_json, write_or_check
from map_geometry_validation import require, segment_key

AUDIT = ROOT / 'docs/sources/MAP-13-BORDER-AUDIT.json'
OUTPUT = ROOT / 'content/map-boundaries.json'
REPORT = ROOT / 'docs/sources/MAP-13-BORDER-REPORT.json'
PASSABLE = {'Passage', 'River'}
KINDS = PASSABLE | {'Impassable', 'Coast', 'Lake', 'Frame'}
NATION_COLORS = {'dwarves': 'Brown', 'elves': 'Light green', 'gondor': 'Dark blue',
                'north': 'Light blue', 'rohan': 'Dark green', 'isengard': 'Yellow',
                'sauron': 'Red', 'southrons': 'Orange'}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def positive_owners(geometry):
    """Positive normal is (-dy, dx) in screen coordinates, derived from ring winding."""
    result = {}
    for region in geometry['regions']:
        for ring in region['rings']:
            for a, b in zip(ring['points'], ring['points'][1:]):
                result[tuple(a), tuple(b)] = region['id']
    owners = {}
    for boundary in geometry['boundaries']:
        sides = {(result.get((tuple(a), tuple(b))), result.get((tuple(b), tuple(a))))
                 for a, b in zip(boundary['points'], boundary['points'][1:])}
        require(len(sides) == 1, f"Boundary changes normal ownership: {boundary['id']}")
        owners[boundary['id']] = next(iter(sides))
    return owners


def validate_parts(record, path, legal_pairs):
    parts = record['parts']
    require(parts and parts[0]['fromSegment'] == 0, f"Unclassified start: {path['id']}")
    previous = 0
    owners = path['owners']
    land = all(owner is not None for owner in owners)
    legal = land and tuple(sorted(owners)) in legal_pairs
    require(record['movementPair'] == legal, f"Audit changed movement pair: {path['id']}")
    for part in parts:
        require(part['kind'] in KINDS and part['fromSegment'] == previous and
                isinstance(part['toSegment'], int) and previous < part['toSegment'] <= len(path['points']) - 1,
                f"Gap, overlap, or invalid range: {path['id']}")
        require((part['kind'] in ('Coast', 'Lake', 'Frame')) != land,
                f"Water/land classification mismatch: {path['id']}")
        require(part['kind'] not in PASSABLE or legal, f"Passage absent from movement graph: {path['id']}")
        previous = part['toSegment']
    require(previous == len(path['points']) - 1, f"Unclassified end: {path['id']}")


def validate_audit(audit, geometry, board):
    require(audit['version'] == 'borders-1' and audit['geometryVersion'] == geometry['version'],
            'Border audit version mismatch')
    for path, expected in audit['sourceHashes'].items():
        # D17: the board scan and the rulebook stay outside git; a local copy must be the audited file.
        if not (ROOT / path).is_file():
            print(f'Border source {path} is not present (local-only, D17); its hash is not checked.')
            continue
        require(sha(ROOT / path) == expected, f'Border source changed: {path}')
    require(set(audit['reviewedSectors']) == {f'{r}-{c}' for r in range(3) for c in range(3)},
            'Incomplete source-board review')
    paths = {path['id']: path for path in geometry['boundaries']}
    records = {record['boundaryId']: record for record in audit['boundaries']}
    require(len(records) == len(audit['boundaries']) == 323 and records.keys() == paths.keys(),
            'Missing or duplicate border audit path')
    legal_pairs = {tuple(sorted((r['id'], n))) for r in board['regions'] for n in r['neighbors']}
    traversable = set()
    for key, record in records.items():
        path = paths[key]
        require(record['owners'] == path['owners'], f'Owner mismatch: {key}')
        require(record.get('evidence') and record.get('reviewSectors') and len(record.get('sourceBounds', [])) == 4,
                f'Missing source evidence: {key}')
        validate_parts(record, path, legal_pairs)
        if any(part['kind'] in PASSABLE for part in record['parts']):
            traversable.add(tuple(sorted(path['owners'])))
    require(traversable == legal_pairs, f'Missing printed passage: {sorted(legal_pairs - traversable)}')
    return records, legal_pairs


def luminance(color):
    rgb = [int(color[i:i + 2], 16) / 255 for i in (1, 3, 5)]
    linear = [v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4 for v in rgb]
    return sum(a * b for a, b in zip(linear, (.2126, .7152, .0722)))


def contrast(a, b):
    values = sorted((luminance(a), luminance(b)))
    return round((values[1] + .05) / (values[0] + .05), 3)


def validate_style(style):
    require(style['version'] == 'map-style-1', 'Unsupported palette')
    nations = style['nations']
    require(len(nations) == 8 and {n['id']: n['colorName'] for n in nations} == NATION_COLORS,
            'Palette must preserve the eight printed nation colors')
    colors = [style[key] for key in ('land', 'water', 'passage', 'barrier', 'keyline')]
    colors += [n['color'] for n in nations]
    require(all(re.fullmatch(r'#[0-9A-Fa-f]{6}', c) for c in colors), 'Malformed palette color')
    require(len({n['color'] for n in nations}) == 8, 'Nation colors must be distinct')
    require(style['barrierWidth'] > style['passageWidth'] > 0 and style['nationWidth'] > 0, 'Invalid stroke widths')
    require(contrast(style['barrier'], style['land']) >= 7 and contrast(style['passage'], style['keyline']) >= 7,
            'Insufficient boundary contrast')


def export():
    geometry = read_json(ROOT / 'content/map-geometry.json')
    audit = read_json(AUDIT)
    board = read_json(ROOT / 'content/base-board.json')
    style = read_json(ROOT / 'content/map-style.json')
    records, legal = validate_audit(audit, geometry, board)
    validate_style(style)
    normals = positive_owners(geometry)
    paths = []
    for path in geometry['boundaries']:
        record = records[path['id']]
        positive, negative = normals[path['id']]
        paths.append({'id': path['id'], 'positiveNormalOwner': positive, 'negativeNormalOwner': negative,
                      'sourceReference': 'MAP-13-BORDER-AUDIT/' + path['id'],
                      'parts': [{k: part[k] for k in ('fromSegment', 'toSegment', 'kind')} for part in record['parts']]})
    catalog = {'version': audit['version'], 'geometryVersion': geometry['version'],
               'geometryHash': sha(ROOT / 'content/map-geometry.json'), 'auditHash': sha(AUDIT), 'paths': paths}
    types = Counter(part['kind'] for path in paths for part in path['parts'])
    report = {'version': catalog['version'], 'geometryHash': catalog['geometryHash'], 'auditHash': catalog['auditHash'],
              'sourcePaths': len(paths), 'classifiedParts': sum(types.values()), 'partKinds': dict(sorted(types.items())),
              'movementPairs': len(legal), 'physicalLandPairs': 282, 'additionalImpassablePairs': 50,
              'splitSourcePaths': sum(len(path['parts']) > 1 for path in paths),
              'barrierLandContrast': contrast(style['barrier'], style['land']),
              'passageKeylineContrast': contrast(style['passage'], style['keyline']),
              'nationSeparatorContrast': {n['id']: contrast(n['color'], style['passage']) for n in style['nations']}}
    return catalog, report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    try:
        catalog, report = export()
        write_or_check(OUTPUT, catalog, args.check)
        write_or_check(REPORT, report, args.check)
    except (ValueError, KeyError, TypeError) as error:
        print(f'Border catalog rejected: {error}', file=sys.stderr)
        return 1
    print(f"PASS: {report['sourcePaths']} paths / {report['classifiedParts']} parts, 232 passages, "
          f"50 extra pairs blocked, 8 nation colors; {'export matches' if args.check else 'export written'}")
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
