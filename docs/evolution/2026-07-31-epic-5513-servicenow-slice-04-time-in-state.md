# A display label is not a stored value — Epic 5513 slice 04

**Feature:** epic-5513-servicenow-integration | **ADO:** Story #5577 (parent Epic #5513) | **Shipped:** 2026-07-31 | **Commits:** `36cd1d10f..d996a9cca` (14 code commits, plus the roadmap, an ADR renumber and the DELIVER record)

## What shipped

A ServiceNow team whose integration account holds `itil` now gets true time-in-progress instead of request-to-resolution. The connector resolves the instance's state-span metric definitions once per sync, reads `metric_instance` in batches of 200 record handles, pairs each record's spans into transitions at the span's `start`, and dates the start of work at the first span the team maps to Doing. Where the instance cannot supply that — the account is refused the metric tables, or nothing measures state spans — the connector downgrades at runtime rather than failing, the existing sync-delta derivation takes over, and `ValidateConnection` tells the administrator which of the two causes fired, what to change, and which number their team reads until they do.

The shape is [ADR-114](../product/architecture/adr-114-servicenow-connection-validation-verdict-ladder.md)'s and nothing about it needed inventing: three pure static cores — `ServiceNowHistoryQuery`, `ServiceNowStateSpanMapper`, `ServiceNowHistoryVerdict` — composed by the connector, which owns all HTTP. No new library, no EF migration, no new route, one additive change to a shared contract (`ConnectionValidationResult` gained `Advisory`, `AdvisoryCode` and a `SuccessWith(...)` factory). Both decisions and their measurements live in [ADR-117](../product/architecture/adr-117-servicenow-started-and-closed-dates-without-itil.md) and [ADR-118](../product/architecture/adr-118-servicenow-transition-history-from-metric-instance-spans.md); they are not restated here.

## A display label is not a stored value

The connector asked `metric_definition` which definitions measure state, filtering on `type=Field value duration`. That is the string a ServiceNow administrator sees on the form. `sysparm_query` matches the **stored** value. Measured against PDI dev191338: `type=Field value duration` returned `{"result":[]}`; `type=field_value_duration` returned four rows. The slice matched zero definitions on a stock instance where the definition is present, active and out-of-the-box, and reported the missing-measurement advisory instead.

What makes this worth an evolution entry is not the mistake — it is that the mistake was **written down in advance and shipped anyway**. Step 04-01's own commit body says: *"the definition query filters on the literal 'Field value duration', which is the choice LABEL, not the underlying value (field_value_duration) … if the PDI returns zero definitions at the dogfood moment, this is the first thing to check."* The crafter who introduced the risk named it, named the symptom it would produce, and named the fix. Then five more steps were built on top of it, an adversarial review passed it, and the whole thing reached a live instance intact. **Being flagged is not the same as being caught.** A risk that costs one HTTP call to settle should be settled at the moment it is written, not annotated and carried; the annotation only paid off after the fact, by making the diagnosis instant.

## The second defect hiding behind the first

Fixing the query alone would have shipped something worse than a non-working slice. `type=field_value_duration` on a stock incident table selects **four** definitions, only one of which measures state: `Incident State Duration` (`incident_state`), but also `Open` (field `active`, values `true`/`false`), `Assignment Group` and `Assigned to Duration`. Real `metric_instance` rows for one demo incident carry `"New"`/`"In Progress"`/`"Resolved"` from the first and `"true"`/`"false"`, group names and user names from the rest. Paired into transitions, that reports `true → false` and `Network Team → Service Desk` as state changes — in a chart whose entire claim is that it measures what the instance measured.

ADR-118 D2 exists precisely to prevent that. Its reasoning had noticed those other definitions exist; it had not noticed that they share the type it chose as the discriminator. **A design can name the right goal and pick a mechanism that does not achieve it, and the gap between the two is invisible to every reader who agrees with the goal.** Worth noting how nearly the rejected alternative fared: D2 turned down a field-name filter because the state field differs per table, and on this PDI a field-name filter would have matched nothing at all — the connector reads state from `state` while the definition sits on `incident_state`. Both candidates were wrong; only one was measured.

The discriminator that survived measurement is the team's own state mapping. A label no team mapped is not a state span. It needs no per-table field knowledge, which is what D2 wanted, and it works for a customer's own definitions. **This amends ADR-118 D2** — the code implements it, the ADR text does not yet.

One consequence carried by the same fix: a slice-02 live integration test asserted `SyncedTransitions` is empty "because ServiceNow supplies no history to a read-only account", while connecting as `admin`. It passed only because of the first defect. It was turned around rather than deleted, and now asserts that history arrives and that every label in it is one the team mapped — which makes it the guard for the second defect too, measured against a real instance.

## Mutation testing found what an adversarial review did not

The adversarial review returned APPROVED with zero defects. It also cited a commit hash absent from the history and a stale test count, and it missed both of the gaps mutation then found. Its verdict was treated with suspicion for those first two reasons, and the suspicion was earned.

Mutation showed that the advisory copy could be blanked to `""` and that `ValidateConnection`'s capability read could be **deleted outright**, with the full suite staying green in both cases. The second is the backend half of the slice's declared exit gate: the obligation ADR-117 left open is discharged only if the advisory reaches an administrator, and nothing proved it was ever attached.

The diagnosis of *why* the existing assertions did not bite is the part that transfers, because the tests looked adequate and the roadmap had explicitly claimed they were sufficient:

- Each advisory is **one sentence concatenated from four fragments**, and every claimed literal sat on exactly one fragment. `itil` lived only on a fragment another mutant already killed, so blanking either of the two before it left the assertion satisfied.
- `Does.Contain(Table)` compared the advisory against **the constant the test had just passed in** — the CI ledger's most common survivor shape — and the table is interpolated twice, so each interpolation covered for the other.
- `Does.Contain("resolution")` was satisfied by a shared caveat fragment belonging to a different constant, so it never constrained either switch arm it was written for.

**Judge survivors, do not count them.** A 60 % score on a small pure type is not a rounding artefact; here each survivor named a real, separately-caused hole. Twelve tests closed them (8 backend, 4 frontend), and every kill was demonstrated by applying the mutant by hand and observing the failure — a Stryker re-run is unaffordable in this repo, because Stryker.NET has no test filter and the live `ServiceNowIntegration` tests execute inside the mutation loop.

## Steps that turn zero red tests green are the ones that ship undone

Three of the ten planned steps (04-06, 04-08, 04-09) had **no authored test over them**, because DISTILL wrote a pure function at each end of the advisory path — the verdict on the backend, `readConnectionValidation` on the frontend — and nothing over the wiring between them. The roadmap called this out and gave those three steps their own visible criteria specifically so they could not be folded into a neighbour and quietly skipped: *"folded into 04-04 it would have no visible criteria and would ship undone."*

That instinct was right and the mitigation was not enough. Mutation later proved the wiring was in fact unverified in both directions: the backend read could be deleted, and `ValidationAdvisory.tsx` had no test at all — its whole component body could be replaced with `{}` and it would render nothing, ever, with a green suite. Only a manual dogfood had ever proved it reached a screen. **A step whose entire value is that two tested things are connected needs a test of the connection, not visibility in the plan.** Making the step visible protects it from being skipped; it does nothing to make it verified.

## Two agents racing on one git history

Reflog, 2026-07-31: `fc0cc7796` committed step 04-06 at 09:48:47 in an incomplete state; the crafter reset to `HEAD~1` at 09:49:21 to redo it; at 09:50:20 an orchestrator `git commit --amend` landed in that 59-second window and folded its change into `e704c7381`, the *neighbouring* step 04-09 commit. Recovered by resetting back and re-committing 04-06 cleanly as `c56dce331` at 09:52:29.

Two things to carry: **re-check `HEAD` immediately before amending**, since an amend is a write to whatever commit happens to be current rather than to the commit you were looking at; and the blast radius stayed small only because `des-commit`'s owned-paths discipline meant the two agents were staging disjoint file sets. Nothing was lost, but the recovery depended on the reflog and on someone noticing within minutes.

## ADR numbering collides when work sits unpushed

Two ADR-118s reached `main`. Epic 5585's DESIGN wave scanned the ADR index and recorded "highest existing number is 117", which was **true of `origin/main` at that moment** — Epic 5513's ADR-118 had been written on 2026-07-30 but sat on an unpushed local branch, and landed first.

Resolved by renumbering 5585's to ADR-122, on cost rather than on precedence: 5585's is a design-wave decision with no shipped code behind it, while 5513's is cited by fifteen commit bodies that cannot be edited, by code comments in `ServiceNowStateSpanMapper` and `ServiceNowWorkTrackingConnector`, and by ADR-117's amendment header. **A sequential identifier allocated by scanning a shared index is only safe while everyone's work is pushed**, and a long-lived local branch quietly breaks that. The generalisation: prefer moving the artefact with the fewest inbound references, and check the *local* branches as well as the index when a wave allocates a number.

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors (`TreatWarningsAsErrors`) |
| `dotnet test` | **4202 passed / 0 failed** |
| `pnpm test` | **3820 passed**, 286 files |
| `pnpm build` + Biome | 0 errors, 0 warnings |
| Pre-authored RED tests | 43 / 43 green (38 backend + 5 frontend); 12 more authored afterwards, driven by mutation rather than by the plan |
| Backend mutation | **90.34 %** over the ServiceNow directory; the three pure cores and `ServiceNowWorkItemMapper` at 100 % |
| Frontend mutation | **100 %** (89.66 % before the survivors were killed) |

The two `LicenseServiceTest` failures the roadmap carried as a pre-existing baseline **did not reproduce**. That assumption is retired rather than inherited again — a useful reminder that an inherited "known failure" is a claim with an expiry date.

For anyone reconstructing the runs: **no Stryker config is committed in this repo**, by `.gitignore` policy, so both configs are written from scratch each time. The frontend one needs a `vitest.stryker.*` `include` list naming *every* test file covering the mutated code; a file missing from that list makes its mutants survive for want of a test run, which reads identically to a real gap. That happened once here and cost a full re-run — the same trap Epic 5459 hit from the `test-case-filter` direction.

## Still open

- **ADR-118 D2's amendment is unratified.** The code implements the label-based discriminator; the ADR text still describes the definition-type-only rule. The maintainer ratifies. ADR-114/115/116/118 all remain **Proposed**.
- **Unmapped states diverge across connectors, deliberately.** Jira / ADO / Linear / CSV keep an unmapped state as a transition endpoint carrying its raw label, so its time drops out of the totals; ServiceNow drops the span *before* pairing, so its time is absorbed into the preceding mapped state. Both kept — ServiceNow needs the filter because `metric_instance` mixes non-state measurements into the same table. Recorded on ADO 5612, and worth knowing before anyone reconciles connector behaviour by assuming they agree.
- **US-06 (#5578) must carry the negative result.** The slice's learning hypothesis — that history is affordable on a least-privilege account — is **formally disproven**; `itil` was accepted as an adoption cost, not because it is cheap. This is easy to lose now that slice 04 ships and looks like a win, and losing it would misrepresent the epic's central adoption argument.
- **No DEVOPS wave has ever run for this epic**, carried since slice 01, which is why DESIGN's Pact / contract-test recommendation for the Table API response shapes is still unrouted. Cheapest alongside slice 05.
- **Ten mutation survivors remain** in the connector's paging, `Link`-header parsing and URI helpers, plus `ServiceNowTeamQueryVerdict` at 86.67 %. All slice-01/02 code this slice did not touch; left rather than widening the blast radius.
- **A usability item, not a defect**: `Correlation ID=LIGHTHOUSE_DEMO` selected all 103 incidents, because ServiceNow silently drops a query term naming an unknown field. Slice 01's guard caught it and said so; the column is `correlation_id`. Filed on ADO 5612.

## Why this one is worth re-reading

Every defect in this slice was already knowable from something written down. The label-versus-value risk was in a commit body. The overlapping definitions were in the DESIGN's own prose. The untested wiring was named in the roadmap. The advisory's fragmented assertions were claimed sufficient in the same document that listed them. Nothing here was found by insight; all of it was found by **contact with a real instance and by mutants**, and each was sitting in plain text beforehand. The lesson is less about ServiceNow than about what a written-down risk is worth: on its own, roughly nothing. It becomes worth something only when it is attached to a cheap action taken immediately, or to a test that fails until it is untrue.
