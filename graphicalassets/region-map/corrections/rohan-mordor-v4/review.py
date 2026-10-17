"""Verify the correction scope and render its two source-aligned details."""

import hashlib
import json
from pathlib import Path
import sys

import cairosvg
import cv2
import numpy as np


FOLDER = Path(__file__).resolve().parent
ASSETS = FOLDER.parents[1]
sys.path.insert(0, str(ASSETS / "tools"))
from export_map import collect_edges, make_chains, make_regions, svg_document


def read_labels(path):
    labels = cv2.imread(str(path), cv2.IMREAD_UNCHANGED)
    if labels is None:
        raise ValueError(f"Cannot read label mask: {path}")
    return labels


def verify_scope(correction, before, after):
    allowed = np.zeros(before.shape, dtype=bool)
    counts = {}
    changed = before != after
    for bounds in correction["edit_windows"]:
        x0, y0, x1, y1 = bounds
        allowed[y0:y1, x0:x1] = True
        counts[",".join(map(str, bounds))] = int(changed[y0:y1, x0:x1].sum())
    outside = int(np.count_nonzero(changed & ~allowed))
    water = int(np.count_nonzero((before == 0) != (after == 0)))
    assert outside == 0 and water == 0
    preserved = {}
    for name in correction["preserve_corrections"]:
        folder = FOLDER.parent / name
        old_before = read_labels(folder / "before-labels.png")
        old_after = read_labels(folder / "candidate-labels.png")
        altered = int(np.count_nonzero(changed & (old_before != old_after)))
        assert altered == 0, f"Earlier correction changed: {name}"
        preserved[name] = altered
    unchanged = [number for number in range(1, 106)
                 if np.array_equal(before == number, after == number)]
    return {
        "changed_pixels": int(changed.sum()),
        "changed_pixels_by_window": counts,
        "outside_windows_changed_pixels": outside,
        "water_mask_changed_pixels": water,
        "earlier_correction_pixels_changed": preserved,
        "entirely_unchanged_region_count": len(unchanged),
        "entirely_unchanged_region_numbers": unchanged,
    }


def render_detail(name, registration, regions, shapes, chains):
    source = cv2.imread(str(FOLDER / registration["source"]))
    matrix = np.array(registration["screenshot_to_working"])
    inverse = cv2.invertAffineTransform(matrix)
    for chain in chains:
        points = cv2.transform(np.array([chain["line"].coords]), inverse)[0]
        cv2.polylines(source, [np.round(points).astype(np.int32)], False,
                      (255, 0, 255), 1, cv2.LINE_AA)
    cv2.imwrite(str(FOLDER / f"{name}-after-overlay.png"), source)
    height, width = source.shape[:2]
    corners = cv2.transform(np.array([[[0, 0], [width, height]]], dtype=float), matrix)[0]
    x0, y0 = corners[0]
    x1, y1 = corners[1]
    svg = svg_document(regions, shapes, chains, [x0, y0, x1 - x0, y1 - y0],
                       width * 2, height * 2)
    path = ASSETS / f"map-detail-{name}.png"
    cairosvg.svg2png(bytestring=svg.encode(), write_to=str(path))


def audit_contacts(regions, before, after):
    old_edges = collect_edges(before)
    new_edges = collect_edges(after)
    assert set(old_edges) == set(new_edges), "Physical contacts changed"
    names = {region["number"]: region["id"] for region in regions}
    expected = json.loads((ASSETS / "reference/movement-neighbors.json").read_text())
    required = {frozenset(pair) for pair in expected}
    contacts = {frozenset((names[a], names[b])) for a, b in new_edges if a and b}
    missing = [pair for pair in expected if frozenset(pair) not in contacts]
    extra = [[names[a], names[b], len(edges)]
             for (a, b), edges in sorted(new_edges.items())
             if a and b and frozenset((names[a], names[b])) not in required]
    assert not missing
    report = {"missing": missing, "extra": extra}
    path = ASSETS / "reference/raster-adjacency-audit.json"
    path.write_text(json.dumps(report, indent=2) + "\n")
    return len(contacts)


def main():
    correction = json.loads((FOLDER / "correction.json").read_text())
    baseline = FOLDER / "before-labels.png"
    assert hashlib.sha256(baseline.read_bytes()).hexdigest() == correction["before_labels_sha256"]
    before = read_labels(baseline)
    after = read_labels(FOLDER / "candidate-labels.png")
    assert np.array_equal(after, read_labels(ASSETS / "reference/region-labels.png"))
    report = verify_scope(correction, before, after)
    regions = json.loads((ASSETS / "reference/region-seeds.json").read_text())
    report["unchanged_physical_region_contacts"] = audit_contacts(regions, before, after)
    chains = make_chains(after)
    shapes = make_regions(after, chains)
    for name, registration in correction["registrations"].items():
        render_detail(name, registration, regions, shapes, chains)
    (FOLDER / "scope-verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
