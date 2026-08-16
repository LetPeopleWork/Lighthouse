# Epic #5775 slice 04b (Story #5789) — mutation testing results (2026-08-16)

| stack | score on the scoped surface | killed | survived | ignored | config |
| --- | --- | --- | --- | --- | --- |
| backend (Stryker.NET 4.16.0) | **100.00 %** (53 / 53) | 53 | 0 | 3 | `stryker.5789.backend.json` |
| frontend | not run — no frontend file changed in this slice | — | — | — | — |

Scope: the three production files this slice touched —
`LegacyDefaultEncryptionKey.cs`, `EncryptionKeyRingBootstrapper.cs`, `PublishedKeySecretCount.cs`.
Run from `Lighthouse.Backend.Tests/`, which is where the relative paths in every per-feature config
here resolve from.

## The first run, and what it was really saying

**79.31 % (46 / 58), twelve survivors.** None of them was a hole in the behaviour this slice added —
the refusal and the count were both fully pinned on the first pass. What the twelve were was three
different things wearing the same badge, and separating them is the whole value of the run.

**Two were sentences nobody had asserted.** The refusal is assembled from seven string fragments and
the tests quoted four of them. The two survivors were *"ships inside every copy of the product and can
be read out of the public source"* — the entire reason the key is no good — and *"and start Lighthouse
again"*, the last instruction. A mutant could empty either one and every test still passed, which is
exactly the shape of the defect this slice exists to fix: an operator told what to do and not why, or
told why and not what to do. Both are now asserted, and the assertions carry the reason in their
message rather than restating the string.

**Seven were argument guards nobody had tested.** Six `ArgumentNullException.ThrowIfNull` calls in the
bootstrapper's constructor and one in `LegacyDefaultEncryptionKey.AppendedTo`. The bootstrapper ones
predate this slice; the run surfaced them because the file was in scope for the first time. Two tests
now cover all seven, following the precedent already in `PublishedKeySecretCountTests`. The guard on
`AppendedTo` is the one worth naming: a ring conjured there instead of refused would carry the
published key as the key secrets are written under, which is the thing this slice refuses everywhere
else.

**Three were equivalent, and are now marked as such** with `// Stryker disable once` and a reason, per
the convention already in `PortfolioMetricsController`, `Delivery` and the two updaters:

| Mutant | Why no test can kill it |
| --- | --- |
| The `"."` separator in `PublishedKeyPrefix` | Widening the prefix can only let *more* values through the narrowing, and every one of them is then handed to the same key to read and rejected there. The count does not change. |
| `&&` → `\|\|` on the access-token emptiness guard | The guard decides which rows are dragged out of the database, never the answer — an empty column is not something that key can read, so `CanRead` returns false either way. |
| `&&` → `\|\|` on the refresh-token emptiness guard | Same. |

The distinction matters more than the number: a test written to kill any of those three would have
been asserting the query plan rather than the behaviour, and would have to be deleted the next time the
narrowing is tuned.

## The second run

**100.00 % (53 / 53), zero survivors.** 15 539 mutants skipped by the `mutate` filter and the ignore
comments, 53 tested — the `13 xxx created` line in the log is the pre-filter count and says nothing
about scope, as the ledger records. Scope confirmed from `reports/mutation-report.json`: 30 tested
mutants in `EncryptionKeyRingBootstrapper.cs`, 23 in `PublishedKeySecretCount.cs`, 5 in
`LegacyDefaultEncryptionKey.cs` on the first run; all three files at zero survivors on the second.
