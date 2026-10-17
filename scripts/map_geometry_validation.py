"""Geometric checks for the reviewed, piecewise-linear map (standard library only)."""

from collections import defaultdict
import math


def require(condition, message):
    if not condition:
        raise ValueError(message)


def cross(a, b, c):
    return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])


def area(ring):
    return sum(a[0] * b[1] - b[0] * a[1] for a, b in zip(ring, ring[1:])) / 2


def point_location(ring, point):
    """Return -1 outside, 0 on the edge, 1 strictly inside."""
    inside = False
    x, y = point
    for a, b in zip(ring, ring[1:]):
        if cross(a, b, point) == 0 and min(a[0], b[0]) <= x <= max(a[0], b[0]) \
                and min(a[1], b[1]) <= y <= max(a[1], b[1]):
            return 0
        if (a[1] > y) != (b[1] > y):
            if x < (b[0] - a[0]) * (y - a[1]) / (b[1] - a[1]) + a[0]:
                inside = not inside
    return 1 if inside else -1


def contains(rings, point):
    return point_location(rings[0], point) == 1 and all(
        point_location(hole, point) == -1 for hole in rings[1:])


def validate_ring(ring, label):
    require(len(ring) >= 4 and ring[0] == ring[-1], f"Unclosed ring: {label}")
    require(all(len(p) == 2 and all(math.isfinite(v) for v in p) for p in ring),
            f"Invalid coordinate: {label}")
    require(len(set(map(tuple, ring[:-1]))) == len(ring) - 1,
            f"Repeated vertex or zero-length edge: {label}")
    require(area(ring) != 0, f"Zero-area ring: {label}")


def segment_key(a, b):
    return tuple(sorted((tuple(a), tuple(b))))


def ring_segments(regions):
    segments = defaultdict(list)
    for region in regions:
        for ring in region['rings']:
            points = ring['points']
            for a, b in zip(points, points[1:]):
                key = segment_key(a, b)
                segments[key].append((region['id'], tuple(a) == key[0]))
    return segments


def interiors_intersect(first, second):
    a, b = first
    c, d = second
    ab_c, ab_d = cross(a, b, c), cross(a, b, d)
    cd_a, cd_b = cross(c, d, a), cross(c, d, b)
    if ab_c * ab_d < 0 and cd_a * cd_b < 0:
        return True
    if ab_c == ab_d == 0:
        axis = 0 if a[0] != b[0] else 1
        return max(min(a[axis], b[axis]), min(c[axis], d[axis])) < \
            min(max(a[axis], b[axis]), max(c[axis], d[axis]))
    for p, start, end, orientation in ((c, a, b, ab_c), (d, a, b, ab_d),
                                      (a, c, d, cd_a), (b, c, d, cd_b)):
        if orientation == 0 and p != start and p != end \
                and min(start[0], end[0]) <= p[0] <= max(start[0], end[0]) \
                and min(start[1], end[1]) <= p[1] <= max(start[1], end[1]):
            return True
    return False


def validate_intersections(segments):
    """Broad phase bins avoid quadratic comparison of all 14k source segments."""
    buckets = defaultdict(list)
    checked = set()
    ordered = list(segments)
    for index, (a, b) in enumerate(ordered):
        for x in range(math.floor(min(a[0], b[0]) / 32), math.floor(max(a[0], b[0]) / 32) + 1):
            for y in range(math.floor(min(a[1], b[1]) / 32), math.floor(max(a[1], b[1]) / 32) + 1):
                bucket = buckets[x, y]
                for other in bucket:
                    pair = (other, index)
                    if pair in checked:
                        continue
                    checked.add(pair)
                    require(not interiors_intersect(ordered[other], (a, b)),
                            f"Crossing or overlapping segments: {ordered[other]}, {(a, b)}")
                bucket.append(index)


def validate_topology(regions, boundaries, legal_pairs):
    segments = ring_segments(regions)
    for edge, owners in segments.items():
        require(len(owners) in (1, 2) and len({o[0] for o in owners}) == len(owners),
                f"Invalid ring ownership: {edge}")
        require(len(owners) == 1 or owners[0][1] != owners[1][1],
                f"Overlapping region interiors: {edge}")
    validate_intersections(segments)
    boundary_segments = {}
    contacts = set()
    for boundary in boundaries:
        owners = {owner for owner in boundary['owners'] if owner is not None}
        if len(owners) == 2:
            contacts.add(tuple(sorted(owners)))
        for a, b in zip(boundary['points'], boundary['points'][1:]):
            edge = segment_key(a, b)
            require(edge not in boundary_segments, f"Duplicate boundary segment: {boundary['id']}")
            require(edge in segments and {o[0] for o in segments[edge]} == owners,
                    f"Boundary differs from its region rings: {boundary['id']}")
            boundary_segments[edge] = boundary['id']
    require(set(boundary_segments) == set(segments), "Missing shared boundary segments")
    require(legal_pairs <= contacts, f"Missing legal contacts: {sorted(legal_pairs - contacts)}")
    # No intersections + opposite seam directions exclude partial overlap. A fully
    # contained component is detected by its interior anchor, including water holes.
    for region in regions:
        rings = [r['points'] for r in region['rings']]
        for other in regions:
            if other is not region:
                require(not contains(rings, other['anchor']),
                        f"Contained region overlap: {region['id']} / {other['id']}")
    return len(segments), len(contacts)
