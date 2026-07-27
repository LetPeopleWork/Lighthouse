# Slice 05 — Docs, demo data, and the viability verdict

**Goal**: A ServiceNow admin can self-serve from the docs without over-granting; the maintainer gets a
recorded go / narrow / stop verdict backed by a run on a real customer instance and real user feedback.

**Stories**: US-05 (value) + US-06 (value).

This slice is where the epic's *stated outcome* lands. It is not "finalisation paperwork" — the epic
exists to produce a verdict, and without US-06 that verdict is an opinion.

## IN scope
- Docs page under the work-tracking-systems docs, matching the Jira/ADO/Linear structure.
- **Minimum role set**, documented and proved — not the roles that happened to work for an admin.
- An explicit statement that **v1 authenticates with basic auth** (D3), and what to check if an instance has basic auth restricted (D3a) — so a customer in that position finds out before installing rather than after.
- A worked ITSM example (D4): a real `sysparm_query` against `incident` or `sc_task`, plus how to point the connector at a different table if the shop runs Agile 2.0.
- `@screenshot` E2E per theme (project convention: `rm` the old PNG before regenerating — a <0.5% diff silently keeps the old image).
- `Scripts/DemoEnv/ServiceNowSystemUpdater.py` brought to parity with its three siblings, seeding **ITSM** records (D4). **The file already exists** — the environment-prereq story created a minimal version to keep the PDI alive and feed the SPIKE. This slice completes and documents it; it does not start from scratch.
- Standalone validation script/checklist runnable on the on-prem instance **without a Lighthouse build** and under a restricted account (D10, US-06 AC1).
- Cloud-vs-on-prem divergence list (US-06 AC3), even if empty.
- Feedback collection from ≥3 ServiceNow users/prospects, written up as a recommendation on ADO 5513.
- If US-03 was cancelled: the team-only limitation stated prominently on the docs page.

## OUT of scope
- The successor epic. Marketing copy on letpeople.work beyond confirming the new-system claim with the maintainer first.
- Any code change to make the on-prem run pass — findings get recorded, and fixes are separately scoped.

## Learning hypothesis
**Disproves** "ServiceNow is a huge untapped market for Lighthouse" **if** real ServiceNow users
cannot follow the page to a working connection, if the minimum role set turns out to be effectively
`admin`, or if the feedback says the integration solves a problem they do not have.
**Confirms** — with evidence rather than hypothesis — that the successor epic is worth scoping.

## Acceptance criteria
See US-05 AC1–AC5 and US-06 AC1–AC5 in `feature-delta.md`.

## Dependencies
- Slice 02 (there must be something to document).
- On-prem instance access — **the user has limited rights and building Lighthouse there is cumbersome**, which is exactly why US-06 AC1 demands a standalone, build-free script. Design it that way from the start, not as a retrofit.
- SPIKE Q8 (the role-set claim being verified) and Q9 (the divergence checklist).

## Effort / reference class
Docs + screenshots + demo script: ≤1 day. **US-06's feedback loop is calendar time, not effort** —
the ≥3-users KPI has a 30-day window. Ship US-05, open the feedback loop, and close US-06 when the
evidence arrives; do not hold the slice open blocking the board.

## Pre-slice SPIKE
Covered by Q8 and Q9. No additional spike.

## Dogfood moment
Hand the docs page to someone who has not seen the implementation and have them connect an instance
from scratch, timed against the ≤15-minute KPI. Then run the standalone validation script against
the on-prem instance under the restricted account.
