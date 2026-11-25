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

##### Covers

P1

```text
The user types 'Waiting for', hovers the suggestion 'Waiting for Deployment', closes the list with Escape and presses Enter.
```

```text
The entered text 'Waiting for' is added once; the suggestion that is no longer shown is not.
```

```text
The user gets the text they entered, not a suggestion they can no longer see.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ItemListManager\/ItemListManager\.pastedEntry\.test\.tsx\:\:adds the entered text\, not a suggestion highlighted before the list was closed with Escape

```text
Enter adds the last highlighted suggestion after the list closed.
```

###### Obligations

None.

##### Covers

P1

```text
The user types 'Waiting for', hovers 'Waiting for Deployment', tabs out of the field, clicks back into it and presses Enter.
```

```text
The entered text 'Waiting for' is added once.
```

```text
Leaving and re-entering the field drops the old highlight.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ItemListManager\/ItemListManager\.pastedEntry\.test\.tsx\:\:adds the entered text\, not a suggestion highlighted before the field lost and regained focus

```text
A highlight survives blur and refocus and is added on Enter.
```

###### Obligations

None.

##### Covers

P1

```text
The user types 'Waiting for A', hovers 'Waiting for Approval' and presses Enter.
```

```text
'Waiting for Approval' is added once.
```

```text
A suggestion the user picked for the current text is added.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ItemListManager\/ItemListManager\.pastedEntry\.test\.tsx\:\:adds the suggestion the mouse picked for the text now in the input

```text
Enter adds the typed text 'Waiting for A' and ignores a mouse pick.
```

###### Obligations

None.

##### Covers

P1

```text
The user clicks into the empty field so the suggestions open, hovers 'Waiting for Deployment', tabs away, clicks back into the field and presses Enter with the field still empty.
```

```text
Nothing is added.
```

```text
An empty field never adds a suggestion the user only looked at earlier.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ItemListManager\/ItemListManager\.pastedEntry\.test\.tsx

```text
Enter on an empty field adds the suggestion highlighted before the field was left.
```

###### Obligations

None.

##### Covers

P1

```text
The user types 'Waiting for', highlights 'Waiting for Deployment', then the suggestions change so that it is no longer offered, and the user presses Enter.
```

```text
Only the entered text 'Waiting for' is added; the suggestion that is no longer shown is never added.
```

```text
A suggestion that has disappeared from the list is not added in place of what the user entered.
```

##### Disposition

Tested

Lighthouse\.Frontend\/src\/components\/Common\/ItemListManager\/ItemListManager\.pastedEntry\.test\.tsx

```text
Enter adds the earlier highlighted suggestion even though the list no longer offers it, or adds nothing at all.
```

###### Obligations

None.

## Expectations

### Rows

#### E2

##### Promises

P1

```text
A suggestion that was highlighted before the input changed is never added in place of the pasted or typed value.
```

#### E3

##### Promises

P1

```text
When the user pastes or types a value with no suggestion picked for it and presses Enter, the value added is the text shown in the input; if a suggestion matches that text, the value added takes that suggestion's casing.
```

#### E4

##### Promises

P1

```text
When the user has picked a suggestion for the text now in the input and presses Enter, the picked suggestion is the value added.
```

## Charter

None.

## Examination

None.
