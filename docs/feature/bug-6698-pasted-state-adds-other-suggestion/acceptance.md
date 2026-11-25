# Acceptance brief: bug\-6698\-pasted\-state\-adds\-other\-suggestion

## Slices

### S1

##### Covers

P1

```text
User hovers the suggestion Waiting for Deployment, then pastes Waiting for Reproduction and presses Enter
```

```text
The list adds Waiting for Reproduction once
```

```text
The pasted status is added, not another suggestion
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ItemListManager\/ItemListManager\.pastedEntry\.test\.tsx

```text
Fix that ignores the highlight only for keyboard highlights, not mouse highlights
```

###### Obligations

None.

##### Covers

P1

```text
User moves the highlight with the arrow key, then types Waiting for Reproduction and presses Enter
```

```text
The list adds Waiting for Reproduction once
```

```text
The typed status is added, not another suggestion
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ItemListManager\/ItemListManager\.pastedEntry\.test\.tsx

```text
Fix that clears the highlight only on paste, not on typing
```

###### Obligations

None.

##### Covers

P1

```text
User types Waiting for A, highlights with arrow key, presses Enter; separately types waiting for approval in lower case and presses Enter
```

```text
The shown suggestion Waiting for Approval is added in both cases, with the suggestion's casing
```

```text
Confirming a shown suggestion and case-insensitive matching still work
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ItemListManager\/ItemListManager\.pastedEntry\.test\.tsx

```text
Fix that never adds the highlighted option, adding typed text only (second case would still pass; first would not)
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
When the user pastes or types a value and confirms it with Enter, the value added is exactly the value shown in the input at confirmation.
```

#### E2

##### Promises

P1

```text
A suggestion that was highlighted before the input changed is never added in place of the pasted or typed value.
```

## Charter

None.

## Examination

None.
