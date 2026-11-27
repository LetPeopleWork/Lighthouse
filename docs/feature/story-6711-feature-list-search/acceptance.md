# Acceptance brief: story\-6711\-feature\-list\-search

## Slices

### S1

##### Covers

P3

```text
On the Features list, the user types proj-12 in lower case into the search box
```

```text
Only the Feature PROJ-12: Checkout is listed
```

```text
The user finds a Feature by its reference ID without opening a column menu
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.search\.test\.tsx

finds a Feature by its reference ID typed in lower case

```text
The Name column matches only the name, so the ID search lists nothing, or the match is case-sensitive
```

###### Obligations


##### Covers

P4

```text
The user types checkout into the search box
```

```text
PROJ-12: Checkout (own name) and PROJ-20: Payments (parent PROJ-3: Checkout) are listed; PROJ-30: Reports is not
```

```text
The user finds a Feature through its parent
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.search\.test\.tsx

finds a Feature through the name of its parent as well as through its own name

```text
The search ignores the Parent column and only matches the Feature's own name
```

###### Obligations


##### Covers

P6

```text
The user types proj-12 and then clears the box with its clear button
```

```text
All three Features are listed again
```

```text
The user gets the whole list back
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.search\.test\.tsx

lists every Feature again when the box is cleared

```text
The list stays narrowed after the box is cleared
```

###### Obligations


##### Covers

P1

```text
The list opens
```

```text
An empty box named Search Features sits in the toolbar row before the copy, export and layout icons
```

```text
The user sees the search box above the list without opening any column menu
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.search\.test\.tsx

offers an empty box named after the Features in the toolbar before its icons

```text
The box sits after the icons or is named with a fixed word instead of the configured Features term
```

###### Obligations

None.

##### Covers

P1

```text
The user types proj, clears the box and types checkout
```

```text
The use of the search is reported once, as a bare FeatureListSearched event with no property
```

```text
The use of the search is counted once per visit and nothing the user typed leaves the browser
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.search\.test\.tsx

reports one use of the search per visit\, however much is typed\, cleared and typed again

```text
An event is sent for every keystroke or carries the text, list or match count
```

###### Obligations

None.

##### Covers

P1

```text
The reporter first answers that the use was not recorded, then that it was, while the user types proj-12
```

```text
Exactly one use is recorded
```

```text
A use made before consent is known is not lost, and is still counted once
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.search\.test\.tsx

still reports the use once the answer to record it arrives after an unrecorded first try

```text
The once-per-visit latch is spent by an unrecorded first try, so the use is never recorded
```

###### Obligations

None.

##### Covers

P1

```text
A text is typed against cells showing PROJ-12: Checkout, special characters or any letter case
```

```text
A cell whose shown text contains the typed text in any case is kept; regex characters are literal
```

```text
Typing part of any shown text finds it, whatever the case
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/quickSearch\.test\.ts

keeps a cell whose shown text contains the typed text\, in any letter case \(law L1\)

```text
The matcher is case-sensitive or treats typed special characters as a pattern
```

###### Obligations


##### Covers

P1

```text
A cell holding an object, list, boolean, nothing or null is tested against a typed text; a number against its digits
```

```text
Cells without text never match; a number matches its digits
```

```text
Typing obj or true does not light up every row
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/quickSearch\.test\.ts

never keeps a cell holding \%s\, whatever is typed \(law L2\)

```text
An object cell matches typing obj through its object text
```

###### Obligations


##### Covers

P6

```text
A blank or whitespace-only text is typed, and more text is typed after some text
```

```text
Blank gives no filter value so every row stays; extending the text never brings a dropped row back; rows kept equal those containing the trimmed text
```

```text
An empty box shows every Feature and more typing only narrows
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/quickSearch\.test\.ts

never brings back a row when more text is typed\, and keeps exactly the rows whose shown text contains it \(law L3\)

```text
Whitespace-only text is treated as a literal and hides rows
```

###### Obligations


##### Covers

P1

```text
A list is shown with no search given
```

```text
No text box is offered
```

```text
The other grids gain no search box
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.search\.test\.tsx

shows no search box when it is not given a search

```text
Every grid gets a search box
```

###### Obligations


##### Covers

P1

```text
The user types 8 PTS in a list whose Weight column shows 8 pts over the value 8
```

```text
Only the row whose shown text contains it is listed
```

```text
Search agrees with what the cell reads
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.search\.test\.tsx

narrows the rows by the text a column shows rather than the value behind it

```text
Search tests the raw value behind the cell instead of its shown text
```

###### Obligations


##### Covers

P1

```text
The user types team, true or false against object and boolean columns
```

```text
No row is listed
```

```text
Typing a word does not match columns that show no text
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.search\.test\.tsx

finds nothing through a column that holds no text when \%s is typed

```text
Object cells match through their object text
```

###### Obligations


##### Covers

P6

```text
The user types al then clears the box
```

```text
The owner of the list hears a, al and then the empty text
```

```text
The list owner can react to the text, such as counting the first use
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/DataGridBase\.search\.test\.tsx

tells its owner the text as it is typed and cleared

```text
The owner hears only the final text or never hears the clear
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A user finds a Feature by typing part of any value shown in the list, into the one search box above the list; case does not matter.
```

```text
Does not show that every column's shown value is searched, only that the listed values are; no check for values shown only on hover.
```

None.

None.

None.

#### E2

##### Promises

P2

```text
A user finds a Feature by its reference ID typed into the Name column filter, because that filter matches the same 'ID: name' text the cell shows.
```

```text
Does not check a Feature whose name is empty, or an ID typed with different case or spacing than the cell shows.
```

None.

None.

None.

#### E3

##### Promises

P3

```text
Typing 'proj-12' in the search box shows only Features whose shown values contain 'proj-12', and includes the Feature with reference ID PROJ-12.
```

```text
Does not show that no other matching Feature is left out when the list spans more than one page.
```

None.

None.

None.

#### E4

##### Promises

P4

```text
Typing 'checkout' in the search box shows a Feature whose parent is shown as 'PROJ-3: Checkout'.
```

```text
Does not cover a parent shown in a different case or only on a second line of the cell.
```

None.

None.

None.

#### E5

##### Promises

P5

```text
When no Feature matches the search 'zzz', the list says in words that no Feature matches, which differs from the message for a list with no Features at all.
```

```text
Does not prove the two messages differ in every locale, or that the empty-search message is not shown in the same case.
```

None.

None.

None.

#### E6

##### Promises

P6

```text
After the user clears the search box, every Feature of the list is shown again.
```

```text
Does not show whether a column filter still narrows the list after the search is cleared.
```

None.

None.

None.

#### E7

##### Promises

P7

```text
With a column filter set and text in the search box, the list shows only Features that meet both the column filter and the search.
```

```text
Does not show the result when the column filter is later cleared while the search stays.
```

None.

None.

None.

#### E8

##### Promises

P8

```text
When the user is on page 2 and types a new search, the list shows the first page of the results.
```

```text
Does not cover a search whose results fit on one page, or a search typed while a page change is still pending.
```

None.

None.

None.

#### E9

##### Promises

P9

```text
The Portfolio Features, Team Features and Delivery Work Items tabs each show the same search box above their list, and the box finds a Feature by its reference ID.
```

```text
Does not show the search box while the tab's list is still loading, and does not check Delivery Work Items beyond a Feature reference ID.
```

None.

None.

None.

#### E10

##### Promises

P10

```text
Typing 'PROJ-12' in the Name column filter lists the Feature with reference ID PROJ-12 and name 'Checkout'.
```

```text
Does not check a Feature whose name alone contains the typed text.
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
