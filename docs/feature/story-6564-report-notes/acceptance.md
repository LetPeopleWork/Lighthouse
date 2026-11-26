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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

A\_note\_keeps\_its\_text\_without\_outer\_blanks\_when\_it\_has\_1\_to\_2000\_characters\_and\_is\_refused\_otherwise

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

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteScenarios\.cs

A\_report\_lists\_exactly\_its\_own\_accepted\_notes\_newest\_first\_whatever\_was\_added\_before

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


##### Covers

P4

```text
2000 and then 2001 characters are inserted straight into the text column on PostgreSQL
```

```text
2000 characters are kept and 2001 are refused by the database as too long
```

```text
A note over the limit cannot be stored even by another writer
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Integration\/Containers\/ReportNotesTextBoundOnPostgresTest\.cs

The\_text\_column\_keeps\_2000\_characters\_and\_refuses\_2001\_on\_PostgreSQL

```text
A text column without the 2000 bound
```

###### Obligations



##### Covers

P1

```text
An editor of a report opens it while it has one written note
```

```text
The note text is listed in the notes area, which sits above the first panel of the numbers
```

```text
The editor sees the written note above the numbers
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.add\.test\.tsx

shows an editor of the report a written note above the first panel of the numbers

```text
A notes area placed below the numbers or showing no loaded note
```

###### Obligations


### S2

##### Covers

P5

```text
A reader with read access only opens a Team report and a Portfolio report that have notes
```

```text
The notes are listed newest first with day and author above the numbers, with no add field, no Add note button and no edit or delete control
```

```text
The reader sees the context the notes give and cannot change it
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.reader\.test\.tsx

lists the notes of a \%s report above the numbers\, newest first\, with no way to add\, change or delete one

```text
The notes area shows for readers but the add field is still visible, or the notes are listed oldest first
```

###### Obligations


##### Covers

P5

```text
A reader opens a report with no notes, a report whose notes cannot be loaded, and a report while the reader roles are still on their way
```

```text
No notes yet. with no field; Could not load the notes with a Retry that brings the notes and the numbers in view; no writing control before or after the roles arrive
```

```text
The reader always sees the notes area in its own state and never a writing control
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.reader\.test\.tsx

says there are no notes yet to a reader of a report without notes\, and still offers no field

```text
A reader whose roles are loading briefly sees the add field, or a failed load shows an empty area
```

###### Obligations



##### Covers

P6

```text
The print page of a report that has notes is opened
```

```text
No note text appears in the printed page and the notes are never requested
```

```text
Print and PDF show the numbers without the notes
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.reader\.test\.tsx

carries none of the notes the report has\, and never asks for them

```text
Notes fetched and hidden by styling only, or drawn in the frame host rather than the print page
```

###### Obligations


##### Covers

P6

```text
The modules the print pages and the export frame import are followed through
```

```text
Neither the notes component nor the notes hook is among them
```

```text
Print and PDF cannot show notes because nothing that draws them is in the print bundle
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/print\/reportPrint\.enforcement\.test\.ts

never reaches \%s

```text
A new note component imported by the print page under another file name
```

###### Obligations


##### Covers

P5

```text
A caller with read access only reads, adds, changes and deletes a note of a Team or Portfolio report by direct request
```

```text
The read answers 200 with the note; the add answers 403 and the note is unchanged; change and delete do not succeed and the note is unchanged
```

```text
A direct write by a reader is refused and nothing changes
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Reports\/ReportNotesWireContractTest\.cs

A\_caller\_with\_read\_access\_only\_is\_refused\_a\_new\_note

```text
Change and delete answer any refusal such as 405 rather than 403 until those routes exist
```

###### Obligations

None.

##### Covers

P5

```text
A writer opens a report with notes
```

```text
The notes are listed and the add field is present
```

```text
A writer can add a note
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.add\.test\.tsx

saves what the writer typed as typed\, shows the new note first and empties the field

```text
The field shows to everyone, which the reader cases pin elsewhere
```

###### Obligations


### S3

##### Covers

P7

```text
The author changes their own note, for a Team and a Portfolio report, then the notes are listed
```

```text
The list shows the new text without outer blanks, marked as edited with the day of the edit, in the same place and with the same writer; the other note is not marked
```

```text
The author corrects their note and sees it corrected and marked as edited
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

The\_author\_changes\_their\_note\_and\_the\_report\_shows\_the\_new\_text\_marked\_as\_edited\_with\_the\_day\_in\_the\_same\_place

```text
A change that rewrites the creation day or moves the note to the top, stores the text with its blanks, or marks every note as edited
```

###### Obligations

None.

##### Covers

P7

```text
The author changes a note to blank text, to 2001 characters, then to exactly 2000 characters between blanks, and to the same text
```

```text
Blank is refused note-text-missing, 2001 is refused note-text-too-long, 2000 is kept, same text still carries the edited day, and refused changes leave no edited mark
```

```text
The same text rules as for writing a note apply when correcting it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

A\_change\_to\_no\_text\_is\_not\_saved\_and\_the\_note\_stays\_as\_it\_was

```text
A change that skips the text rule and saves blank text, or marks a refused change as edited
```

###### Obligations

None.

##### Covers

P8

```text
The author deletes their own note on a Team and a Portfolio report, then the notes are listed
```

```text
The delete is accepted and answers the removed note; the list no longer has it and keeps the others
```

```text
The author removes their note and the live report no longer shows it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

The\_author\_deletes\_their\_note\_and\_the\_report\_no\_longer\_lists\_it\_and\_keeps\_the\_others

```text
A delete that removes every note of the report or answers success without removing the note
```

###### Obligations

None.

##### Covers

P9

```text
A writer who did not sign a note changes it and deletes it; then the notes are listed for the author and for that writer
```

```text
Both are refused note-not-yours, the note is unchanged, and the other writer is offered no controls while the author is
```

```text
Somebody else cannot rewrite or remove another person's words and sees no control to try
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

A\_writer\_who\_is\_not\_the\_author\_is\_refused\_a\_change\_and\_a\_delete\_and\_the\_note\_stays\_as\_written

```text
A refusal under a different code, a refusal that still changes the note, or a list that offers the controls to the wrong person
```

###### Obligations

None.

##### Covers

P9

```text
A writer changes a signed note of another person with blank text
```

```text
The refusal is note-not-yours, not a missing-text refusal
```

```text
A person told they may not change a note is told that first
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

Who\_may\_change\_a\_note\_is\_judged\_before\_what\_is\_written

```text
A rule that judges the text before the author
```

###### Obligations

None.

##### Covers

P10

```text
A signed-in writer and a writer with sign-in off each change one note nobody signed and delete another, after listing the notes
```

```text
Both are offered the controls and accepted, and the list shows the change marked as edited with no author and the deleted note gone
```

```text
Any writer can correct or remove a note nobody signed
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

A\_note\_nobody\_signed\_is\_changed\_and\_deleted\_by\_any\_writer\_whether\_sign\_in\_is\_on\_or\_off\_now

```text
A rule that compares ids directly so an unsigned note is changeable by nobody, or by only one of the two kinds of writer
```

###### Obligations

None.

##### Covers

P12

```text
A note signed while sign-in was on is listed, changed and deleted with sign-in now off
```

```text
No control is offered, the change and the delete are refused note-not-yours, and the note is unchanged
```

```text
A signed note stays its author's after sign-in is switched off
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

A\_note\_signed\_while\_sign\_in\_was\_on\_stays\_its\_authors\_after\_sign\_in\_is\_switched\_off

```text
A rule that lets any caller with no profile change a signed note
```

###### Obligations

None.

##### Covers

P9

P10

P12

```text
Five pairings of signer and caller: own signed note, another's signed note, unsigned with sign-in on, unsigned with sign-in off, signed with sign-in off; list the notes, then change one and delete another
```

```text
What the list offers equals whether the change and the delete are accepted, and a refusal names note-not-yours
```

```text
The controls shown beside a note agree with what the server will accept
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

A\_note\_is\_offered\_a\_change\_and\_a\_delete\_exactly\_when\_a\_direct\_request\_for\_them\_is\_accepted

```text
A list that always says false or always true, or a flag decided by a different rule from the command
```

###### Obligations






##### Covers

P9

```text
Signed in with no resolvable profile, somebody changes a signed note and deletes an unsigned one
```

```text
Both are refused note-author-unknown and the notes are unchanged
```

```text
Nobody can be named, so nothing is changed in their name
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

Signed\_in\_with\_nobody\_nameable\_a\_change\_and\_a\_delete\_are\_refused\_for\_that\_reason\_whoever\_signed\_the\_note\_and\_the\_notes\_stay\_as\_they\_were

```text
An unresolved caller treated as anonymous, which would let them change an unsigned note
```

###### Obligations



##### Covers

P9

P10

```text
Every pairing of signer (nobody or one of three people) and caller, changing one note and deleting another
```

```text
Each change or delete is accepted only for an unsigned note or its signer
```

```text
Whoever signed a note keeps it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

Every\_pairing\_of\_signer\_and\_caller\_is\_accepted\_only\_for\_an\_unsigned\_note\_or\_the\_signer

```text
A rule right for one signer but wrong for another profile id, or one that lets a no-profile caller change a signed note
```

###### Obligations



##### Covers

P9

P10

```text
Random sequences of writes by people signed in or not, then changes and deletes by any of them, with a printed seed
```

```text
Each is accepted exactly as the model of signers says and the report lists exactly the notes still held
```

```text
No history of writes lets someone take over another person's note
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

Whatever\_was\_written\_and\_changed\_before\_a\_change\_is\_accepted\_only\_for\_an\_unsigned\_note\_or\_the\_signer

```text
A rule that only holds for a note's first change
```

###### Obligations


##### Covers

P8

```text
A note already deleted is changed with an unresolved caller and blank text and deleted again, a note id that never existed, and a note asked for through a report or owner that does not hold it
```

```text
Each answers not found before anything else is judged and the other notes stay
```

```text
A note that is gone is reported gone, not refused for some other reason
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

A\_note\_that\_is\_gone\_is\_found\_missing\_before\_anything\_else\_is\_judged\_and\_the\_other\_notes\_stay

```text
A vanished note answered with a refusal about the author or the text
```

###### Obligations

None.

##### Covers

P9

```text
A reader with read access only sends PUT and DELETE for a note of a Team and of a Portfolio report
```

```text
Both answer 403 and the note is unchanged
```

```text
A reader cannot change or delete notes even by sending the request directly
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Reports\/ReportNotesWireContractTest\.cs

A\_caller\_with\_read\_access\_only\_is\_forbidden\_to\_change\_or\_delete\_a\_note

```text
A route that answers 404 or 405 for a reader, or lets a reader through
```

###### Obligations


##### Covers

P9

P12

```text
A writer who did not sign a note sends PUT and DELETE for a Team and a Portfolio note signed by somebody else
```

```text
Both answer 403 naming note-not-yours and the note is unchanged
```

```text
Another writer is turned away on the wire and told the note is not theirs
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Contracts\/Reports\/ReportNotesWireContractTest\.cs

A\_writer\_who\_did\_not\_sign\_a\_note\_is\_forbidden\_to\_change\_or\_delete\_it\_and\_is\_told\_it\_is\_not\_theirs

```text
A 403 without the problem code, or a 400 for the not-yours refusal
```

###### Obligations

None.

##### Covers

P7

P8

P9

P12

```text
The notes area is shown for a writer with notes the server marks changeable and not changeable, and for a user who may not write; the user edits and saves, cancels, saves refused text, deletes, and a delete fails
```

```text
Edit and Delete appear only on changeable notes and only for a writer; a saved edit shows the new text marked edited with the day; cancel changes nothing; refused text says why and keeps the field; a deleted note leaves the list; a failed delete keeps the note
```

```text
The screen shows controls only where the user may use them and every outcome is visible
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/ReportNotes\.change\.test\.tsx

Changing and deleting the notes of a report

```text
Controls shown on every note, hidden from the writer who may change a note, or an edit marker that is not shown
```

###### Obligations

None.

##### Covers

P9

```text
The access rules of every report note route action are checked, including the new PUT and DELETE
```

```text
Every action declares its access
```

```text
No note route is left open to somebody who may not use it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/EveryActionDeclaresItsAccessArchitectureTest\.cs

EveryActionDeclaresItsAccessArchitectureTest

```text
A new route without an access guard on the unchanged list
```

###### Obligations


##### Covers

P7

```text
The Reports module boundaries are checked with the new change types
```

```text
The module rules hold with the new types
```

```text
Notes stay inside the Reports module the way the design promises
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsModuleBoundariesArchUnitTest\.cs

ReportsModuleBoundariesArchUnitTest

```text
A new type placed outside the module that nothing flags
```

###### Obligations


##### Covers

P7

```text
The Reports membership lists are checked with the new change types
```

```text
Every Reports type is on its membership list
```

```text
Notes stay inside the Reports module the way the design promises
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/ReportsMembershipArchUnitTest\.cs

ReportsMembershipArchUnitTest

```text
A new type missing from the lists
```

###### Obligations


##### Covers

P9

```text
The count of guarded actions in the Access HTTP adapters is measured after the four new PUT and DELETE note actions are guarded
```

```text
The measured number of guarded actions is 251, four more than before
```

```text
Every new note route is guarded the way the other routes are
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Architecture\/AccessHttpAdaptersLayoutArchUnitTest\.cs

AccessHttpAdaptersLayoutArchUnitTest

```text
A new unguarded route that leaves the measured number unchanged
```

###### Obligations

None.

##### Covers

P7

```text
A refusal of a changed note's text is turned into words for the user: no text, more than 2000 characters, another reason, and something that is not a refusal
```

```text
Each gets its own sentence, and anything else gets the general could-not-save sentence
```

```text
A user whose correction is refused is told why in plain words
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/Reports\/reportNoteRefusalCopy\.test\.ts

The words for a note the server did not keep

```text
A mapping that gives every refusal the general sentence
```

###### Obligations

None.

##### Covers

P8

```text
A change to a note is read, somebody deletes the note, then the change is saved; and the same without the deletion
```

```text
The change is not saved when the note was deleted meanwhile and the deleted note does not come back; it is saved otherwise
```

```text
A correction of a note that somebody removed is not silently reported as saved
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Acceptance\/ReportNotes\/ReportNoteChangeScenarios\.cs

A\_change\_is\_saved\_unless\_somebody\_deleted\_the\_note\_after\_it\_was\_read\_and\_a\_deleted\_note\_stays\_deleted

```text
A save that answers true although the note is gone, or one that answers false for every change
```

###### Obligations

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

### E1

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```

### E2

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```

### E3

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```

### E4

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```

### E5

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```

### E6

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```

### E7

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```

### E8

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```

### E9

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```

### E10

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```

### E11

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```

### E12

not_examined

```text
No public observation of report notes is available to the examiner: public projection empty and no admitted probe drives the stimulus (O1, O2, O3). Evidence-capture owner: operator; prerequisite: declare examine.public or a candidate console script for report notes, then rebuild the examine prompt.
```
