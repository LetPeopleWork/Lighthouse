# Wave Decisions — DISCUSS — epic-5510-5881-refinement

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-02 · **Mode**: autonomous subagent
**Predecessor**: DIVERGE (Option 4 + maintainer overrides; D1–D31 settled, not re-opened).
**Successor**: DESIGN (`nw-solution-architect`). Peer review (`nw-product-owner-reviewer`) not run — coordinator's call.

## Config (given, not asked)

feature_type user-facing (full stack) · walking skeleton brownfield · research depth comprehensive, reusing DIVERGE ·
JTBD on (every story has a `job_id`) · density lean · per-wave review skipped.

## Scope Assessment: OVERSIZED → split proposed

~18 stories, ≥4 behavioural areas, ~16–18 days, three independently shippable outcomes. Proposed split: E1 Refinement
tab (new) → E2 Refinement need (#5881 retitled) ∥ E3 Sizing votes (#5510 retitled) → E4 Live Refinement sessions
(new) ; E5 Sizing calibration (Premium, unsliced). **Accepted by the maintainer 2026-10-02 (DD-18)**; ADO not changed
by this wave. One slice over 1 day (slice 11, ~1½d), called out. Slice 17 split into 17a/17b after DD-19.

## Decisions taken in this wave

Full text in `feature-delta.md` → "Wave: DISCUSS / [REF] Locked Decisions".

- **DD-1** Votes always open on every Work Item in a refinement state; the need number highlights the next N with an "enough for ‹date›" line (maintainer, 2026-10-02 — supersedes DIVERGE Option 4's shortfall gating).
- **DD-2** Band defaults: low 50% (median), high 85%.
- **DD-3** Verdict below / in / above, "stop" weighted like "refine more".
- **DD-4** Line after the Nth Work Item, N = high end, over all refinement Work Items in backlog order.
- **DD-5** Ready resolution: stage rule → state stage → votes meeting readiness.
- **DD-6** Next Refinement = first cadence date strictly after today (instance time zone); starting week for N > 1.
- **DD-7** "Yes, but…" counts as Yes; veto configurable on No and/or "Yes, but…".
- **DD-8** min Yes ≥ 1 (its 2/2 defaults superseded by DD-21).
- **DD-9** Append-only vote log; latest entry per voter counts.
- **DD-10** Identity: account (auth on) / self-declared per-browser name (auth off).
- **DD-11** No "can't tell yet" answer; comment-only entries flag an open question (confirmed, DD-17).
- **DD-12** Split hidden until you vote; presenter mode exempt; cancellable slice.
- **DD-13** ~~CLI/MCP read-only~~ — superseded by DD-19.
- **DD-14** Settings in a new "Refinement" section of Team Settings.
- **DD-15** Tab order and role-specific disabled tooltip.
- **DD-16** Tab opening reuses `TeamTabOpened` with a new route key.

Maintainer answers 2026-10-02 (recorded as decisions):

- **DD-17** No "can't tell yet" — DD-11 confirmed (Q1).
- **DD-18** Epic split accepted: E1 new, #5881 → E2, #5510 → E3, E4 + E5 new (Q2).
- **DD-19** Clients may vote and take back (once 16 exists); identity as in the UI; Lighthouse-Clients minor bump.
  DESIGN decides: (a) credential → person, else refuse; (b) where the required auth-off name lives on the client;
  (c) marking client-cast votes in the log; (d) MCP tool wording so the assistant confirms first (Q3).
- **DD-20** Presenter mode never records votes on anyone's behalf; it saves only the facilitator's own vote/comment
  (account with auth on, self-declared name with auth off); the room's conclusion goes into a comment (Q4).
- **DD-21** Readiness defaults: 3 Yes, veto off; min voters default 3 and never below min Yes (Q5).
- **DD-22** Voting first: 01 → 02 → 10 → 11 → 13 → 03 → 04 → 05 → 06 → 12 → 07 → 15 → 14 → 08 → 16 → 09 → 17a →
  17b → 18 → 19. Slice 13 ships standalone; 03 and 05 pick its vote-ready count up when they land.

## SSOT updates

- `docs/product/jobs.yaml`: NEW job `job-team-member-give-sizing-view`; `job-flow-coach-refine-just-enough` functional
  dimension corrected to total Throughput (D20) and always-open votes (DD-1); `updated:` unchanged (already 2026-10-02).
- `docs/product/personas/team-member-voter.yaml`: NEW persona.
- `docs/product/journeys/epic-5510-5881-refinement.yaml`: jobs re-pointed, steps aligned with D/DD decisions, voter
  journey reshaped, changelog entry.
- YAML parse validation: **not run** — no shell available to this agent. Hand-checked indentation only; DESIGN or the
  coordinator should run a YAML parse.

## Risks carried forward

- R4 (async votes are used) — measured by K4; no push channel (D21).
- R1 (setup friction) — first tested by slices 01/02.
- R3 (Lighthouse users run SLE-based refinement) — 5 Mom Test conversations still owed (maintainer).
- DIVERGE peer review status: "pending" in DIVERGE; not verified here.

## Maintainer questions

All resolved 2026-10-02 — Q1 → DD-17, Q2 → DD-18, Q3 → DD-19, Q4 → DD-20, Q5 → DD-21. None open; DD-19 (a)–(d) go to
DESIGN.
