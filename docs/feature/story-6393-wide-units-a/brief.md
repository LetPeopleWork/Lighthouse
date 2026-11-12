# Product brief: story\-6393\-wide\-units\-a

## Request

```text
Story #6393 FE5 Split wide units, part A (slices S1-S5 of docs/feature/story-6393-fe5-split-wide-units/feature-delta.md): split ModifyConnectionSettings.tsx, ragRules.ts and BaseMetricsView.tsx by responsibility so no production frontend file is over 1000 lines unless listed by name on a size rule that only shrinks; replace the duplicated Team and Portfolio member and group-mapping handlers with one shared hook guarded against new copies; the six member failure texts use the configured Team and Portfolio term. Behaviour-free otherwise.
```

## Work

L

```text
The widest frontend files are split by responsibility and kept small by a named size rule, and Team and Portfolio access handling lives in one place
```

### Rework

None.

## Promises

### P1

Outcome

```text
No production frontend file is over 1000 lines unless it is named on the size rule's list, and the list only shrinks
```

### P2

Outcome

```text
Each split unit behaves and renders exactly as before: its existing tests stay green unmodified, apart from tests that move with the code they cover
```

### P3

Outcome

```text
Team and Portfolio member and group-mapping handling lives in one shared hook, and no page calls those access methods directly
```

### P4

Outcome

```text
The member failure messages on the Team and Portfolio Access tabs use the configured Team and Portfolio words, lower-cased; with default terms they read as today
```

## Scope

```text
ModifyConnectionSettings.tsx, ragRules.ts and BaseMetricsView.tsx split by responsibility; a size rule listing files over 1000 lines by name; one shared Team/Portfolio access hook with a guard; the six member failure texts use the configured terms
```

```text
Splitting DeliverySection, DeliveryCreateModal, useMetricsData and the metrics view's effectful hooks (feature B); packaging into modules (FE2); per-module API ports (FE4); any other visible change
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The connection settings form is split into five smaller files with no visible or behavioural change, and the size rule exists, naming the two files still over 1000 lines
```

#### Promises

P1

P2

#### Depends on

None.

#### Slice work

None.

### S2

```text
The RAG rules are split into three rule-family files plus a types file with no change in any rule's answer, and their entry leaves the size rule's list
```

#### Promises

P1

P2

#### Depends on

S1

#### Slice work

None.

### S3

```text
The pure builders of the metrics view live in their own modules with no visible or behavioural change in any metrics widget
```

#### Promises

P2

#### Depends on

None.

#### Slice work

None.

### S4

```text
The metrics view's widget nodes and frame helpers live in their own modules, the view is under 1000 lines and its entry leaves the size rule's list
```

#### Promises

P1

P2

#### Depends on

S3

S1

#### Slice work

None.

### S5

```text
Team and Portfolio Access tabs handle members and group mappings through one shared hook, a guard names any page that calls those access methods again, and the member failure messages use the configured Team and Portfolio words
```

#### Promises

P2

P3

P4

#### Depends on

None.

#### Slice work

None.
