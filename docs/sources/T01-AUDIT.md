<a id="t01--audyt-źródeł-2026-10-04"></a>
# T01 — Audit of sources, 2026-10-04

Local instructions retain the sum of the manifesto. Base: 96 events, 14 cards characters, 16 standard and eight special tiles. Lords: 13 events and special card Sméagola events. Kings: 19 events. Alternative components and larger collectors' edition tiles do not increase the correct pool.

<a id="plansza-i-setup"></a>
## board and setup

[readable reproduction board Second Edition](materials/board-reference.jpg) was acquired, used as a comparison material. The candidate directory comes from John Champaign, commit `6716a03d3c6ce99a41693a38adaf6d541b10844c`; its FIRST PASS annotation is not proof of correctness. [MIT note](materials/map-data-MIT-LICENSE.txt) has been preserved. No alien engine was imported.

The names, affiliations, settlement and the region's connection to board have been compared. [Tested facts](materials/map-facts-reviewed.json) include 105 regions and 232 uncompiled connections, 16 fortresses, six cities, 14 villages and two fortifications. The black border and the sea do not form the neighbourhood; policy and the remaining legality of traffic checks separately the engine.

The review included Lindon/Arnor, the passage of the Mist Mountains, Anduina/Mirkwood/Rhovanion, Rohan/Gondor and Mordor/Rhûn/Harad. Special regressions T04: no Angmar–North Downs, no Northern Rhovan–Southern Dorwinion, High Pass–Goblin’s Gate, no High Pass–Old Ford, Moria–Dimrill Dale, impermeable White Mountains, narrow borders Osgiliaath, and the only normal access to Barad-dûr via Gorgoroth.

Osgiliath has the initial Gondor units, but lies beyond the nation's borders. The affiliation of the area, the nation of the individual and the current control are distinct. Setup and reserves are compared with BASE p. 7 and 16–17 Politics comes from p. 14 : Elves and nations Shadow active; distance to At War: 3 for Elves/Dwarves/North/Rohan , 2 for Gondor/Southrons , 1 for Sauron/Isengard. Fellowship : Rivendell, Hidden, progress/ Corruption 0 , Gandalf the Grey as Guide and seven Companion . Initial dice FP 4 , Shadow 7 ; three Rings on the side FP .

T04 normalizes production data, separates geometry and launches graph tests, figures sums, reserves and setup. The mere restoration of the connection does not mean the legality of any army's movement.

## FAQ

[Official FAQ 1.2 with 2014-09-09](materials/base-faq-v1.2.pdf) was retrieved from a copy of warofthering.eu. On page 1: first, all cards of a given procedure is finished, then rejected to six. The recovery phase selects both card before discarding; T10 must take this order into account.

Ares returns 403 at the direct readings. The index archive confirms available material families. A hint about the newer instruction 2.4 has been found, but we do not identify it with the document read or change the specific rule. The study of the later errat remains SRC-08. The range to T10 is based on the local instruction and read FAQ.

<a id="karty-i-kontynuacja"></a>
## card and continuation

[96 index](base-card-index.json) binds packages to the numbers and metadata of the transcription. Full effects remain unverified. The transcription refers to the reproduction of cards, which must be compared to the obverses before T20/T21.

SRC-01–SRC-08 take over eight remaining shortcomings: cards/characters, Fate and newer FAQ texts. expansions packages remain unrelated to ID confirmation. This does not block the turn model and cycle to T10.

Audit checks: SHA-256 sources, comparison board and setup, symmetry/ Graph communications, four separate decks after 24 slots and assigning the owner to every missing. An overview of the sources is performed by an agent; it does not declare manual game tests by the user.

Control detected an outdated edge counter in the auxiliary source metadata (234 instead of the actual 232) and a duplicate number Shadow Character 8 and missing 11. All 96 printed slots remain in the index; ambiguous title assignments are explicitly unconfirmed. SRC-01 requires comparison of obverses, without default transfer of card to the free number.
