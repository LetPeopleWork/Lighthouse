# Architecture brief: bug\-6697\-zero\-size\-features\-on\-teams

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\/Modules\/WorkItems\/Application\/WorkItemService\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/WorkItems\/Application\/ZeroDefaultFeatureSizeTest\.cs

#### Verification

```text
dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter FullyQualifiedName~ZeroDefaultFeatureSizeTest
```

#### Supports

None.

#### Public oracle

```text
Save a Portfolio with default sizing on and default Feature size 0 (once with no Owning Team, once with an Owning Team, once with a Feature Owner field naming a Team), give it an open Feature with no child Work Items and no estimate, refresh the Portfolio through the WorkItemService
```

```text
The Feature has no FeatureWork row for any Team, IsUsingDefaultFeatureSize is true, the Feature is still in the Portfolio; a Feature with an estimate above 0 under the same Portfolio still carries that estimate for its Team(s)
```

```text
The Feature carries a 0/0 FeatureWork row for any Team (fallback, Owning Team or Feature Owner), or loses IsUsingDefaultFeatureSize, or a Feature with an estimate above 0 loses its Team work
```

##### Promises

P1

#### Prefactoring

None.

#### Records

None.

## ADRs

None.
