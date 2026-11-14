# Architecture brief: bug\-6624\-tomorrow\-west\-of\-utc

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.ts

Extend

None.

Lighthouse\.Frontend\/src\/pages\/Portfolios\/Detail\/Components\/DeliveryGrid\/deliveryFormRules\.westOfUtc\.test\.ts

#### Verification

```text
pnpm --dir Lighthouse.Frontend exec vitest run src/pages/Portfolios/Detail/Components/DeliveryGrid/deliveryFormRules.westOfUtc.test.ts
```

#### Supports

Lighthouse\.Frontend\/vitest\.config\.ts

#### Public oracle

```text
With the clock in America/New_York, the launch-date rule is asked about today, tomorrow and yesterday, each written as the YYYY-MM-DD string the date field holds
```

```text
Tomorrow is accepted with no date error; today and yesterday are refused with '<Delivery term> date must be in the future'
```

```text
Tomorrow is refused as not in the future, or today is accepted, while the clock is west of UTC
```

##### Promises

P1

#### Prefactoring

None.

#### Records

None.

## ADRs

None.
