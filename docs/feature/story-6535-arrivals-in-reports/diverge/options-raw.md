# Options — story-6535-arrivals-in-reports (DIVERGE, rev. 1)

**HMW**: How might we show a report reader whether the balance between Work Items arriving and finishing got
better from Then to Now, without making them compare numbers across panels or windows of different length?

SCAMPER prompts used: **S**ubstitute (ratio for difference → C), **C**ombine (arrivals with Throughput in one
panel → B), **A**dapt (Little's law: arrivals − finished = change in WIP → E), **M**odify (grow the existing
Throughput panel → D), **E**liminate (no balance, arrivals alone → A; no report change at all → F),
**R**everse (send the reader to the charts instead of bringing the number to the report → F).

Probability = how likely a typical team would build this shape; the six sum to 1.00. Head (A, B) = 0.60; the
tails (E, F) were sampled on purpose.

rev. 1 (review 2026-11-15): the old options 1, 2 and 6 were one mechanism (an arrivals series in a panel, with
or without extras). Old 6 (a header "balance line") is dropped as a variant of A that also breaks Epic 5878
D39 (no verdicts). A new tail, F, uses a different mechanism (navigation, no new computation). B's two
directions of good are scored as separate rows, because they change what the panel says.

| # | Handle | name_origin | p | Mechanism | Assumption it rests on | Cost |
|---|---|---|---|---|---|---|
| A | **Arrivals** panel | user-words ("Arrivals", E1) | 0.30 | A new metric shows **arrivals alone**: total and per day, Then and Now, change on per day with **no good direction** (neutral mark). | Readers can relate two panels themselves; arrivals alone are informative. | Low: one backend metric (copy of Throughput on daily arrivals), one panel, a neutral direction kind. |
| B | **Arrivals vs Throughput** panel | user-words (E2) | 0.30 | A new metric shows a **difference of two rates**: per side, arrivals per day, {Throughput} per day, and the gap per day (arrivals − {Throughput}). Change mark on the gap. | The balance is the point; repeating {Throughput} is acceptable. | Low-Medium: as A, plus the gap line and signed wording. |
| C | **Arrivals per finished Work Item** | coined | 0.12 | A new metric shows a **ratio**: arrivals ÷ {Throughput} per side ("1.3 started for each finished"); 1.0 = balance; lower is better. | A scale-free number explains better to a sponsor than a difference. | Medium: dash when nothing finished; a new unitless number. |
| D | **Throughput with arrivals** | coined | 0.12 | **No new panel**: ticking Arrivals **modifies the Throughput panel**, adding an arrivals line and a gap line. | Arrivals only mean something next to {Throughput}. | Medium: a panel's content depends on another checkbox; breaks "same props, same panel". |
| E | **WIP grew by** (net flow) | coined | 0.10 | Shows the **consequence instead of the cause**: {WIP} at the start and end of each window and the change (arrivals − finished = change in {WIP}). | Readers care whether the queue grew, not about arrivals. | Low-Medium: WIP history exists; not what the requester asked. |
| F | **Open on Metrics** | coined | 0.06 | **No report computation**: the report links to the Metrics tab's Arrivals and Throughput run charts, opened once on the Then dates and once on the Now dates. | The existing charts already answer the question; the report only needs to point at them. | Low: two links; nothing frozen. |

### B's direction of good (scored as two rows)

- **B-lower** — lower gap is better (E2: arrivals above {Throughput} is bad; draining is good). Fits the
  existing `"lower" | "higher"` kinds.
- **B-zero** — a gap closer to zero is better (E3: arrivals should match {Throughput}). Needs a third direction
  kind, and reads a slowing drain (−2 → −0.5 per day) as better, which E2 would not.
