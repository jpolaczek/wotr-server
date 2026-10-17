Correction of Dunland, Orthanc and Fangorn — 2026-10-05

Source: user-source.png, board drop sent by user-before.png shows the indicated incorrect part of previous export.

Identified errors
- North/South Dunland: reading left the white line and went down on the sign.
- Lórien: The region stretched incorrectly along the mountains all the way to Orthanec.
- Orthanc: The northern contour cut off a large portion of the area under the ridge.
- The boundaries at Gap of Rohan and north of Fangorn were diverged from the white lines.

The implementation Replaced the local network of common borders, then re-attributed the surfaces of seven regions. The outer boundaries of this set are preserved. Each changed edge has one common representation for both owners. Points and designation of the source type: correction.json. Lines marked as interpolation run on the ridge of the mountains; they are not a claim that the photograph contains a clear white boundary there.

Validation  verification.json includes the result of the application of the amendment, verification of consistency, field application, anchorages, traffic connections and range of changes. before-labels.png and before-geometry.json They're keeping the input state. after-source-overlay.jpg and detail-overlay.png show new boundaries on board The numbers are for control only; they do not appear in the final graphics. ../../asset-verification.json also confirms the division of fields in PNG.

When playing the fix, the tool requires an input mask that is compatible with the saved sum of SHA-256; it does not overwrite the newer mask. The usual generation of SVG/PNG uses the current reference/region-labels.png and does not require the re-use of this fix.
