# Epic #5775 slice 06b (Story #5793) — mutation testing results (2026-08-16)

| stack | score on the scoped surface | config |
| --- | --- | --- |
| backend (Stryker.NET 4.16.0) | **93.33 %** — 14 killed, 0 survived | `stryker.5793.backend.json` |
| frontend (StrykerJS) | not run — the change is one conditional row, covered from both sides | — |

Scope: `SystemInfo.cs`, `SystemInfoController.cs`, `StoredSecretSummaryReader.cs`.

## Three of the four runs were wrong, and each was wrong differently

Worth writing down, because every one of them produced a number that could have been reported.

**Run 1 — 0.00 %, ten survived, none killed.** Not a result. The tree was edited while the run was in
flight: a logger was added to the controller and to its test after Stryker had already copied the
solution, so it scored a source and test pair that never existed together. Zero killed against
sixty-seven green tests fails the standing rule about disbelieving a score you have not sanity-checked
— nothing that thoroughly covered scores zero.

**Run 2 — 68.75 %, four survived.** A real result, and below the gate. Two survivors were the argument
guards on the merged reader; two were the warning added during review. That second pair mattered: the
whole argument for adding the log was that a safe answer must not also be a silent one, and nothing was
checking it was said. A test now injects a failure into the administrator check and asserts three
things at once — the fields are withheld, the request still succeeds, and a warning is written.

**Run 3 — 80.00 %, two survived.** Meets the gate and is still not to be believed. The two survivors
were the guards the new test had just been written for, and they survived because the run never
executed that test: the `test-case-filter` listed five name fragments and `StoredSecretSummaryReaderTests`
matched none of them. This is the trap the ledger already records — the config excludes quietly and the
number still looks respectable. A passing score over a filter you have not checked is worth nothing.

**Run 4 — 93.33 %, fourteen killed, none survived**, with the filter corrected to name the new fixture.
All three files at zero. The number is only worth this much because the run that produced it was
checked: scope confirmed per file from `reports/mutation-report.json`, not read off the summary line.

## What is deliberately not asserted

The text of the warning carries a `// Stryker disable once String`. That it is said at warning level is
behaviour and is asserted; the sentence is for whoever reads the log, and freezing it in a test would
make rewording it a test change for no gain.
