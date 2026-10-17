<a id="src-03--awersy-kart-wydarzeń-lords"></a>
# SRC-03 — Averses cards events Lords

The review with 2026-10-05 includes **13 normal cards events and one special**: print numbers, page, deck, cost icon, whole application of event, conditions/choices, maintenance and discard of table and all combat application with initiative. [Index](lords-card-index.json) includes 14 disconnected ID, location of each averse and summary of rules with implementation cases. `effectsVerified=true` indicates source reading. At the time of audit all card had `implemented=false`; three later T27 packages implemented a full set. The current connections and the result of acceptance describe [report 14 kart](../rules/T27-CARD-COVERAGE.md) and [T27](../rules/T27-ACCEPTANCE.md).

<a id="pochodzenie-i-powtórzenie-kontroli"></a>
## Origin and repetition of checks

First checked [Ares](https://www.aresgames.eu/games/war-of-the-ring-line/lords-of-middle-earth) and the publisher's materials. Direct reading HTTP returned 403; search index confirms the addendum Second Edition, but does not replace obverses. Local `LORDS`, [instructions WOTR005](../../WOTR005-Rulebook_EN_web.pdf), p.2–3 confirms the exact composition, four decks and the replacement of Balrog; p.14 and 21–25 binds card to Rings, Sméagol and Balrog. Official [FAQ 1.2](materials/base-faq-v1.2.pdf), p.3, settles the order of two Sméagol events.

Full obverses come from physical reproduction of cards on Steam CDN, indicated by the same [pinned Resource Index](https://github.com/johnchampaign/war-of-the-ring/blob/6716a03d3c6ce99a41693a38adaf6d541b10844c/assets/asset-urls.json) as SRC-01/02. The metadata of this repository were only a locator; the numbers, icons and initiatives were read from images. No alien engine was copied or interpreted. Five sheets already exist in [Manifest](manifest.json); the same bytes have the same ID, even if the historical name ID starts with `BASE`.

| Sheet | SHA-256 | Size / Bytes | Read Tabs |
| --- | --- | --- | --- |
| BASE-CARD-SHEET-AAA4252F | `aaa4252f94f5fa1affc6920d87ed0677719fe1a34dd8e13e41bb3112b45477a2` | 8154×8192 / 17917425 | FP Character 25–28 |
| BASE-CARD-SHEET-CC8441E3 | `cc8441e33779501e816c3754acfde429b31dd15593b59113e0391373c2a112c7` | 8154×8192 / 13699542 | FP Strategy 25–26 |
| BASE-CARD-SHEET-72634326 | `72634326bdc676a986123d003d1e790e451d5a45a20ccd4549f32b637f0840c4` | 8154×8192 / 17849023 | SH Character 17, 25–28 |
| BASE-CARD-SHEET-40183E02 | `40183e02f02224edcf32e3f69e4f114394f7546801c457e083bbd9ff0bc58332` | 8154×8192 / 16455705 | SH Strategy 25–26 |
| BASE-CHAR-SHEET-EDA5E27F | `eda5e27fbf6de13a037188400ae098f0c8f2a686bf5e6674dc39b212fcbcef56` | 8154×8192 / 14910545 | We Shall Get It |

Additional independent control is [Physical photography cards with 2012-11-10](https://boardgamegeek.com/image/1471576/war-of-the-ring-lords-of-middle-earth): We Won’t Go Back, Shadow of Dol Guldur and A Balrog is Come! Both halves, icons, numbers and initiatives have been compared. Her original file has SHA-256 `f9de3fca35ee50beaf26391afc8ae3b778357abbac80c07783d6c26fc0c8a963`, 1500146 bytes; URL and dimensions are recorded as `LORDS-CARDS-PHOTO-2012`. The photo confirms these three card, not the other eleven. Neighborhood photo 1471575 presents characters; it is not evidence of events.

Each obverse was viewed separately; local Apple Vision OCR was a help, not the ultimate source of numbering. In cache `.artifacts/source-audit/SRC03` five whole sheets have been preserved, 14 clippings and results OCR , photo metadata, Ares response 403 and authors' replies. Reproduction and full OCR not added to the client's graphics or distribution of the project. Sheets are files JPEG , though historical cache SRC-01/02 and working download SRC-03 used extension `.png` ; control recognizes the actual signature and saves new downloads as `.jpg` A permanent manifesto keeps URL , hash and rectangles; the limits have coordinates left/g side/ right/lower in pixels of the original image, with the beginning at the top on the left.

`python3 scripts/source_lords_cards.py` checks the set, prints, applications, deck composition, replacement and separate split of packages. `--fetch` additionally downloads five pinned images from JPEG images to ignore `.artifacts/lords-card-sources` and verifies the SHA-256 bytes and image sizes. Before implementing a particular card, you should reopen the obverses indicated by the index.

<a id="manifest-i-zastąpienie"></a>
## Manifesto and Replacement

The order is public: FreePeoples before Shadow; Character before Strategy; special card outside the deck after the usual deck; then the print number and stable ID. This is the sorting detail of `side/deck/printedNumber/stableId` for a component that has no deck. The packages remain 6 + 6 + 2; the dependencies and range do not change.

| ID (prefix `lords.`) | Print | Event / type | Combat / initiative | Pack |
| --- | --- | --- | --- | --- |
| fp.character.25 | LM 25/24 | You Know the Way There / Character | Blade of Westernesse / 6 | 01 |
| fp.character.26 | LM 26/24 | Safe Paths in the Dark / Character | It Is a Gift / 3 | 01 |
| fp.character.27 | LM 27/24 | Wind from the West / Character | Orderly Withdrawal / 6 | 01 |
| fp.character.28 | LM 28/24 | Three Rings for the Elven-Kings / Character | Orderly Withdrawal / 6 | 01 |
| fp.strategy.25 | LM 25/24 | Every Little is a Gain / Muster | Shield-wall / 6 | 01 |
| fp.strategy.26 | LM 26/24 | Rangers of the North / Army | Advantageous Position / 3 | 01 |
| sh.character.17 | 17*/24 | A Balrog is Come! / Character | Durin’s Bane / 2 | 02 |
| sh.character.25 | LM 25/24 | We Won’t Go Back / Character | Fear of Their Masters / 3 | 02 |
| sh.character.26 | LM 26/24 | The Lore of the Elven Rings / Character | Fear of Their Masters / 3 | 02 |
| sh.character.27 | LM 27/24 | The Gates are Closed / Character | Words of Power / 1 | 02 |
| sh.character.28 | LM 28/24 | Captain of Despair / Character | Servant of the Shadow / 3 | 02 |
| sh.strategy.25 | LM 25/24 | Shadow of Dol Guldur / Army | Mûmakil / 3 and 5 | 02 |
| sh.strategy.26 | LM 26/24 | Madness and Horror / Army | Swarm of Bats / 0 | 03 |
| sh.special.05 | LM 5/5 | We Shall Get It / Character icon, automatic entry | none | 03 |

`lords.sh.character.17` replaces exactly `base.sh.character.17` according to LORDS p. 3 . The other twelve usuals cards it is added to the deck: 4/2/4/2. After the Lords themselves, the decks have **28/26/28/26 = 108** cards . `lords.sh.special.05` is placed outside the decks; is not 109 . card for shuffling. Print 25/24 does not mean an error, or 24 cards Lords: is the numbering of the base slot extension. Print 5/5 special card doesn't make her Minion Or a part of the Character deck.

<a id="istotne-różnice-i-logika-do-testów"></a>
## Significant Differences and Logic for Tests

- **Print a FAQ.** Photo We Won't Go Back of 2012 in the year has no final sentence excluding
  protection Hunt. Reproduction contains them; official FAQ 1.2 in the page 3 confirms this
  principle. There is no conflicting meaning. We save extended print according to FAQ, without
  claiming the date of its release.
- It is not chosen, never counts in hand and does not cost dice . LORDS p. 22 inserts them after
  elimination/rejection Sméagola, except for the last Companion Gollum. [Kevin Chapman, 12467544
  Response](https://boardgamegeek.com/thread/981433/article/12467544#12467544) confirms that Shadow
  may keep card for later tile; [21001986
  response](https://boardgamegeek.com/thread/981433/article/21001986#21001986) confirms its removal
  by Wind from the West. W API both entries have the author 34645 . Example of the test: the first
  tile with Gollum → refusal; the second → replacement, rebooting the window does not re-select. FAQ
  p. 3 place the replacement against the protection of the new tile.
- **Rings.** The player chooses the identity of the retrieved Ring. First used outside the game,
  then with Shadow. Example of the test: two successes, one Ring out of the game and two with Shadow
  → the first mandatory, second selected, not automatic replacement of any two.
- **Wind.** A hand review is only available in the absence of events Shadow on the table. The
  decision requires temporary, permanent, reconnect permission to watch others cards; the later
  projection cannot further reveal the hand. card special on the table disables the review
  alternative.
- **Orderly Withdrawal.**The condition is battle half or out and participating commander/Companion.
  Cancellation of the hit is optional, its consequence at the end of the round mandatory when
  performing. Do not replace this with a simple choice of Stop/Retreat and do not move to the siege
  storm.
- **The new Durin’s Bane has an additional alternative to legality: Balrog’s participation outside
  Moroia’s reach. It is not allowed to connect without changing the base condition of card 17.
  Activation does not contain additional movement; movement/attack is already involved in active Balroga
  instead. The capacity of the character itself still requires SRC-04/T25.
- **Madness.** [Chapman, response
  35552456](https://boardgamegeek.com/thread/2482588/article/35552456#35552456), author of 34645,
  confirms that after attacking an unobsessed army, recruitment refers to regions adjacent to the FP
  target. Example of the test: FP in Eagles’ Eyrie → free Old Ford/Carrock/Mount Gundabad are
  candidates, Mount Gram is not. The play condition is Sauron At War; the event icon is Army,
  despite recruitment.
- **Dol Guldur.** Recruits respect the stack limit already in this step, before the optional
  movement/attack; this is not a simple permission of excess to the end of action. Mûmakil has a
  public two windows and also compares pre-emptive hits of card FP. Example of the test: nine units
  in the field and Leadership Nazgíli 3 → at most one recruit before moving.
- **Captain.** die pays for the same play does not go to Hunt. The next results of Character are
  taken over the entire action; the departure of Witch-king removes card under the general rule BASE
  2.4.1 p.22. Correction of T27-CARDS-02: the earlier remark about the absence of this condition on
  the obverse is no exception to the instructions. The origin of these eyes remains relevant to
  KINGS p.17. You need to check the ending of nested battle/choice before moving dice.
- **Fear / Servant.** The first effect is devoted to the entire Leadership, the second Leadership
  selected Minion. Fear is premium on natural FP sixes in both throws; Servant retains a minimum of
  one dice FP. A joint initiative 3 does not mean a common cost logic.

The full conditions and order of all fourteen items are recorded per card in the index. These cases are the requirements of future T27 tests, **not the results of the domain tests performed**. No new unresolved source conflict has been detected that would block the reading of the obverses. The audit does not replace the exact integration scenarios T25–T27/T31/T33.

<a id="wynik-i-granice"></a>
## Result and limits

GAP-LORDS-CARDS can be closed after index and parcel control. This unlocks gate source T27 and T27-CARDS-01–03; they remain their implementation relationships, including T26. card character Lords are a separate GAP-LORDS-CHAR/SRC-04. Direct Ares 403 is a transport restriction, documented alongside the collected and compared sources. Changes do not include expansions in runtime and do not require a manual user test.

Checks carried out: `source_lords_cards.py --fetch` — PASS 14 components, 108 cards decks, packages 6 + 6 + 2 and five SHA-256/dimension the above; `source_cards.py` — PASS unchanged directory 96 cards and 14 the form of the base; `check_catalog.py` — PASS 30 families/34 windows; `tasks.py validate` — PASS 87 tasks. First `--fetch` detected actual JPEG under historical enlargement PNG ; format recognition improved and final run is counted. After change gate refreshed index by `tasks.py render` No startup .NET/Unity or hand tests: no game code changed.
