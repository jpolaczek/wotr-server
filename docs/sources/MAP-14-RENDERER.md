<a id="map-14--źródła-i-odtwarzanie-renderera"></a>
# MAP-14 — Sources and Playback of the renderer

The geometry of the game comes from MAP-12/MAP-13: `content/map-geometry.json` (`contours-2`), `map-boundaries.json` (`borders-1`) and `map-style.json` (`map-style-1`). Mask, hit region, anchors and pass rules do not depend on texture or colour bands contact.

<a id="papier-i-teren"></a>
## Paper and land

Source `graphicalassets/maps/parchment-v4/map-composition.svg` is an existing composition MAP-10 Exporter removes group vectors only regions/boundaries ; paper, images, terrain masks and Black retouches Gate/Erebor are checked as identical nodes XML . No new area is generated. Report `MAP-14-TERRAIN.json` read SHA-256 sources, compositions after removal of the line, result image and source transformation → PNG .

```sh
npm install --prefix /tmp/wotr-map-render @resvg/resvg-js@2.6.2
NODE_PATH=/tmp/wotr-map-render/node_modules python3 scripts/export_map_terrain.py
python3 scripts/export_map_terrain.py --check
```

Renderer Node is an existing `pencil-v2/render_svg.cjs` . PNG (from UI-102 `graphicalassets/maps/terrain-map14/MapTerrain.png` Except `Resources` ) has 4096×4096 ( Pool ), with full source viewBox 335,40,1632,1338 Exports use `preserveAspectRatio=none` and the rectangle MapTerrainVisual restores proportions 1632×1338 . Terrain Report- 2 saves a separate scale X/Y ; we don't add margins or new terrain. It's a change in bitmap sampling, without changing map coordinates. PNG does not set the edge and does not contain burnt limits. MapTerrainVisual reverses the V-axis texture, and the original full preview PNG6K is not a runtime resource.

`MapCanvas/*.jpg.meta` (formerly `MapTerrain.png.meta`) saves the mipmaps, sRGB, trilinear filter, aniso2, Clamp, no reading of CPU, no automatic scaling of NPOT, max4096 and open-ended DXT1 for Standalone. Default settings and desktop overwrite are saved in repo, regardless of the configuration of the local Editor. [Unity describes DXT1 as desktop format](https://docs.unity3d.com/Manual/class-TextureImporterOverride.html). Import result and memory must be confirmed by test/report Player; the size of the compressed PNG is not a measurement of the memory of GPU.

<a id="płótno-mapy--ui-102"></a>
## Map canvas — UI-102

From UI-102 runtime does not load `MapTerrain.png`. The script `scripts/export_map_canvas.py` (Pillow, NumPy, SciPy; `scripts/requirements-map-canvas.txt`) creates in `Resources/MapCanvas`:

- `terrain.jpg`  (4096 ², map rectangle);
- `margins.jpg`  (4096 ², whole canvas);
- `map-canvas.json` layout with margins in map units: west 300, north 220, east 200, south 320.

The `UI-102-MAP-CANVAS.json` report attaches SHA-256 input (MAP-14 area, MAP-10 composition, water bands, scripts), tool versions and outputs.

```sh
python3 -m venv <venv> && <venv>/bin/pip install -r scripts/requirements-map-canvas.txt
<venv>/bin/python -I scripts/export_map_canvas.py
<venv>/bin/python -I scripts/export_map_canvas.py --check
```

**Paper.**
- Margin paper is a seamless tile inside of old MAP-10 paper (flated with a blur 30 j.), mixed with
  a displaced copy and low frequency spots.
- In pass 230 j. at the edge of the map, the paper under the ink of the site passes gently into the
  same paper (carcase = area / old paper), so both textures meet seamlessly.
- The wine, or measured profile of the old paper, lies at the edge of the canvas.

**Water.**
- The sea is an offshore area connected to the west. N/E/S belts and the North-East cut-out are
  paper, and the sea border extends the shore in margins.
- The sea and lakes have lavender, pencil wave lines from the shore (according to the distance of
  EDT) and fine wave lines.
- The `map-water` vector layer disappears. The water band stays in the data for transition and
  measurement.
- From UI-104 sections `Frame` (edge N/E/S and arc NE) are not drawn in a barrier layer
  (`MapRenderer.Inked`). The shore, lake and barrier retain ink.

`MapCanvasLayout` reads the layout and checks the version and size of the map. `MapTerrainVisual` draws two rectangles: margins under the whole canvas and the area above the map.

<a id="pasma-i-woda"></a>
## Band and water

```sh
python3 -m venv /tmp/wotr-map-trace-venv
/tmp/wotr-map-trace-venv/bin/pip install -r graphicalassets/region-map/tools/requirements.txt
/tmp/wotr-map-trace-venv/bin/python scripts/export_map_border_bands.py
/tmp/wotr-map-trace-venv/bin/python scripts/export_map_border_bands.py --check
/tmp/wotr-map-trace-venv/bin/python scripts/map_render_asset_tests.py -v
python3 scripts/sync_content.py
python3 scripts/export_map_geometry.py --check --borders
```

`map-border-bands.json` has a version `border-bands-1` Report MAP-14-BANDS pin versions Shapely/GEOS and SHA-256 three input directories. positive/negative The normal border passerby is cut to the polygon of the appropriate owner. The keyline outline is also cut. Before serialization is used the grid is used 10 ⁻ ⁶ with the preservation of topology; this does not change the source geometry of regions or sections of passage. 935 the resulting rings are correct and located in their own region. Islands/openings Water comes from the difference between the staff and the totality of the regions, without adding playable regions. The boundaries of barriers, water and framework do not have national bands. Disablement of colour for two regions of one nation is based on Nation, never Control.

<a id="cache-i-warstwy"></a>
## Caches and layers

MapRenderer maintain separate, non-interactive elements: texture, water, region filling, common border crossings, barriers. MapVectorLayer uses native cache mess UI Toolkit. Container has hint GroupTransform , his descendants DynamicTransform A common final layer of white means prevents the covering of the passage through the next path on the node. White width 3 barriers 5 The camera transforms the common container instead of copying or teseling paths every frame. The choice refreshes one region and the change of the nation only related bands. settlement , a separate control symbol, attention and arrow; children with units and UI They stay above geography. All layers of maps have Picking Ignore, so they do not change hit-tests MAP-12 .

We do not use SaveToVectorImage: the [removes offset content](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElements/Core/Renderer/UIRPainter2D.cs) method, and the DrawVectorImage has copied too many vertices while the camera is moving. Retained Painter2D retains source coordinates and allows Unity to manage buffers without private API.

<a id="odbiór-obrazu"></a>
## acceptance image

Development build: `Wotr.Editor.BuildTasks.BuildMac` or BuildWindows.

```sh
python3 scripts/review-map-client.py --output .artifacts/map14-player
# At a custom location:
python3 scripts/review-map-client.py --player /path/to/Wotr --output .artifacts/map14-player
```

The player flag `--wotr-map-review <folder>` only works in Development or Editor. It runs the actual BoardScreen with the setup; it requires no server, account or player data. Launcher requires a report from the actual Player Development, 26 PNG and 12 measurements of four screen configurations. Images include fit/maxzoom/legend in each configuration and details of both Mordor, Erebor, Moria, High Pass gates, two nations, water and nodes before/after highlights. The report separates data points from the actual Graphics Vertics counter. The times include input/end rendering support, without delaying the monitor. The viewing is a separate step of acceptance — a set of files does not prove the correct image. The command and actual results captures MAP-14.md.

Legend is a cover in a board container, above the siblings of the map; it does not remain under the Zoom/Effects. Palette controls has a compact layout with up to two columns, without scrolling. Full BoardScreen checks the header, all names and color samples at 1366×768/1920×1080 and 100/125%. The legend click does not select the region and does not move the camera; Escape closes it without the game's decision.

Received Development Player macOS (Metal, Apple M1 Pro, Unity 6000.6 .0f1) has 11 185 272 B textures DXT1 ed 13 Mipmapami. Measurements 12 The runs confirm zero regeneration of static boundaries at pan/zoom/highlightach ; counter GPU vertics is correct and reaches peak 911 075 for the whole UI Full [report](https://github.com/jpolaczek/wotr-client/blob/60ea00814c48f12e8931dbcd3cf336789785360d/docs/ui/profiles/map14-macos.json) keeps times, GC and configuration; FPS is without limit/vsync , is not a Hz monitor. The viewing includes 26 final PNG , including legend in four layouts. Details acceptance and restrictions are in MAP-14 .
