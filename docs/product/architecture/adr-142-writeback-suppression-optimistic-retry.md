# ADR-142: Notification suppression is attempted optimistically and retried without it on 403 — the write never regresses

**Status**: Accepted
**Date**: 2026-08-08
**Feature**: `quiet-jira-writeback` (ADO Epic #5500 "Quiet write-back", slice 04 / Story #5505)
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE
**Evidence**: SPIKE-03 Q3/Q4, `docs/feature/quiet-jira-writeback/slices/slice-03-spike-jira-notification-suppression.md`

---

## Context

The Azure DevOps connector suppresses notifications unconditionally
(`AzureDevOpsWorkTrackingConnector.cs:356`, `suppressNotifications: true`). The Jira connector issues a
bare `PUT rest/api/latest/issue/{id}` (`JiraWorkTrackingConnector.cs:325`) with no suppression parameter,
so every write-back emails every watcher of the target Jira issue. DISCUSS framed this as a connector
parity gap and pre-committed to D3 — suppression always on, no toggle, mirroring ADO.

**SPIKE-03 measured the failure mode and D3 as written turned out to be a regression.** Against
`letpeoplework.atlassian.net`, with a credential lacking `ADMINISTER_PROJECTS` on the target project:

```
PUT /rest/api/2/issue/SPIKEPRM-1?notifyUsers=false
403 {"errorMessages":["To discard the user notification either admin or project admin permissions are required."],"errors":{}}
PUT /rest/api/2/issue/SPIKEPRM-2                      -> 204   (same credential, no param — control)
```

`SPIKEPRM-1.duedate` was still `null` afterwards. **The 403 rejects the entire request, not just the
suppression.** Atlassian's Cloud documentation — which says the parameter is silently ignored when the
caller lacks permission — is wrong; the community report is right.

The consequence is asymmetric and decides the design. Shipping unconditional `notifyUsers=false` would
convert *working-but-noisy* write-back into *no write-back at all* for every customer whose credential
lacks Administer Jira or Administer Projects on a project it writes into. The permission is granted per
project, so this can be true of one project on a connection and false of the next.

Data Center was not probed — no instance is obtainable before release — and the scope decision is to
design for Cloud and assume DC behaves identically (`feature-delta.md`, SPIKE-03 OUTCOME). Whichever of
the three possible DC behaviours is real (403 like Cloud / silent ignore / correct suppression), the
design below leaves no customer worse off than today. That property is what makes the assumption safe,
and it is the property to defend in review.

## Decision

**Attempt suppression; on HTTP 403, retry the identical payload once without the suppression parameter,
and report the outcome of the retry.**

1. `JiraWorkTrackingConnector` sends `?notifyUsers=false` on every write-back PUT.
2. **403 and only 403 triggers the fallback.** The identical payload is re-sent exactly once, without the
   query parameter. Success or failure of that second call is what the item result reports. Every other
   non-success status keeps today's semantics: one call, one failure, no retry.
3. **No error-body matching — the retry's *outcome* is the discriminator.** A Jira PUT can 403 for
   reasons that have nothing to do with suppression: the credential lacks Edit Issues on that project, or
   cannot see the work item at all. The design must not confuse those with a suppression refusal, because
   the remedies are different and the wrong one wastes an administrator's time on a permission that was
   never the problem. The signal that separates them is already in hand and costs nothing extra:

   | First PUT | Retry (no parameter) | Diagnosis | `NotificationSuppression` |
   |---|---|---|---|
   | 403 | succeeds | The credential can write but not suppress — the 403 *was* about suppression | `NotSuppressed` |
   | 403 | also fails | The credential could not have written either way — the 403 was **not** about suppression | `Unknown` |
   | success | — | Suppressed on the first attempt | `Suppressed` |
   | non-403 failure | not attempted | A write failure unrelated to notifications | `NotApplicable` |

   **A 403 that survives the retry is never reported as a suppression problem.** It does not set
   `NotSuppressed`, it does not feed the Warning in §6, and it does not enter slice 05's rollup — it is a
   plain write failure with an honest "we could not tell" on the suppression axis. This is what keeps
   diagnosis correct, which is the entire job of slice 05.

   The rule is expressed as retry outcome rather than as a parsed message deliberately. Matching
   `"To discard the user notification either admin or project admin permissions are required."` would be a
   second substrate assumption — localisable, DC-version-dependent, on a deployment never probed — layered
   on top of the one already being defended against. Behaviour is cheaper evidence than prose.
4. **The retry is idempotent by construction.** The 403 path was verified not to write, and the retry
   carries a byte-identical `fields` object, so a future Jira that *does* write before refusing the
   suppression produces one redundant identical write rather than a corrupted one.
5. **The outcome is a first-class fact on the result contract, not a log line.**
   `WriteBackItemResult` gains `NotificationSuppression` with values `Suppressed` / `NotSuppressed` /
   `Unknown` / `NotApplicable`, assigned exactly per the table in §3. Azure DevOps always reports
   `Suppressed`. `Unknown` and `NotApplicable` are distinct on purpose: `NotApplicable` means the question
   never arose, `Unknown` means it arose and we could not answer it — and only the second is worth telling
   a human about, as an absence of information rather than as a finding.
6. **The operator-facing warning is emitted above the connector, once per connection per flush.**
   `WriteBackService` already owns the per-connection, per-cycle boundary and already logs the summary
   there. It aggregates the items whose `NotificationSuppression` is **`NotSuppressed`** — and only those,
   never `Unknown` — derives their Jira project keys, and emits **one** `LogWarning` naming the connection,
   the affected projects and the remedy. The connector stays free of cross-item state and of any knowledge
   about connections or remedies.
   - **Warning, deliberately louder than its neighbours.** Write-back failures log at `LogDebug`
     (`JiraWorkTrackingConnector.cs:292`, `:339`) and are invisible at production log levels. Do not
     "align" this back down in review: until the slice-05 surface ships, this log is the only signal the
     administrator gets.

## Alternatives Considered

**Pre-check the permission and choose the parameter accordingly (the original D5 reading).** Call
`GET /rest/api/{2,3}/mypermissions?projectKey=…` before writing and send `notifyUsers=false` only where it
answers yes. Verified accurate by SPIKE-03 Q6, so it would work. **Rejected as a gate in the write path**
on three counts. It adds one probe request per project to every write-back cycle for information that the
write itself returns for free. It introduces a time-of-check/time-of-use window — the scheme can change
between probe and write — so the fallback would still be required, and then it is the fallback that is
load-bearing and the probe that is decoration. And it makes a permission read a precondition for a
*write*, so an unreachable or slow `mypermissions` endpoint degrades write-back rather than degrading
only the reporting. The probe survives in slice 05, where it answers a question a write cannot: *before*
the first cycle, will this be quiet? (See [ADR-145](./adr-145-writeback-notification-suppression-visibility.md).)

**Make suppression a per-connection setting (reject D3).** An administrator whose credential cannot
suppress switches it off and keeps a working write-back. **Rejected**: it asks the user to know and
maintain a fact Lighthouse can discover per request, it is per connection while the permission is per
project, so it is unable to express the real state, and it adds a settings field, a DTO change, an EF
migration and a UI control to deliver strictly less than the retry.

**Send `notifyUsers=false` only where a prior cycle observed success.** A learned, per-project mode.
**Rejected**: it needs persisted state that is only ever an echo of what the next request will tell us,
it is wrong for exactly one cycle after any permission change in either direction, and its failure mode
(stuck believing suppression works) is the dangerous direction.

**Match the error body and retry only on the suppression message.** Saves one wasted request on a
genuine permission failure. **Rejected**: it trades a measured, cheap cost for a dependency on a vendor
string across locales, API versions and two deployment types, one of which has never been probed.

## Consequences

**Positive**

- The write can never regress. Every customer either gains quiet write-back or keeps exactly today's
  behaviour; no configuration reaches a worse state than it is in now.
- The Data Center assumption becomes safe without a Data Center instance: all three possible DC
  behaviours land on a defined, non-regressing outcome.
- Suppression capability becomes observed ground truth carried on the result contract, so slice 05 and
  the operator log read the same fact rather than two independently-derived guesses.
- D3 (always-on, no toggle, no settings, no migration) survives intact — *because* of the fallback, not
  in spite of it.

**Negative / accepted**

- An under-permissioned connection pays two requests per written Jira issue instead of one, every cycle,
  indefinitely. It is not cached and not learned, by the decision above. Batching (ADR-143) reduces the
  base call count enough that the doubled failure path stays below today's unbatched count.
- `WriteBackItemResult` is a shared contract; per the project rule its usages are grepped and the test
  builders extended before it is widened.
- The warning fires every cycle while the condition holds. That is intended — it is a standing
  configuration fault, and granting the permission is what clears it — but it is repetition, and the
  ADR-127 objection about advisories the user cannot act on applies here too. Mitigated by being one
  line per connection per cycle at Warning, not per issue.

## Earned Trust — the substrate lies, and the probe exercises the lie

The dependency is a vendor REST API whose own documentation misdescribes the failure mode this design
turns on. Catalogued lies and the tests that must exercise them:

| Substrate lie | Probe |
|---|---|
| Docs: "the parameter is silently ignored" | Gold test: 403 on the suppressed PUT → exactly one retry without the parameter → item reports `Success` + `NotSuppressed` |
| A 403 might mean "cannot edit at all", not "cannot suppress" | Gold test: 403 on **both** attempts → item reports failure with `NotificationSuppression = Unknown`, exactly two requests issued, **no** Warning emitted and nothing added to the per-project rollup. *A 403 that persists across the retry is never reported as a suppression problem.* |
| Retry could fire on the wrong status | Gold test: 400 on the first PUT → **no** retry, one request, failure |
| Suppression might not actually suppress | Manual verification against a real Jira instance with a real watcher — a mocked `HttpClient` proves the URL and can never prove an inbox |
| DC may differ | Post-release DC checklist Q1/Q2/Q10, recorded in the SPIKE brief. No design change is owed under any outcome. |

## Cross-reference

- [ADR-143](./adr-143-batched-writeback-with-unbatched-retry.md) — the same "optimistic call, degrade on
  refusal" shape applied to batching. The two fallbacks compose: a batched, suppressed PUT that 403s
  retries unsuppressed *as a batch* first.
- [ADR-145](./adr-145-writeback-notification-suppression-visibility.md) — where the observed
  `NotSuppressed` fact and the pre-flight probe meet the administrator.
- SPIKE evidence, verbatim status codes and bodies:
  `docs/feature/quiet-jira-writeback/slices/slice-03-spike-jira-notification-suppression.md`.
- Parity target: `AzureDevOpsWorkTrackingConnector.cs:356`.
