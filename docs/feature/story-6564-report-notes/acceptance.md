# Acceptance brief: story\-6564\-report\-notes

## Slices

### S1

##### Covers

P2

```text
A signed-in writer adds two notes to a Team report and to a Portfolio report, the second written late in the evening UTC, then the notes are listed
```

```text
The notes are listed newest first, the text without its outer blanks, the day written on the instance calendar (the 3rd for the late evening note) and the author name Ann
```

```text
The writer sees both notes, newest on top, with the day and their name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice11AddReportNoteScenarios\.cs

A\_signed\_in\_writer\_adds\_notes\_and\_the\_report\_lists\_them\_newest\_first\_with\_the\_day\_written\_and\_their\_name

```text
A list that is ascending, uses the UTC day, keeps outer blanks or drops the name
```

###### Obligations



##### Covers

P3

```text
A note is added with sign-in off
```

```text
The note is listed with its text and day and no author name
```

```text
The note shows with its day and without a name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice11AddReportNoteScenarios\.cs

A\_note\_written\_with\_sign\_in\_off\_is\_listed\_with\_its\_text\_and\_day\_and\_no\_author

```text
A note that shows a name or a default author when nobody signed
```

###### Obligations



##### Covers

P4

```text
A note with empty text, only blanks, tabs and line breaks, or no text at all is added next to an existing note
```

```text
Refused with note-text-missing and the list is unchanged
```

```text
The writer is told the note needs text and nothing is saved
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice11AddReportNoteScenarios\.cs

A\_note\_without\_text\_is\_not\_saved\_and\_the\_notes\_stay\_as\_they\_were

```text
A blank note that is saved, or a refusal that also drops the existing note
```

###### Obligations


##### Covers

P4

```text
A note of 2001 characters is added after one of exactly 2000 characters between blanks
```

```text
2000 kept, 2001 refused with note-text-too-long, list holds the one note
```

```text
The writer is told the note is too long and it is not saved
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice11AddReportNoteScenarios\.cs

A\_note\_of\_more\_than\_2000\_characters\_is\_not\_saved\_for\_that\_reason\_while\_one\_of\_exactly\_2000\_between\_blanks\_is

```text
A bound counted before trimming, or a too-long refusal reported as missing text
```

###### Obligations


##### Covers

P4

```text
A note is added while sign-in is on and nobody can be named, then another with blank text in the same state
```

```text
Refused with note-author-unknown (judged before the text) and nothing is stored
```

```text
The writer cannot add a note that nobody can be named for
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice11AddReportNoteScenarios\.cs

A\_note\_is\_refused\_when\_sign\_in\_is\_on\_and\_nobody\_can\_be\_named\_and\_the\_notes\_stay\_as\_they\_were

```text
A note stored unsigned while sign-in is on
```

###### Obligations



##### Covers

P4

```text
A note is added or listed through an owner that does not hold the report, even with an unknown caller and blank text
```

```text
NotFound for both and nothing stored on the real report
```

```text
A report that is not the owner's has no notes to add to or read
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice11AddReportNoteScenarios\.cs

A\_report\_the\_owner\_does\_not\_hold\_has\_no\_notes\_to\_add\_to\_or\_list\_and\_it\_is\_found\_missing\_before\_anything\_else\_is\_judged

```text
A note landing on another owner's report, or a refusal reason instead of not found
```

###### Obligations


##### Covers

P11

```text
A report with two notes is deleted while another report keeps a note
```

```text
Its notes cannot be read or added to; the other report keeps its note
```

```text
The notes of a deleted report are gone with it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice11AddReportNoteScenarios\.cs

Deleting\_a\_report\_deletes\_its\_notes\_and\_leaves\_the\_notes\_of\_other\_reports

```text
Notes that stay readable after the report is deleted
```

###### Obligations



##### Covers

P11

```text
Two reports with notes are removed together, a third keeps its note
```

```text
Both are removed and unreadable; the third lists its note
```

```text
Bulk removal removes the notes of every selected report
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice11AddReportNoteScenarios\.cs

Deleting\_reports\_in\_bulk\_deletes\_the\_notes\_of\_each\_and\_leaves\_the\_notes\_of\_a\_report\_not\_deleted

```text
A bulk removal that leaves the notes of the second report
```

###### Obligations



##### Covers

P11

```text
The Team owning a report with notes is deleted
```

```text
The report and its notes cannot be read; another Team keeps its notes
```

```text
Deleting a Team removes its reports notes
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice11AddReportNoteScenarios\.cs

Deleting\_the\_Team\_that\_owns\_a\_report\_deletes\_the\_report\_and\_its\_notes\_and\_leaves\_another\_Teams\_notes

```text
Notes left behind by an owner deletion
```

###### Obligations



##### Covers

P4

```text
Generated texts of 0 to 2200 characters with outer blanks, interior line breaks, and the boundary lengths are written as notes
```

```text
A note keeps the text without outer blanks when it has 1 to 2000 characters, else is refused with missing or too-long
```

```text
A note is kept trimmed or refused for its reason
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice11AddReportNoteScenarios\.cs

L1\_for\_any\_text\_a\_note\_keeps\_it\_trimmed\_when\_it\_has\_1\_to\_2000\_characters\_and\_is\_refused\_otherwise

```text
A bound applied before trimming or blanks kept
```

###### Obligations



##### Covers

P2

```text
Up to 12 adds per run, 100 runs, across two owners and two reports with tied instants and blank and foreign adds
```

```text
Each report lists exactly its own accepted notes ordered by instant then order of adding, newest first
```

```text
Each report shows only its own notes, newest first
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/Reports\/Slice11AddReportNoteScenarios\.cs

L2\_for\_any\_sequence\_of\_adds\_a\_report\_lists\_exactly\_its\_own\_accepted\_notes\_newest\_first

```text
Ascending order, a tie ordered oldest first, or another report note listed
```

###### Obligations



##### Covers

P2

```text
The authentication mode and profile presence of every combination are mapped to a note caller
```

```text
Disabled is anonymous even with a profile; Enabled with a profile is signed with its id and name; Enabled without, Misconfigured and Blocked are unresolved
```

```text
A note is signed only when sign-in is on and the writer is known
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/ReportNotesControllerCallerTest\.cs

Nobody\_can\_be\_named\_when\_sign\_in\_is\_misconfigured\_or\_blocked

```text
A disabled mode that signs with the profile, or a blocked mode that stays anonymous
```

###### Obligations







##### Covers

P11

```text
Notes rows are written in the real migrated schema: the table is read, a report is deleted, an author removed, 2001 characters inserted on PostgreSQL
```

```text
The table has the declared columns; deleting a report deletes its notes; removing the author clears only the link; 2000 characters kept and 2001 refused
```

```text
Notes live in their own table and go with their report
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Data\/ReportNotesTableConstraintsTest\.cs

Deleting\_a\_report\_deletes\_its\_notes\_and\_leaves\_the\_notes\_of\_other\_reports

```text
A note table without a cascade, or without a bound on the text
```

###### Obligations




##### Covers

P1

```text
A writer opens the notes of a report: list, add, blank and too long refused, loading and failure look, and the notes above the numbers of the live report
```

```text
Notes newest first with day and author, added note on top and the field emptied, refusal copy shown, loading and failure look pinned, numbers still shown
```

```text
A writer reads and adds notes next to the numbers and is told what is wrong
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.add\.test\.tsx

saves what the writer typed as typed\, shows the new note first and empties the field

```text
A trimmed text sent, a note below the numbers, or an empty area on failure
```

###### Obligations


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
