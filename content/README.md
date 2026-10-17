<a id="dane-zawartości"></a>
# Content data

`base-board.json` is an independent from Unity and graphics directory of regions, graph, figures and reserves, policies, dice, tiles and stable slots. Engine loader verifies version and relationships. The same DTO .NET Standard reads Unity adapter by JSON. `map-geometry.json` contains only polygons and label points; it does not set legality.

Closed graph means: normal transition is only legal geographically only when it exists in `neighbors`. `impassableBorders` distinguishes examples of field barriers; no other edges also prohibits transitions. Other traffic rules (politics, opponent, characters) still require their validators. Distance points on the screen does not mean neighbourhood.

193 figure army/dow boaters/Nazgûli plus 12 separate physical figures The characters give 205 List 14 cards the forms include versions Gandalf/Aragorn , no 14 independent figures Slots 96 cards and ID characters do not contain guessed abilities or unverified titles. Their implementation remains in T20/T21.

Original photos in `docs/sources/materials` serve to compare sources, are not client resources. Placeholder UI are created from figures geometric; the graphic manifest will tie them to regionId/typeId.

`base-cards.json` contains confirmed metadata 96 obverses from SRC-01 . `implementation-manifest.json` separates the event implemented from the combat application, states programs/initiatives Fights and tests. Programs cards are the typed code in `Domain/Cards` and joint implementation in `Domain/Effects` ; data JSON are not an executable language. T14 provided four partial card (events); packages T21-FP-CHAR-01/02/03/04 , T21-FP-STR-01/02/03/04 and T21-SH-CHAR-01/02/03/04 and T21-SH-STR-01/02/03/04 include all programmes 96 Baseline cards in both applications. Manifesto binds each ID with behavior tests; status acceptance parcels and bulk T21 still come exclusively from task registry .

UI-06: `base-card-text.json` contains functional descriptions of the Event/Combat rulesAll 96 cards, written in its own words on the basis of the SRC-01 obverses; titles and initiatives retain the data of the verified index. `source_cards.py` controls the completeness of ID and the compatibility of the combat/initiatives titles, fails to automatically verify text semantics. Unity collects the directory and descriptions from the attached public content package (DIST-02). card text is not proof of its execution; the manifest of implementation remains independent.

T15: `base-characters.json` transfers confirmed statistics, roles, icons, identities and initial composition 14 cards SRC-02 to domain directory. `source_cards.py` compares all these fields with source index. `null` at level/command remains separate from zero; Witch-king movement and specific T20 support capabilities. The directory itself does not mean implementation with these capabilities.

UI-08 synchronizes the same character directory to Unity. Client uses public names; the composition, current versions and statistics figures takes from the player's projection, without guessing secret versions based on public identity.

T21-FP-STR-03: `RegionDefinition.Coastal` describes 17 regions bordering the western sea according to BASE pp. 8 and board. It does not appear from geometry or the neighbouring rivers. By default, false fields maintain the read compatibility of previous directories.

T21-SH-STR-02: comparison with `docs/sources/materials/board-reference.jpg` confirmed a section of the Lamedon coast between Dol Amroth and Pelargire. Fixed `Coastal` Lamedon; the four coastal regions of Gondor are the targets of the Corsairs, and the common classification also applies to Círdana.

DIST-02: `unity-public-content.json` has `version` (SemVer) and a list of public files. `scripts/package_deps.py` builds `com.wotr.public-content` from it only from these files, along with fingerprint, the version of Contracts and `ContentVersion`. Every change to a file from the list requires the conquest of `version`; the client accepts it with an open change of `wotr-deps.lock.json`. The other files of this directory (manifest implementation, setup, sources) do not go to the client.
