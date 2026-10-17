"""Prepare clipped render ribbons once; keep the authoritative contours and movement graph unchanged."""

import argparse
import hashlib
from collections import Counter

import shapely
from shapely.geometry import LineString, Polygon, box
from shapely.geometry.polygon import orient
from shapely.ops import unary_union

from export_map_geometry import ROOT, read_json, write_or_check
from map_geometry_validation import require

OUTPUT = ROOT / 'content/map-border-bands.json'
REPORT = ROOT / 'docs/sources/MAP-14-BANDS.json'
SOURCES = ('map-geometry.json', 'map-boundaries.json', 'map-style.json')
KEYLINE = .45


def polygons(shape):
    if shape.is_empty:
        return []
    if shape.geom_type == 'Polygon':
        return [shape]
    return [polygon for part in getattr(shape, 'geoms', []) for polygon in polygons(part)]


def rings(shape):
    result = []
    # Quantize topology before serialization, rather than rounding adjacent
    # intersection vertices into self-touching ribbons in the client.
    quantized = shapely.set_precision(shape, 1e-6, mode='valid_output')
    for polygon in polygons(quantized):
        polygon = orient(polygon, sign=1)
        for index, ring in enumerate([polygon.exterior, *polygon.interiors]):
            result.append({'hole': index > 0, 'points': [[round(x, 6), round(y, 6)] for x, y in ring.coords]})
    return result


def ribbon(line, region, inner, outer, sign):
    if region is None:
        return []
    strip = line.buffer(sign * outer, single_sided=True, cap_style='flat', join_style='round', quad_segs=2)
    center = line.buffer(inner, cap_style='flat', join_style='round', quad_segs=2)
    clipped = strip.difference(center).intersection(region)
    require(clipped.is_valid and clipped.difference(region).area < 1e-7, 'Ribbon escapes its owning region')
    return rings(clipped)


def export():
    geometry, borders, style = [read_json(ROOT / 'content' / path) for path in SOURCES]
    regions = {region['id']: Polygon(region['rings'][0]['points'],
                                    [ring['points'] for ring in region['rings'][1:]]) for region in geometry['regions']}
    require(all(region.is_valid for region in regions.values()), 'Invalid source polygon')
    geometry_paths = {path['id']: path for path in geometry['boundaries']}
    result = []
    inner = style['passageWidth'] / 2
    outer = inner + style['nationWidth']
    for path in borders['paths']:
        source = geometry_paths[path['id']]
        parts = []
        for part in path['parts']:
            item = {'fromSegment': part['fromSegment'], 'toSegment': part['toSegment']}
            line = LineString(source['points'][part['fromSegment']:part['toSegment'] + 1])
            for side, sign in (('positive', 1), ('negative', -1)):
                owner = path[side + 'NormalOwner']
                region = regions.get(owner) if part['kind'] in ('Passage', 'River') else None
                item[side] = ribbon(line, region, inner, outer, sign)
                item[side + 'Outline'] = ribbon(line, region, inner, outer + KEYLINE, sign)
            parts.append(item)
        result.append({'id': path['id'], 'parts': parts})
    hashes = {name: hashlib.sha256((ROOT / 'content' / name).read_bytes()).hexdigest() for name in SOURCES}
    water = box(0, 0, geometry['width'], geometry['height']).difference(unary_union(list(regions.values())))
    data = {'version': 'border-bands-1', 'sourceHashes': hashes, 'keylineWidth': KEYLINE,
            'water': rings(water), 'paths': result}
    counts = Counter()
    for path in result:
        for part in path['parts']:
            for side in ('positive', 'negative', 'positiveOutline', 'negativeOutline'):
                counts['rings'] += len(part[side])
                counts['points'] += sum(len(r['points']) - 1 for r in part[side])
    report = {'version': data['version'], 'sourceHashes': hashes, 'paths': len(result),
              'parts': sum(len(path['parts']) for path in result), **counts, 'clipToOwner': True,
              'cap': 'flat', 'join': 'round, 2 segments per quadrant', 'coordinateDecimals': 6,
              'shapely': shapely.__version__, 'geos': shapely.geos_version_string,
              'waterRings': len(data['water']), 'waterPoints': sum(len(r['points']) - 1 for r in data['water']),
              'geometryChanged': False, 'runtimeClipping': False}
    return data, report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    data, report = export()
    write_or_check(OUTPUT, data, args.check)
    write_or_check(REPORT, report, args.check)
    print(f"PASS: {report['paths']} border paths / {report['parts']} parts, {report['rings']} clipped rings, "
          f"{report['points']} render points; {'export matches' if args.check else 'export written'}")


if __name__ == '__main__':
    main()
