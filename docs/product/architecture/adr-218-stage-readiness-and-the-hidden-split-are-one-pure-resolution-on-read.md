# ADR-218: Stage, readiness, the open question and the hidden split are one pure resolution on read, enforced in the API

- **Status**: Proposed (DESIGN, 2026-10-02); **amended by the maintainer 2026-10-04** — see the amendment below,
  which supersedes decisions 1 and 2 where they differ. The hidden split (slice 14) is dropped.

> **Amendment (2026-10-04, E2 UI review).** There is no stage per state: stages come only from optional rules
> ("Ready when", "Being refined when"; unmatched rows are Waiting; when both match, Ready wins). Stage and votes are
> **two independent signals**: a rule decides the stage only, and the votes' readiness is computed as before and
> never overridden by a rule. Which one feeds the ready count depends on the Team: **without stage rules, the votes**;
> **with stage rules, the stage**. The row carries a mismatch flag when votes have been cast and disagree with the
> stage (no votes, no flag). The hidden split is gone: every vote and comment is visible to everyone. Recorded in
> `docs/feature/epic-5510-5881-refinement/feature-delta.md` (maintainer-decision blocks of 2026-10-04) and
> `distill/upstream-issues.md`.
- **Date**: 2026-10-02
- **Feature**: epic-5510-5881-refinement (slices 03, 08, 12, 13, 14, 18)
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect)

## Context

Whether a Work Item in refinement is Ready comes from three sources in a fixed order (DD-5): an optional stage rule,
the stage of its state, and the votes meeting the Team's readiness setting (min Yes, min voters, optional veto;
DD-7, DD-21). "Yes, but…" counts as Yes; a comment without a vote counts for nothing and marks an open question
(DD-11). Others' votes and comments stay hidden from you until you have voted on that Work Item (DD-12), and the
API — not only the UI — must hide them (AC-14). Presenter mode shows the split to the room (DD-12 exemption).
Slice 13 ships before slices 03 and 08 (DD-22), so the resolution grows in steps.

DD-5 left two points open: whether a matching rule also overrides the votes when it does **not** say Ready, and
what happens when several stage rules match.

## Decision

1. **One static, pure `RefinementResolution`** computes, per listed Work Item: stage, stage source, readiness
   (`Ready` / `NeedsVotes` with the number of Yes and of voters still missing / `NeedsDiscussion`), ready source
   (`Rule` / `Stage` / `Votes`), open question, and whether the caller may see the split. Inputs are facts only:
   the state's stage, the rule stages that matched, the current entries per voter, the readiness setting and the
   caller's voter key and presenter permission. No I/O, no clock.
2. **Precedence**: a matching stage rule **decides** the stage and the readiness — votes neither lift nor sink a
   rule-decided row (D16: "a configured rule overrides the vote outcome"). If several rules match, the most
   advanced stage wins (Ready > Being refined > Waiting). Otherwise the state's stage applies, and a row whose stage
   is not Ready becomes **Ready by votes** when `yes + yesBut ≥ MinYes`, `voters ≥ MinVoters` and no veto trips.
   A veto (≥ Threshold of `No`, or of `No` ∪ `YesBut`, as configured) gives `NeedsDiscussion`. Before slice 03 every
   state's stage is `Waiting`; before slice 08 no rule matches — the same function, fewer inputs.
3. **Ready count** = rows Ready from any source, each counted once. The verdict (ADR-215) reads this count.
4. **Open question** = a voter whose latest entry is a comment and who has no current vote. The asker clears it by
   voting.
5. **Hidden split, in the API**: for a caller with no current vote on a row, the response omits the split and every
   comment, keeping only `voteCount` and the readiness status (which reveals no split, AC-14.3). The per-Work-Item
   log answers `{ hidden: true, voteCount }`. **Presenter mode reveals splits only to a caller who passes
   `TeamWrite`** — with authentication or RBAC off that is everyone; a Viewer who presents sees splits only where
   they have voted. There is no bypass for anyone outside presenter mode (AC-14.2).

## Alternatives considered

- **Resolve in the browser from raw entries.** Cannot hide the split — the data would already be on the wire.
  **Rejected** (AC-14).
- **A `presenting=true` flag honoured for anyone.** Turns the API guarantee into an honour system any reader can
  bypass with one query parameter. **Rejected**; the chosen rule is the maintainer's to overturn (MQ-2).
- **Rules decide only when they say Ready** (votes may lift a rule-Waiting row). Makes a "Waiting" rule meaningless
  as soon as three people vote, which contradicts "the rule decides". **Rejected.**
- **Store readiness when a vote lands.** A second truth that a settings or state change silently invalidates.
  **Rejected** — derive on read.

## Consequences

- **Positive**: slices 03, 08, 12, 13, 14 and 18 each add an input or an output to one function; the rule is testable
  exhaustively without a database.
- **Positive**: slice 14 is cancellable by deleting one predicate.
- **Negative**: a Team admin who wants an unanchored vote must vote before opening presenter mode. Stated in the docs.

## Enforcement

- `RefinementResolution` is static and references nothing in `Services.Implementation` (ArchUnitNET).
- Table-driven tests over the precedence rows above, including "rule says Waiting, three Yes votes → not Ready".
- API test: a caller without a current vote receives no split and no comment text, in the view and in the log.
- Frontend: exhaustive `Record<RowReadiness, …>` and `Record<RefinementStage, …>` maps, no `default:`.

Cross-refs: [ADR-215](./adr-215-the-need-band-is-the-manual-how-many-for-the-next-refinement-read-at-100-minus-p.md),
[ADR-216](./adr-216-the-sizing-log-is-append-only-and-keyed-by-a-voter-key.md),
[ADR-217](./adr-217-a-sizing-vote-is-a-write-gated-by-team-read-through-a-named-requirement.md),
[ADR-013](./adr-013-rule-match-semantics.md).
