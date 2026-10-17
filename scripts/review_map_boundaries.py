"""Create reproducible source-board review overlays for the border classification audit."""

import argparse
from html import escape
import math

from export_map_boundaries import export
from export_map_geometry import ROOT, read_json

OUTPUT = ROOT / 'docs/sources/map13-review'
COLORS = {'Passage': '#35ED61', 'River': '#63DDEC', 'Impassable': '#FF344B',
          'Coast': '#1597FF', 'Lake': '#1597FF', 'Frame': '#D7A5FF'}
DETAILS = {'black-gate': (1465, 745, 245, 205),
           'minas-morgul': (1485, 885, 250, 230),
           'moria': (955, 405, 275, 225),
           'high-pass': (945, 210, 275, 205),
           'two-nations': (425, 235, 225, 260)}


def coordinates(points):
    return ' '.join(f'{x + 335:g},{y + 40:g}' for x, y in points)


def polyline(points, color, width=1.4):
    return (f'<polyline points="{coordinates(points)}" fill="none" '
            f'stroke="{color}" stroke-width="{width}" stroke-linejoin="round"/>')


def label(point, text, size=7):
    attributes = (f'x="{point[0]:g}" y="{point[1]:g}" font-size="{size}" '
                  'font-family="monospace" fill="white"')
    return (f'<text {attributes} stroke="#14181C" stroke-width="1.8">{escape(text)}</text>'
            f'<text {attributes}>{escape(text)}</text>')


def visible(points, box):
    x, y, width, height = box
    return not (max(p[0] for p in points) + 335 < x or min(p[0] for p in points) + 335 > x + width or
                max(p[1] for p in points) + 40 < y or min(p[1] for p in points) + 40 > y + height)


def nation_bands(points, border, board, style):
    """Diagnostic offset segments, not the joined production renderer in MAP-14."""
    nations = [board[border[side]]['nation'] if border[side] else None
               for side in ('positiveNormalOwner', 'negativeNormalOwner')]
    if nations[0] == nations[1]:
        return []
    palette = {nation['id']: nation['color'] for nation in style['nations']}
    result = []
    for a, b in zip(points, points[1:]):
        dx, dy = b[0] - a[0], b[1] - a[1]
        length = math.hypot(dx, dy)
        for sign, nation in zip((1, -1), nations):
            if nation:
                offset = (-dy / length * sign * 2, dx / length * sign * 2)
                shifted = [[p[0] + offset[0], p[1] + offset[1]] for p in (a, b)]
                result.append(polyline(shifted, palette[nation], 2.2))
    result.append(polyline(points, style['passage'], 1.5))
    return result


def draw_path(path, border, board, style, show_nations):
    result = []
    for number, part in enumerate(border['parts']):
        points = path['points'][part['fromSegment']:part['toSegment'] + 1]
        kind = part['kind']
        if show_nations and kind in ('Passage', 'River'):
            result.extend(nation_bands(points, border, board, style))
        else:
            result.append(polyline(points, COLORS[kind]))
        midpoint = points[len(points) // 2]
        result.append(label((midpoint[0] + 336, midpoint[1] + 38),
                            f"{path['id'][9:]}:{number}/{kind[0]}"))
        if number:
            x, y = points[0]
            result.append(f'<circle cx="{x + 335:g}" cy="{y + 40:g}" r="2" '
                          'fill="white" stroke="black" stroke-width=".8"/>')
    return result


def overlay(name, box, geometry, catalog, board, style):
    x, y, width, height = box
    result = [f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" '
              f'viewBox="{x} {y} {width} {height}" width="{width * 2}" height="{height * 2}">',
              '<title>MAP-13 source classification review: ' + name + '</title>',
              '<desc>Source board overlaid with audited ranges, not gameplay rendering. '
              'P green passage; R cyan river; I red impassable; C/L blue water; F violet frame. '
              'Labels are boundary:part/type. White dots mark classification changes. '
              'Two-nations detail uses nation bands and a white separator for passable boundaries.</desc>',
              '<image x="0" y="0" width="2008" height="1418" preserveAspectRatio="none" '
              'xlink:href="../../../graphicalassets/highedfmap.jpeg"/>']
    by_id = {border['id']: border for border in catalog['paths']}
    for path in geometry['boundaries']:
        if visible(path['points'], box):
            result.extend(draw_path(path, by_id[path['id']], board, style, name == 'two-nations'))
    result.extend(['</svg>', ''])
    return '\n'.join(result)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    parser.add_argument('--render', action='store_true', help='Also render PNGs with optional CairoSVG in .artifacts')
    args = parser.parse_args()
    catalog, _ = export()
    geometry = read_json(ROOT / 'content/map-geometry.json')
    board = {r['id']: r for r in read_json(ROOT / 'content/base-board.json')['regions']}
    style = read_json(ROOT / 'content/map-style.json')
    boxes = {'whole-map': (335, 40, 1632, 1338)}
    boxes.update({f'sector-{row}-{column}': (335 + column * 544, 40 + row * 446, 544, 446)
                  for row in range(3) for column in range(3)})
    boxes.update(DETAILS)
    OUTPUT.mkdir(exist_ok=True)
    for name, box in boxes.items():
        path = OUTPUT / (name + '.svg')
        value = overlay(name, box, geometry, catalog, board, style)
        if args.check:
            if not path.exists() or path.read_text() != value:
                raise ValueError(f'Stale border review: {path.relative_to(ROOT)}')
        else:
            path.write_text(value)
        if args.render:
            import cairosvg
            destination = ROOT / '.artifacts/map13-review'
            destination.mkdir(exist_ok=True)
            # The generated SVG references only the checked-in local board JPEG.
            cairosvg.svg2png(url=str(path), write_to=str(destination / (name + '.png')), unsafe=True)
    print(f'PASS: {len(boxes)} source review overlays {"match" if args.check else "written"}')


if __name__ == '__main__':
    main()
