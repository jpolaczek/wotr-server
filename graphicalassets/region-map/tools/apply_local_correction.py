"""Replace shared boundaries in one recorded map correction, preserving other cells."""

import argparse
import hashlib
import json
from pathlib import Path

import cv2
import numpy as np
from shapely import intersects_xy
from shapely.geometry import LineString, Point
from shapely.ops import polygonize, unary_union

from export_map import ASSETS, make_chains, make_regions, validate


def load_faces(folder, correction, regions):
    geometry = json.loads((folder / "before-geometry.json").read_text())
    affected = set(correction["affected_regions"])
    replaced = correction.get("replace_pairs")
    if replaced is None:
        replaced = [edge["owners"] for edge in geometry["shared_boundaries"]
                    if set(edge["owners"]).issubset(affected)]
    replaced = {tuple(sorted(pair)) for pair in replaced}
    retained = [boundary for boundary in geometry["shared_boundaries"]
                if tuple(boundary["owners"]) not in replaced]
    boundaries = retained + correction["replacement_boundaries"]
    network = unary_union([LineString(edge["points"]) for edge in boundaries])
    seeds = {r["number"]: Point(r["point"]) for r in regions}
    faces = {}
    unclaimed = []
    for polygon in polygonize(network):
        owners = [number for number, point in seeds.items() if polygon.covers(point)]
        if not owners:
            unclaimed.append(polygon)
            continue
        if len(owners) != 1 or owners[0] in faces:
            raise ValueError(f"Ambiguous region ownership: {owners}")
        faces[owners[0]] = polygon
    if len(faces) != 105:
        raise ValueError(f"The correction produced {len(faces)} regions")
    if 0 in affected:
        faces[0] = unary_union(unclaimed)
    return faces


def rasterize_patch(before, faces, affected, edit_bounds=None, edit_windows=None):
    selected = np.isin(before, affected)
    windows = edit_windows if edit_windows is not None else (
        [edit_bounds] if edit_bounds is not None else None)
    if windows is not None:
        window = np.zeros(before.shape, dtype=bool)
        for x0, y0, x1, y1 in windows:
            window[y0:y1, x0:x1] = True
        selected &= window
    ys, xs = np.nonzero(selected)
    candidate = before.copy()
    for number in affected:
        covered = intersects_xy(faces[number], xs + 0.5, ys + 0.5)
        candidate[ys[covered], xs[covered]] = number
    if not np.array_equal(candidate[~selected], before[~selected]):
        raise ValueError("The patch changes a region outside its scope")
    return candidate, selected


def boundary_pixels(labels):
    boundary = np.zeros(labels.shape, dtype=bool)
    boundary[:, :-1] |= labels[:, :-1] != labels[:, 1:]
    boundary[:-1, :] |= labels[:-1, :] != labels[1:, :]
    return boundary


def make_overlay(labels, regions):
    scan = ASSETS.parent / "highedfmap.jpeg"
    if not scan.is_file():
        raise SystemExit(f"{scan} is a local-only board scan (D17, docs/sources/LOCAL-ONLY.json); place a copy there first.")
    source = cv2.imread(str(scan))
    preview = cv2.resize(source, (labels.shape[1], labels.shape[0]),
                         interpolation=cv2.INTER_AREA)
    preview[boundary_pixels(labels)] = [240, 0, 240]
    for region in regions:
        x, y = region["point"]
        cv2.circle(preview, (x, y), 6, (255, 255, 255), -1)
        cv2.putText(preview, str(region["number"]), (x - 5, y + 3),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.3, (0, 0, 0), 1, cv2.LINE_AA)
    return preview


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("correction", type=Path)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    folder = args.correction.resolve().parent
    correction = json.loads(args.correction.read_text())
    regions = json.loads((ASSETS / "reference/region-seeds.json").read_text())
    before = cv2.imread(str(folder / "before-labels.png"), cv2.IMREAD_UNCHANGED)
    faces = load_faces(folder, correction, regions)
    candidate, selected = rasterize_patch(before, faces, correction["affected_regions"],
                                          correction.get("edit_bounds"),
                                          correction.get("edit_windows"))
    chains = make_chains(candidate)
    shapes = make_regions(candidate, chains)
    report = validate(regions, shapes, chains, candidate)
    report["changed_pixels"] = int(np.count_nonzero(before != candidate))
    report["outside_selected_regions_changed_pixels"] = 0
    report["outside_edit_window_changed_pixels"] = int(
        np.count_nonzero(before[~selected] != candidate[~selected]))
    report["changed_regions"] = sorted(set(before[before != candidate].tolist())
                                      | set(candidate[before != candidate].tolist()))
    if report["errors"]:
        raise ValueError(report["errors"])
    cv2.imwrite(str(folder / "candidate-labels.png"), candidate)
    overlay = make_overlay(candidate, regions)
    cv2.imwrite(str(folder / "after-source-overlay.jpg"), overlay)
    x0, y0, x1, y1 = correction.get("detail_bounds", [820, 450, 1230, 840])
    cv2.imwrite(str(folder / "detail-overlay.png"),
                cv2.resize(overlay[y0:y1, x0:x1], None, fx=2, fy=2))
    if args.apply:
        current = ASSETS / "reference/region-labels.png"
        if hashlib.sha256(current.read_bytes()).hexdigest() != correction["before_labels_sha256"]:
            raise ValueError("The current mask differs from the recorded baseline")
        cv2.imwrite(str(current), candidate)
        cv2.imwrite(str(ASSETS / "reference/source-overlay.jpg"), overlay)
    report["applied"] = args.apply
    (folder / "verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
