<a id="kontrola-danych-t04"></a>
# Data control T04

2026-10-04. The production facts of the map are from the T01 comparison; the standardization of T04 retains all 105 regions and 232 uncrossed edges. `BoardTests.PrintedBordersDetermineMovement` protects five characteristic passages, including Moria–Dimrill Dale and the lack of High Pass–Old Ford. The whole graph checks symmetry and the absence of foreign and repeated ID. Graf is closed: no edge forbids normal passage regardless of distance on the screen.

Setup was compared again with BASE p. 16–17; all 35 from the start regions and reserves are in `content/base-board.json`. The totals were compared independently with the figures BASE p. 7:

| Nation | Regular | Elites | Commanders | Nazgûle | Reserves R/E/L/N |
| --- | --- | --- | --- | --- | --- |
| Dwarves | 5 | 5 | 4 | 0 | 2/3/3/0 |
| Elves | 5 | 10 | 4 | 0 | 2/4/0/0 |
| Gondor | 15 | 5 | 4 | 0 | 6/4/3/0 |
| North | 10 | 5 | 4 | 0 | 6/4/3/0 |
| Rohan | 10 | 5 | 4 | 0 | 6/4/3/0 |
| Isengard | 12 | 6 | 0 | 0 | 6/5/0/0 |
| Sauron | 36 | 6 | 0 | 8 | 8/4/0/4 |
| Southrons | 24 | 6 | 0 | 0 | 10/3/0/0 |

Politics: Active Elves and nations Shadow; distance to war 3 for Elves/Dwarves/North/Rohan, 2 Gondor/Southrons, 1 Sauron/Isengard. Osgiliaath is a neutral region with the initial army of Gondor. Current control does not change the original affiliation of miasta/twierdzy.

dice : BASE p. 18–21 , six walls of each standard dice , initially 4/7 maximum 6/10. Characters and card They have stable slots without unverified capabilities. Fellowship (Rivendell, Hidden, progress/disappointment 0 , seven Companion , Gandalf Grey) and three Rings FP Creates a factory MatchState in T05.

Cables: BASE p. 4,6,40 confirms 16 standard + 8 Special, categories and meaning of symbols. The Krotality was compared between reading the component scans in the T01 candidate and independent, visible full [a set of substitutes](https://www.etsy.com/listing/1027526575/hunt-tiles-for-war-of-the-ring-board) : standard Eye+Reveal × 4 , 3 × 3 , 2 × 2 , 2 +Reveal × 1 , 1 × 2 , 1 +Reveal × 2 , 0 +Reveal × 2 ; FP 0/0/ − 1/ − 2 ; SH die/Eye+Reveal/1+Reveal/3 , all SH From Stop. [Set photo](materials/hunt-layout-reference.jpg) is secondary evidence, not an official errata. [Photograph of original components](materials/hunt-component-reference.png) confirms the markings, but stacks do not allow independently counting the entire set. cards the introducing tiles remain outside T04.

Geometry: polygonal coordinates with SirMartin/WarOfRingMap, converted to 1920×1324, internal points calculated locally. All 105 points are located in their own polygon. These approximate areas of clicking to a neutral map, not a graph source. Visual verification and label collisions belong to UI-02.

`dotnet test --configuration Release`: 10/10 (including architecture test). Independent data loader is in the engine, DTO are shared with Unity; client builds after adding adapter checks UI-02/T09.

Complement T21-FP-STR-03 , 2026-10-05 : BASE 2.3 p. 8 defines the coast by the western sea. Reread reproduction board : Andrast, Anphalas, Arnor, Dol Amroth, Drúwaith Iaur, Enedwaith, Evendim, Forlindon, Grey Havens, Harlindon, Minhiriath, North Ered Luin, Pelargir, South Ered Luin, Umbar, West Harondor. All of them. 16 regions `coastal: true` ; neighborhood lakes/rivers Ered Luin and Minas Tirith are not coastal. `BoardTests.CoastalRegionsFollowPrintedSeaBorderRatherThanNearbyRivers` protects the full collection. Graf, setup and geometry remain independent.

<a id="map-13--audyt-prezentacji-granic-2026-10-08"></a>
## MAP-13 — Border presentation audit, 2026-10-08

The current geometry of the presentation is `contours-2` ed MAP-12 , coming from the accepted contours MAP-01/region-map ; replaces historical areas SirMartin the above described. `base-board.json` and graph 105/232 they remain unchanged. 282 physical pairs: 232 graph pair and 50 other contacts that do not become new neighbours.

Reread BASE-2 . 4.1 p. 9–10 : colors of nations, passers-by white lines, rivers, thick black barriers and exclusion of water from regions. Each of the 323 paths assigned range in source board , review sector, clarification and component types. [Audit](MAP-13-BORDER-AUDIT.json) pin hashe board JPEG , instructions, base-board, geometry and two corrections MAP-01 . [Report](MAP-13-BORDER-REPORT.json) count 360 parts/ 33 breakdowns, a [Caps](map13-review/README.md) allow you to re-create the review.

An example requiring a division: Morannon/Dagorlad has black mountains on two sides of the white entrance. An example requiring separate evaluation of two paths: Nurn/Gorgoroth has a white passage west of the lake and a black non-passive contact to the east. Legal pair is not a proof of the color of each fragment. MapContourBorderTests and test_map_boundaries protect both cases, a set of additional contacts and orientations of national bands. Classification inherits limitations route/interpolation MAP-12 ; does not add new geometry or default motion rule.
