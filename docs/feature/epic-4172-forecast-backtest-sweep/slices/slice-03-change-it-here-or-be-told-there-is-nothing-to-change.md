# Slice 03 — Change it here, or be told there is nothing to change

**Feature**: epic-4172-forecast-backtest-sweep · **ADO**: to create under Epic #4172 · **Story**: US-03
**Estimate**: ~3h · **Job**: `job-forecaster-check-the-forecast-against-what-happened`
**Depends on**: slice 01 (the region and the recommended value)

**Reference class**: `ThroughputQuickSetting` in `QuickSettingsBar` (`TeamDetail.tsx:402-430`) — already
present on every Team tab, already writing `throughputHistory` through `canUpdateTeamData` with its own
validation. This slice opens it pre-filled. **It is a pre-filled open of a shipped control, not new
machinery** — which is the only reason Apply is in the MVP at all.

## Goal

When the check finds the Team's sampling window outside the sound region, the forecaster changes it in one
press without leaving the page. When it finds the window inside the region — the modal case — there is
nothing to press and the artifact says so.

## The coherence problem this slice answers

The check now reports two things. The **sampling window** is a setting, and the research says it barely
matters in this range. The **confidence level** is where the signal is, and — confirmed by searching the
tree, not assumed — **there is no Team setting for it.** `Team` carries no percentile field;
`ServiceLevelExpectationProbability` is a cycle-time SLE consumed only by `GetSleRiskForTeam`, bounding
how long one Work Item may take, not which percentile a How-Many forecast is quoted at.

"We showed you the axis that matters and gave you a button for the one that does not" would be a real
incoherence. It is answered by saying the true thing out loud, as permanent copy:

> *The sampling window is a setting. It has a control, and this check found it barely matters in this
> range.*
> *The confidence level is not a setting — it is which of the four numbers you say out loud in the room.
> This check found it matters enormously. There is no button for it, because there is nothing to press.*

So the feature's recommendation on the axis that carries the signal is a **behaviour** change, not a
settings change — which is exactly what a Community feature meant to convince people of a method should
produce.

## IN scope

- An apply control beside the verdict, **rendered only when the Team's current `ThroughputHistory` falls
  outside the sound region**, naming the value it would set.
- Pressing it opens the shipped `ThroughputQuickSetting` in the Team detail header, pre-filled. The write
  happens only on the user's save, through that control's existing validation and existing endpoint.
- The "no control, and here is why" state when the window is inside the region.
- The permanent copy above, explaining the asymmetry between the two axes.
- RBAC gating through `useRbac()`: a user with Team read but not Team write sees the verdict and the
  evidence and is not offered the control.

## OUT of scope

- **A new write endpoint or a new write path of any kind.** If this slice adds one, it has gone wrong.
- **Inventing a Team-level confidence-level setting.** Escalated as its own ADO item, explicitly not
  folded into this Epic. DISCUSS does not invent settings.
- Auto-applying anything. C2, and O3 scored 9.7 — over-served. The diagnosis is the scarce thing; the
  keystroke is not.
- Any change to `ThroughputQuickSetting` itself beyond accepting a pre-filled value.

## Learning hypothesis

**Disproves, if it fails**: that Apply is near-free. The whole reason it survives a 9.7 over-served
opportunity score is that `ThroughputQuickSetting` already does the job from every Team tab. If opening it
pre-filled from another component turns out to need lifted state, a context, or a change to the control's
own contract, then Apply is not near-free and the honest response is to **cut it**, not to build the
machinery. O3 says so.

**Confirms, if it succeeds**: the irreducible function the job analysis named — *replay → compare →
adjust* — reaches its third step, which is what M1 won on (DECISION-CHANGING 5).

## Acceptance criteria

AC-3.1 through AC-3.5, in `feature-delta.md` under US-03.

## Notes for the implementer

- The RBAC rule is architectural, not incidental: **no component may fetch
  `/api/latest/authorization/my-summary` directly.** All UI gating derives from the `useRbac()` hook.
- `ThroughputQuickSetting` sits in the Team detail *header*, which is rendered above every tab, so no
  navigation is needed — the control is already on screen when the Forecast tab is.
- **Do not reach for ADR-127's team-settings advisory channel.** It describes a mechanism that no longer
  exists: Story #5612 deleted the `Advisory`/`AdvisoryCode` pair and `SuccessWith`, `ValidationAdvisory.tsx`
  is absent from the whole frontend, and a later rung was explicitly built *"rather than reviving them"*.
  ADR-127 carries no note saying so.
- Bear `Team.ThroughputHistory`'s split default in mind while testing (entity 30, both UIs seed 90) — it is
  a real defect with its own ADO item and is **not** fixed here, but it will show up in fixtures.
