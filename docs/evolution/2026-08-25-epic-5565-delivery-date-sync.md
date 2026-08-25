# Delivery dates that come from the tracker, and forecasts that go back — Epic 5565

Shipped 2026-08-23 → 2026-08-25 in six slices plus a spike. Premium.

The complaint behind it is that a Delivery's date was always typed by hand. Teams already keep that date
in their tracker — a Jira Release with a date on it — and every Lighthouse user was copying it across,
then copying it again each time it moved. Half the Epic fixes that. The other half sends the forecast
back, so people who plan in Jira and never open Lighthouse can see it on the Release they already read.

## What shipped

| Slice | ADO | What it added |
| --- | --- | --- |
| 00 | 5827 | A timeboxed probe against a live Jira. Findings only, no code — and four of its assumptions were wrong, which is the whole reason it ran first. |
| 01a | 5828 | See what a Release would give a Delivery: the picker, the preview, and the verdicts that say why a Release cannot be bound. |
| 01b | 5829 | Create a Delivery from a Release. The Release owns the name, the date and the Features; every hand-write to those three is refused while bound. |
| 02 | 5830 | Keep the date in step, on the refresh that already runs. Nothing about a bound Delivery is maintained by hand. |
| 03 | 5831 | Say so when the Release is gone. The values freeze and stay; nothing unbinds on its own. |
| 04 | 4463 | Publish the forecast onto the Release's description, per Delivery, off by default. |
| 05 | 5832 | Say so when the tracker refuses the write, on the Delivery it refused. |

ADRs **166**-**171** (inbound) and **178**-**180** (outbound).

## The probe changed the design four times

It was scoped to ship findings and no code, and that paid for itself immediately. The refusal for a
Version write is **HTTP 400, not the 403** the API documents — code written against 403 would have
shipped a silent no-op. `ADMINISTER_PROJECTS` is **per project, not per site**, which is why the refusal
report ended up on the Delivery rather than on the connection. The Releases list **collapses the
newlines** its detail view honours, which decided the block's ordering. And this Epic's own DISCUSS note
that the demo Release sat six months in the past was simply wrong — it sat six months in the future.

## Remote always wins, and only "gone" may retire a binding

The inbound half rests on one distinction that everything else is arranged around: **a source that could
not be read has said nothing about whether it still exists.** A network blip, an expired credential and a
deleted Release all arrive as a failed call, and treating them alike would have Deliveries quietly
stop syncing — or worse, report a Release as finished on the evidence of a bad minute.

So the transient reason is refused by the aggregate outright and filtered by the service before it is
ever offered, and the classifier names what is **permanent** rather than what is transient, so a reason
appended to the enum later defaults to saying nothing.

A Release that really is gone freezes everything it last gave and says so. It does not unbind: the name,
the date and the Features are why somebody keeps the Delivery rather than deleting it.

## The outbound half is opt-in per Delivery, and that moved twice

It started as a Portfolio switch. **D8a moved it to the Delivery** on the observation that a Portfolio
routinely holds some Releases shared with a customer and some not, so an all-or-nothing answer forces the
coarser one on both. Living on the binding also makes an invariant free that is unrepresentable anywhere
else: the switch is meaningless without a Release, so the aggregate refuses it on a Delivery that follows
nothing and letting go of the Release clears it.

The refusal report followed the switch down, superseding ADR-180's first point. The argument that had
moved it off the connection — the permission is per project — applies just as well to a Portfolio. What
decided it was ADR-180's own rule that the state clears on the next successful publish: **shared state
cannot express a mixed outcome**, so one Delivery's success would clear another's standing refusal and
tell an administrator the problem had gone away while it was still there.

## What is written into somebody else's Jira

A delimited block in the Release description, opened by a `🔮` line and closed by a lone one, carrying
four things: that Lighthouse wrote it, when, the 70/85/95 forecasts, and the likelihood of hitting the
target with the target named. The percentiles come from the same call the product's own screen makes, so
the two can never show different sets.

**Never `releaseDate`.** Writing the forecast into the field the target lives in would make Lighthouse
overwrite the value it declares the tracker owns, and the likelihood would converge on the chosen
percentile by construction — a number that has stopped carrying information.

## The rule that mattered most: never delete what Lighthouse did not write

Adversarial review demonstrated two ways the merge could eat a user's own prose, and both were the exact
failure ADR-179 exists to prevent.

A block whose closing marker somebody had deleted was paired with the **next block's** closing marker,
and everything between them went — in the repro, a line reading "DO NOT SHIP BEFORE LEGAL SIGN-OFF".
And the opening line was matched on a prefix, so a sentence that merely *began* with the phrase opened a
span; quoting the line Lighthouse wrote and typing underneath it is the obvious way to argue with a
forecast.

Each opening line is now matched only against a closer that appears before the next opening line, the
anchor includes the separator that always follows, and a closer is believed only when the line above it
is the one the block always ends with. When the pair cannot be found whole, a fresh block is **appended**:
a visible duplicate is recoverable, and eaten prose is not.

## What was deliberately not built

- **No writes to the member issues.** Repeating one Delivery-level number across N issues is the noise
  `quiet-jira-writeback` spent an Epic removing. The Release is the object the number is about.
- **No Portfolio-level default with a per-Delivery override.** Off-by-default plus per-Delivery means
  poor discoverability, and that cost is accepted: the remedy is easy to add once adoption shows it is
  needed, and awkward to unpick once people depend on a bulk switch.
- **No pre-flight permission check.** The write attempt is the check; `mypermissions` is a reporting
  companion, not a gate.
- **The refusal report does not name the project.** Lighthouse never persists which project a bound
  Release belongs to. Recorded as an unmet clause of AC-06.1 rather than reinterpreted.

## Lessons

- **A probe that ships findings and no code is cheap and it was right four times.** Every one of those
  four would have been a shipped defect or a wasted slice.
- **Adversarial review found something real on every slice, and twice by running an experiment rather
  than reasoning.** The two data-loss paths in the merge were invisible to seven specifications written
  for exactly that rule.
- **Mutation testing found whole untested branches, not just weak assertions.** The carriage-return
  branch of the line reader had nine survivors on one expression — nothing exercises it by accident,
  because the build host separates lines with a bare line feed. A description pasted from a Windows
  editor would have made every publish append.
- **Registering a second handler for a domain event silently hijacks every `GetRequiredService` of it.**
  Three fixtures began exercising the new handler and reported that no snapshot had been recorded — a
  failure naming the recorder and pointing nowhere near the cause. Resolve by type when more than one
  handler can listen.
- **Sonar's cognitive-complexity rule bit three times in one Epic**, always in a React render body one
  conditional over the line. The pre-push `biome lint --only=complexity/noExcessiveCognitiveComplexity`
  check caught all three before CI did.
- **Mutating a presentational component measures the renderer.** Scoring the whole notice gave 65 % on
  four `sx` survivors jsdom cannot see; line-scoped to its logic it reads 86.67 %.
