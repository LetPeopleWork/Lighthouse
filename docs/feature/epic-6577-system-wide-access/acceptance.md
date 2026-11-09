# Acceptance brief: epic\-6577\-system\-wide\-access

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
A System Admin can add Viewer on all for one SSO group, and a member of that group can then read every existing Team and Portfolio, and any Team or Portfolio created afterwards, without being added to any of them.
```

#### E2

##### Promises

P1

```text
A limited API key owned by a member of that group reads the Teams and Portfolios its owner reaches through the rule and its own limit allows, and nothing it was not allowed before.
```

#### E3

##### Promises

P2

```text
A limited API key whose owner reaches a unit only through a system-wide rule works on that unit at the key's exact scope (read-only stays read-only, wider scope works as granted), and never reaches any unit or action beyond the key's limit, even if the owner's rule is wider.
```

#### E4

##### Promises

P3

```text
On a Team or Portfolio Access tab, an Admin of that unit sees a read-only list of the system-wide rules that also reach it, which they cannot edit or remove there; a person who is not an Admin of the unit does not gain this view.
```

#### E5

##### Promises

P3

```text
Where Settings showed Unassigned for a person with no direct role on a unit, it now shows No direct role.
```

#### E6

##### Promises

P4

```text
Everyone signed in is off by default and a fresh or existing install changes no one's access until a System Admin turns it on; turning it on asks for a confirm first, and declining leaves it off.
```

#### E7

##### Promises

P4

```text
After it is confirmed on, every signed-in person with a profile reads every Team and Portfolio, including ones created later; turning it off removes that reach again.
```

#### E8

##### Promises

P5

```text
Admin on all lets members of the chosen group edit settings and members on every Team and Portfolio, including ones created later.
```

#### E9

##### Promises

P5

```text
Holders of Admin on all are still refused every System-Admin-only action (for example system settings and managing system-wide rules).
```

## Charter

None.

## Examination

None.
