<a id="t14--efekty-kontynuacje-i-koszty"></a>
# T14 — Effects, continuations and costs

Sources: BASE pp. 22–23, 29, 34–35; BASE-2.3 pp. 22 and 30; FAQ 1.2 p. 1–3; confirmed obverses with SRC-01. T14 implements Bilbo’s Song, Visdom of Elrond, Celeborn’s Galadhrim and Threats and Promises. Their combat applications and other card are part of the T21 packages.

<a id="kontrakt-wykonania"></a>
## Implementation contract

Before paying, the server checks the hand, event program, play condition, target required and specific use dice Event or printed type card are legal costs; Will of the West records the specific result of use. This decision is relevant to later selection capabilities cards . UI-28 binds a simple shortplay using Event without a separate category menu; UI-29 shows the cost at Play and provides alternative use of the same dice without clicking on the desired window. Selecting Event still opens the required windows Gandalf/An áriona; the client does not choose the answer for the player.

Approval removes card from hand, reveals it and pays die once. die remains Resolving until the end of the entire effect and the required army discards/excess. Local undo does not withdraw this approval. Continuation stores the program, next step, page, source, target and waiting status; programs and operations are typed C#, without scripts saved in JSON.

The execution stops at selecting figures or response. The answer checks the current candidates, not the old list in DTO. The nested response adds the frame to the same stack. Return completes the right step and resumes the parent. The empty reaction window still allows you to refuse the number of secret reactions available to reveal the hand. The definitions of the cards specific windows will arise from their rules, the testing infrastructure windows are not the new rules of the game.

The mandatory selection of one candidate is done without additional click. Grouping physically equivalent to figures does not merge different types: available regular and elite are still two choices. Automation does not include omitting response or spending alternative costs.

The mandatory part is performed to the maximum possible extent. The goals required as a play condition are checked before the cost; the lack of reserve in the effect manual is not automatically such a condition. The objectives of the individual steps are re-checked on execution. For example, the absence of an Elf to recruit by Celeborn’s Galadhrim does not miss the following selection. The event can be recruited before At War and into the besieged garrison, but does not take over the enemy settlement without any obvious exception.

<a id="efekty-na-stole-i-straty"></a>
## Effects on the table and losses

Active effect saves source, owner, target, modifier type and time Duration/condition expiration. The projection shows only the fields available to the page. Expiration moves card from table to pile rejected; temporary conditions and end turn/battle/Rounds are separate events.

Threes and Promises only blocks the normal displacement of the passive nation of FP by Muster. Events retain their own rules. card expires after the real displacement of the FP nation due to attack or capabilities of Companion. Self-activation of the nation or attempt to move the nation already At War is not a shift.

Taking over an empty settlement by movement is a separate reason for political progress and is not an attack itself. The effect of the event and capabilities of Companion also remains separate types.

The removal of the unit records the cause: the event either battle. After the event, the lone commander FP dies, Nazgûl and the figure remain; after the elimination of the entire army in battle, also participating characters and commanders die. The Shadow Reserve and permanent losses FP remain separate. The political effect is a separate typical event: only the indicated exceptions (e.g. Ents/Dead Men) are treated as an attack, not every loss from the event.

<a id="weryfikacja"></a>
## Verification

Tests compare normal execution with recording and resuming in the decision, maintaining a single cost, partial effect, privacy and expiration. Test HTTP/PostgreSQL destroys the first host in the middle of the event, creates a new and recovers the receipt. Manifest differs the implemented event from the unimplemented combat application.
