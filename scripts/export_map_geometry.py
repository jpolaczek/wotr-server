"""Import reviewed contours without retracing, resimplifying, or deriving game rules."""

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

from map_geometry_validation import area, contains, point_location, require, validate_ring, validate_topology

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'graphicalassets/region-map'
OUTPUT = ROOT / 'content/map-geometry.json'
REPORT = ROOT / 'docs/sources/MAP-12-GEOMETRY.json'
VERSION = 'contours-2'
VIEW_BOX = [335, 40, 1632, 1338]
PNG_SIZE = [6144, 5037]
SOURCES = ('graphicalassets/region-map/geometry.json',
           'graphicalassets/region-map/map-regions.svg',
           'graphicalassets/region-map/reference/region-seeds.json',
           'graphicalassets/region-map/reference/region-labels.png',
           'graphicalassets/region-map/reference/raster-adjacency-audit.json',
           'content/base-board.json')
TOKEN = re.compile(r'[MLZ]|-?(?:\d+(?:\.\d*)?|\.\d+)')


def read_json(path):
    return json.loads(path.read_text())


def parse_rings(path):
    require(not re.sub(r'[\s,]+', '', TOKEN.sub('', path)), 'Unsupported SVG path command')
    tokens = TOKEN.findall(path)
    rings, points = [], []
    index = 0
    while index < len(tokens):
        command = tokens[index]
        index += 1
        if command == 'Z':
            require(bool(points), 'Empty SVG ring')
            rings.append(points + [points[0]])
            points = []
            continue
        require(command in ('M', 'L') and index + 1 < len(tokens), 'Malformed SVG path')
        require((command == 'M') == (not points), 'SVG ring must start with M and end with Z')
        points.append([float(tokens[index]), float(tokens[index + 1])])
        index += 2
    require(not points and rings, 'Unclosed SVG ring')
    return rings


def transform(point):
    return [point[0] - VIEW_BOX[0], point[1] - VIEW_BOX[1]]


def import_regions(source, seeds, board):
    expected = {region['id'] for region in board['regions']}
    require(len(source) == len(expected) == 105, 'Expected 105 regions')
    require({r['id'] for r in source} == expected, 'Foreign, missing, or repeated region ID')
    require(len(seeds) == 105 and {r['id'] for r in seeds} == expected, 'Invalid seed IDs')
    by_id = {r['id']: r for r in seeds}
    numbers = {0: None}
    result = []
    for region in source:
        seed = by_id[region['id']]
        number = region['number']
        require(number not in numbers and number == seed['number'], 'Duplicate or mismatched mask number')
        numbers[number] = region['id']
        rings = []
        for index, ring in enumerate(parse_rings(region['path'])):
            points = [transform(point) for point in ring]
            validate_ring(points, region['id'])
            require(all(0 <= p[0] <= VIEW_BOX[2] and 0 <= p[1] <= VIEW_BOX[3] for p in points),
                    f"Ring outside client viewBox: {region['id']}")
            if (area(points) > 0) == (index > 0):
                points.reverse()
            rings.append({'hole': index > 0, 'points': points})
        anchor = transform(seed['point'])
        raw = [r['points'] for r in rings]
        require(contains(raw, anchor), f"Anchor outside land: {region['id']}")
        for hole in raw[1:]:
            locations = [point_location(raw[0], point) for point in hole[:-1]]
            require(min(locations) >= 0 and max(locations) == 1, f"Hole outside shell: {region['id']}")
        result.append({'id': region['id'], 'maskNumber': number, 'rings': rings, 'anchor': anchor})
    return result, numbers


def import_boundaries(source, numbers):
    require(len(source) == 323, 'Expected 323 reviewed boundary paths')
    result = []
    for index, boundary in enumerate(source):
        owners = boundary['owners']
        require(len(owners) == 2 and len(set(owners)) == 2 and all(o in numbers for o in owners),
                f'Invalid boundary owners: {index}')
        points = [transform(point) for point in boundary['points']]
        require(len(points) >= 2, f'Empty boundary: {index}')
        result.append({'id': f'boundary-{index:04d}', 'sourceIndex': index,
                       'owners': [numbers[o] for o in owners], 'points': points})
    return result


def verify_svg(source):
    root = ET.parse(ASSETS / 'map-regions.svg').getroot()
    require(list(map(float, root.attrib['viewBox'].split())) == VIEW_BOX, 'SVG viewBox mismatch')
    paths = {node.attrib['id']: node.attrib['d'] for node in root.iter()
             if node.tag.endswith('}path')}
    for region in source['regions']:
        require(paths.get('region-' + region['id']) == region['path'], f"SVG region mismatch: {region['id']}")
    for index, boundary in enumerate(source['shared_boundaries']):
        path = paths.get(f'boundary-{index:04d}', '')
        numbers = [float(t) for t in TOKEN.findall(path) if t not in ('M', 'L', 'Z')]
        require(numbers == [n for p in boundary['points'] for n in p], f'SVG boundary mismatch: {index}')


def export():
    source = read_json(ASSETS / 'geometry.json')
    require(source['schema'] == 1 and source['coordinate_system'] == '2008x1418 source working pixels',
            'Unsupported source coordinate system')
    require(source['view_box'] == VIEW_BOX and source['png_size'] == PNG_SIZE, 'Source viewBox/PNG mismatch')
    verify_svg(source)
    board = read_json(ROOT / 'content/base-board.json')
    regions, numbers = import_regions(source['regions'], read_json(ASSETS / 'reference/region-seeds.json'), board)
    boundaries = import_boundaries(source['shared_boundaries'], numbers)
    legal = {tuple(sorted((r['id'], n))) for r in board['regions'] for n in r['neighbors']}
    require(len(legal) == 232, 'Movement catalog changed; review the import')
    segments, contacts = validate_topology(regions, boundaries, legal)
    hashes = {path: hashlib.sha256((ROOT / path).read_bytes()).hexdigest() for path in SOURCES}
    scale = min(PNG_SIZE[0] / VIEW_BOX[2], PNG_SIZE[1] / VIEW_BOX[3])
    geometry = {'version': VERSION, 'width': VIEW_BOX[2], 'height': VIEW_BOX[3],
                'transform': {'sourceViewBox': VIEW_BOX, 'pngSize': PNG_SIZE,
                              'sourceOffset': [-VIEW_BOX[0], -VIEW_BOX[1]], 'sourceScale': 1,
                              'pngScale': scale,
                              'pngOffset': [(PNG_SIZE[i] - VIEW_BOX[i + 2] * scale) / 2 for i in range(2)]},
                'sourceHashes': hashes, 'regions': regions, 'boundaries': boundaries}
    report = {'version': VERSION, 'sourceHashes': hashes, 'regions': len(regions),
              'rings': sum(len(r['rings']) for r in regions),
              'ringPointsWithoutClosure': sum(len(ring['points']) - 1 for r in regions for ring in r['rings']),
              'sharedPaths': len(boundaries), 'uniqueSegments': segments, 'contactPairs': contacts,
              'legalContactPairs': len(legal), 'additionalContactPairs': contacts - len(legal),
              'addedSimplification': 0, 'sourceSimplificationWorkingPixels': 0.6,
              'overlappingRegions': 0, 'waterHoleRegions': [r['id'] for r in regions if len(r['rings']) > 1],
              'anchorSource': 'reference/region-seeds.json:point (not seed)',
              'interpolatedSourceAreas': ['Misty Mountains ridges', 'mountains around Mordor',
                                         'South Rhun/Barad-dur/Gorgoroth under the character panel',
                                         'northeast crop under the Fellowship track']}
    return geometry, report


def write_or_check(path, value, check):
    text = json.dumps(value, indent=2, ensure_ascii=False)
    # One coordinate pair per line keeps the versioned mesh reviewable.
    number = r'-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?'
    text = re.sub(r'\[\n\s+(' + number + r'),\n\s+(' + number + r')\n\s+\]', r'[\1, \2]', text)
    data = (text + '\n').encode()
    if check:
        require(path.exists() and path.read_bytes() == data, f'Stale export: {path.relative_to(ROOT)}')
    else:
        path.write_bytes(data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    parser.add_argument('--borders', action='store_true', help='Also validate the reviewed border catalog and palette')
    args = parser.parse_args()
    try:
        geometry, report = export()
        write_or_check(OUTPUT, geometry, args.check)
        write_or_check(REPORT, report, args.check)
        if args.borders:
            from export_map_boundaries import export as export_borders, OUTPUT as BORDERS, REPORT as BORDER_REPORT
            catalog, border_report = export_borders()
            write_or_check(BORDERS, catalog, args.check)
            write_or_check(BORDER_REPORT, border_report, args.check)
            print(f"PASS: {border_report['sourcePaths']} border paths / {border_report['classifiedParts']} classified parts")
    except (ValueError, KeyError, TypeError) as error:
        print(f'Map geometry rejected: {error}', file=sys.stderr)
        return 1
    print(f"PASS: {report['regions']} regions, {report['rings']} rings, "
          f"{report['sharedPaths']} paths, {report['legalContactPairs']} legal contacts; "
          f"{'export matches' if args.check else 'export written'}")
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
