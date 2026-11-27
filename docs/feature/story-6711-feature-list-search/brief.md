# Product brief: story\-6711\-feature\-list\-search

## Request

```text
ADO Story 6711 "Improve Search on Feature List": It is not obvious that you have to click on the "..." in a column and then get the filter. At least to filter: Feature Reference ID; Feature Name; Parent ID & Reference. A textbox on top would be nice. This applies everywhere we have the feature list. Also right now we only search for the "name" and not the "Reference ID" if we filter on Feature Name (which is confusing).
```

## Work

S

```text
A user finds a Feature in any feature list by typing part of its ID, name, parent or any other shown value into one search box above the list, and the Name column filter also finds a Feature by its ID.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A user finds a Feature in a feature list by typing part of any value shown in the list (reference ID, name, parent ID or name, or any other column) into one search box above the list; case does not matter and part of a word is enough.
```

#### Amendment

None.

### P2

Outcome

```text
A user finds a Feature by its reference ID when typing it into the Name column filter, because the Name column filter now matches the same 'ID: name' text the cell shows.
```

#### Amendment

None.

### P3

Scenario

```text
User finds a Feature by its reference ID
```

```text
the Features page lists Features and one has the reference ID PROJ-12 (Proposal value)
```

```text
the user types 'proj-12' into the search box above the list
```

```text
the list shows only Features whose shown values contain 'proj-12', including that Feature
```

#### Amendment

None.

### P4

Scenario

```text
User finds a Feature by its parent
```

```text
a Feature has a parent shown as 'PROJ-3: Checkout'
```

```text
the user types 'checkout' into the search box
```

```text
that Feature appears in the list
```

#### Amendment

None.

### P5

Scenario

```text
User sees that nothing matches the search
```

```text
no Feature shows the text 'zzz'
```

```text
the user types 'zzz' into the search box
```

```text
the list says in words that no Feature matches the search, distinct from the message for a list that has no Features at all
```

#### Amendment

None.

### P6

Scenario

```text
User clears the search and sees every Feature again
```

```text
the search box holds text and the list is narrowed
```

```text
the user clears the box
```

```text
every Feature of the list is shown again
```

#### Amendment

None.

### P7

Scenario

```text
Search and a column filter narrow the list together
```

```text
a column filter is set and the search box holds text
```

```text
the list is shown
```

```text
only Features meeting both the column filter and the search are listed
```

#### Amendment

None.

### P8

Scenario

```text
A new search opens the first page of results
```

```text
the user is on page 2 of a long list
```

```text
the user types a search
```

```text
the list shows the first page of the results
```

#### Amendment

None.

### P9

Scenario

```text
The search box is found on every full feature list
```

```text
the Portfolio Features tab, the Team Features tab and the Delivery Work Items tab
```

```text
the user opens each
```

```text
each shows the same search box above its list and it finds a Feature by its reference ID
```

#### Amendment

None.

### P10

Scenario

```text
Name column filter finds a Feature by ID
```

```text
a Feature has reference ID PROJ-12 and name 'Checkout'
```

```text
the user types 'PROJ-12' in the Name column filter
```

```text
that Feature is listed
```

#### Amendment

None.

## Scope

```text
One search box above every full feature list: Features page, Portfolio Features tab, Team Features tab, Delivery Work Items tab. Matches every visible column as the cell reads, case-insensitive, part of a word enough.
```

```text
The Name column filter also matches the Feature reference ID.
```

```text
Delivery dialog Feature pickers, the Refinement grid and the Archived features grid get no search box now; Suggestion: could follow later.
```

```text
No change to the Parent column filter (it already matches ID and name), no remembered search text, no backend change, no change to persisted column filters, sort or layout.
```

## Decisions

```text
Search box on the full feature list only (four pages named in scope); pickers, Refinement and Archived grids excluded and suggested for later.
```

#### Status

Confirmed


```text
Search matches every visible column as the cell value reads; named fields (reference ID, name, parent ID and name) must be findable.
```

#### Status

Confirmed


```text
Name column filter also matches the Feature reference ID.
```

#### Status

Confirmed


```text
Search text is not remembered; box is empty each time a page opens.
```

#### Status

Confirmed


```text
Fact: the Parent column filter already matches both ID and name (its value is the same 'ID: name' text the cell shows); no change.
```

#### Status

Confirmed


```text
Placeholder copy such as 'Search Features' and the no-match wording are DELIVER sketch decisions; reuse of the existing FilterBar or the third-party-packages search is a Design decision.
```

#### Status

Proposed


```text
How the box behaves while the list loads or could not load: shown or hidden, and loading and failure looks stay unchanged.
```

#### Status

Open

Design

## Journey

```text
User opens a feature list and sees a search box above it without opening any column menu
```

```text
not supplied (request says the column menu is not obvious)
```

```text
User types part of an ID, name or parent and the list narrows to matching Features
```

```text
not supplied
```

```text
User clears the box, or sees a no-match message, and knows the list is not simply empty
```

```text
not supplied (request calls the current ID/name behaviour confusing)
```

## Slices

### S1

```text
On the Features page, the user types part of a Feature's reference ID, name or parent into one search box above the list and sees only matching Features; clearing the box shows every Feature again.
```

#### Promises

P1

P3

P4

P6

#### Depends on

None.

#### Slice work

S

```text
Features are findable without knowing the column menu
```

##### Rework

None.

### S2

```text
On the Features page, a search with no match says so in words distinct from an empty list; a search narrows together with a column filter; a new search from page 2 or later opens the first page of results.
```

#### Promises

P5

P7

P8

#### Depends on

S1

#### Slice work

S

```text
Search results are trustworthy in edge states
```

##### Rework

None.

### S3

```text
The same search box appears above the Portfolio Features tab, the Team Features tab and the Delivery Work Items tab and finds a Feature by its reference ID.
```

#### Promises

P9

#### Depends on

S1

#### Slice work

S

```text
Same search wherever the full feature list shows
```

##### Rework

None.

### S4

```text
Typing a Feature's reference ID into the Name column filter finds that Feature in every full feature list.
```

#### Promises

P2

P10

#### Depends on

S1

#### Slice work

S

```text
Name filter no longer ignores the ID shown in the cell
```

##### Rework

None.
