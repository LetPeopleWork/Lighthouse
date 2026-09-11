# Slice 00 (SPIKE) — Draft and review the licence text

**Feature**: `epic-5874-relicense-source-available` | **Stories**: none | **ADO**: [#5967](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5967) | **Estimate**: ~4h of drafting
plus external legal turnaround

This is a **pre-slice SPIKE, not a slice.** It has no user-visible output and would fail the
slice-composition gate on its own. It lands as a precursor to slice 01, not as something shipped.

## Goal

Produce a reviewed, signable licence text — ELv2's three prohibitions plus a no-competing-use clause
plus an explicit AI sentence — so that slice 01 has something to commit and slices 02–03 have terms
they can describe accurately.

## The assumption being probed

**That ELv2's published text can be adapted and renamed at all, and that a no-competing-use clause
can be written without sweeping in a customer's own internal modifications.**

Disproves if it fails: the whole D1 shape. If Elastic's text cannot be adapted, the fallback is to
write the licence from scratch rather than derive it — more legal cost, same intent, and slice 01
slips rather than changes. If the competing-use wording cannot be bounded, the fallback is plain ELv2
plus the AI sentence, accepting that internal derivation stays permitted (this is materially less
than the epic set out to do, and it is a decision to take back to the founders, not to absorb
quietly).

Confirms if it succeeds: that the instrument is settled and every downstream slice can quote it.

## Questions this SPIKE must answer

1. May ELv2's text be adapted and redistributed under a different name? Elastic publishes it for
   others to adopt; adopting it **modified** is a different question and is the one that matters here.
2. Does the no-competing-use clause, as drafted, permit an organisation to modify Lighthouse for its
   own internal use? It must — that is the difference between a source-available licence and a
   closed one, and the whole point of keeping the source inspectable.
3. **The partner-hosting question, carried unanswered from the BDR since 2026-08-01.** ELv2 clause 1
   forbids providing the software to others as a hosted service. Does any partner motion we want look
   like "a PKT hosts Lighthouse for their client"? If yes, an explicit carve-out is drafted. This is a
   business-model call that has to be made before the wording closes, not after.
4. What SPDX identifier do we publish? A `LicenseRef-` string that we then use consistently, so the
   name in `LICENSE`, `README.md` and the docs is one string and not three near-misses.
5. What is the change date, stated as a calendar date, so that the MIT-in-perpetuity grant in
   AC-01.2 has an unambiguous boundary?

## Out of scope

- Any change to `LICENSE` itself — that is slice 01.
- Any copy change anywhere — those are slices 02 and 03.
- The Premium-gate hardening. Independent of this text, and a separate ADO item.

## Done when

The drafted text exists in the feature workspace, a lawyer has read it, and questions 1–5 each have a
written answer. Question 3's answer is recorded back into the BDR's open-item list, since that record
has carried it unanswered since August.
