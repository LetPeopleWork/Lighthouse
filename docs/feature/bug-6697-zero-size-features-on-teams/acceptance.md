# Acceptance brief: bug\-6697\-zero\-size\-features\-on\-teams

## Slices

### S1

##### Covers

P1

```text
The owner saves a Portfolio with default sizing on and default Feature size 0 (no Owning Team, an Owning Team, or a Feature Owner field naming a Team); it holds an open Feature without child Work Items and without an estimate; the Portfolio is refreshed
```

```text
The Feature has no work row for any Team and is listed on no Team, is still marked as sized by the default, and is still in the Portfolio
```

```text
The Feature appears on the Portfolio but on no Team's Features tab
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/ZeroDefaultFeatureSizeTest\.cs\:\:A\_Feature\_whose\_default\_size\_is\_zero\_is\_not\_listed\_on\_any\_Team\_but\_stays\_in\_its\_Portfolio

```text
A fix that removes the Feature from the Portfolio, or clears the default-size flag, or only handles the fallback teams but still gives the Owning Team or Feature Owner team a 0/0 row, would fail here; a fix that left the Team list empty but kept a 0/0 row would also fail
```

###### Obligations

None.

##### Covers

P1

```text
Same Portfolio settings (default Feature size 0, default sizing on, three Team placements); the open Feature without child Work Items has a tracker estimate of 9; the Portfolio is refreshed
```

```text
The Feature still carries 9 remaining Work Items for its Team(s): shared by both Teams, or all with the Owning Team, or all with the Team named in the Feature Owner field
```

```text
A Feature with an estimate stays listed on the Teams that carry its work
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/ZeroDefaultFeatureSizeTest\.cs\:\:A\_Feature\_with\_a\_tracker\_estimate\_above\_zero\_still\_carries\_it\_for\_its\_Teams\_while\_the\_default\_size\_is\_zero

```text
A fix that skips Team assignment for every default-sized Feature regardless of estimate would fail; passes today as a guard (already satisfied behaviour)
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A Feature whose default size works out to 0 does not appear in the Features list of any Team. Observable: open each Team's Features list and check that the zero-size Feature is absent from all of them.
```

#### E2

##### Promises

P1

```text
A Feature whose default size works out to 0 still appears in the Feature list of its Portfolio. Observable: open the Portfolio's Feature list and check that the zero-size Feature is present.
```

## Charter

None.

## Examination

None.
