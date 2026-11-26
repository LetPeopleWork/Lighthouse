# Acceptance brief: story\-6564\-report\-notes

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user reading a report in the live report sees the notes written on that report alongside the report numbers, so the numbers carry the context that explains them.
```

```text
Checks that notes are visible; does not show that the notes sit next to the numbers they explain in the live view for a report with notes and for one without.
```

None.

None.

None.

#### E2

##### Promises

P2

```text
With authentication on, a user with write access on the owning Team or Portfolio adds a note with text, and the live report lists it newest first with its text, the day written and the name of the user who wrote it.
```

```text
A single-note check cannot show newest-first order across several notes or the author name shown for each of several authors.
```

None.

None.

None.

#### E3

##### Promises

P3

```text
With authentication off, a note added to a report appears in the live report with its text and the day written, and shows no author name.
```

```text
Covers only the live report; does not show that no author name appears in a reloaded or exported view of the same note.
```

None.

None.

None.

#### E4

##### Promises

P4

```text
A note with empty or blank-only text is not saved when a writer tries to save it; the user is told that a note needs some text.
```

```text
Does not show that the note list is unchanged after the refused save when the report is reloaded, nor the exact message wording.
```

None.

None.

None.

#### E5

##### Promises

P5

```text
A user with read access but no write access on the owning Team or Portfolio sees the notes of a report, has no control to add, change or delete a note, and a direct request to add, change or delete a note is refused.
```

```text
Checks the visible controls and one refused request; does not show a refused direct request for each of add, change and delete, nor for a note the reader did not write.
```

None.

None.

None.

#### E6

##### Promises

P6

```text
Printing or exporting to PDF a report that has notes, alone or together with other reports, produces output that shows no notes.
```

```text
Does not show absence of notes in a bulk export of several reports that each have notes, nor in a note added moments before the print.
```

None.

None.

None.

#### E7

##### Promises

P7

```text
With authentication on, the author of a note on a report they wrote changes its text, and the live report shows the new text marked as edited with the day of the edit.
```

```text
Does not show that the edited mark and day survive a reload, nor that the original text is no longer shown anywhere in the live report.
```

None.

None.

None.

#### E8

##### Promises

P8

```text
With authentication on, the author of a note on a report they wrote deletes it, and the live report no longer lists the note.
```

```text
Does not show that the deleted note stays gone after a reload or cannot be read by a direct request.
```

None.

None.

None.

#### E9

##### Promises

P9

```text
With authentication on, a writer on the owner who did not write a note on a report sees no control to change or delete it, and a direct request to change or delete that note is refused.
```

```text
Covers one note by another author; does not show the refusal for a note by a user who has since been removed, nor the refusal of a request that names an already deleted note.
```

None.

None.

None.

#### E10

##### Promises

P10

```text
As amended on 2026-11-26 to follow delivery notes: a note with no author, written while authentication was off, can be changed or deleted by any user with write access on the owner, whether authentication is on or off now, and the change or deletion is saved and shows in the live report.
```

```text
Does not show the edited mark on the changed note, nor that a changed unsigned note stays unsigned, nor that a signed note on the same report stays protected in the same session.
```

None.

None.

None.

#### E11

##### Promises

P11

```text
Deleting a report, alone or through bulk removal, deletes its notes, which can then no longer be read in the report or by a direct request.
```

```text
Does not show that every report selected in a bulk removal loses its notes, nor that a direct read of a deleted note fails rather than returning stale content.
```

None.

None.

None.

#### E12

##### Promises

P12

```text
After authentication is switched off, a note signed while authentication was on stays protected: a writer on the owner sees no control to change or delete it, and a direct request to do so is refused.
```

```text
Does not show the protection after authentication is switched back on, nor for a note whose author has since been removed.
```

None.

None.

None.

### Waivers

None.

## Charter

None.

## Examination

None.
