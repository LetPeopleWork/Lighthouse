# Product brief: story\-6564\-report\-notes

## Request

```text
Idea: similar to deliveries, I'd like to bea ble to add notes to reports. if auth is on, we should identify who wrote it. Notes should be visible in the "live report" in lighthouse, i think (for nwo) we keep them out of the print to pdf
```

## Work

M

```text
A person reading a report in Lighthouse sees the notes written on it (what happened, how large the Team is), with who wrote them when authentication is on, so the numbers are not misread without their context.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A reader of a report in Lighthouse sees the notes written on it, so its numbers carry the context that explains them.
```

#### Amendment

None.

### P2

Scenario

```text
A writer adds a note to a report
```

```text
authentication is on, and a user has write access on the Team or Portfolio that owns a report
```

```text
the user adds a note with text to the report in the live report
```

```text
the live report lists the note, newest first, with its text, the day it was written and the name of the user who wrote it
```

#### Amendment

None.

### P3

Scenario

```text
A note written with authentication off has no author
```

```text
authentication is off
```

```text
a user adds a note to a report
```

```text
the live report lists the note with its text and the day it was written, and shows no author name
```

#### Amendment

None.

### P4

Scenario

```text
A note without text is not saved
```

```text
a user with write access on the owner of a report
```

```text
the user tries to save a note that is empty or holds only blanks
```

```text
no note is saved, and the user is told that a note needs some text
```

#### Amendment

None.

### P5

Scenario

```text
A reader without write access sees notes but cannot add or change them
```

```text
a user with read access, and no write access, on the Team or Portfolio that owns a report with notes
```

```text
the user opens the report in the live report
```

```text
the user sees the notes, with no control to add, change or delete a note, and a direct request to add, change or delete one is refused
```

#### Amendment

None.

### P6

Scenario

```text
Print and PDF leave the notes out
```

```text
a report that has notes
```

```text
a user prints the report or exports it to PDF, alone or together with other reports
```

```text
the printed or PDF version shows no notes
```

#### Amendment

None.

### P7

Scenario

```text
The author changes their own note
```

```text
authentication is on, and a note on a report that the user wrote
```

```text
the user changes the text of the note
```

```text
the live report shows the new text, marked as edited with the day of the edit
```

#### Amendment

None.

### P8

Scenario

```text
The author deletes their own note
```

```text
authentication is on, and a note on a report that the user wrote
```

```text
the user deletes the note
```

```text
the live report no longer lists the note
```

#### Amendment

None.

### P9

Scenario

```text
Only the author may change or delete a note
```

```text
authentication is on, and a note on a report written by another user, and the user has write access on the owner
```

```text
the user opens the report, or sends a direct request to change or delete that note
```

```text
the user sees no control to change or delete that note, and the direct request is refused
```

#### Amendment

None.

### P10

Scenario

```text
A note nobody signed may be changed or deleted by any writer
```

```text
a note on a report that has no author, because it was written while authentication was off
```

```text
any user with write access on the owner changes or deletes the note, whether authentication is on or off now
```

```text
the change or the deletion is saved and shows in the live report
```

#### Amendment

```text
Maintainer, 2026-11-26, answering who may change or delete a note: "same as delivery notes".
```

```text
The first wording said any writer may change any note while authentication is off. Delivery notes decide by the note instead: a note nobody signed belongs to nobody, and a signed note stays its author's.
```

### P11

Scenario

```text
Deleting a report deletes its notes
```

```text
a report that has notes
```

```text
a user deletes the report, alone or through bulk removal
```

```text
the notes of that report are deleted with it and cannot be read any more
```

#### Amendment

None.

### P12

Scenario

```text
A signed note stays its author's after authentication is switched off
```

```text
a note on a report signed by a user while authentication was on, and authentication is now off
```

```text
a user with write access on the owner tries to change or delete the note
```

```text
the user sees no control to change or delete that note, and a direct request to do so is refused
```

#### Amendment

None.

## Scope

```text
Notes on reports: plain text, listed newest first, each with the day it was written and, when authentication is on, its author.
```

```text
Notes in the live report in Lighthouse, for every report type and for both Team and Portfolio reports.
```

```text
Adding a note with write access on the report owner; changing and deleting a note by the same rule as Delivery notes.
```

```text
Notes are deleted together with their report.
```

```text
Notes in print and PDF (maintainer, 2026-11-26: out for now). A reader of only the PDF sees no context.
```

```text
A separate event date on a note, or two kinds of note (maintainer, 2026-11-26: the day it was written is enough).
```

```text
A structured Team size value; how large the Team is goes in as note text.
```

```text
Notes in the Lighthouse CLI and MCP report reads (maintainer, 2026-11-26: not now).
```

```text
Notes drawn as marks on the report charts.
```

```text
Notes written by the system.
```

```text
A frozen, read-only state for notes: reports have no archived state, unlike Deliveries, so no note is ever frozen.
```

## Decisions

```text
The day a note was written is enough; there is one kind of note, with no separate event date (maintainer, 2026-11-26).
```

#### Status

Confirmed


```text
Write access on the Team or Portfolio that owns the report is enough to add a note (maintainer, 2026-11-26).
```

#### Status

Confirmed


```text
Changing and deleting a note follows Delivery notes, decided by the note: a signed note only by its author; a note nobody signed (written with authentication off) by any user with write access on the owner (maintainer, 2026-11-26: "same as delivery notes").
```

#### Status

Confirmed


```text
Notes are deleted when their report is deleted (maintainer, 2026-11-26).
```

#### Status

Confirmed


```text
Notes stay out of print and PDF for now (maintainer, 2026-11-26).
```

#### Status

Confirmed


```text
Notes are not added to the CLI and MCP report reads now (maintainer, 2026-11-26).
```

#### Status

Confirmed


```text
Notes belong to the report, not to its template, so every report type has them (ADO #6564: "a generic concept for all report types"); how large the Team is goes in as note text (DIVERGE recommendation; the maintainer was asked to object to this direction and answered with the four DISCUSS answers and "Start discuss", 2026-11-26).
```

#### Status

Confirmed


```text
The author name is the name as it stood when the note was written, as for Delivery notes, so a note does not re-label itself when someone is renamed or removed (maintainer, 2026-11-26: "similar to deliveries", "same as delivery notes").
```

#### Status

Confirmed


```text
When a Team or Portfolio is deleted, its reports and their notes go with it.
```

#### Status

Proposed


```text
Two users who change the same note at once: the last save wins, as for Delivery notes.
```

#### Status

Proposed


```text
Whether a note has a maximum length; Delivery notes have none today.
```

#### Status

Open

Design

```text
Where the notes sit in the live report (above the panels, below them, or beside them); settled by the UI sketch at the start of DELIVER.
```

#### Status

Open

Design

## Journey

```text
A coach opens a Team or Portfolio report in the live report and reads its notes next to the numbers.
```

```text
emotion: not supplied
```

```text
The coach adds a note, for example how large the Team is or a training that took place, and sees it at the top of the list with the day and their name.
```

```text
emotion: not supplied
```

```text
If the note has no text, the coach is told it needs some text and writes it again.
```

```text
emotion: not supplied
```

```text
Later the coach corrects or deletes their own note.
```

```text
emotion: not supplied
```

```text
A reader with read access opens the same report and reads the notes, without controls to change them.
```

```text
emotion: not supplied
```

```text
The coach exports the report to PDF; the PDF shows the numbers without the notes.
```

```text
emotion: not supplied
```

## Slices

### S1

```text
A user with write access adds a note to a report in the live report and sees it listed, newest first, with the day it was written and their name (no name when authentication is off); a note without text is not saved; deleting the report deletes its notes.
```

#### Promises

P1

P2

P3

P4

P11

#### Depends on

None.

#### Slice work

M

```text
Context can be put on a report and is read next to its numbers.
```

##### Rework

None.

### S2

```text
A reader without write access sees the notes of a report but no control to add or change them, and print and PDF leave the notes out.
```

#### Promises

P5

P6

#### Depends on

S1

#### Slice work

S

```text
Every reader of the live report gets the context, and the notes reach no one through print or PDF.
```

##### Rework

None.

### S3

```text
The author changes or deletes their own note, others cannot, and a note nobody signed may be changed or deleted by any writer.
```

#### Promises

P7

P8

P9

P10

P12

#### Depends on

S1

#### Slice work

M

```text
Notes stay correct over time without anyone rewriting another person's words.
```

##### Rework

None.
