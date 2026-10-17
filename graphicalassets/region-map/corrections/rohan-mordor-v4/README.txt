Correction of Rohan, Gondor and Western Mordor — 2026-10-05, MAP-01

Sources rohan-user-source.png: user drop from 23:43:08 , 908 × 670 . mordor-user-source.png: drop from 21:33:49 , 782 × 556 *-user-before.png files show both passages indicated for improvement. board reference: ../../../highedfmap.jpeg , 4016 × 2835 Registration SIFT/RANSAC matched 2028 and 2743 points to board . Screenshot_to_working Matrix is written in correction.json. The target system remains raster 2008 × 1418 Rivers were led by the middle of a visible trough.

Amendments
- Eastemnet/Westemnet: border runs by river, without prior departure to the east and mistaken
  flattening of the connection at Folde.
- Fords of Isen, Helm's Deep, Westemnet, Edoras and Folde: white boundaries, common corners and
  southern contours at the White Mountains.
- Erech, Lamedon, Minas Tirith, Lossarnach and Drúadan Forest: mountain contour connections and
  improved Minas Tirith/Lossarnach.
- Morannon: Dagolad wedge at North Ithilien was removed; the white border Morannon/Gorgoroth
  replaced the section led by the field interior.
- Minas Morgul: western and eastern outlines of the white line, southern dorsal lock. The
  Barad-dûr/Gorgoroth border and visible section of northern mountains have also been improved.

The White Lines and Rivers are readable in the sources. The mountains partially obscure the boundaries and require interpolation. These sections are described separately in source_interpretation fields. In particular, the lack of a separate white line in the White Mountains and Ephel Dúath does not allow to declare the compatibility of each 1:1 point. The task concerns the graphics resource, without changing legal movement or Unity integration.

The scope and evidence before-labels.png and before-geometry.json retains the input state. candidate-labels.png is a mask of the amendment applied. correction.json contains common edges, regions, sum of SHA-256 and two edit windows: [965,710,1372,1053] and [1438,768,1793,1040], with an exclusive right/bottom limit. In the first window, 7898 pixels were changed, in the second 5566; together 13464. Outside the windows and in the water mask, 0 pixels were changed. The whole 85 regions were retained unchanged. No pixel of the Dunland-v2 and the shire-v3 corrections were affected.

verification.json check 105 correct closed polygons, all 232 the required contacts and no overlap. scope-verification.json checks the scope and behavior of previous corrections and physical contact graph. * -before-overlay.png  and * -after-overlay.png show contours on the drop; after-source-overlay.jpg and detail-overlay.png are previews on full board Previews without names: ../../map-detail-rohan.png and ../../map-detail-mordor.png. The final raster checks ../../asset-verification.json Not the geometry itself.

Commands made from the project directory
/tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/tools/apply_local_correction.py graphicalassets/region-map/corrections/rohan-mordor-v4/correction.json --apply
/tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/tools/export_map.py
/tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/tools/verify_assets.py
/tmp/wotr-map-trace-venv/bin/python graphicalassets/region-map/corrections/rohan-mordor-v4/review.py
python3 scripts/tasks.py validate
git diff --check

First exports PNG had two small artifacts (1 and 4 white pixels) at the fugitive Morannon/Dagorlad/Barad-d ûr. Corrected corner edge directions and re-exported; the condition has not been changed exactly 105 white fields. Repeatability of exports saves reproducibility.json Checking the compliance of previous corrections with the extended rasterizer is in compatibility.json.

Re-use Regular export uses the current reference/region-labels.png. --apply requires a mask with saved input total; no corrections should be re-imposed on the new version. review.py requires the current 4 mask to not save old previews to a newer map. The next map adjustment requires the next passage. T22 remains a separate, parallel work on UI, outside the MAP-01 range.
