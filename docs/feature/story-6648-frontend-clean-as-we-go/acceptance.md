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



### S2

##### Covers

P1

```text
Run the timing measure for the combined PDF page tests and the new dialog and function tests: 5 alternating before and after pairs
```

```text
Exit 0 with no failed assertion, the page file alone at most 0.75 and with the new tests at most 0.80 of the before-commit time per test, over at least 3 quiet pairs
```

```text
The combined PDF test file gives its answer in less time per test, so the suite is faster to wait for
```

##### Disposition

Tested

bash docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/measure\-slice\.sh S2

```text
the page file keeps all 18 tests and only new files are added on top
```

###### Obligations

None.

##### Covers

P2

```text
Run the check that maps every old page test to a kept test or to the tests that now prove its rule
```

```text
The 18 old ids equal the rows, 12 stay on the page, each removed row names tests that exist and pass, and the new test files hold exactly the listed tests
```

```text
No rule of the combined PDF export, its failure dialog or Leave it out is left without a test
```

##### Disposition

Tested

bash docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/check\-slice\.sh S2

```text
a removed row that names an unrelated passing test (the break list proves each rule separately)
```

###### Obligations

None.

##### Covers

P3

```text
Run the check against the files changed since the before commit, found by its subject line on the branch history
```

```text
No production file, shared test kit, time-budget file or end-to-end file differs, except the two Reports files the feature's allow-list names (the reviewed move of the failure rules), and no skip, only, todo, retry, timeout or weaker matcher was added
```

```text
The Reports page and its dialog look and behave exactly as before; the loading and failure looks stay pinned
```

##### Disposition

Tested

bash docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/check\-slice\.sh S2

```text
a further edit inside the two allowed Reports files (the reviewer reads that move)
```

###### Obligations

None.

##### Covers

P2

```text
The failure dialog is shown alone with its failed reports and its handlers, then each button and Escape is used
```

```text
It names each failed report in print order with its owner kind word, is described by that list, shows the part-of-the-page message for an empty list, offers Cancel, Leave it out, Retry only when offered, and each button and Escape call their own handler once
```

```text
A reader who could not export is told which reports failed and gets the buttons that do what they say
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Reports\/NoPdfDialog\.test\.tsx\:1

```text
a dialog with two handlers swapped on a button no test presses; all four buttons and Escape are pressed
```

###### Obligations




##### Covers

P2

```text
The failed-report and Leave it out decisions are called directly with reports of one Team and of different owners
```

```text
failedAmong names only the failed reports in listed order and matches by id, owner kind and owner id; leaving out is not offered when nothing failed or nothing would be left, and leaves out only the failed report, not the other report of the same Team
```

```text
Leave it out never drops a report that loaded and is never offered when it would print nothing
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Reports\/printFailures\.test\.ts\:1

```text
matching by id only or by owner only (the fixture has a same-id other owner, a same-owner other id and a same-id portfolio)
```

###### Obligations





##### Covers

P2

```text
On the Reports page, four reports are picked and two cannot be read; the reader presses Leave it out, and in another test Cancel
```

```text
Leave it out unpicks both failed reports, closes the dialog and prints the rest in table order with Export PDF (2); Cancel closes the dialog, prints nothing and keeps the picks
```

```text
Leave it out and Cancel on the real page do what the dialog says and the picks follow
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Reports\/ReportsPage\.combinedPdf\.test\.tsx\:1

```text
an unpick of only the first failed report (two failures are picked); a no-op Cancel (the dialog must close)
```

###### Obligations



##### Covers

P2

```text
On the Reports page every picked report cannot be read
```

```text
The dialog named No PDF was made shows exactly Cancel and Retry, no Leave it out, and nothing is printed
```

```text
A reader whose every report failed is not offered a Leave it out that would print nothing
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Reports\/ReportsPage\.combinedPdf\.test\.tsx\:1

```text
Leave it out wired to always show (a break removes the undefined case) and only the first report printed (a break prints one report)
```

###### Obligations


### S3

##### Covers

P2

```text
A reader asked for the condition of a Yes, if leaves it blank, pastes it padded or cancels
```

```text
Vote stays disabled for a blank condition, a padded condition is confirmed trimmed once, Cancel confirms nothing
```

```text
Nothing is cast without a usable condition
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/ConditionPrompt\.test\.tsx

```text
Vote enabled for spaces, or the untrimmed draft confirmed
```

###### Obligations



##### Covers

P2

```text
The open Work Item changes, a reader key arrives, or a read is asked for a Work Item no longer open
```

```text
Nothing is read for a Work Item no longer open, a new key alone does not read again and the next read carries it, the open read carries the key
```

```text
The log shown is always that of the Work Item now open
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/useSizingLog\.test\.tsx

```text
A late read replaces the open log, or a new key triggers a second read
```

###### Obligations



##### Covers

P2

```text
A comment is refused, sent twice while on its way, or sent by a browser without a name
```

```text
The refusal reaches onFailure, onSent never runs and the comment is no longer counted as being sent; a second send of the same Work Item's comment waits for the first answer; the name is asked and the comment sent once under it
```

```text
A refused comment keeps its text and a comment is never sent twice
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/useCommentAdding\.test\.tsx

```text
A refusal is swallowed, a second send goes out, or the comment is sent without asking for a name
```

###### Obligations


##### Covers

P2

```text
A reader opens the votes and comments of a Work Item with votes, comments, revocations and open questions, and writes a comment
```

```text
Only entries with words are listed in log order, open questions marked only where the log says so, counts from the log voters with tooltip; Send disabled for blank and while sending, trimmed text sent, draft kept until onSent
```

```text
Everybody reads the same split, names and comments; a comment is never sent twice by the box
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/VotesAndCommentsDialog\.test\.tsx

```text
Votes without words listed, marker inverted, counts from the row, Send enabled while sending
```

###### Obligations





##### Covers

P2

```text
A Work Item's log cannot be read: not found with renamed terms, or any other error
```

```text
Not found names the renamed terms; any other error returns its message; one page row keeps the 404 wording
```

```text
The reader sees why the log is missing, in their own words
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/sizingLogWording\.test\.ts

```text
Every refusal worded as left refinement
```

###### Obligations


##### Covers

P1

```text
measure-slice.sh S3 alternates 5 before and after pairs
```

```text
Exit 0 with file-alone ratio at most 0.60 and combined at most 0.80; observed on the unchanged page file: exit 1, ratios 1.038 and 1.251, assertions_failed=1
```

```text
The comments tests run faster per test
```

##### Disposition

Tested

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/measure\-slice\.sh

```text
A timeout added instead of making tests smaller
```

###### Obligations

None.

##### Covers

P3

```text
check-slice.sh S3 derives the before commit's test ids from a Vitest run of its page file and compares the changed files with the before commit
```

```text
No production, kit, budget or end-to-end file differs, every removed page test maps to a passing test, no skip or weaker matcher; observed on the unchanged page file: exit 1 with exactly the 17 page tests that are not kept rows
```

```text
Lighthouse behaves and looks as before
```

##### Disposition

Tested

docs\/feature\/story\-6648\-frontend\-clean\-as\-we\-go\/measurement\/check\-slice\.sh

```text
A page test dropped without a row
```

###### Obligations

None.

##### Covers

P2

```text
A reader votes Yes, if
```

```text
The vote is held and nothing is cast until its condition is given, then cast once with it as the comment; cancelling casts nothing; a browser without a name is asked the name first
```

```text
A Yes, if never counts without its condition
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Teams\/Detail\/Refinement\/useVoteCasting\.test\.tsx

```text
A Yes, if cast at once
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
