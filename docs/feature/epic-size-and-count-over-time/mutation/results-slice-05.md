# Mutation testing — Epic 5585 slice 05 (US-05, ADO #5618)

**Stack**: frontend (StrykerJS) · **Date**: 2026-08-02 · **Score**: **86.76%** (59 killed / 68 mutants)

Scope is `DeliveryBurnupChart.tsx` — the slice's one production file.

| File | Score | Killed | Survived |
|---|---|---|---|
| `DeliveryBurnupChart.tsx` | 86.76 | 59 | 9 |

## First pass was 70.59%, and one of the gaps was dead logic, not a missing test

Three of the twenty survivors sat inside a single expression:

```ts
point.estimatedItemCount && point.estimatedItemCount > 0 ? point.estimatedItemCount : null
```

The truthiness check already excludes both `0` and `null`, so `> 0` is only ever consulted for values
that have already passed it. **No test could have killed those three mutants** — forcing the comparison
to `true`, to `>= 0`, or to `||` all produce the same output for every reachable input, because the first
operand short-circuits first. The fix is to say what is meant:

```ts
point.estimatedItemCount !== null && point.estimatedItemCount > 0 ? … : null
```

All three then die against scenarios that already existed (`gaps the dotted line where a point is fully
broken down` pins `0 → null`). This is the case for reading survivors rather than counting them: the
remedy was deleting a redundant clause, and adding tests would have hidden the redundancy instead.

Three further contracts had shipped unasserted and got scenarios:

| Mutant that survived | What it proved was untested |
|---|---|
| `showMark: false` → `true` | The estimated line's markers. Backlog and Done were pinned; the third series never was. |
| `label: "Date"` → `""` | The x-axis label. |
| `formatDate` → `() => undefined` | The x-axis `valueFormatter` — dates printed in the reader's locale. |
| `slotProps.legend` position/direction (6 mutants) | The legend's placement along the top. |

## The 9 that remain

**8 are `sx` style literals** — `ObjectLiteral → {}` and `StringLiteral → ""` on `p`, `borderRadius`,
`height`, `display`, `flexDirection`. Killing them means asserting layout CSS, which pins appearance
rather than behaviour; same call as slices 03 and 04.

**1 is equivalent**: `estimatedItemCount !== null` → `true`. With the first operand forced open, `> 0`
still rejects both `null` and `0`, so no reachable input distinguishes it. It is only reachable for a
*negative* estimate, which the recorder cannot produce. The clause stays because it states the domain
rule; the mutant is unkillable without inventing impossible data.

## Reproducing

`stryker.5585.frontend.slice-05.json` + `vitest.stryker.mutation.slice-05.ts` alongside this file. The
vitest config narrows `include` to the three specs covering the burnup chart — sweeping all 292 OOMs the
node heap (Bug #5628 precedent).
