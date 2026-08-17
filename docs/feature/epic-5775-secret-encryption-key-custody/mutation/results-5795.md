# Mutation testing — 5795 (The key that won at startup is the key that stays)

Run 2026-08-17 against `main` @ `938c7dc90` plus the slice 08 working tree. Gate is 80 % kill rate on
every stack with changed files.

| stack | score | tested | killed | survived | no coverage | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET) | **95.60 %** | 91 | 87 | 4 | 0 | 0 | 5 m 17 s |
| Frontend | **N/A** | — | — | — | — | — | — |

**Frontend is N/A, not skipped.** This slice changes four backend files and three backend test files.
Nothing under `Lighthouse.Frontend/` is touched.

Config: `stryker.5795.backend.json`.

## Backend

| file | killed | survived | no coverage |
| --- | --- | --- | --- |
| `WhereTheKeyCameFrom.cs` | 26 | 0 | 0 |
| `StartupBanner.cs` | 61 | 4 | 0 |

Three runs. The first scored **70.33 %** and failed the gate; the second **92.31 %**; this is the third.
Both jumps came from the survivor list, and the first one is the finding worth keeping.

## What the first run caught

**`WhereTheKeyCameFrom` had 20 mutants with no coverage at all — the type had no tests.**

It was extracted during this slice's refactor, out of `EncryptionController.WhatTheKeyArrivedIn`, so
that the startup line and the encryption panel could not name two different settings. The extraction
was right and the behaviour was covered — but only *indirectly*, through boot-level tests that the
mutation filter excludes. Nothing exercised the type on its own.

Every signal available said this was fine. The suite was green throughout. The dispatched code review
examined the extraction specifically and judged it faithful. Only mutation testing said that a unit had
been created and left untested, and it said so by finding that two thirds of its mutants were never
even reached.

Closed by `WhereTheKeyCameFromTests` — 19 tests over custody precedence, the blank-and-empty setting
cases, the order settings are read in, the operator-facing spelling of the names it hands back, and
`InMoreThanOnePlace`. That file now kills 26 of 26.

**The lesson is about extraction, not about this type.** Pulling logic out of a covered caller moves it
somewhere its old tests no longer reach. The caller stays green and says nothing. Worth checking on
every future extraction in this epic rather than trusting the suite.

## Closed by the second pass

Four survivors in the notice this slice adds, all of them mine:

- **`:45`, `:46`, `:47` — the three fragments of the sentence.** Each was individually deletable with
  no test noticing. The notice has to do three things — say a key was supplied more than once, say
  which place is winning, and say what to do about the rest — and losing any one of them leaves a
  statement of fact an operator cannot act on. Each is now pinned by the phrase that carries it.
- **`:182` — the `Warning` label.** Without it the notice renders as an ordinary line about a healthy
  instance rather than as something to go and tidy up. Pinned via `LabelColumn("Warning")`.

## Accepted survivors

**`StartupBanner.cs:45` — the `". "` between the list of settings and the next sentence.** The phrase
and the interpolated list are both pinned; what survives is the punctuation joining this fragment to
the next. Asserting a separator would pin the layout rather than what the line says, and this banner
deliberately keeps its layout unpinned — the surrounding code carries `Stryker disable` comments over
the blank rows and rules for the same reason.

**`StartupBanner.cs:182` and `:187` — the emoji.** Same category. `:187` is the start-anyway notice,
which this slice does not touch at all.

**`StartupBanner.cs:98` — `ArgumentNullException.ThrowIfNull(facts)` in `BuildInfoLines`.** Pre-existing
guard on a pre-existing method. `BuildEncryptionCustodyLines` has its guards pinned by
`TheCustodyLines_RefuseToBeBuiltWithoutARingOrAKeyStore`; the whole-banner entry point does not, and
adding that is `StartupBanner`'s own debt rather than this story's.

## Not mutated

- **`Program.cs`** — two changed regions, `WatchTheMountedKeysFile` and the banner facts assembled in
  `PrintSystemInfo`, in a file of some 1900 lines. Stryker.NET ignores line ranges and widens them to
  the whole file, so mutating it would put 748 ignored mutants and every unrelated branch into the
  denominator to judge a guard clause. The behaviour of both regions is covered directly:
  `EncryptionBootstrapOrderTests` drives the real bootstrap through the real configuration provider and
  asserts what the registration did, including asserting **how many watchers were driven** so that
  "nothing re-read the file" cannot be mistaken for "the file was re-read and changed nothing".
- **`EncryptionController.cs`** — the change there is a deletion: 27 lines of duplicated resolution
  replaced by a call to `WhereTheKeyCameFrom`. What was removed is now mutated in its new home at 26 of
  26, and the controller's own behaviour is pinned over HTTP in `EncryptionControllerTests`.
