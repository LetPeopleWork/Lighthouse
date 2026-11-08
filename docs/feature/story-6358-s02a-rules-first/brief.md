# Product brief: story\-6358\-s02a\-rules\-first

## Request

```text
Story 6358 S02 Flow metrics pilot, part A of 3 (DESIGN slice 1): before any Flow metrics type moves out of Lighthouse.Backend.Services into Lighthouse.Backend.Modules.FlowMetrics, widen the architecture rules that key on the Services namespace so a moved type stays examined, and pin the domain-event handler order. Design: docs/feature/story-6358-s02-metrics-pilot/feature-delta.md (R0, R6, E2, E3, D-8).
```

## Work

S

```text
Moving code into Modules cannot silently drop it out of an architecture rule or reorder event handlers
```

### Rework

None.

## Promises

### P1

Outcome

```text
Every architecture rule that examines or forbids the application core (types under Lighthouse.Backend.Services) examines or forbids a type under Lighthouse.Backend.Modules in the same way, so moving a type into a module never drops it from a rule
```

### P2

Outcome

```text
The order in which domain-event handlers run for TeamDataRefreshed and PortfolioFeaturesRefreshed is pinned by a test, so a later move or registration change cannot reorder them unnoticed
```

### P3

Outcome

```text
No production behaviour changes: no production file is edited, and every existing test stays green
```

## Scope

None.

```text
Moving any Flow metrics type (feature B), and the module rules R1 to R5 (feature C)
```

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
A made-up type under Lighthouse.Backend.Modules that breaks an application-core rule is reported by that rule, exactly as the same type under Lighthouse.Backend.Services is
```

#### Promises

P1

P3

#### Depends on

None.

#### Slice work

None.

### S2

```text
The resolved handler order for TeamDataRefreshed and PortfolioFeaturesRefreshed is pinned, and swapping two registrations fails the pin
```

#### Promises

P2

P3

#### Depends on

None.

#### Slice work

None.
