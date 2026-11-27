# Acceptance brief: story\-6711\-feature\-list\-search

## Slices

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
