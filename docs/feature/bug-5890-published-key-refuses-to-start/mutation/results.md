# Mutation testing — 5890 (published key left in the settings file refuses to start)

Run 2026-09-01 against `main` @ `f444935b8`. Gate is 80 % kill rate on each stack touched.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **93.55 %** | 124 | 116 | 8 | 0 | 22 m 29 s |
| Frontend (StrykerJS) | **N/A** | — | — | — | — | — |

Frontend is N/A because this change touches no frontend file. The fix is entirely in backend key
resolution and the startup banner; nothing under `Lighthouse.Frontend/src` was modified.

Config: `stryker.5890.backend.json`. Scope sanity-check: 17786 mutants created across the backend,
17662 skipped, **124 tested** — consistent with two small files.

## Backend

| file | tested | killed | survived |
| --- | --- | --- | --- |
| `Services/Implementation/Encryption/ConfiguredKeyRingSource.cs` | 41 | 41 | **0** |
| `Startup/StartupBanner.cs` | 83 | 75 | 8 |

The file carrying the decision this bug fix turns on — whether the published key under the retired
name is read as no key — is fully killed. Every boundary in
`ThePublishedKeyUnderTheRetiredName` (the `RetiredKeys.Count == 0` guard, the `Matches` call, the
`TryParse` result) and every clause of `AnsweredByTheRetiredName` is pinned by a test.

### Not mutated

`Program.cs` was excluded from `mutate`. This change adds one argument to one call there
(`ThePublishedKeyUnderTheRetiredName(suppliedUnderTheRetiredName)` passed into `StartupBannerFacts`);
the file is 1584 lines, and mutating it would bury 1 changed line under the whole application
bootstrap. The argument it passes is covered directly by `ConfiguredKeyRingSourceTests`, and the
banner behaviour it drives by `StartupBannerEncryptionKeyLineTest`.

### Accepted survivors

All eight are in `StartupBanner.cs`, and none is a gap in the logic this fix changed.

| line | mutation | why it is accepted |
| --- | --- | --- |
| 206 (x2) | `"⚠️"` -> `""`, `"Warning"` -> `""` | The emoji and the label column of the new notice line. Decoration, not content. The banner tests assert what a line *says*, deliberately — pinning the emoji would freeze the layout against the next person who wants to move it, which is the same reason the file already carries `Stryker disable` on its spacing. The three sibling warning lines (211, 216 and the custody line) survive identically and always have. |
| 211, 216 | notice string -> `""` | Pre-existing lines, untouched by this change: the supplied-in-more-than-one-place notice and the unreadable-secrets notice. |
| 61, 78, 79 | notice string -> `""` | Pre-existing prose inside `AKeySuppliedInMoreThanOnePlace` and the no-durable-store guidance. Untouched by this change. |
| 116 | `ArgumentNullException.ThrowIfNull(facts);` -> `;` | Defensive guard on a method whose only caller passes a constructed record. Unreachable through the public path; a test for it would assert the language, not the product. Pre-existing. |

Killing the 206 pair would raise the score to roughly 95 % and would pin the decoration of one
warning line while its three siblings stay unpinned. The gate is met at 93.55 % with the decision
logic fully covered, so the inconsistency is not worth buying.
