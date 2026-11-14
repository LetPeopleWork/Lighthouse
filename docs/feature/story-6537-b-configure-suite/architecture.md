# Architecture brief: story\-6537\-b\-configure\-suite

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice05bReferencedFieldsAndOlderPayloadsScenarios\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice05aTeamsParentOverrideDefaultScenarios\.cs

Extend

None.

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s1\_compare\_ids\.py

#### Verification

```text
python3 docs/feature/story-6537-b-configure-suite/evidence/s1_compare_ids.py
```

#### Supports

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s1\-expected\-ids\.txt

#### Public oracle

```text
Run the host classes Slice05bReferencedFieldsAndOlderPayloads Slice05aTeamsParentOverrideDefault and the unit classes named in the S1 id table (Debug, tokens removed), compare their sorted test ids with the expected id lists, check that no production or budget file changed since the slice base and that the host-user guard list only lost the lines of classes that left the host, then apply one deliberate break per moved rule and run its named row
```

```text
Each host class of the slice holds only the rows the S1 id table keeps as wire, concurrency or end-to-end rows, and they pass; every expected id is present and passes; each removed host row maps in the id table to a named unit or port row, and each deliberate break of a production line on its path turns that row red
```

```text
A removed host row whose rule, or a loop, gate or branch it walked, no unit or port row turns red for; an id missing or extra; a production or budget file in the diff; a guard-list line added
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

None.

### S2

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice02NamedLinkThatMatchedNothingScenarios\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice01JiraDependenciesFromANamedLinkScenarios\.cs

Extend

None.

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s2\_compare\_ids\.py

#### Verification

```text
python3 docs/feature/story-6537-b-configure-suite/evidence/s2_compare_ids.py
```

#### Supports

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s2\-expected\-ids\.txt

#### Public oracle

```text
Run the host classes Slice02NamedLinkThatMatchedNothing Slice01JiraDependenciesFromANamedLink and the unit classes named in the S2 id table (Debug, tokens removed), compare their sorted test ids with the expected id lists, check that no production or budget file changed since the slice base and that the host-user guard list only lost the lines of classes that left the host, then apply one deliberate break per moved rule and run its named row
```

```text
Each host class of the slice holds only the rows the S2 id table keeps as wire, concurrency or end-to-end rows, and they pass; every expected id is present and passes; each removed host row maps in the id table to a named unit or port row, and each deliberate break of a production line on its path turns that row red
```

```text
A removed host row whose rule, or a loop, gate or branch it walked, no unit or port row turns red for; an id missing or extra; a production or budget file in the diff; a guard-list line added
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

None.

### S3

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice04AzureDevOpsDependenciesFromANamedRelationEndScenarios\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice03AzureDevOpsRelationTypeAcceptedScenarios\.cs

Extend

None.

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s3\_compare\_ids\.py

#### Verification

```text
python3 docs/feature/story-6537-b-configure-suite/evidence/s3_compare_ids.py
```

#### Supports

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s3\-expected\-ids\.txt

#### Public oracle

```text
Run the host classes Slice04AzureDevOpsDependenciesFromANamedRelationEnd Slice03AzureDevOpsRelationTypeAccepted and the unit classes named in the S3 id table (Debug, tokens removed), compare their sorted test ids with the expected id lists, check that no production or budget file changed since the slice base and that the host-user guard list only lost the lines of classes that left the host, then apply one deliberate break per moved rule and run its named row
```

```text
Each host class of the slice holds only the rows the S3 id table keeps as wire, concurrency or end-to-end rows, and they pass; every expected id is present and passes; each removed host row maps in the id table to a named unit or port row, and each deliberate break of a production line on its path turns that row red
```

```text
A removed host row whose rule, or a loop, gate or branch it walked, no unit or port row turns red for; an id missing or extra; a production or budget file in the diff; a guard-list line added
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

None.

### S4

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice12EstimationDefaultScenarios\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice09PortfoliosDependencyFieldDefaultScenarios\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice10PortfoliosSizeEstimateFieldDefaultScenarios\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice11PortfoliosFeatureOwnerFieldDefaultScenarios\.cs

Extend

None.

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s4\_compare\_ids\.py

#### Verification

```text
python3 docs/feature/story-6537-b-configure-suite/evidence/s4_compare_ids.py
```

#### Supports

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s4\-expected\-ids\.txt

#### Public oracle

```text
Run the host classes Slice12EstimationDefault Slice09PortfoliosDependencyFieldDefault Slice10PortfoliosSizeEstimateFieldDefault Slice11PortfoliosFeatureOwnerFieldDefault and the unit classes named in the S4 id table (Debug, tokens removed), compare their sorted test ids with the expected id lists, check that no production or budget file changed since the slice base and that the host-user guard list only lost the lines of classes that left the host, then apply one deliberate break per moved rule and run its named row
```

```text
Each host class of the slice holds only the rows the S4 id table keeps as wire, concurrency or end-to-end rows, and they pass; every expected id is present and passes; each removed host row maps in the id table to a named unit or port row, and each deliberate break of a production line on its path turns that row red
```

```text
A removed host row whose rule, or a loop, gate or branch it walked, no unit or port row turns red for; an id missing or extra; a production or budget file in the diff; a guard-list line added
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

None.

### S5

#### Targets

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice06DefaultChangeUpdatesTheFollowersScenarios\.cs

Extend

None.

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/API\/Integration\/ConfigureOncePerConnection\/Slice08PortfoliosParentOverrideDefaultScenarios\.cs

Extend

None.

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s5\_compare\_ids\.py

#### Verification

```text
python3 docs/feature/story-6537-b-configure-suite/evidence/s5_compare_ids.py
```

#### Supports

docs\/feature\/story\-6537\-b\-configure\-suite\/evidence\/s5\-expected\-ids\.txt

#### Public oracle

```text
Run the host classes Slice06DefaultChangeUpdatesTheFollowers Slice08PortfoliosParentOverrideDefault and the unit classes named in the S5 id table (Debug, tokens removed), compare their sorted test ids with the expected id lists, check that no production or budget file changed since the slice base and that the host-user guard list only lost the lines of classes that left the host, then apply one deliberate break per moved rule and run its named row
```

```text
Each host class of the slice holds only the rows the S5 id table keeps as wire, concurrency or end-to-end rows, and they pass; every expected id is present and passes; each removed host row maps in the id table to a named unit or port row, and each deliberate break of a production line on its path turns that row red
```

```text
A removed host row whose rule, or a loop, gate or branch it walked, no unit or port row turns red for; an id missing or extra; a production or budget file in the diff; a guard-list line added
```

##### Promises

P1

P2

P3

#### Prefactoring

None.

#### Records

None.

## ADRs

None.
