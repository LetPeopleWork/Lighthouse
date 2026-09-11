# Slice 00 (SPIKE) — Draft and review the licence text

**Feature**: `epic-5874-relicense-source-available` | **Stories**: none | **ADO**: [#5967](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5967) | **Estimate**: ~4h of drafting
plus external legal turnaround

This is a **pre-slice SPIKE, not a slice.** It has no user-visible output and would fail the
slice-composition gate on its own. It lands as a precursor to slice 01, not as something shipped.

> **Status 2026-09-11 — drafting done through three review rounds, legal review outstanding.**
> Everything is in `../drafts/`. `LICENSE.draft` is one file in two parts — notice, then the
> Lighthouse Additional Terms, then ELv2 verbatim at the tail, round-trip verified byte-identical
> against Elastic's published text. `NOTICE` carries the version boundary and the preserved MIT text.
> `README.md` records provenance, the drafting departures from PolyForm Shield, and what each review
> round changed. `questions-for-counsel.md` is the self-contained brief.
>
> Three rounds of practitioner review have run and are folded in: the additional terms moved from a
> second file into `LICENSE` itself, the fill-at-release placeholder was removed in favour of a
> boundary anchored on v26.9.9.9, and sections 3–6 (mandatory rights, commercial agreements,
> termination, governing law) were added. What remains needs a lawyer.

## Goal

Produce a reviewed, signable `LICENSE` — one file in two parts, the Lighthouse Additional Terms above
the Elastic License 2.0 reproduced verbatim — plus the matching `NOTICE`, so that slice 01 has
something to commit and slices 02–03 have terms they can describe accurately.

## The assumption being probed

**That the no-competing-use clause can be written without sweeping in a customer's own internal
modifications, or into rights that mandatory law will not let us restrict.**

Disproves if it fails: the whole point of staying source-available. If the competing-use wording
cannot be bounded away from a customer's own instance, the fallback is plain ELv2 plus the AI
clause, accepting that internal derivation stays permitted. That is materially less than the epic
set out to do, and it is a decision to take back to the founders, not to absorb quietly.

*The assumption this section used to probe — whether a second document incorporated by reference
binds a recipient who opens only `LICENSE` — was retired in review round 2 by not relying on it. The
Additional Terms now live inside `LICENSE` as Part 1.*

Confirms if it succeeds: that the instrument is settled and every downstream slice can quote it.

## Questions this SPIKE must answer

1. ~~May ELv2's text be adapted and redistributed under a different name?~~ **Answered 2026-09-11 by
   research, and designed around rather than relied on.** No grant exists: ELv2 has no
   modified-versions clause (unlike MPL 2.0 §10.3), its seven-question FAQ never raises adaptation,
   and the document bears no copyright or reuse statement. Verbatim adoption is clearly fine and
   widespread; editing is unanswered. Hence the two-document shape — **Elastic's text is never
   edited**. Not legal advice; the lawyer confirms the shape rather than re-running the question.
2. Does incorporation by reference bind a recipient who reads only `LICENSE`? This is now the primary
   legal question and the shape's known weak point. The header block above Elastic's text is the
   intended mitigation; the lawyer says whether it suffices.
3. Does the no-competing-use clause permit an organisation to modify Lighthouse for its own internal
   use? It must — that is the difference between a source-available licence and a closed one, and the
   whole point of keeping the source inspectable. The wording comes from PolyForm Shield 1.0.0, whose
   Changes and New Works License already carries that property; it has to survive the reshaping.
4. Is PolyForm's README permission enough authority to reuse Shield's wording under a different
   name, once every mention of "PolyForm" and polyformproject.org is stripped as it requires?
5. Does the AI sentence — which has no precedent in any software licence — do anything a court would
   read, and is there a drafting of it broad enough to matter but narrow enough not to void something
   we did intend to permit?
6. What SPDX identifier do we publish? A `LicenseRef-` string used consistently, so the name in
   `LICENSE`, `NOTICE`, `README.md` and the docs is one string and not four near-misses. It is
   **not** `Elastic-2.0`. Current draft uses `LicenseRef-Lighthouse-SAL-1.0`.
7. What is the change date, stated as a calendar date, so that the MIT-in-perpetuity grant in AC-01.4
   has an unambiguous boundary?

~~The partner-hosting question.~~ **Closed 2026-09-11**: a partner hosting Lighthouse for their
client is not a motion we want and would not be permitted regardless, so ELv2 clause 1 restricts
nothing we intended to do and no carve-out is drafted. Carried unanswered in the BDR since
2026-08-01; record the answer there.

## Out of scope

- Any change to `LICENSE` itself — that is slice 01.
- Any copy change anywhere — those are slices 02 and 03.
- The Premium-gate hardening. Independent of this text, and a separate ADO item.

## Done when

Both drafted documents exist in the feature workspace, a lawyer has read them, and questions 2–7 each
have a written answer.
