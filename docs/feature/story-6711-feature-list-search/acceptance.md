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
An empty box named Search Features sits in the toolbar row before every icon (copy, export, layout, column order)
```

```text
The user sees the search box above the list without opening any column menu
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.search\.test\.tsx

offers an empty box named after the Features in the toolbar before its icons

```text
The box sits between two icons, or after them, or is named with a fixed word instead of the configured Features term
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

keeps a cell whose shown text contains the typed text\, in any letter case

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

never keeps a cell holding \%s\, whatever is typed

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

never brings back a row when more text is typed\, and keeps exactly the rows whose shown text contains it

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

##### Covers

P4

```text
The user types proj-3: in lower case, the ID of one Feature's parent
```

```text
Only PROJ-20: Payments, whose parent is PROJ-3: Checkout, is listed
```

```text
The user finds a Feature by its parent's reference ID
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.search\.test\.tsx

finds a Feature through the reference ID of its parent

```text
The parent is matched by its name only, so the parent's ID finds nothing
```

###### Obligations

None.

### S2

##### Covers

P5

```text
A person types text that matches no Feature, 'zzz', into the search box of a feature list that holds Features
```

```text
The list says No Features match "zzz". and lists no Feature; the message for a list with no Features does not appear
```

```text
The person knows the search found nothing and the list is not simply empty
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.searchEdges\.test\.tsx

FeatureListDataGrid search in its edge states says that no Feature matches the search\, not that the list is empty

```text
The no-match case reuses the empty-list message, or shows a blank area
```

###### Obligations


##### Covers

P5

```text
A feature list with no Features is shown and nobody searched
```

```text
The list shows its own empty-list message and no sentence saying a search matched nothing
```

```text
The person sees an empty list described as empty, not as an unmatched search
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.searchEdges\.test\.tsx

FeatureListDataGrid search in its edge states keeps the empty\-list message for a list that holds no Features

```text
The no-match wording replaces the empty-list wording everywhere
```

###### Obligations


##### Covers

P7

```text
A person has typed 'pay' into the search box and then filters the State column by 'Doing'
```

```text
Only the Feature that meets both is listed; Features meeting just one are left out
```

```text
The person sees the search and the column filter narrow the list together
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.searchEdges\.test\.tsx

FeatureListDataGrid search in its edge states lists only the Features that meet both the column filter and the search

```text
The search and column filter are combined with OR, or the search drops the column filter
```

###### Obligations


##### Covers

P8

```text
A person on page 2 of a list of 101 Features types 'proj', which still matches all of them
```

```text
The list shows the first page of results, 1 to 100 of 101, starting at the first Feature
```

```text
The person lands on the first page of the new search results
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.searchEdges\.test\.tsx

FeatureListDataGrid search in its edge states opens the first page of the results when a search is typed on the second page

```text
The page stays on page 2 after a search that leaves more than one page
```

###### Obligations


### S3

##### Covers

P9

```text
Open the Portfolio Features tab with a Feature PROJ-12 and type proj-12 into the Search Features box
```

```text
Only the PROJ-12 Feature is listed
```

```text
The user finds the Feature by its ID in the box above the list
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/featureListSearch\.everyList\.test\.tsx

finds a Feature by its reference ID on the Portfolio Features tab

```text
A list that ignores the typed text would fail; a box that only matched names would fail
```

###### Obligations


##### Covers

P9

```text
Open the Team Features tab with a Feature PROJ-12 and type proj-12 into the Search Features box
```

```text
Only the PROJ-12 Feature is listed
```

```text
The user finds the Feature by its ID in the box above the list
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/featureListSearch\.everyList\.test\.tsx

finds a Feature by its reference ID on the Team Features tab

```text
A list that ignores the typed text would fail; a box that only matched names would fail
```

###### Obligations


##### Covers

P9

```text
Open the Team Features tab and type mobile where one Feature belongs to Northwind Platform and another to Northwind Mobile
```

```text
Only the Feature in Northwind Mobile is listed
```

```text
The user finds Features by the Portfolio shown in the list
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/featureListSearch\.everyList\.test\.tsx

finds a Feature by the Portfolio it belongs to on the Team Features tab

```text
A Portfolio column without shown text for search finds nothing and fails
```

###### Obligations


##### Covers

P9

```text
Open the Delivery Work Items tab with a Feature PROJ-12 and type proj-12 into the Search Features box
```

```text
Only the PROJ-12 Feature is listed
```

```text
The user finds the Feature by its ID on the Delivery tab
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/featureListSearch\.everyList\.test\.tsx

finds a Feature by its reference ID on the Delivery Work Items tab

```text
A Delivery name column matching the name only misses PROJ-12 and fails
```

###### Obligations


##### Covers

P9

```text
Open the Delivery Work Items tab and type atlas where one Feature has work for Team Atlas and another for Team Borealis
```

```text
Only the Feature Atlas works on is listed
```

```text
The user finds Features by the Team shown in the Team column
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/pages\/Common\/featureListSearch\.everyList\.test\.tsx

finds the Features a Team works on by typing the Team name on the Delivery Work Items tab

```text
A Team column with no shown text for search finds nothing and fails
```

###### Obligations


### S4

##### Covers

P2

```text
The user sorts the Name column ascending over names not in ID order (Zebra under PROJ-12, Apple under PROJ-20, Mango under PROJ-30)
```

```text
Features are ordered Apple, Mango, Zebra by name, not by ID prefix
```

```text
Sorting the Name column still orders by Feature name
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.nameFilter\.test\.tsx

keep sorting the Name column by Feature name rather than by reference ID

```text
A Name column sorted by the shown ID: name text
```

###### Obligations

None.

##### Covers

P2

```text
A cell's shown text is searched with any slice of it in any letter case, including an ID-like PROJ-12 prefix
```

```text
Its row stays
```

```text
The reader finds a Feature by what the list shows
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/quickSearch\.test\.ts

keeps a cell whose shown text contains the typed text\, in any letter case

```text
A case-sensitive or regex-based match
```

###### Obligations


##### Covers

P2

```text
Digits are typed that a number cell shows
```

```text
The row with that number stays
```

```text
A number is found by its digits
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/quickSearch\.test\.ts

keeps a cell holding a number when its digits are typed

```text
Numbers never matching
```

###### Obligations


##### Covers

P2

```text
Only spaces are typed
```

```text
Every row stays
```

```text
A blank search narrows nothing
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/quickSearch\.test\.ts

hands the grid nothing for \%j\, so every row stays

```text
Whitespace treated as a search
```

###### Obligations


##### Covers

P2

```text
A search is typed while a column filter is set
```

```text
Only rows meeting both are listed
```

```text
Search and column filter work together
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.searchEdges\.test\.tsx

FeatureListDataGrid search in its edge states lists only the Features that meet both the column filter and the search

```text
Search ignoring the column filter
```

###### Obligations


##### Covers

P2

```text
A search is typed on page two
```

```text
The first page of results shows
```

```text
A new search starts at the first page
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.searchEdges\.test\.tsx

FeatureListDataGrid search in its edge states opens the first page of the results when a search is typed on the second page

```text
Staying on a page past the results
```

###### Obligations


##### Covers

P2

```text
The search box is cleared
```

```text
Every Feature is listed again
```

```text
Clearing restores the list
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.search\.test\.tsx

lists every Feature again when the box is cleared

```text
Rows staying hidden
```

###### Obligations


##### Covers

P2

P10

```text
A Feature with reference ID PROJ-12 and name Zebra is in a feature list; the user opens the Name column filter and types PROJ-12
```

```text
Only the PROJ-12 Feature is listed
```

```text
The user finds a Feature by the ID shown in its Name cell
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/FeatureListDataGrid\/FeatureListDataGrid\.nameFilter\.test\.tsx

list a Feature when its reference ID is typed into the Name column filter

```text
A Name filter that matches only the name text, ignoring the ID
```

###### Obligations

None.

##### Covers

P2

```text
Text is typed that matches only a cell holding an object, list or yes/no value
```

```text
No row stays
```

```text
Cells without shown text never match
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/DataGrid\/quickSearch\.test\.ts

never keeps a cell holding \%s\, whatever is typed

```text
A match on the raw value
```

###### Obligations





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
