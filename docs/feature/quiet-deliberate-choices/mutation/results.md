# Mutation testing — quiet-deliberate-choices

Run 2026-08-03 against `main` @ `1d3daf618`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 4.16.0) | **92.39 %** | 276 | 250 | 18 | 5 | 3 | 7 m 01 s |
| Frontend (StrykerJS 9.6.1) | **95.00 %** | 20 | 19 | 1 | 0 | 0 | 38 s |

Configs: `stryker.quiet-deliberate-choices.backend.json`,
`stryker.quiet-deliberate-choices.frontend.json`, `vitest.stryker.mutation.ts`.

Both stacks changed, so both were run. Note on interpretation: the backend half of this feature is a
pure **deletion**, so there is no new production logic for Stryker to mutate. The backend run's value
is therefore not the headline number but the confirmation that (a) nothing survived at the code the
deletion touched, and (b) the four warnings the feature deliberately kept are still pinned by tests.

---

## Backend

`ServiceNowWorkTrackingConnector.cs` — the only changed backend file. Mutated whole-file (Stryker.NET
ignores line ranges), scoped by `test-case-filter` to the ServiceNow suites.

The `**/`-prefixed whole-file glob **did** scope correctly here: 13 513 of 13 786 mutants skipped,
273 tested. This contradicts an earlier note claiming `mutate` globs widen to the whole backend — the
config shape in this directory works, and is the one to copy.

### Changed region: clean

No survivor sits in anything this feature touched. The `MappedRecord` construction (L282-286) and its
declaration (L1139, which lost the `Label` positional parameter) are fully killed, as is the call site
the deleted `ReportStatesTheTeamNeverMapped` used to occupy.

### Closed by this pass

- **L518-520, the `unmeasured.Count < 1` guard in `ReportKindsOfWorkNothingMeasuresStateOn`.** A
  statement-removal mutant on the `return;` survived: with the guard gone, a correctly-configured
  instance is warned about an *empty* list of record classes. That is the same defect this feature
  removes for unmapped states — a warning firing when there is nothing to report — so it was killed
  rather than accepted. New test: `ServiceNowTransitionHistoryTest.AnInstanceMeasuringStateOnEveryKindOfWork_IsNotWarnedAbout`.

  Two false starts are recorded because both would have shipped a test that proved nothing. The first
  matched on `"no transition history"`, which also matches the *team-level* `ReportHistoryUnavailable`
  message, so it failed against a warning that legitimately fires. The second used a fixture whose
  spans all get discarded, which takes the `NoStateMetric` branch at L347 and never reaches the guard
  at all — it passed vacuously. The final version uses `ThreeRecords()` + `ATeam()` (one record class,
  spans surviving) and matches the per-class message only. Verified by hand-applying the mutation:
  the test fails with the guard removed and passes with it restored.

### Accepted survivors

| Lines | Kind | Why |
| --- | --- | --- |
| L257, L526, L1061 | Log-message string mutations | The message text is not behaviour. Asserting exact wording would pin copy that is meant to be edited. |
| L597, L599, L606, L608 | Paging loop counters and page-URI handling (2 timeouts, 1 survivor) | Pre-existing offset-paging guards. Timeouts here mean the mutant produced an infinite page loop, which is the guard doing its job. |
| L1028-L1075 | `Link` header parsing arithmetic and bounds | Pre-existing RFC 5988 header parser. Untouched by this feature. |
| L178, L490, L657, L986, L1010 | Lane/queue-date/identity/LINQ mutations | Pre-existing, in code paths this feature does not touch. |
| L804, L911, L929 | No coverage — block removal | Pre-existing branches with no ServiceNow-suite coverage. |

All 26 are pre-existing debt in a 1263-line connector that this feature only deleted from. Writing
tests for the paging and `Link`-header parser is a connector-wide job, not a rider on a log removal.

---

## Frontend

`OnboardingStepper.tsx`, scoped by line range to the changed lines.

### Closed by this pass — a real defect in the tests

The first run scored **72 %** with two `NoCoverage` mutants in `readDismissed`'s `catch` block. That
was correct and it caught a genuine flaw: the error-path test was passing **vacuously**.
`vi.spyOn(Storage.prototype, "getItem")` does not intercept jsdom's `localStorage` instance, so
`getItem` returned `null` — and "not dismissed" renders the panel, which is the same outcome the
`catch` produces. The test could never have failed.

Fixed two ways, so the paths are now distinguishable:

- spy on `window.localStorage` (the instance) rather than `Storage.prototype`;
- seed the key to `"true"` first, so a working `catch` renders the panel while a broken one hides it.

Same treatment for the write path: `setItem` throws, and the test now also asserts the key was *not*
written. Both spies are restored inside the test — the instance spy leaks past `vi.restoreAllMocks()`
in `beforeEach` and poisons the next test's assertions otherwise.

Result: `NoCoverage` 2 → 0, and the `return false` → `return true` mutant is now killed.

### Accepted survivor

- **L25, `catch { return false; }` → `catch {}`.** Equivalent mutant: the function then returns
  `undefined`, and `activeStep === 3 || isDismissed` treats `undefined` and `false` identically. No
  test can distinguish them because no behaviour differs.

### Not mutated

Lines 117-128 — the header `Box` layout and the `Typography` `sx` — were dropped from the `mutate`
range after the first run reported five survivors there (`sx={{}}`, `display: ""`,
`alignItems: ""`, `justifyContent: ""`, `fontWeight` removed). Killing those means asserting MUI's
computed styling in a unit test, which pins the framework rather than this component's behaviour. The
`IconButton` itself (L129-136) **is** in scope, because its `onClick`, `aria-label` and `data-testid`
are behaviour. Recorded here rather than left silent: excluding them is what moved the score from
76 % to 95 %, and a reader deserves to know which.
