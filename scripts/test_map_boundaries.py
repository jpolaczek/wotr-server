"""Behavioral checks for source-audited border coverage, orientation, and colors."""

import copy
import unittest

import export_map_boundaries as borders
from export_map_geometry import ROOT, read_json


class MapBorderImportTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.geometry = read_json(ROOT / 'content/map-geometry.json')
        cls.board = read_json(ROOT / 'content/base-board.json')
        cls.audit = read_json(borders.AUDIT)

    def test_catalog_is_reproducible_and_covers_all_paths(self):
        catalog, report = borders.export()
        self.assertEqual(read_json(borders.OUTPUT), catalog)
        self.assertEqual(read_json(borders.REPORT), report)
        self.assertEqual((323, 360, 33), tuple(report[k] for k in
                                             ('sourcePaths', 'classifiedParts', 'splitSourcePaths')))

    def test_missing_duplicate_and_unsourced_paths_are_rejected(self):
        for change in ('missing', 'duplicate', 'source'):
            audit = copy.deepcopy(self.audit)
            if change == 'missing':
                audit['boundaries'].pop()
            elif change == 'duplicate':
                audit['boundaries'][1] = audit['boundaries'][0]
            else:
                audit['boundaries'][0]['evidence'] = ''
            with self.subTest(change=change), self.assertRaises(ValueError):
                borders.validate_audit(audit, self.geometry, self.board)

    def test_range_gaps_overlap_and_water_land_confusion_are_rejected(self):
        for change in ('gap', 'overlap', 'water'):
            audit = copy.deepcopy(self.audit)
            path = audit['boundaries'][208]
            if change == 'gap':
                path['parts'][1]['fromSegment'] += 1
            elif change == 'overlap':
                path['parts'][1]['fromSegment'] -= 1
            else:
                path['parts'][0]['kind'] = 'Lake'
            with self.subTest(change=change), self.assertRaises(ValueError):
                borders.validate_audit(audit, self.geometry, self.board)

    def test_an_extra_contact_cannot_gain_passage(self):
        audit = copy.deepcopy(self.audit)
        audit['boundaries'][50]['parts'][0]['kind'] = 'Passage'
        with self.assertRaisesRegex(ValueError, 'absent from movement graph'):
            borders.validate_audit(audit, self.geometry, self.board)

    def test_nurn_has_only_the_western_passage_despite_two_shared_paths(self):
        self.assertTrue(all(p['kind'] == 'Impassable' for p in self.audit['boundaries'][213]['parts']))
        audit = copy.deepcopy(self.audit)
        for part in audit['boundaries'][212]['parts']:
            part['kind'] = 'Impassable'
        with self.assertRaisesRegex(ValueError, 'Missing printed passage'):
            borders.validate_audit(audit, self.geometry, self.board)

    def test_reversing_every_path_swaps_normal_owners_without_changing_physical_sides(self):
        original = borders.positive_owners(self.geometry)
        reverse = copy.deepcopy(self.geometry)
        for path in reverse['boundaries']:
            path['points'].reverse()
        swapped = borders.positive_owners(reverse)
        for key, owners in original.items():
            self.assertEqual(tuple(reversed(owners)), swapped[key], key)

    def test_palette_preserves_printed_nations_and_readable_line_hierarchy(self):
        style = read_json(ROOT / 'content/map-style.json')
        borders.validate_style(style)
        self.assertGreaterEqual(borders.contrast(style['barrier'], style['land']), 7)
        self.assertGreaterEqual(borders.contrast(style['passage'], style['keyline']), 7)
        for change in ('nation', 'color', 'width'):
            broken = copy.deepcopy(style)
            if change == 'nation':
                broken['nations'][0]['id'] = 'free-peoples'
            elif change == 'color':
                broken['nations'][0]['color'] = 'brown'
            else:
                broken['barrierWidth'] = broken['passageWidth']
            with self.subTest(change=change), self.assertRaises(ValueError):
                borders.validate_style(broken)


if __name__ == '__main__':
    unittest.main()
