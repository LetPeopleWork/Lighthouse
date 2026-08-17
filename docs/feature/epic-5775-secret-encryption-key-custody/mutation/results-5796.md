# Mutation testing — 5796 (A pass that survives the ring changing under it)

Run 2026-08-17 against `main` @ `dedffbf04` plus the slice 09 working tree. Gate is 80 % kill rate on
every stack with changed files.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **96.34 %** | 83 | 79 | 3 | 1 | 0 | ~18 m |
| Frontend (StrykerJS) | **92.31 %** | 13 | 12 | 1 | 0 | 0 | 23 s |

Configs: `stryker.5796.backend.json`, `stryker.5796.frontend.json`.

## Backend

| file | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- |
| `SecretCustodyService.cs` | 53 | 1 | 1 | 98.15 % |
| `CryptoService.cs` | 15 | 1 | 0 | 93.75 % |
| `SecretReadabilityReport.cs` | 11 | 1 | 0 | 91.67 % |

Three runs: **93.90 %**, then **95.12 %**, then this one. Both jumps came from the survivor list, and
both are the finding worth keeping.

## What the runs caught

**Two survivors in the merge, and the same blind spot behind both.** `WhatEachCredentialEndedUpAs`
reconciles the two looks a disturbed pass takes: one line per credential, the later look winning. The
first run showed that removing the `!` from its filter changed nothing any test could see. The second
run, after that was closed, showed the same for `WhichCredential` — the function naming *which*
credential a record is about — whose body Stryker replaces with a stub returning nothing.

Both survived for one reason: every scenario written for this slice had the second look seeing a
superset of the first. When that holds, the "records only the first look saw" branch is never
populated and the identity function is never meaningfully asked anything — so the entire reason the
merge exists went unexercised while the suite stayed green.

Closed by two scenarios that break the superset assumption:

- **A key added behind the one in force.** What is held changes, so the pass looks again, but the key
  credentials are written under has not moved — nothing it just moved is a candidate a second time, so
  the second look sees none of them. They reach the operator only because the first look named them.
- **A mix of moved and unreadable.** The two looks overlap without matching: the moved credential is on
  the key in force and is not asked about again, the unreadable one is still where it was and is found
  twice. Each has to be named once, with its own outcome.

The second was verified against the mutant directly before being committed — `WhichCredential` stubbed
to return nothing, suite re-run, and that scenario was the only one of forty-two that failed.

**The lesson is about which branch a test reaches, not about this merge.** Both gaps were in code
written and tested the same morning, and in both cases what was tested was the path just built rather
than the branch that exists only for the case not pictured. Worth assuming, on any reconciliation of
two collections, that the asymmetric case is the one nothing covers.

## Accepted survivors

**`SecretReadabilityReport.cs:49` and `CryptoService.cs:23` — `ArgumentNullException.ThrowIfNull`.**
Pre-existing guards on pre-existing constructors, neither touched by this slice. Same category as the
guard accepted in 5795.

**`SecretCustodyService.cs:142` — `cancellationToken.ThrowIfCancellationRequested()`.** Real debt
rather than noise: a pass cancelled mid-walk is not covered, and was not before this slice either. The
line is pre-existing and only moved here by the extraction of `WalkOnceAsync`. Recorded rather than
fixed, because cancellation is outside what slice 09 set out to change; covering it belongs to the
custody service's own debt.

**`EncryptionPanel.tsx:206` — `sx={{ mt: 2 }}` emptied.** The margin above the report box, caught
because it sits inside the mutated line range rather than because this slice wrote it. Asserting a
margin pins the layout rather than what the panel says, which is the same reason 5795 accepted the
banner's punctuation and emoji.

## Not covered

**`SecretCustodyService.cs:215` — the message on the `NotSupportedException` for a fourth secret
column.** Unreachable by construction: the enum has three members and the throw exists so that adding a
fourth arrives loudly rather than being written into the refresh token. A test would have to invent an
enum value that does not exist.

## Not mutated

- **`SecretReadabilityReportDto.cs`** — a carrier. Every member is an assignment from the report it
  wraps, so its mutants would be arithmetic on the denominator rather than a question about behaviour.
  What it carries is asserted over HTTP and in the panel.
- **`LighthouseAppContextFactory.cs`** — `DesignTimeCryptoService` exists for `dotnet ef` at design
  time and no test reaches it. It grew the new port member; mutating it would score a stub.
- **`Program.cs`** — untouched by this slice, and Stryker.NET widens line ranges to whole files, which
  is why the report shows 748 of its mutants ignored. The scoped `mutate` globs are what keep them out
  of the denominator.
