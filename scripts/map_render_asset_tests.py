"""Asset preparation invariants for MAP-14 (run with the pinned Shapely tool environment)."""

import unittest
from xml.etree import ElementTree as ET

from shapely.geometry import Polygon

from export_map_border_bands import OUTPUT, REPORT, export
from export_map_geometry import ROOT, read_json
from export_map_terrain import SOURCE, SVG, terrain_only


class MapRenderAssetTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data = read_json(OUTPUT)
        cls.geometry = read_json(ROOT / 'content/map-geometry.json')
        cls.borders = read_json(ROOT / 'content/map-boundaries.json')
        cls.regions = {region['id']: Polygon(region['rings'][0]['points'],
                      [ring['points'] for ring in region['rings'][1:]]) for region in cls.geometry['regions']}

    def test_bands_and_report_are_reproducible(self):
        data, report = export()
        self.assertEqual(self.data, data)
        self.assertEqual(read_json(REPORT), report)

    def test_all_serialized_ribbons_are_valid_and_remain_on_the_owning_side(self):
        count = 0
        for path, border in zip(self.data['paths'], self.borders['paths']):
            self.assertEqual(path['id'], border['id'])
            for part in path['parts']:
                for side in ('positive', 'negative'):
                    owner = border[side + 'NormalOwner']
                    for ring in part[side] + part[side + 'Outline']:
                        polygon = Polygon(ring['points'])
                        self.assertTrue(polygon.is_valid, (path['id'], side))
                        self.assertGreater(polygon.area, 1e-8)
                        self.assertTrue(self.regions[owner].buffer(2e-6).covers(polygon))
                        count += 1
        self.assertEqual(935, count)

    def test_barriers_water_and_frame_never_gain_nation_ribbons(self):
        for path, border in zip(self.data['paths'], self.borders['paths']):
            for part, source in zip(path['parts'], border['parts']):
                self.assertEqual((source['fromSegment'], source['toSegment']), (part['fromSegment'], part['toSegment']))
                if source['kind'] not in ('Passage', 'River'):
                    self.assertFalse(part['positive'] or part['negative'] or part['positiveOutline'] or part['negativeOutline'])

    def test_both_map10_patches_and_all_original_paper_terrain_nodes_survive(self):
        source = ET.parse(SOURCE).getroot()
        terrain = terrain_only()
        self.assertEqual('none', terrain.get('preserveAspectRatio'))
        self.assertEqual(('4096', '4096'), (terrain.get('width'), terrain.get('height')))
        for query in (f'{SVG}image[@id="aged-paper"]', f'{SVG}defs',
                      f'{SVG}g[@id="map-ink"]/{SVG}g[@id="terrain-layers"]'):
            self.assertEqual(ET.tostring(source.find(query)), ET.tostring(terrain.find(query)))
        self.assertIsNone(terrain.find(f'{SVG}g[@id="map-ink"]/{SVG}g[@id="boundaries"]'))
        self.assertIsNone(terrain.find(f'{SVG}g[@id="map-ink"]/{SVG}g[@id="regions"]'))
        nodes = terrain.find(f'{SVG}g[@id="map-ink"]/{SVG}g[@id="terrain-layers"]')
        self.assertTrue({'map10-black-gate', 'map10-erebor'} <= {node.get('id') for node in nodes})


if __name__ == '__main__':
    unittest.main()
