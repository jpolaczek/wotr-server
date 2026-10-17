<a id="dowody-przeglądu-granic-map-13"></a>
# Evidence of the MAP-13 border review

Source: `graphicalassets/highedfmap.jpeg` (4016×2835), transformation to the working system 2008×1418. Policies: BASE-2.4.1 p.9–10. Classification is in [MAP-13-BORDER-AUDIT.json](../MAP-13-BORDER-AUDIT.json), and the control numbers in [MAP-13-BORDER-REPORT.json](../MAP-13-BORDER-REPORT.json).

Each SVG refers to the relative path to the existing board We don't include any more copies. SVG directly in the browser from the repo directory or play PNG the command below.

<a id="oznaczenia-diagnostyczne"></a>
## Diagnostic markings

The `0208:1/P` label means the path of `boundary-0208`, part 1 and type `Passage`. Green P and turquoise R indicate the passerby section; red I — passible; blue C/L — coast/lake; purple F — external frames. White dots mean change of type within one path. Colours serve audit; the game view palette is a separate resource of `content/map-style.json`.

In [two-nations.svg](two-nations.svg), the contacts pass by instead of the diagnostic color show the bands of nations on the right side of white. Grey Havens/Ered Luin (`0039`) has a range of elves (clear green) on the side of Grey Havens and dwarves (brown) on the side of Ered Luin. The mountain part remains red in the audience and will be black in the MAP-14 renderer. This previews normal episodes, not the target band joining algorithm.

<a id="przejrzane-wycinki--2026-10-08"></a>
## Transparent clippings — 2026-10-08

- [Whole Map](whole-map.svg) and all `sector-0-0`...`sector-2-2` (9 sectors): 323 path, 360 parts,
  282 physical pairs, 232 legal and 50 additional contacts without movement law.
- [The Black Gate](black-gate.svg): Morannon/Dagorlad has a white opening between two mountain
  fragments; the contact with North Ithilien does not open.
- [Minas Morgul](minas-morgul.svg): a preserved passerby northern contact with South Ithilien and
  southern ridge, a passage to Gorgoroth and its mountain ending. Nurn/Gorgoroth has a western
  crossing `0212`; the second contact `0213` east of the lake is completely impenetrable.
- [Moria](moria.svg): White transition to Dimrill Dale with a mountain fragment to the south.
  Moria/Lórien remains impenetrable.
- [High Pass](high-pass.svg): preserved transitions to Goblin’s Gate and Fords of Bruinen; contacts
  Rivendell/High Pass and High Pass/Old Ford remain impenetrable.
- [Two Nations](two-nations.svg): different bands do not blend in one color and come from `Nation`,
  regardless of the current `Control`.

Geometry is not a new pixel outline. It preserves adopted in MAP-12 ridge interpolation, routing deviations and paneled sections characters/track Fellowship Class divisions are pinned to existing peaks, not seemingly accurate new points. Rivers are only marked where the source of correction confirms them; both classes Passage/River The traffic graph is still from T04, not from color or geometric contact. The rules and neighbourhoods of the regions have not been changed.

<a id="odtwarzanie"></a>
## Playback

```sh
python3 scripts/export_map_geometry.py --check --borders
python3 scripts/review_map_boundaries.py --check
python3 scripts/review_map_boundaries.py --render
```

The first two commands use a standard library. The optional PNG render requires CairoSVG and saves only `.artifacts/map13-review`. SVG does not contain the game status, player data or secrets. The final caps were compared with board by reading generated PNG; the click and drawing test in the client are part of the integration of MAP-14/MAP-17.
