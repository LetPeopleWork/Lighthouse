# ADR-179: The published block is emoji-delimited, anchored on its opening line, and appends rather than guessing when the markers are gone

- **Status**: **Accepted, hardened in DELIVER** (2026-08-25). Points 2 and 4 are narrower than written
  here after adversarial review demonstrated two ways to delete a user's own prose — see the closing note.
- **Date**: 2026-08-22
- **Feature**: epic-5565-delivery-date-sync (ADO Epic #5565, slice 04 / ADO #4463)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Slice 04 writes a Lighthouse forecast into a Jira Version's `description` - a field humans also write
in. Two requirements sit in tension. The block must be **machine-findable**, so a later write replaces
its predecessor rather than appending; and it must be **unobtrusive**, because a reader sees it next to
their own prose.

Slice 00 measured the field rather than assuming it (all evidence in the slice brief's `SPIKE OUTCOME`):

- The description is **plain text**, not ADF and not wiki markup. It round-trips byte-identically over
  the API, newlines included.
- A **hand edit through the Jira UI preserves it exactly** - every newline and both delimiters survived
  a real edit-and-save. This is the finding that makes marker-keyed replacement safe at all; the
  description-spam failure the brief feared does not occur.
- The **detail view renders the line breaks**; the Releases **list column collapses them**, wrapping at
  its own width instead.
- The ceiling is **under 16,384 bytes**. The designed block is around 200.
- An **emoji survives** the same round trip, stored as the literal codepoint with **no shortcode
  substitution**.

## Decision

**A multi-line block, opened by a `🔮` line and closed by a lone `🔮`, found by matching the opening
line rather than the emoji.**

1. **The block stays four elements over multiple lines**, in the order slice 04 requires - attribution
   first, then the write date, the 70/85/95 forecasts, and the target likelihood:

```
🔮 Lighthouse forecast - updated 2026-08-22
70%: 2026-09-15
85%: 2026-09-29
95%: 2026-10-13
Target 2026-10-01: 88% likely
🔮
```

   The detail view renders this as lines. The list column collapses it, and that is **accepted**: the
   list is a teaser, not the read path. Attribution leads precisely so the collapsed preview still reads
   as something true and attributable rather than a fragment.

2. **Detection anchors on the whole opening line** - the pattern `^🔮 Lighthouse forecast`, not the
   bare emoji. A person will not type that line by accident, so marker collision stops being a practical
   concern without the delimiters having to look like machine output.

3. **The percentiles come from the same call the product renders.**
   `CalculateMetrics(today, blackoutPeriods, 70, 85, 95)` is what `DeliveryWithLikelihoodDto` already
   uses, so the Release and the Lighthouse screen cannot disagree about which percentiles are on show.

4. **If the markers are unbalanced or absent, append a fresh block. Never infer a range to delete.** A
   user who deletes one delimiter, or pastes the emoji into their own sentence, must not be able to
   cause Lighthouse to remove text it did not write. The worst case becomes a visible duplicate block
   that a human can delete, which is recoverable; silently eating someone's prose is not.

5. **A hand-edited block is replaced wholesale**, not merged. Lighthouse owns the span between its
   markers and rewrites it entirely.

6. **Notification suppression does not apply.** ADR-142's mechanism is issue-scoped - `notifyUsers` is a
   parameter of the issue-edit endpoint, and `PUT rest/api/3/version/{id}` has no equivalent because a
   version edit does not mail issue watchers. Slice 04's "reuses the existing suppression posture"
   criterion is therefore satisfied vacuously: there is no notification to suppress. Recorded rather
   than skipped, and worth one confirming observation the first time a real Release is written.

## Rejected alternatives

**Bracketed keyword delimiters** (`[lighthouse:forecast]` ... `[/lighthouse:forecast]`), the original
scoping. They work - slice 00 proved it - but they read as machine junk in a field humans write in, and
the maintainer asked for better. Nothing about them is safer than an anchored opening line.

**A bare paired emoji** (`🔮` ... `🔮`) with detection on the emoji alone. Rejected: identical open
and close markers cannot tell which half was lost when a user deletes one, so the replace range becomes
a guess. Anchoring the open on a full line removes the ambiguity at no cost to appearance.

**A single sentinel with "Lighthouse owns everything after it".** Prettier still, and wrong: a user who
types a note below the block would have it deleted on the next refresh.

**Flattening the block to one line** so the Releases list column reads well. Rejected once the detail
view was measured - the line breaks do survive where people actually read, and dropping any of the four
required elements is a failed slice rather than a trim.

**A zero-width or invisible delimiter.** Invisible to the user is the problem, not the feature: someone
deleting characters they cannot see produces exactly the unbalanced state point 4 exists to survive.

## Consequences

- The block is legible on the release detail page and attributable even where the list truncates it.
- Replacement is safe against hand edits, which is measured rather than assumed.
- The append-on-unbalanced rule means a pathological description can accumulate blocks. That is
  deliberate, visible, and user-fixable - and it is the failure mode chosen over data loss.
- The 16 KB ceiling is far away but not infinite, so the appending path is a degradation to notice, not
  a steady state to design around.

## Hardened after adversarial review (2026-08-25, DELIVER slice 04)

The design held. Two of its rules were too loose to deliver it, and both were **demonstrated** rather than
argued.

**Point 2's anchor is the whole invariant part of the line, not the phrase.** `^🔮 Lighthouse forecast`
also matches a sentence that merely *begins* with it — and quoting the line Lighthouse wrote and typing
underneath it is the obvious way for somebody to argue with a forecast. Everything they typed was then
inside the span the next write replaced. The implemented anchor is `🔮 Lighthouse forecast - updated `,
trailing separator included.

**Point 4 needs a pairing rule, not just a balance check.** "Unbalanced ⇒ append" is not enough when the
description holds *more than one* opening line — which point 4 itself produces, since appending is what it
prescribes. An orphaned opening line paired with the **next block's** closing marker, and everything
between them went: in the repro, a line reading "DO NOT SHIP BEFORE LEGAL SIGN-OFF". Each opening line is
now matched only against a closing marker that appears **before the next opening line**.

**A closing marker is only believed when the line above it is the target line.** Not in the ADR, and
needed: a crystal ball typed *inside* the block closed it early, so the replace cut the block in half and
left the tail of an old forecast standing outside every marker — dates a reader takes for current, that no
later write could ever reach. Appending instead costs a visible duplicate, which is the failure this ADR
already chose.

**Leading whitespace is ignored on both markers.** It was ignored on the closing one and not the opening
one, so a block that picked up an indent stopped being findable while still being written: every publish
appended, and the indented block stayed for good.
