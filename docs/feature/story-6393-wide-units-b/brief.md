# Product brief: story\-6393\-wide\-units\-b

## Request

```text
Story #6393 FE5 Split wide units, part B (slices S6-S9 of docs/feature/story-6393-fe5-split-wide-units/feature-delta.md, which the nwdes slices S1-S4 here map onto in order): move the metrics view's two effectful parts (named-percentiles scope, state drill-down) out of BaseMetricsView.tsx into hooks; split useMetricsData.ts (970 lines) into its applicability rules, owner key and query helper; split DeliverySection.tsx (992) into its dialogs, row parts and header copy; split DeliveryCreateModal.tsx (989) into its selection content, tab row, form rules and small parts, with rule-only render rows moving down to the form-rules unit tests. Behaviour-free; the query order of useMetricsData is kept; the Delivery files stay in their one folder with no barrel.
```

## Work

M

```text
The three files just under the 1000-line rule and the metrics view's effectful code are split by responsibility, with room to grow before the size rule would list them
```

### Rework

None.

## Promises

### P1

Outcome

```text
Each split unit behaves and renders exactly as before: its existing tests stay green unmodified, apart from tests that move with the code they cover, and the order in which the metrics queries start is unchanged
```

### P2

Outcome

```text
The metrics view's named-percentiles scope and state drill-down live in their own hooks, outside the metrics view component
```

### P3

Outcome

```text
useMetricsData.ts, DeliverySection.tsx and DeliveryCreateModal.tsx are each split by responsibility to well under 1000 lines, and the size rule's list stays empty
```

### P4

Outcome

```text
The Delivery form's pure rules (dates, name, blocking error, offered tabs, bound source) are tested without rendering the dialog
```

## Scope

```text
BaseMetricsView's two effectful parts as hooks; useMetricsData.ts, DeliverySection.tsx and DeliveryCreateModal.tsx split by responsibility; import-line edits in their importers and the renamed entries of the layer-boundary leak list; rule-only render rows moved down to unit tests
```

```text
Splitting useMetricsData's query calls into sub-hooks; packaging into modules (FE2) or a Delivery module; per-module API ports (FE4); a barrel or a change to DeliveryGrid/index.ts; any visible or behavioural change
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
The metrics view's named-percentiles scope and state drill-down are hooks of their own, and every metrics widget loads, draws and drills down exactly as before
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
useMetricsData.ts gives its applicability rules, owner key and query helper to three modules of their own; every metrics query starts in the same order and answers as before
```

#### Promises

P1

P3

#### Depends on

None.

#### Slice work

None.

### S3

```text
DeliverySection.tsx gives its unbind dialog, work-items tab, feature-row parts and header copy to files of their own in the same folder; a Delivery section looks and acts exactly as before
```

#### Promises

P1

P3

#### Depends on

None.

#### Slice work

None.

### S4

```text
DeliveryCreateModal.tsx gives its selection content, tab row, form rules and small parts to files of their own in the same folder; the form rules are tested without rendering; the create and edit dialog looks and acts exactly as before
```

#### Promises

P1

P3

P4

#### Depends on

None.

#### Slice work

None.
