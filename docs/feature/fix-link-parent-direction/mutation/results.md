# Mutation testing — 6203 (Parent from Jira issue links drops every item that is both a child and a parent)

Run 2026-10-09 against the worktree at `dde418fc4` (fix `92e28a80b`, refactor `8e7841504`, review tests
`dde418fc4`). Gate is 80 % kill rate; only the backend changed, so the frontend is **N/A — no frontend
file was touched**.

| scope | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| `IssueExtensions.cs`, whole file (Stryker.NET 5.0.0) | 73.44 % | 64 | 47 | 6 | 11 | 3 m 39 s |
| `IssueExtensions.cs`, lines this fix changed | **100 %** | 4 | 4 | 0 | 0 | — |
| `JiraWorkTrackingConnector.cs`, lines this fix changed (hand probes) | **100 %** | 8 | 8 | 0 | 0 | ~2 m |

Config: `stryker.6203.backend.json` (unit tests of the Jira connector folder; live `JiraIntegration`
classes, dogfood tests and the WebApplicationFactory acceptance suite excluded).

## Backend

### `IssueExtensions.cs` — changed lines all killed

Stryker generated four mutants on the lines this fix changed, all on the both-ways arm
`_ => outwardEnd.Length > 0 ? outwardEnd : inwardEnd` (conditional true/false, two equality mutations);
all four were killed. Stryker.NET generates no mutants for the `Inward`/`Outward` switch arms, so those
were probed by hand (P7, P8 below).

The 73.44 % whole-file figure is set entirely by pre-existing code this fix did not touch:

- **6 survivors** — `LabelOf` / `KeyOf` JSON shape guards (lines 183, 185, 188, 190, 198, 203):
  `||` → `&&` on `!TryGetProperty(...) || ValueKind != Object/String`, and the `string.Empty` return
  values. Killing them needs a link whose `type` or issue end is present but not an object; no test sends
  one. Pre-existing, outside this fix — worth a small follow-up test, not a reason to widen a bug fix.
- **11 no-coverage** — `GetFieldValue`-style option parsing (lines 40, 45, 193, 208–219), reached only by
  tests outside this run's filter. Pre-existing, untouched.

### `JiraWorkTrackingConnector.cs` — not mutated by Stryker, probed by hand

About 30 changed lines in a ~3300-line file, and every one of them is reached only through the
WebApplicationFactory acceptance suite, which Stryker cannot drive in reasonable time. Each probe below
applied one compiling mutation, ran `ParentFromIssueLinks` + `IssueExtensionsParentLinkTest` (76 tests),
and restored the file.

| probe | mutation | result |
| --- | --- | --- |
| P1 | call site passes `LinkDirection.Both` instead of `theOverride.Direction` | killed (2 failed) |
| P2 | `ALinkType` built with `Both` whatever was typed | killed (2 failed) |
| P3 | the type's name no longer wins over a label | killed (1 failed) |
| P4 | `readsInward == readsOutward` → `!readsInward && !readsOutward` | killed (1 failed) |
| P5 | outward label reads as `Inward` | killed (1 failed) |
| P6 | inward and outward directions swapped | killed (2 failed) |
| P7 | `Inward` arm reads the outward end (`IssueExtensions.cs`) | killed (2 failed) |
| P8 | `Outward` arm reads the inward end (`IssueExtensions.cs`) | killed (2 failed) |

P2 first ran as "drop the `DirectionReadFrom` call", which does not compile (S1144, unused private
method), and then as a constant conditional, which S3923 refuses; the recorded run multiplies the
computed direction by zero so the code builds and the direction is still lost.

### Closed by this pass

None needed on changed lines. The review pass before this one added the symmetric-label and
outward-label acceptance scenarios that kill P4 and P5.

### Accepted survivors

The six whole-file survivors above, all pre-existing JSON shape guards outside this fix.

### Not mutated

`JiraWorkTrackingConnector.cs` and `LinkDirection.cs` (an enum with no behaviour) — see the hand probes.
