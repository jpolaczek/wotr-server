"""Export shared map boundaries and named regions from the reviewed label mask."""

from __future__ import annotations

import json
from collections import defaultdict
from pathlib import Path
from xml.sax.saxutils import escape

import cairosvg
import cv2
import numpy as np
from shapely.geometry import LineString, Point
from shapely.ops import polygonize, unary_union


ASSETS = Path(__file__).resolve().parents[1]
PNG_WIDTH = 6144
SIMPLIFICATION = 0.6


def collect_edges(labels):
    """Place each edge once on pixel corners, indexed by its two owners."""
    padded = np.pad(labels, 1)
    by_pair = defaultdict(list)
    for vertical in (True, False):
        if vertical:
            first, second = padded[:, :-1], padded[:, 1:]
        else:
            first, second = padded[:-1, :], padded[1:, :]
        ys, xs = np.nonzero(first != second)
        for x, y in zip(xs.tolist(), ys.tolist()):
            pair = tuple(sorted((int(first[y, x]), int(second[y, x]))))
            if vertical:
                edge = ((x, y - 1), (x, y))
            else:
                edge = ((x - 1, y), (x, y))
            by_pair[pair].append(edge)
    return by_pair


def trace_pair(edges):
    graph = defaultdict(set)
    for start, end in edges:
        graph[start].add(end)
        graph[end].add(start)
    unvisited = {tuple(sorted(edge)) for edge in edges}

    def walk(start, following):
        path = [start]
        current = start
        while True:
            unvisited.remove(tuple(sorted((current, following))))
            path.append(following)
            previous, current = current, following
            if len(graph[current]) != 2 or current == start:
                break
            candidates = graph[current] - {previous}
            following = next(iter(candidates))
            if tuple(sorted((current, following))) not in unvisited:
                break
        return path

    for start in sorted(graph):
        if len(graph[start]) == 2:
            continue
        for following in sorted(graph[start]):
            if tuple(sorted((start, following))) in unvisited:
                yield walk(start, following)
    while unvisited:
        start, following = min(unvisited)
        yield walk(start, following)


def make_chains(labels):
    chains = []
    for pair, edges in sorted(collect_edges(labels).items()):
        for points in trace_pair(edges):
            line = LineString(points).simplify(SIMPLIFICATION)
            chains.append({"owners": pair, "line": line})
    return chains


def make_regions(labels, chains):
    pieces = defaultdict(list)
    for polygon in polygonize(unary_union([c["line"] for c in chains])):
        point = polygon.representative_point()
        x, y = int(point.x), int(point.y)
        if 0 <= y < labels.shape[0] and 0 <= x < labels.shape[1]:
            number = int(labels[y, x])
            if number:
                pieces[number].append(polygon)
    return {number: unary_union(parts) for number, parts in pieces.items()}


def path_points(points, close=False):
    points = list(points)
    if close and points[-1] == points[0]:
        points.pop()
    commands = [f"M{points[0][0]:.2f},{points[0][1]:.2f}"]
    commands.extend(f"L{x:.2f},{y:.2f}" for x, y in points[1:])
    if close:
        commands.append("Z")
    return " ".join(commands)


def polygon_path(polygon):
    contours = [polygon.exterior, *polygon.interiors]
    return " ".join(path_points(ring.coords, close=True) for ring in contours)


def svg_document(regions, shapes, chains, viewbox, width, height):
    extent = " ".join(str(value) for value in viewbox)
    lines = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" '
        f'height="{height}" viewBox="{extent}">',
        '<title>War of the Ring — 105 region outlines</title>',
        '<desc>Shared territorial contours; transparent sea and lakes. '
        'Region identifiers are metadata, not movement permissions.</desc>',
        '<g id="regions" fill="#fff" fill-rule="evenodd" stroke="none">',
    ]
    for region in regions:
        shape = shapes[region["number"]]
        lines.append(
            f'<path id="region-{region["id"]}" '
            f'data-region="{region["id"]}" d="{polygon_path(shape)}">'
            f'<title>{escape(region["name"])}</title></path>'
        )
    lines.append('</g><g id="boundaries" fill="none" stroke="#171717" '
                 'stroke-width="1.45" stroke-linejoin="round" '
                 'stroke-linecap="round">')
    names = {r["number"]: r["id"] for r in regions}
    names[0] = "outside"
    for index, chain in enumerate(chains):
        first, second = (names[n] for n in chain["owners"])
        lines.append(
            f'<path id="boundary-{index:04d}" data-region-a="{first}" '
            f'data-region-b="{second}" d="{path_points(chain["line"].coords)}"/>'
        )
    lines.append('</g></svg>')
    return "\n".join(lines)


def validate(regions, shapes, chains, labels):
    errors = []
    for region in regions:
        number = region["number"]
        shape = shapes.get(number)
        if shape is None:
            errors.append(f"Missing region: {region['id']}")
            continue
        if shape.geom_type != "Polygon" or not shape.is_valid:
            errors.append(f"Invalid or disconnected region: {region['id']}")
        if not shape.covers(Point(region["point"])):
            errors.append(f"Region does not include its seed: {region['id']}")
    merged = unary_union(list(shapes.values()))
    overlap = sum(shape.area for shape in shapes.values()) - merged.area
    if overlap > 1e-6:
        errors.append(f"Overlapping areas: {overlap}")
    numbers = {r["id"]: r["number"] for r in regions}
    expected = json.loads((ASSETS / "reference/movement-neighbors.json").read_text())
    missing = []
    for first, second in expected:
        a, b = shapes[numbers[first]], shapes[numbers[second]]
        if a.boundary.intersection(b.boundary).length <= 0.5:
            missing.append([first, second])
    if missing:
        errors.append(f"Missing legal contacts: {missing}")
    return {
        "regions": len(shapes),
        "single_closed_valid_polygons": sum(
            p.is_valid and p.geom_type == "Polygon" for p in shapes.values()
        ),
        "shared_boundary_chains": len(chains),
        "expected_movement_contacts": len(expected),
        "missing_movement_contacts": missing,
        "overlap_area_working_pixels": round(overlap, 9),
        "working_raster_size": [int(labels.shape[1]), int(labels.shape[0])],
        "simplification_tolerance_working_pixels": SIMPLIFICATION,
        "errors": errors,
    }


def main():
    labels = cv2.imread(str(ASSETS / "reference/region-labels.png"), cv2.IMREAD_UNCHANGED)
    regions = json.loads((ASSETS / "reference/region-seeds.json").read_text())
    chains = make_chains(labels)
    shapes = make_regions(labels, chains)
    report = validate(regions, shapes, chains, labels)
    (ASSETS / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    if report["errors"]:
        raise ValueError(report["errors"])
    x0, y0, x1, y1 = unary_union(list(shapes.values())).bounds
    viewbox = [x0 - 20, y0 - 20, x1 - x0 + 40, y1 - y0 + 40]
    height = round(PNG_WIDTH * viewbox[3] / viewbox[2])
    svg = svg_document(regions, shapes, chains, viewbox, PNG_WIDTH, height)
    (ASSETS / "map-regions.svg").write_text(svg + "\n")
    cairosvg.svg2png(bytestring=svg.encode(), write_to=str(ASSETS / "map-regions.png"))
    cairosvg.svg2png(bytestring=svg.encode(), write_to=str(ASSETS / "map-preview.png"),
                    output_width=1536, output_height=round(height / 4))
    geometry = {
        "schema": 1,
        "coordinate_system": "2008x1418 source working pixels",
        "view_box": viewbox,
        "png_size": [PNG_WIDTH, height],
        "regions": [dict(id=r["id"], name=r["name"], number=r["number"],
                         path=polygon_path(shapes[r["number"]]),
                         bounds=list(shapes[r["number"]].bounds)) for r in regions],
        "shared_boundaries": [dict(owners=c["owners"], points=list(c["line"].coords))
                              for c in chains],
    }
    (ASSETS / "geometry.json").write_text(json.dumps(geometry, indent=2) + "\n")
    report["png_size"] = [PNG_WIDTH, height]
    (ASSETS / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
