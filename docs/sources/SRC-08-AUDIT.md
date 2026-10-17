<a id="src-08--faq-i-aktualność-instrukcji"></a>
# SRC-08 — FAQ and Manuals up-to-date

Review 2026-10-05 : Complete official instruction was obtained**2.4.1**, all comparisons 48 pages from 2.3 and rechecked the indexes FAQ publishers. `GAP-FAQ` is closed for attached instructions and confirmed FAQ 1.2 ; the result shall not replace the audits of the obverses expansions nor implementation T23/T33. Direct host Ares continues to return HTTP 403 , therefore origin PDF confirms the publisher's reply archive.

<a id="potwierdzone-materiały"></a>
## Confirmed materials

[FAQ Ares page](https://www.aresgames.eu/2648) still indicates 1.2 with 2014-09-09 and the range of baza/Lords. All three pages of [local copy](materials/base-faq-v1.2.pdf) have been read. [Publisher Archives](https://www.aresgames.eu/downloads/archive) and [product Kings](https://www.aresgames.eu/games/war-of-the-ring-line/kings-of-middle-earth) do not list a separate FAQ Kings. This is the result of a review of these entry points, not proof of the absence of all the authors' settlements. FAQ cards and Hunt for the Ring apply to other games; the era of Italian cards does not replace English obverses.

Previously retrieved [2.3 instruction](materials/base-rulebook-v2.3.pdf) from [copy of Ares document](https://warofthering.eu/WotR_2nd_Edition_Rules.pdf). The version is printed on p. 48; metadata PDF: 2017-04-11, 48 from pages. Manifesto attaches SHA-256 as `BASE-2.3`. Original `BASE` and `BASE-2.3` were preserved as reference material. The second tested copy of [Gamer’s HQ](https://gamers-hq.de/media/pdf/af/8b/f2/WOTR001-Rulebook-EN-v23-web.pdf), as well as the copy of University of Waterloo read on resume, is also described as 2.3.

<a id="instrukcja-241-pochodzenie-i-kompletność"></a>
## Instructions 2.4.1: origin and completeness

[Local PDF 2.4.1](materials/base-rulebook-v2.4.1.pdf) is from [Ares answers saved 2024-05-20](https://web.archive.org/web/20240520185937id_/https://www.aresgames.eu/download/20980/) . [Independent save with 2022-03-25](https://web.archive.org/web/20220325084954id_/https://www.aresgames.eu/download/20980/) contains identical bytes. Both returned HTTP 200 and `application/pdf` ; the preserved header indicates `WOTR001-Rulebook-EN-v24_1-web.pdf` The document has 5 867 232 bytes, 48 readable pages, end marker PDF and printed version 2.4.1 ed. 48 . Create metadata: 2022-01-20 15:22:49 UTC , modifications: 15:27:12 UTC .

SHA-256: `ebb4d6d4ab1348e8e4c604ea7d9d414e928034d93255d58bd61fa8df6d2f28a4`. [Manifest](manifest.json) attaches it as `BASE-2.4.1`; [record of evidence](base-rulebook-v2.4.1-audit.json) contains the address and control range. We store the material in the existing source directory, with the names of the publisher and copyright; it is not a client resource or a declaration of the right to redistribute it.

[Later recording Ares from 2025-10-09](https://web.archive.org/web/20251009115742id_/https://www.aresgames.eu/download/20980/?tmstv=1759580847) is cut to 5 Mi B Each of these. 5 242 880 Bytes corresponds to the beginning of a full PDF , and the headers retain the same name and original length 5 867 232 We do not treat a cut copy as a complete source. [Ares page from 2026-04-22](https://web.archive.org/web/20260422045501id_/https://www.aresgames.eu/768) still indicates the same resource `download/20980` ; the current index of the page also lists it. We do not assign the date of indexing the message of proof of subsequent change of text.

<a id="porównanie-23--241"></a>
## Comparison 2.3 → 2.4.1

PDFKit read all pages of both documents. The text of the page on the page on the standardization of Unicode NFKC and white characters was compared; the word order and number differences were reviewed, separating the columns from the changed content. Also, pp. 5 and 48 of the new PDF: a large difference in the extraction of p. 5 came from miniature instructions and player help. No new rule or change in the meaning of the rules 1v1 was found.

| Location | Actual change or confirmation |
| --- | --- |
| s. 20 | `sege` letterpad changed to `seige`; the meaning of action siege unchanged. |
| s. 28 | Link to end battle corrected from p. 32 to 31. |
| s. 47 | A reference to the contents of the battle-line, changed from 29 to 30. |
| s. 48 | 2.4.1 version and copyright year 2021 instead of 2016. |
| s. 41 | The Hunt's transmission text remains compatible with 2.3: possibility to use 0..maximum; confirms the existing `HuntTests.ShadowCanDeclineRerollsEvenAfterASuccess` scenario. |
| s. 43 | The declaration precedes the entry into the Mordor track and the re-enactment of the pool; no change to Balrog's earlier decision. |
| s. 34–35 | No new change in the policy text; we retain Chapman's confirmed decisions as indicated below. |

The other differences concern the order of headhead, column, point, component illustration and format extraction of the contents. Workpieces of comparison: `.artifacts/source-audit/SRC08-followup-v23.txt`, `SRC08-followup-v241.txt`, `SRC08-followup-word-diff.txt`, `SRC08-followup-word-count-delta.json`; reproduction consists in the extraction of both sides of pinned PDF and compared to the exclusion of system differences. No code changes or re-starting of behavior tests were required: this test is an existing cover, not a new result of the performance of SRC-08.

Rereading the indexes of the publisher 2026-10-05 confirms FAQ 1.2 with the scope of baza/Lords. Sites [Lords](https://www.aresgames.eu/games/war-of-the-ring-line/lords-of-middle-earth), [Kings](https://www.aresgames.eu/games/war-of-the-ring-line/kings-of-middle-earth), [FAQ](https://www.aresgames.eu/2648) and [archive](https://www.aresgames.eu/downloads/archive) do not indicate a separate FAQ Kings. Treebeard update is related to promosa/Warriors and FAQ cards, Hunt for the Ring and the new Sieges are related to the scope beyond that MVP. No separate entry of Kings is the result of a review of these indices; a specific later undiscovered settlement still requires verification in the appropriate package.

<a id="rozstrzygnięcia-do-implementacji-i-testów"></a>
## Implementation and testing decisions

The following findings are derived from FAQ 1.2; the locations refer to its three sides.

| Location | Required behaviour | Tasks |
| --- | --- | --- |
| s. 1, settlements | The settlement affiliation on the card means the original page; the current control is checked separately when the text requires it. | T14, T20, T21 packages |
| s. 1, drawing | Complete the whole selection procedure before discarding to six. T10 is already doing so. | T14, T21 |
| s. 1, reveal/declaration | The final discovery region ban applies to cities/Stronghold FP still controlled by FP. It is also necessary to ignore the discovery icon after declaration. | T15, T16 |
| s. 1, combat timing | Mûmakil has separate effects initiatives; the SRC-01 index stores [3,5]. | T17, T21-SH-S-02 |
| s. 2, activation | Companion activates the right nation also at the entrance to the game in its city/Stronghold. | T20 |
| s. 2, strongholds | Each controlled by Shadow Stronghold on the revealed route gives a separate tile. | T16 |
| s. 2, Dead Men | card can recruit and take over the region controlled by Shadow. It is a clear exception. | T21-FP-C-04 |
| s. 2, Riders | The recruiting commander is from Rohan. | T21-FP-S-03 |
| s. 2, combat cards | Confusion checks the unmodified one; Heroic Death removes the commander of FP; subtract/forfeit leadership are equivalent in Cruel as Death and They Are Territorial. | T17, T21 |
| s. 3, Sméagol | The effect of his tile precedes the usual procedure of Hunt; Mithril Coat will not cancel this entry or We Won’t Go Back. | T25, T27 |
| s. 3, Lords guides | Alternative Boromir/Gimli/Legolas disconnected by the same tile are one group. The return of an alternative hobbit does not circumvent the ban on entering the besieged friendly Stronghold. | T26 |
| s. 3, Lords cards | In Won’t Go Back, he adds direct Corruption outside the Hunt modifiers. In Shall Get It lists a tile before settling it. | T27 |

The `BASE-2.3` manual further clearly describes on p. 11 meaning unconquered, and on p. 22 effects of losing the last unit by event: the commanders of FP are removed, but Nazgûle and characters remain, unless card provides otherwise. The loss of the army in battle has different effects (p. 30). T14 must retain the cause of the loss.

<a id="historia-niedostępności-i-granice-audytu"></a>
## History of inaccessibility and audit limits

[Ares English Rules](https://www.aresgames.eu/768) provides an update to 2017-04-11. [Update message](https://www.aresgames.eu/18664) is indexed with date 2026-09-01. During the first attempt, social tips exchanged the file `WOTR001-Rulebook-EN-v24_1-web.pdf`, but the name itself did not confirm the content or version. Direct download [publisher file](https://www.aresgames.eu/download/20980/) and the address variants continue to return HTTP 403. Reading the index works through the search engine. The problem of getting the full document has only solved the above copies of the publisher's archival responses.

The community almanac v98 (2025-08, `https://tinyurl.com/WOTRAlmanac`) was acquired by hand as an index of links to settlements. It was not given the status of an independent official errata or copied a foreign engine.

The condition of the resume has been fulfilled: the English PDF 2.4.1 has been obtained, the printed version has been checked, the rules have been compared and the result is clipped. The `GAP-FAQ` closure removes the gate T23/T33, but the tasks still require completion of their implementation dependencies. No new conflict has been detected requiring separate gate. Subsequent audits of the Lords/Kings/Fate obverses remain separate tasks of SRC-03–SRC-07.

Checks: reading the whole FAQ in the first part of the audit; re-checking the Ares indexes; revision control, metadata, 48 pages and full PDF 2.4.1 compliance SHA-256 copy 2022/2024 ; compliance with the prefix of copies 2025 ; comparison 48 pages from 2.3 ; validation of the registry, sources and directory. Actual commands and end result saves history SRC-08 User tests remain postponed.

The supplement with T21-SH-CHAR-02 (2026-10-05): almanacs p. 24 contains a non-renotice claim that the Hunt reroll is mandatory. The local official BASE 2.3 p. 41 uses “can” and “may” in examples. Without a confirmed newer errata we retain the choice of 0...maximum of reroll, consistent with this source and test `HuntTests.ShadowCanDeclineRerollsEvenAfterASuccess`. Comparison with 2.4.1 when resuming the audit confirmed the unchanged text of this paragraph. The Naggûl Strike! separately allows optional rejection of card or Hunt; it is not to confuse the choice of the effect with the performance of the already begun procedure.

The addition with T21-SH-CHAR-03 (2026-10-05): BASE p. 43 and the confirmed version of 2.3 require full resolution of the declaration before entering the Mordor track and recreating the pool. The unsupported almanac paragraph about moving to the track before Balrogo does not overwrite the primary source. The implementation keeps the declaration for ending the reaction and additional fortresses; it then re-releases the phase decision. Comparison with the official PDF 2.4.1 confirmed the same order. The other directly checked Chapman's decisions about Breaking, remaining in Moria, sanctuary and Daring were written with links in [specification kart](../rules/T21-SH-CHAR-03.md). At this stage, GAP-FAQ remained open; comparison with 2.4.1 described above closed missing.

Supplement with T21-SH-CHAR-04 ( 2026-10-05 ): Chapman's direct statements about Jerkír were checked all over action ( 19480575/35129694 ), cost of the Ring ( 22851460 ), reverse ( 22536963/45328584 ) and Devilry ( 35365796 ) Policy settlement with 2021 r. ( 37881658/37884619 ), confirmed in 2022 r. ( 40589850 ) replaces the older moment activation/przesuni owls at the start of the attack: both effects settle the end of the whole action The record keeps the attacked nations from loss. The Strider and Aragorn obverses allowed to correct the earlier data error SRC-02 : Strider has only the icon of the North. [Sources, examples and package tests](../rules/T21-SH-CHAR-04.md) At this stage GAP-FAQ remained open; comparison 2.4.1 the above has closed the missing.

T21-SH-STR-01 (2026-10-05): the statements of Chapman 35346194 (Elves only for Valinor), 35387928 (Denethor also save and get away) and 35426600/35428935/35429088 (Onslaughter makes an additional throw after its own elimination, Relativeless does not occur to the future Combat). [Description and links](../rules/T21-SH-STR-01.md). This does not close the independent gate with the updates of the entire instruction.
