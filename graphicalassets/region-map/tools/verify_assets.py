"""Check named SVG regions, raster separation and the accepted forest fragment."""

import hashlib
import json
from pathlib import Path
from xml.etree import ElementTree

import cv2
import numpy as np


ASSETS = Path(__file__).resolve().parents[1]
FOREST_SAMPLES = {
    "western-mirkwood": (80, 110),
    "woodland-realm": (164, 140),
    "old-forest-road": (147, 192),
    "narrows-of-the-forest": (114, 250),
    "eastern-mirkwood": (177, 260),
    "dol-guldur": (106, 367),
    "southern-mirkwood": (183, 357),
}


def check_svg(regions):
    root = ElementTree.parse(ASSETS / "map-regions.svg").getroot()
    paths = root.findall(".//{http://www.w3.org/2000/svg}path")
    region_paths = [p for p in paths if p.get("data-region")]
    found = {p.get("data-region") for p in region_paths}
    expected = {r["id"] for r in regions}
    assert len(region_paths) == 105 and found == expected
    assert not root.findall(".//{http://www.w3.org/2000/svg}text")
    return {"named_regions": len(region_paths), "visible_text_elements": 0}


def check_png():
    image = cv2.imread(str(ASSETS / "map-regions.png"), cv2.IMREAD_UNCHANGED)
    assert image.shape == (5037, 6144, 4)
    white = (image[:, :, :3].min(axis=2) > 230) & (image[:, :, 3] > 200)
    count, _, stats, _ = cv2.connectedComponentsWithStats(white.astype(np.uint8), 4)
    assert count - 1 == 105
    assert image[0, 0, 3] == 0
    return {"size": [image.shape[1], image.shape[0]], "white_components": count - 1,
            "minimum_white_component_area": int(stats[1:, cv2.CC_STAT_AREA].min()),
            "transparent_exterior": True}


def check_fragment(regions):
    labels = cv2.imread(str(ASSETS / "reference/region-labels.png"), cv2.IMREAD_UNCHANGED)
    fragment = cv2.imread(str(ASSETS.parent / "mirkwood-regions-outline-v1.png"), 0)
    registration = json.loads((ASSETS / "reference/registration.json").read_text())
    transform = (np.diag([0.5, 0.5, 1.0]) @ np.array(registration["crop"]["matrix"])
                 @ np.diag([264 / fragment.shape[1], 456 / fragment.shape[0], 1.0]))
    _, components = cv2.connectedComponents((fragment > 170).astype(np.uint8), 8)
    numbers = {r["id"]: r["number"] for r in regions}
    retention = {}
    for name, (x, y) in FOREST_SAMPLES.items():
        component = components[int(y / 456 * fragment.shape[0]),
                               int(x / 264 * fragment.shape[1])]
        mask = cv2.warpPerspective((components == component).astype(np.uint8), transform,
                                   (labels.shape[1], labels.shape[0]),
                                   flags=cv2.INTER_NEAREST) > 0
        retention[name] = round(float(np.mean(labels[mask] == numbers[name])), 6)
        assert retention[name] >= 0.999
    return retention


def file_hash(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    regions = json.loads((ASSETS / "reference/region-seeds.json").read_text())
    report = {
        "svg": check_svg(regions),
        "png": check_png(),
        "accepted_fragment_interior_retention": check_fragment(regions),
        "sha256": {name: file_hash(ASSETS / name)
                   for name in ["map-regions.svg", "map-regions.png",
                                "reference/region-labels.png"]},
    }
    (ASSETS / "asset-verification.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
