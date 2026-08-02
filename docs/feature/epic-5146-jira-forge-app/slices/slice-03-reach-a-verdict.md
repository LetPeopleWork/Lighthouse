# Slice 03 — Reach a verdict

**Story**: US-03 · **Epic**: [#5146](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5146)
**Repo**: `LetPeopleWork/Lighthouse-Jira-App`. **Not** this repository.

## Goal

Turn the app into an answer: show it to real prospects and write down whether Jira-nativeness is
worth investing in.

## IN scope

- `README.md` in the new repo: zero → rendered Lighthouse in a Jira Cloud site, including Forge CLI
  setup, `forge deploy`, `forge install`, and the URL configuration from slice 02.
- Known-limitations section in the README: Cloud-only (P2), HTTPS-only (P3), auth-disabled instances
  for demos (R2), double navigation (D1).
- **≥3** prospect or user conversations with the app installed and shown.
- `docs/verdict.md`: go / no-go, the reactions behind it, the platform limits found, and either a
  named follow-up epic or an explicit stop.

## OUT of scope

- Building anything the demos ask for. Feedback lands in the verdict; acting on it is the follow-up
  epic's job. This is the slice most likely to absorb scope — every good demo generates a feature
  request, and honouring them here turns a qualification exercise into an unplanned product.
- Marketplace submission, privacy policy, security self-assessment.
- Any change to the Lighthouse repository.

## Learning hypothesis

**Disproves if it fails**: *"Jira-nativeness is a real buying trigger, not a conversational
throwaway."*

The failure mode is not a crash — it is a shrug. If prospects who asked "is there a Jira app?" see
one and respond with mild interest and no change in the conversation, the honest reading is that the
question was a checkbox and the answer is **no-go**. That is a successful slice: it saves the cost
of a marketplace-grade app.

**Confirms if it succeeds**: prospects react to the embedded view specifically — they ask what else
it could do, where it would sit, who else would use it. Then the follow-up epic has both a mandate
and, from D2's revisit trigger, a first feature.

## Acceptance criteria

- **AC-03.1** Someone other than the author follows `README.md` end to end without asking the author
  a question, and it takes **≤10 minutes** (K2).
- **AC-03.2** ≥3 dated conversation notes, each capturing what the person said **unprompted** (K1).
  Unprompted matters: "do you like it?" produces agreement, not signal.
- **AC-03.3** `docs/verdict.md` states an explicit go or no-go with reactions, platform limits, and a
  named next epic or an explicit stop (K3).
- **AC-03.4** The verdict answers D2 directly — did the double navigation come up unprompted, and
  does scoped-view work move into the follow-up epic?
- **AC-03.5** Lighthouse repository unchanged across the whole epic (K4) — verified once here, since
  this is the epic's last slice.

## Dependencies

- Slices 01 and 02 green and installable.
- **Access to ≥3 prospects or users with Jira Cloud.** This is the real constraint: it is calendar
  time and other people's availability, not build effort, and it is outside the maintainer's
  unilateral control.
- At least one demo against a prospect's **own** instance, per slice 02's production-data
  requirement — the strongest version of the demo, and the one most likely to be declined.

## Effort estimate

Build effort ≤1 day (README + verdict scaffold). **Elapsed** time is governed entirely by demo
scheduling and will exceed one day. Recorded plainly rather than hidden: the carpaccio ≤1-day rule
is met on the work, not on the waiting, and pretending otherwise would make the estimate useless.

Practical sequencing: write the README and the verdict skeleton the day slice 02 lands, then append
one note per conversation as it happens, and close the epic when the third note is in.

## Reference class

The ServiceNow post-release verdict (`docs/feature/epic-5513-servicenow-integration/`, slice 05 —
"docs and verdict") is the direct precedent in this project: shipped connector, then a written
judgement on whether it earned its keep. The lesson carried forward is that the verdict must be
written **against pre-stated criteria**, or it becomes a summary of what happened rather than a
decision — which is why K1–K4 are fixed now, at DISCUSS, and not after the demos.

## Pre-slice SPIKE

None. Nothing here is technically uncertain.

## Dogfood moment

The maintainer's own first demo is the dogfood. If the README does not survive the maintainer's own
second install on a clean machine, it will not survive a prospect's first.
