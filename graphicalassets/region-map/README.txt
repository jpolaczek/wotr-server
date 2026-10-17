Map of regions — vector version, 2026-10-05

Result map-regions.svg  —  105 closed regions in a common coordinate system. map-regions.png  —  6144   ×   5037 px, white fields, dark boundaries, transparent background, sea and lakes. Without names, symbols and illustrations of the interior of the regions. map-preview.png — preview 1536   ×   1259  px.  geometry.json  —  identifier, paths, field ranges and common edges. map-detail-dunland.png — enlarge the revision of the text 2.  map-detail-shire.png — enlarge the revision of the text 3.  map-detail-rohan.png — Rohan and Gondor after revision 4.  map-detail-mordor.png — Western Mordor after correction in revision 4.

Each region is a separate element of SVG by id="region-<identifier>". Region group contains fillings; boundaries draws each common edge once. This allows shapes to be cut or animated separately, maintaining a common coordinate system. Field contours are polygonal; later rendering style does not require re-recognition of regions from the image.

Sources and method
- ../highedfmap.jpeg — board user, 4016 × 2835 px.
- ../mirkwood-regions-outline-v1.png — accepted cutout, 954 × 1648 px.
- ../mirkwood-source-2026-10-05-180801.png — the original drop of this place.
- ../../content/base-board.json — 105 identifier and 232 traffic connection. Copy of connections
  themselves is in reference/movement-neighbors.json.
- The initial names anchors came from content/map-geometry.json. Those approximate click polygons
  were not used as final outlines.

This is a deterministic export, without the next generation of the map by AI. Worked on board scaled to 2008 × 1418 px, with the recognition of clear lines and rivers, assigning areas, manual corrections and control on the original. The reference/region-labels.png mask is technically proven exporter input; the pixel value corresponds to the number in region-seeds.json and 0 means background. It is not a color graphics for displaying in the game.

Slice Fits to board scale and offset. Result of matching the original drop: 0,964200 in the correlation of the template. The matrix is in reference/registration.json. Seven forest regions were separately preserved, including Dol separation Guldur/Southern Mirkwood and Narrows/Eastern Mirkwood. There is no regenerative tracing This fragment.

The raster has been restored to a common edge network. Simplifying sections has the tolerance of 0,6 working pixel, or about 1,2 source image pixel. This value refers to vector export, is not a declaration of such accuracy of the entire read board. The large PNG does not increase the amount of information in the source.

Verification performed Commands in an isolated temporary environment: /tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/tools/export_map.py /tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/tools/verify_assets.py

- 105 correct, single, closed polygons.
- 105 identifier SVG compatible with the directory, without visible text.
- No polygonal surface application.
- Each anchor belongs to its region.
- All 232 traffic connections have a common section of the boundary in geometry.
- Rendered PNG also contains exactly 105 separate white fields.
- Moving the interior of seven accepted fields: 100% for six fields and 99,986% for Old Forest Road
  (one working pixel at the edge).
- A preview of the entire map and the cover on the source was examined in detail, including the Dark
  Forest, Pass, Rohan/Gondor, Mordor, Rhûn and Dorwinion.
The exact results are in verification.json and asset-verification.json.

Reading limitations Some sections are not clearly visible: they are blocked by mountain illustrations and a form panel in the eastern part of the map. At these points, contours are an interpolation. Rhun/Barad-dur/Gorgoroth. The cut in the northeast corresponds to the area covered by the track Fellowship There the photo does not provide a full map to tracing The accepted section of the Dark Forest itself was a pre-generated approximation of a small drop. 1:1 every breakdown with printed board .

Control 105 fields and 232 connections detect deficiencies and errors of topology, but it does not prove the flawless course of each border fragment. The reference/source-overlay.jpg cover allows you to evaluate contours on the source; the numbers correspond to reference/region-seeds.json. The manually delineated points are in reference/traced-corrections.json. The later fixes are saved separately in corrections; the current mask and SVG take them into account after the original reading.

Correction of Dunland / Orthanc / Fangorn — revision of 2 Based on two dumps transmitted after the first export, the incorrect extension of Lórien towards Orthanc was removed. The border North/South Dunland was led by a real white line, without a name being drawn down. The white boundaries at Gap of Rohan and the northern contours of Orthanc were corrected. The boundaries of Fangorn and Parth Celebrant at the ridges were closed. The sections through the mountains remain an interpretation of the ridge; the source does not show a separate white line. They are marked at specific paths.

Materials, previous mask, new border points, overlay and report: corrections/dunland-v2/. Correction changed 15439 Working pixels in seven regions: Lórien, North Dunland, South Dunland, Gap of Rohan, Orthanc, Fangorn and Parth Celebrant. 98 regions were preserved without changing the mask.

Commands executed for this revision:
/tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/tools/apply_local_correction.py graphicalassets/region-map/corrections/dunland-v2/correction.json --apply
/tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/tools/export_map.py
/tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/tools/verify_assets.py

The correction retains 105 single closed fields, all 232 required contacts and no overlapping surfaces. It has also proven that there are no changes in the mask outside the indicated regions and the preservation of the fragment of the Dark Forest.

The Shire / Buckland / Old Forest revision — 3 revision Based on subsequent user discharges, the river at Shire, the Buckland border with Bree and the contour of Old Forest. Old Forest now has a long common border with Buckland; the mistaken narrow pass Cardolanu over the forest has been removed. The southern and eastern forest boundary runs along a white line rather than after the silhouettes of trees. The mask of Lake Evendim has also been improved: it no longer covers the narrow blue glare of the North Downs border.

Materials and report: corrections/shire-v3/. Changed 4546 work pixels in window x=602..885, y=224.473. The correction includes fragments of the regions and water (value 0 mask). No pixel has been changed outside this window. The previous correction of Dunland and the accepted cut of the Dark Forest has been preserved.

Review commands 3:
/tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/tools/apply_local_correction.py graphicalassets/region-map/corrections/shire-v3/correction.json --apply
/tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/tools/export_map.py
/tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/tools/verify_assets.py

acceptance includes comparison of contours at source and control 105 closed regions, 232 required connections, no field application and separation in the final PNG. Export results are in the current files verification.json and asset-verification.json; the scope of the amendment documents the local report.

Correction of Rohan, Gondor and West Mordor — revision of 4, MAP-01 Based on four user discharges, the 37 common edges were improved: rivers and white borders of Rohan, southern contours at the White Mountains, a river between Minas Tirith and Lossarnach, a combination of Dagorlad and Morannon and the contours of Morannon, Minas Morgul and Barad-dûr. The mistaken wedge of Dagolad was removed on the western side of the mountains. The sections through the mountains are still interpolation of the ridge; the source type was recorded at each edge.

Materials, points and reports: corrections/rohan-mordor-v4/. Amended 13464 working pixels in two windows: [ 965,710,1372,1053 ] and [ 1438,768,1793,1040 The mask is identical, but the mask remains 85 The whole regions have not changed water or any pixel of the earlier amendments of Dunland and Shire. The set of all physical contacts is the same as before the correction. reference/raster-adjacency-audit.json contains updated lengths.

The full map was re-exported to map-regions.svg  /  map-regions.png,  geometry.json and map-preview.png Previews of both fragments are separately in map-detail-rohan.png and map-detail-mordor.png. Final PNG preserves 6144   ×   5037 px, transparency and precise 105 separate white fields. Commands and restrictions are recorded in local README.txt correction; its review.py check the scope and play previews. The project registry carries out the task MAP-01.

Geometry does not replace traffic rules. Additional physical contacts, e.g. through impermeable mountains, do not constitute legal connections. The full list of raster contacts is in reference/raster-adjacency-audit.json.

Reexport In a separate Python environment, install tools/requirements.txt and start tools/export_map.py, then tools/verify_assets.py. CairoSVG requires available system Cairo. The exporter plays the files from the attached mask; it does not restart the image recognition.

The scope of previous revisions of 1–3 was then only saved graphic and auxiliary files in graphicalassets/region-map, without changing engine, region catalog and task registry. The revision of 4 has a separate task of MAP-01 and local commit according to the current workflow. Previous map files and accepted section are preserved. Next use: styling or exporting regions to separate drinks with a common reference point. They were not integrated into Unity yet.

Import of the client — MAP-12 , 2026-10-08 Existing graphic sources remain unchanged. New tool python3 scripts/export_map_geometry.py (started from main repo) export content/map-geometry.json and docs/sources/MAP-12-GEOMETRY.json ; --check compares playable bytes. It only requires standard Python. python3 scripts/sync_content.py copy result to Unity , keeping the .met. From DIST-02 sync_content.py does not exist: the changed content goes to Unity by scripts/package_deps.py and scripts/bootstrap_deps.py pin ( docs/DEVELOPMENT.md ). python3 scripts/test_map_geometry.py check sources and incorrect inputs.

Contours format- 2 has 105 regions, 106 openly closed rings, 26 455 points without closing repetitions, 323 and 14 348 different sections. Path owners are mapped by number by dictionary ID ; null means 0/wod ê/outdoor. ID Boundary- NNNN and sourceIndex retain origin SVG The control shall include intersections, imposition, ownership of joint sections, anchors and all. 232 legal couples among 282 physical contacts. SHA-256 Six entrances are pinned In the inventory and report.

The source goes to the client with a shift (x−335,y−40), without changing the scale; customer dimensions are 1632×1338. Anchors use point, never seed. The first ring has a positive field directed in the screen layout, negative holes. A small hole Gorgoroth touches the outer ring at one point; it is preserved. PNG 6144×5037 uses a uniform scale of min(6144/1632,5037/1338) with centering, according to SVG xMidymid meet (approximately 0,108 pixel of the horizontal margin). We do not extend independently the axis.

Unity rejects the incorrect version, scale, regions, rings, anchors or edges before opening the map. Drawing and hittings use the same rings; geometry and ranges are loaded/validated once. MAP-13/MAP-14 separately implements classification of barriers and national colors and layers of papieru/terenu. The limitations of interpolated fragments described above do not disappear after technical integration with Unity.
