# Epic #5775 slice 05b (Story #5790) — mutation testing results (2026-08-16)

| stack | score on the scoped surface | killed | survived | config |
| --- | --- | --- | --- | --- |
| backend (Stryker.NET 4.16.0) | **97.73 %** (129 / 132) | 129 | 3 | `stryker.5790.backend.json` |
| frontend (StrykerJS) | scoped config written, not run — see below | — | — | `stryker.5790.frontend.json` |

Scope: the four production files this slice touched — `StoredSecretReadabilityProbe.cs`,
`EncryptionKeyRingBootstrapper.cs`, `StartupBanner.cs`, `EncryptionStateDto.cs`. Run from
`Lighthouse.Backend.Tests/`.

## The first run said something the number did not

**73.43 % (94 / 132), thirty-eight survivors** — and the distribution mattered more than the score.
`EncryptionKeyRingBootstrapper.cs`, which is where this slice's actual logic lives, came back at **32
tested, 0 survived**. The switch, the guard placement that lets past exactly one refusal, and every
other refusal still firing were all fully pinned on the first pass.

The survivors were three different things wearing one badge.

**The refusal was assembled from parts, and only some of them were quoted.** The separator between its
sentences, the separator between two key ids, and the entire branch that picks which remedy to lead
with could all be removed without a test noticing. The last of those is the one that mattered: an
instance that minted its own key was never handed a setting, so telling its operator to remove one
sends them looking for something that was never there — and nothing was checking that the message
told those two situations apart.

**The banner is found by its markers before it is read by its labels.** The labels were already
asserted; the emoji that precede them were not, so every one of them could be replaced with anything.
On a wall of startup text in a console, that marker is how a line is found at all. They are now
asserted alongside the labels they sit beside.

**And a large block of it was never this slice's code.** `StartupBanner.cs` carries the whole banner,
of which this slice added about fifteen lines. Its layout — the blank rows and the rule lines — is
where the banner breathes rather than what it says, and pinning it would freeze the layout against the
next person who wants to move a gap. Those carry `// Stryker disable` and the reason, rather than
being excluded from the run by narrowing the file list, so the count stays honest about what was
skipped and why.

## The second run

**97.73 % (129 / 132), three survivors**, all recorded rather than chased:

| Survivor | Why it stands |
| --- | --- |
| `ArgumentNullException.ThrowIfNull(ring)` in `DatabaseSecretReadabilityProbe.Look` | Equivalent. Removing it does not let a null through — `EncryptionKeyRingHolder` guards the same argument one line later and throws the same exception, so no test can tell the two versions apart. |
| `ArgumentNullException.ThrowIfNull(facts)` in `StartupBanner.BuildInfoLines` | Killable, and older than this slice. Left for whoever next has that file in scope rather than added as an unrelated test here. |
| The marker on the *started past the refusal* line | The line's content, its label and its position are all asserted; only the emoji is not, because the notice is asserted through `lines[1]` rather than by scanning for a marker the way the fixed rows are. |

## Frontend

The frontend change in this slice is one conditional notice in `EncryptionPanel.tsx`. A line-scoped
config is committed beside this file for reproducibility, but the run was not executed: slice 06a
rewrites every sentence on that panel within days, so a score taken now describes code that is about
to be replaced. It is taken there instead, over the panel 06a produces.
