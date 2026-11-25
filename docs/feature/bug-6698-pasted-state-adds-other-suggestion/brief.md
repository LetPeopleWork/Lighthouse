# Product brief: bug\-6698\-pasted\-state\-adds\-other\-suggestion

## Request

```text
Bug 6698, reported on Slack: When using the free version of Lighthouse, I am updating the definition of a team's workflow statuses. As I had documented those statuses elsewhere before, I have my list ready in a simple text file. Because I fear typos and uppercase/lowercase mismatches, I copy/paste them. Now, when I paste the status 'Waiting for Reproduction', it is not added but a different, previously added one (e.g. for another team) appears. Two examples of what was chosen instead of what I wanted: 'Waiting for Deployment' resp. 'Waiting for Approval'. This means I have to check the outcome of my insert in 'slightly paranoid mode'. The maintainer has seen the same or similar behaviour.
```

## Work

S

```text
A state (or any list entry) that the user pastes or types and confirms with Enter is the one that gets added, never a different suggestion that was highlighted earlier.
```

### Rework

None.

## Promises

### P1

Outcome

```text
When the user pastes or types a value and presses Enter, the value added is the entered text (in a matching suggestion's casing when one matches it), or the suggestion the user picked for the text now in the input; a suggestion highlighted before the input changed is never added in its place.
```

## Scope

None.

## Decisions

None.

## Journey

None.

## Slices

### S1

```text
In a states list (and every other list built on ItemListManager), the user lets a suggestion become highlighted, then pastes or types a different value and presses Enter: the pasted or typed value is added, not the earlier highlighted suggestion.
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.
