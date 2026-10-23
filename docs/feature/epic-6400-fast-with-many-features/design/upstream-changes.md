# Upstream changes from DESIGN — epic-6400-fast-with-many-features

DESIGN, 2026-10-23, revised the same day after the architecture review (iteration 1). Each entry quotes the DISCUSS
wording, says what the code showed, and states the reading DESIGN takes. No story is removed. The DISCUSS sections of
`feature-delta.md` are not edited. Each entry that changes behaviour is a question for the maintainer, with a default.

## UC-1 — Licence gates: "exactly as today" once loaded; only creation refuses while unknown (US-02, D30, AC-2.4, AC-2.5)

**Was (D30, verbatim)**: "Licence counts keep today's meaning. Whatever slice 02 shares, a gated action is allowed or
refused exactly as today, with RBAC on and off. A count that failed to load never reads as 'under the limit'."

**Was (AC-2.4 / AC-2.5, verbatim)**: "AC-2.4 Licence decisions unchanged, RBAC on and off (D30); the count is requested
once per page, not per mount. AC-2.5 A failed count never enables a gated action."

**What the code shows**: `hooks/useLicenseRestrictions.ts` is permissive today in two states. While the three requests
are on their way, and after any of them fails, `licenseStatus` is `null` and the hook answers `true` for
`canCreateTeam`, `canUpdateTeamData`, `canCreatePortfolio` and `canUpdatePortfolioData` ("When license status is null
(API error), be permissive and allow access"). So "exactly as today" and "a failed count never enables" cannot both
hold. Of the five consumers of the counts, two are create gates (Overview: new Team, new Portfolio) and three are edit or
update gates (`EditTeam`, `ModifyProjectSettings`, the detail pages).

**Now (DDD-11, ADR-259)**: once loaded, every answer is exactly today's. While loading or failed, only the two **create**
actions are refused (no reason while loading; "Couldn't check your licence" when failed). Edits and updates of existing
Teams and Portfolios stay allowed in every state, as today. This narrows AC-2.5 from "a gated action" to "a gated create
action". The server's `LicenseGuard` is unchanged and still refuses anything over the limit. Cost of the alternative
(refuse edits too): a transient failure would block saving settings someone is mid-way through, on an instance that is
within its licence. Maintainer question Q-M1.

## UC-2 — The heartbeat stalls: cause classified by measurement; thread placement is a hypothesis (US-01, W8, AC-1.3, K3)

**Was (DISCOVER W8, verbatim)**: "Each run still holds pool threads … Every request that arrives meanwhile blocks one
more pool thread in its own synchronous P\*/F\* read."

**What DESIGN adds**: whether the .NET 10 Kestrel heartbeat runs on a dedicated thread or on a timer on the thread pool
cannot be checked offline (OQ-1, an unverified hypothesis). On a pool timer, the warnings measure pool starvation; on a
dedicated thread, they also measure stop-the-world GC pauses (the round builds very large tracked graphs) and CPU
throttling of the container.

**Now (DDD-3)**: AC-1.3 and K3 stand. Task 01a classifies the stalls by thresholds fixed before any reading: pool
starvation if raising only the pool's minimum (experiment C) removes ≥ 80% of the heartbeat delay and the page-only
control (C0) adds no warnings; GC if GC pause time is ≥ 10% of wall time, or ≥ 50% inside ±1 s of each warning; CPU
quota if ≥ 20% of cgroup periods are throttled. The D27 order follows the branch: pool → 01, then 03, 02, 05; GC → 01
still ships, 05 and 03 move ahead of 02; CPU → 01 ships and the finding goes to the maintainer as host sizing.

## UC-3 — K4's computed-round target may be out of reach (US-05, AC-5.2)

**Was (AC-5.2, verbatim)**: "Computed round ≥ 30% shorter."

**What the code shows**: a computed round is gather + simulation + save + publish. Slice 05 shortens the gather and the
delete half of the save (ADR-261). It cannot shorten the simulation (C12) or the insert of the new histogram rows. If
01a's split shows the simulation is more than about 70% of a computed round, −30% is unreachable within the Epic's
constraints.

**Now**: AC-5.2 stands as a provisional figure (DISCUSS Q6). The D27 re-score sets it from the measured split.
Maintainer question Q-M4.

## UC-4 — A reference id stored twice: the in-progress lookup answers instead of throwing (US-03, AC-3.4)

**Was (AC-3.4, verbatim)**: "In-progress answers unchanged, on the list and in a Team update."

**What the code shows**: `FeatureRepository.GetByPredicate` runs `SingleOrDefault` in memory (`FeatureRepository.cs:52`),
so when two stored Features share a reference id, `TeamMetricsService.GetCurrentFeaturesInProgressForTeam` throws. That
breaks `featuresInProgress` and the Team's Feature WIP and worked-on counts. Today there is no answer to keep unchanged
in that case.

**Now (DDD-13)**: wherever today answers, the answer is unchanged. Where two stored Features share a reference id, the
first in forecast order answers (the dependency path's rule, `DependencyHonourPolicy.cs:180-181`), counted once, and a
warning naming the reference id is logged once per refresh. Maintainer question Q-M2.

## UC-5 — The size note says when its count could not be read, and how old the count is (US-06 Looks, AC-6.1)

**Was (US-06, verbatim)**: "**Looks**: no note while the count is unknown or failed to load — a missing nudge cannot be
misread as data; **sketched at the start of DISTILL**."

**What DESIGN weighs**: CLAUDE.md asks every fetching view for a failure look that never reads as "no data". A silent
failure reads exactly like "under 100". The count is of the Features stored at the last update, not of what the query
would bring in now.

**Now (DDD-19, ADR-262)**: while loading, nothing; on failure, the same info note with one line, "Couldn't check this
{Portfolio}'s size."; when over 100, the note reads "This {Portfolio} holds {N} {Features} (as of its last update). …".
Never updated, or at or under 100: nothing. Copy is provisional and settled at DISTILL's sketch. Maintainer question
Q-M3; the maintainer may restore DISCUSS's silent failure.
