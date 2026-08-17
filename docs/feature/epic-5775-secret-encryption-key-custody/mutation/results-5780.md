# Epic #5775 slice 05 (Story #5780) — mutation testing results (2026-08-17)

| stack | score on the scoped surface | killed | survived | config |
| --- | --- | --- | --- | --- |
| backend (Stryker.NET 4.14.1) | **96.55 %** (56 / 58) | 56 | 2, both deliberate | `stryker.5780.backend.json` |
| frontend (StrykerJS) | not run — this slice changes no frontend code | — | — | — |
| chart (helm) | no equivalent — the chart carries no mutable code; its stand-in is the 13-case `encryption_test.yaml` suite plus the two shell gates in `ci_chart.yml` | — | — | — |

Scope: the two production files this slice added logic to — `KeyRingFileWatcher.cs` (44 mutants
tested) and `MountedFileKeyRingSource.cs` (14). Confirmed from `reports/mutation-report.json` rather
than from the headline, per the standing rule.

## The run that would have lied, and the refactor it forced

The first run reported a score that described none of the reload logic. `ReadOnce` and `Apply` each
assigned a local inside a `try` whose `catch` returns; Stryker mutates by emptying blocks, an emptied
`try` leaves that local unassigned, the method stops compiling, and Stryker drops **every mutant in
it** — announcing it only in a `Safe Mode!` warning among thousands of lines:

```
KeyRingFileWatcher.cs (101:16) CS0165: Use of unassigned local variable 'contents'
  → Safe Mode! Stryker will remove all mutations in ReadOnce
KeyRingFileWatcher.cs (143:16) CS0165: Use of unassigned local variable 'candidate'
  → Safe Mode! Stryker will remove all mutations in Apply
```

Both `try` blocks became named methods returning what they found or nothing
(`ContentsOrNothing`, `RingOrNothing`). Behaviour is identical and no test changed. This is the same
family as the traps already in the ledger: the number looks fine and covers the wrong code.

## What the survivors were worth

First honest run: **81.03 %**, 47 / 56. Of the nine survivors, two were real:

- **Lines 108 and 113 had no coverage at all** — the `IOException` and `UnauthorizedAccessException`
  arms. That is the *file is there and will not be read* path: a tightened file mode, or a substrate
  that went away underneath the pod. It is exactly the failure the `0444` mount mode exists to avoid,
  and nothing exercised it. Three tests now do, including the recovery when the file becomes readable
  again.
- **`Prepend` → `Append` on line 200 survived.** `IdsOn` puts the active key first, and an operator
  matching `encryption.keyring.reloaded` against their own store reads it that way round. Nothing
  pinned the order; the set-difference that finds dropped keys is order-insensitive, so nothing broke
  when it flipped. Now pinned, on both the informational and the warning line.

The remaining seven were four `ArgumentNullException.ThrowIfNull` guards and three `", "` separators.
Killed the guards — the epic's own slice 06a set that precedent — and pinned two of the three
separators as a side effect of pinning the order.

## The two left standing, and why

- **`KeyRingFileWatcher.cs:177`** — the separator on the *remaining* key list inside the Warning
  branch. The warning test pins which keys went away, which is what an operator acts on; what the
  instance still holds is pinned on the informational line instead. Cosmetic, and killing it would
  add an assertion that duplicates one already made.
- **`MountedFileKeyRingSource.cs:18`** — `ArgumentNullException.ThrowIfNull(fileSystem)`, shipped in
  slice 01. This slice touched that file only to separate reading from parsing so the reload could
  ask them separately.

Neither is behavioural. The gate is 80 %; the scoped surface is at 96.55 %.

## Substrates

Run on the rebased tree, after `dotnet test` came back 5575 / 0 against the bumped 10.0.11 packages.
Stryker was run alone — a backend run overlapping anything heavy reports timeouts that are not
results — and from `Lighthouse.Backend.Tests/`, because `test-projects` resolves relative to the
working directory rather than to the config file.
