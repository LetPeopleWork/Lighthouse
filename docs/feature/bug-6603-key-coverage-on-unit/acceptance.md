# Acceptance brief: bug\-6603\-key\-coverage\-on\-unit

## Slices

### S1

##### Covers

P1

```text
Quoted last lines: "End of SKILL.md" and "End of backend.md". An owner holds one role on a Team or a Portfolio and limits a key to Viewer, or to the admin role of that unit, on the same unit; the key asks to read and to write it. Owners whose role gives nothing there: Portfolio Admin on a Team, Team Admin on a Portfolio, System Admin stored on either. Controls: Team Admin on the Team, Portfolio Admin on the Portfolio.
```

```text
Keys of owners whose role gives nothing on the unit are refused read and write there. The control owners Viewer key reads but does not write, and their admin key reads and writes.
```

```text
A limited key cannot read or administer a Team or Portfolio its owner cannot; keys of owners who administer the unit keep working.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/Authorization\/EffectiveAccessTest\.cs\:\:A\_key\_reaches\_a\_unit\_only\_as\_far\_as\_its\_owners\_role\_there\_gives

```text
A fix that refuses only the stored roles in the table but still lets a misplaced role pass through another path, such as an owner Viewer row from a group rule combined with a misplaced admin row; that path already resolves to Viewer and is refused today.
```

###### Obligations

None.

##### Covers

P1

```text
One key limited to Team Admin on a Team is judged against its owner while the owner is Team Admin there, after the owner is lowered to Viewer, and after the owner loses the Team.
```

```text
The key writes while the owner is Team Admin, no longer writes once the owner is Viewer, and cannot read once the owner holds nothing there.
```

```text
A key follows its owner role on the unit as that role changes, with no new key.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/Authorization\/EffectiveAccessTest\.cs\:\:The\_same\_key\_follows\_its\_owners\_role\_on\_the\_Team\_as\_it\_changes

```text
A service that caches the owner access between requests; that re-reading per use lives in the service and is not observed at this level.
```

###### Obligations

None.

##### Covers

P1

```text
For every unit kind (Team, Portfolio), every owner role stored on the unit or none, and every role a key can be limited to on that unit, the key asks to read and to write the unit.
```

```text
The key never reads a unit its owner cannot read, and never writes a unit its owner cannot write.
```

```text
No limited key ever reaches beyond its owner on a unit.
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Models\/Authorization\/EffectiveAccessTest\.cs\:\:A\_key\_never\_reads\_or\_writes\_a\_unit\_its\_owner\_cannot

```text
A fix that refuses every key row, so keys of admin owners lose access; the table test pins that the control keys keep their access.
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
When a limited API key is used on a unit where its owner holds a role, the key can act on that unit at that role, and no higher.
```

#### E2

##### Promises

P1

```text
When a limited API key is used on a unit where its owner holds no role, the key is refused, however the key itself is scoped.
```

#### E3

##### Promises

P1

```text
When the owner role on a unit changes (raised, lowered or removed), the key follows the owner role on its next use, with no new key needed.
```

## Charter

None.

## Examination

### E1

met

#### Observations

```text
Probe backend-tests _key_ : all green; named parameterised matrix A_key_never_reads_or_writes_a_unit_its_owner_cannot(unitKind in Team/Portfolio, ownerRole in SystemAdmin/TeamAdmin/PortfolioAdmin/Viewer/UnitAdmin/null, keyRole in same set) incl. (Team,PortfolioAdmin,TeamAdmin) and (Team,PortfolioAdmin,Viewer); plus A_key_whose_owner_reaches_nothing_reads_nothing, L10_on_a_key_whose_owner_has_no_profile_reads_nothing, A_Team_Admin_key_writes_the_Team_its_owner_administers_while_a_Viewer_key_there_only_reads, A_Team_Admin_key_of_an_owner_who_only_reads_through_the_rule_writes_nothing. Names and totals only; responses not shown.
```

```text
Full unfiltered _key_ probe output (128 passed, 0 failed) names: The_same_key_follows_its_owners_role_on_the_Team_as_it_changes (passed), Removing_the_rule_stops_the_limited_key_on_its_next_request (passed), and a matrix A_key_reaches_a_unit_only_as_far_as_its_owners_role_there_gives(unit,ownerRole,keyRole,reads,writes) e.g. (Team,PortfolioAdmin,Viewer,False,False), (Team,PortfolioAdmin,TeamAdmin,False,False), (Team,TeamAdmin,Viewer,True,False), (Team,TeamAdmin,TeamAdmin,True,True). Names and arguments only; role-change sequence itself not shown.
```

### E2

met

#### Observations

```text
Probe backend-tests _key_ : all green; named parameterised matrix A_key_never_reads_or_writes_a_unit_its_owner_cannot(unitKind in Team/Portfolio, ownerRole in SystemAdmin/TeamAdmin/PortfolioAdmin/Viewer/UnitAdmin/null, keyRole in same set) incl. (Team,PortfolioAdmin,TeamAdmin) and (Team,PortfolioAdmin,Viewer); plus A_key_whose_owner_reaches_nothing_reads_nothing, L10_on_a_key_whose_owner_has_no_profile_reads_nothing, A_Team_Admin_key_writes_the_Team_its_owner_administers_while_a_Viewer_key_there_only_reads, A_Team_Admin_key_of_an_owner_who_only_reads_through_the_rule_writes_nothing. Names and totals only; responses not shown.
```

```text
Full unfiltered _key_ probe output (128 passed, 0 failed) names: The_same_key_follows_its_owners_role_on_the_Team_as_it_changes (passed), Removing_the_rule_stops_the_limited_key_on_its_next_request (passed), and a matrix A_key_reaches_a_unit_only_as_far_as_its_owners_role_there_gives(unit,ownerRole,keyRole,reads,writes) e.g. (Team,PortfolioAdmin,Viewer,False,False), (Team,PortfolioAdmin,TeamAdmin,False,False), (Team,TeamAdmin,Viewer,True,False), (Team,TeamAdmin,TeamAdmin,True,True). Names and arguments only; role-change sequence itself not shown.
```

### E3

met

#### Observations

```text
Full unfiltered _key_ probe output (128 passed, 0 failed) names: The_same_key_follows_its_owners_role_on_the_Team_as_it_changes (passed), Removing_the_rule_stops_the_limited_key_on_its_next_request (passed), and a matrix A_key_reaches_a_unit_only_as_far_as_its_owners_role_there_gives(unit,ownerRole,keyRole,reads,writes) e.g. (Team,PortfolioAdmin,Viewer,False,False), (Team,PortfolioAdmin,TeamAdmin,False,False), (Team,TeamAdmin,Viewer,True,False), (Team,TeamAdmin,TeamAdmin,True,True). Names and arguments only; role-change sequence itself not shown.
```
