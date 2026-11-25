# Architecture brief: bug\-6698\-pasted\-state\-adds\-other\-suggestion

## Paradigm

object_oriented

## Shared records

None.

## Slices

### S1

#### Targets

Lighthouse\.Frontend\/src\/components\/Common\/ItemListManager\/ItemListManager\.tsx

Extend

None.

Lighthouse\.Frontend\/src\/components\/Common\/ItemListManager\/ItemListManager\.pastedEntry\.test\.tsx

#### Verification

```text
pnpm --dir Lighthouse.Frontend test src/components/Common/ItemListManager/ItemListManager.pastedEntry.test.tsx
```

#### Supports

None.

#### Public oracle

```text
Render the list with suggestions such as 'Waiting for Deployment' and 'Waiting for Approval'; open the suggestions and highlight 'Waiting for Deployment' with the mouse (and, in a second case, with the keyboard); then paste or type 'Waiting for Reproduction' and press Enter.
```

```text
onAddItem is called once with 'Waiting for Reproduction'. A suggestion that the user highlights and confirms with Enter while it is still shown is still added as before, and a case-insensitive match of a typed value to a suggestion still takes the suggestion's casing.
```

```text
onAddItem is called with 'Waiting for Deployment' (or any suggestion other than the one matching the entered text), or is not called at all.
```

##### Promises

P1

#### Prefactoring

None.

#### Records

None.

## ADRs

None.
