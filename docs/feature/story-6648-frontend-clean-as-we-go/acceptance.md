# Acceptance brief: story\-6648\-frontend\-clean\-as\-we\-go

## Slices

### S1

##### Covers

P1

```text
A maintainer runs the measure script for the removal slice on a quiet machine: it alternates five before and after pairs of the page test file and runs the new direct tests.
```

```text
Exit 0 and a gate line with no failed assertion, a file-alone per-test seconds ratio of at most 0.60 and a combined ratio of at most 0.80 over at least three valid pairs; on the unchanged page file the ratio is 1.0 and the gate reports one failed assertion; on a loaded machine it reports unmeasured and never a pass.
```

```text
The removal tests of the Reports page take less time per test than before, so the suite gives its answer sooner.
```

##### Disposition

Tested

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/measure\-slice\.sh

```text
The page file was merely renamed or split so one file looks faster while the summed seconds stay the same; the combined ratio over the page file plus the direct tests refuses that.
```

###### Obligations


##### Covers

P2

P3

```text
The maintainer runs the break script: each listed production line is broken in turn, the named tests run, and the line is put back. The list holds one break per moved rule, seventeen lines in all.
```

```text
Every listed break turns its named test red, including the two that name a kept page test (Cancel at the confirmation; Done clearing the picks, caught by the not-removed report that stays listed and must read zero selected), every production file is restored, and the gate line shows no surviving break once the page file is rewritten.
```

```text
Every moved rule is shown to fail when its production line is broken, and no production behaviour is left changed.
```

##### Disposition

Tested

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/deliberate\-breaks\.sh

```text
A moved test that asserts a value computed by the code under test, or a kept page test rewritten without its Cancel click or its zero-selected assertion after Done; the break lines turn those red.
```

###### Obligations


##### Covers

P2

P3

```text
The maintainer runs the check script after the page file is rewritten.
```

```text
The page file holds exactly the six kept tests, every removed test maps to an existing passing test, nothing is skipped or loosened, and no production file, shared kit or time-budget file differs from the before commit.
```

```text
Nothing a user sees changes, and the loading and failure looks the page still pins stay pinned.
```

##### Disposition

Tested

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/check\-slice\.sh

```text
A page file that silently drops a wiring test and keeps the count at six; the kept ids are listed by name.
```

###### Obligations


##### Covers

P2

```text
A reader presses Remove on picked reports and works through the confirmation dialog alone: the list, the warnings for one, two and none, the headline, already-gone reports, no answer, progress that cannot be closed, the result, Escape and Done.
```

```text
Each rule the page tests proved is now proven by a test of the dialog rendered alone, and the check script finds a row and a passing replacement for every removed page test.
```

```text
Removing picked reports warns, counts and closes exactly as before.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Reports\/RemoveSelectedDialog\.test\.tsx

```text
A dialog that counts every outcome as removed or lets Escape close it while the removal is under way; the deliberate-break lines turn those tests red.
```

###### Obligations







##### Covers

P2

```text
The removal hook receives answers for picked reports, in an order other than the picks, including one not removed, one already gone and one lost answer.
```

```text
Only the removed and already-gone reports are forgotten, matched by id; every report list is read again also after a failure.
```

```text
A removed report disappears from every list and a report that stayed is still shown.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Reports\/useBulkReportRemoval\.test\.tsx

```text
A hook that forgets every picked report or the report at the same position; the answer order differs from the pick order so those are refused.
```

###### Obligations




##### Covers

P2

```text
The toolbar Remove button is shown for picks on owners she only reads (one of three, then two of three), for failed and for loading permissions, then pressed when enabled and when disabled.
```

```text
It is disabled with the exact reason where the count of owners she only reads differs from the count she can edit, disabled for failed permissions and while loading; an enabled press opens the confirmation and a disabled press opens none.
```

```text
Remove is offered only where she may remove, and says why when not.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Reports\/useSelectionActions\.test\.tsx

```text
A button that counts owners she can edit as not Admin or enables itself while a reason holds it back; both reason tests now turn red under the inverted count.
```

###### Obligations



## Expectations

### Rows

#### E1

##### Promises

P1

```text
Each of the three test files gets faster per test: the median per-test time over at least 4 main runs, measured the same way as the baseline, is lower than before for each file.
```

#### E2

##### Promises

P2

```text
Every test removed from or moved out of the three test files has a named replacement test that now proves the same rule.
```

#### E3

##### Promises

P2

```text
Every moved rule is shown to fail when its production line is deliberately broken, so the moved test still has teeth.
```

#### E4

##### Promises

P3

```text
Users see and experience Lighthouse exactly as before: no production behaviour change and no pixel change in the rendered views.
```

#### E5

##### Promises

P3

```text
The views that remain keep their loading look and their failure look, so a slow or failed load still shows the same loading and could-not-load states as before.
```

## Charter

None.

## Examination

None.
