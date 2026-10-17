"""Regression cases for the map importer and its independent topology checks."""

import copy
import json
from pathlib import Path
import unittest

import export_map_geometry as exporter
from map_geometry_validation import validate_intersections, validate_ring, validate_topology


class MapGeometryImportTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.source = exporter.read_json(exporter.ASSETS / 'geometry.json')
        cls.seeds = exporter.read_json(exporter.ASSETS / 'reference/region-seeds.json')
        cls.board = exporter.read_json(exporter.ROOT / 'content/base-board.json')

    def test_all_reviewed_sources_export_deterministically(self):
        geometry, report = exporter.export()
        self.assertEqual(exporter.read_json(exporter.OUTPUT), geometry)
        self.assertEqual(exporter.read_json(exporter.REPORT), report)
        self.assertEqual((105, 106, 323, 14348, 282, 232), tuple(report[key] for key in
                         ['regions', 'rings', 'sharedPaths', 'uniqueSegments', 'contactPairs', 'legalContactPairs']))

    def test_wrong_ids_and_mask_identity_are_rejected(self):
        for field, value in [('id', 'foreign'), ('id', self.source['regions'][1]['id']),
                             ('number', 0), ('number', self.source['regions'][1]['number'])]:
            with self.subTest(field=field, value=value):
                regions = copy.deepcopy(self.source['regions'])
                regions[0][field] = value
                with self.assertRaises(ValueError):
                    exporter.import_regions(regions, self.seeds, self.board)

    def test_anchor_in_water_is_rejected(self):
        seeds = copy.deepcopy(self.seeds)
        next(r for r in seeds if r['id'] == 'gorgoroth')['point'] = [1839.875, 1028.5]
        with self.assertRaisesRegex(ValueError, 'Anchor outside land: gorgoroth'):
            exporter.import_regions(self.source['regions'], seeds, self.board)

    def test_svg_parser_requires_explicit_closed_linear_rings(self):
        for path in ('M0,0 L2,0 L2,2', 'M0,0 Q2,0 2,2 Z', 'M0,0 LNaN,0 Z', 'M0,0 M2,2 Z', 'Z'):
            with self.subTest(path=path), self.assertRaises(ValueError):
                exporter.parse_rings(path)

    def test_degenerate_and_nonfinite_rings_are_rejected(self):
        rings = [((0, 0), (1, 1), (2, 2), (0, 0)),
                 ((0, 0), (1, 0), (1, 0), (0, 0)),
                 ((0, 0), (1, 0), (1, float('inf')), (0, 0)),
                 ((0, 0), (1, 0), (1, 1), (0, 1))]
        for ring in rings:
            with self.subTest(ring=ring), self.assertRaises(ValueError):
                validate_ring(ring, 'broken')

    def test_crossings_overlaps_and_unnoded_contacts_are_rejected(self):
        invalid = [(((0, 0), (4, 4)), ((0, 4), (4, 0))),
                   (((0, 0), (4, 0)), ((2, 0), (6, 0))),
                   (((0, 0), (4, 0)), ((2, 0), (2, 3)))]
        for segments in invalid:
            with self.subTest(segments=segments), self.assertRaises(ValueError):
                validate_intersections(segments)
        validate_intersections([((0, 0), (4, 0)), ((4, 0), (4, 3))])

    def test_shared_segments_must_match_both_owners_and_cover_all_rings(self):
        regions, numbers = exporter.import_regions(self.source['regions'], self.seeds, self.board)
        boundaries = exporter.import_boundaries(self.source['shared_boundaries'], numbers)
        for change in ('owner', 'missing', 'duplicate'):
            broken = copy.deepcopy(boundaries)
            if change == 'owner':
                broken[0]['owners'][1] = 'gorgoroth'
            elif change == 'missing':
                broken.pop()
            else:
                broken.append(broken[0])
            with self.subTest(change=change), self.assertRaises(ValueError):
                validate_topology(regions, broken, set())

    def test_wrong_viewbox_is_rejected_before_export(self):
        from unittest.mock import patch
        with patch.object(exporter, 'VIEW_BOX', [0, 0, 1632, 1338]):
            with self.assertRaisesRegex(ValueError, 'viewBox'):
                exporter.export()


if __name__ == '__main__':
    unittest.main()
