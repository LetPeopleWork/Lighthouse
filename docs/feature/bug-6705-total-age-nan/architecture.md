# Architecture brief: bug\-6705\-total\-age\-nan

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/API\/DTO\/WorkItemDto\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/DTO\/WorkItemDtoTests\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~WorkItemDtoTests
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Build the work item DTO for an item in a Doing state whose StartedDate is null and whose CreatedDate is set
```

```text
The DTO's StartedDate equals the item's CreatedDate; a To Do item with no start date still has none, and an item with a start date keeps its own
```

```text
The DTO's StartedDate is null for the in-progress item, or a To Do item gains a start date
```

##### Promises

P1

#### Prefactoring

None.

#### Records

None.

### S2

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/TotalWorkItemAgeRunChart\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/components\/Common\/Charts\/TotalWorkItemAgeRunChart\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend vitest run src/components/Common/Charts/TotalWorkItemAgeRunChart.test.tsx
```

#### Supports

None.

None.

None.

#### Public oracle

```text
Render the Total Work Item Age Over Time chart for days whose in-progress items include one with startedDate null or undefined and others with start dates
```

```text
Each day's total is the sum of the ages of the items that have a start date, and its item count leaves the skipped item out; no value or tooltip shows NaN
```

```text
A day's total or tooltip shows NaN, the line is missing, or the item without a start date adds to the total or the count
```

##### Promises

P2

#### Prefactoring

None.

#### Records

None.

## ADRs

None.
