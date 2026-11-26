# Acceptance brief: bug\-6706\-jira\-partial\-history

## Slices

### S1

##### Covers

P1

```text
Jira's search answer for an item in a Doing state holds only its latest move within Doing while Jira counts four history entries; the deployment's whole-history route holds the earlier move into Doing (Data Center and Cloud)
```

```text
The item's start date is 16 May 2025, the day it first entered Doing in its whole history
```

```text
An old item still in progress shows the date it really started, not an empty start or its created date
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraPartialHistoryDatingTest\.cs

ItemInDoingWhoseSearchAnswerHoldsOnlyItsLatestMove\_StartsWhenItFirstEnteredDoing

```text
A connector that re-reads only when the entry total is over 30, or dates a Data Center item from the search answer, or sets the start to the created date
```

###### Obligations

None.

##### Covers

P1

```text
Jira's search answer for a finished item holds only its last move into Done while its whole history also holds the earlier move into Doing (Data Center and Cloud)
```

```text
Start date is 16 May 2025 and closed date is 20 June 2025, not a start equal to the closed date
```

```text
A finished item shows its real cycle time instead of zero
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraPartialHistoryDatingTest\.cs

FinishedItemWhoseSearchAnswerHoldsOnlyItsLastMove\_StartsWhenItFirstEnteredDoing

```text
A connector that dates a finished item from the truncated answer and falls back to the closed date as its start
```

###### Obligations

None.

##### Covers

P1

```text
The whole-history read of an item holds every entry Jira counts for it (Data Center and Cloud)
```

```text
The item is marked as holding its whole history
```

```text
The item is reported as holding its whole history
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraPartialHistoryDatingTest\.cs

ItemWhoseWholeHistoryWasRead\_IsMarkedAsHoldingItsWholeHistory

```text
A connector that dates the item correctly but leaves it marked as partial
```

###### Obligations

None.

##### Covers

P1

```text
The whole-history read of an item still holds fewer entries than Jira counts; on Cloud the page says more follow, on Data Center the issue's changelog total exceeds the entries it holds
```

```text
The item stays marked as not holding its whole history
```

```text
The item is reported as holding only part of its history, so a stored history is not replaced by it
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraPartialHistoryDatingTest\.cs

ItemWhoseWholeHistoryReadIsStillPartial\_StaysMarkedAsNotHoldingItsWholeHistory

```text
A connector that marks every re-read item as holding its whole history; already satisfied today and preserved. Which entries the item exposes is not pinned: replacing the stored history is decided downstream from the marker
```

###### Obligations

None.

##### Covers

P1

```text
Jira answers the item's whole-history read with a server error while the search answer holds only part of its history (Data Center and Cloud)
```

```text
The read of the team's items fails loudly with an HTTP request error instead of dating the item from the partial answer
```

```text
A wrong start date is never stored; the failure shows and the next refresh reads the item again
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraPartialHistoryDatingTest\.cs

ItemWhoseWholeHistoryReadIsRefused\_FailsTheReadInsteadOfDatingFromThePartialAnswer

```text
A connector that swallows the refusal and dates the item from the partial search answer
```

###### Obligations

None.

##### Covers

P1

```text
Jira's search answer holds every entry Jira counts for the item (Data Center and Cloud)
```

```text
The item is dated from the search answer (start 16 May 2025) and its whole-history route is never asked
```

```text
No extra calls to Jira for items that are already whole
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraPartialHistoryDatingTest\.cs

ItemWhoseSearchAnswerHoldsEveryEntryJiraCounts\_IsDatedFromItWithoutAskingJiraAgain

```text
A connector that re-reads every item's whole history whether or not it is partial
```

###### Obligations

None.

##### Covers

P1

```text
Jira's search answer carries the item's entries but no count of them (Data Center and Cloud)
```

```text
The item is dated from the entries it came with (start 16 May 2025) and its whole-history route is never asked
```

```text
No extra calls to Jira when nothing says the answer is partial
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraPartialHistoryDatingTest\.cs

ItemWhoseSearchAnswerCountsNoEntries\_IsDatedFromItWithoutAskingJiraAgain

```text
A connector that treats a missing count as partial and re-reads the item
```

###### Obligations

None.

##### Covers

P1

```text
The search counted four entries, but when the whole history is read the item has changed: Jira counts five and holds four (Data Center and Cloud)
```

```text
The item stays marked as not holding its whole history
```

```text
An item that changed while being read is not reported as complete
```

##### Disposition

Tested

Lighthouse\.Backend\/Lighthouse\.Backend\.Tests\/Modules\/TrackerIntegration\/Adapters\/Connectors\/Jira\/JiraPartialHistoryDatingTest\.cs

ItemThatChangedBeforeItsWholeHistoryWasRead\_StaysMarkedAsNotHoldingItsWholeHistory

```text
A connector that trusts the search answer's count and ignores the count reported by the whole-history read
```

###### Obligations

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
On Jira Data Center, an item whose search answer carries only part of its history shows the same date it would show with its whole history.
```

```text
Observes only the fixtures supplied; does not show partial histories split across several search pages, nor a partial history whose earliest change falls outside the returned part.
```

yes

no

None.

#### E2

##### Promises

P1

```text
On Jira Cloud, an item whose search answer carries only part of its history shows the same date it would show with its whole history.
```

```text
Observes only the fixtures supplied; does not show Cloud partial histories split across several search pages, nor a partial history whose earliest change falls outside the returned part.
```

yes

no

None.

### Waivers

#### E1

```text
Benjamin Huser-Berta (maintainer)
```

```text
2026-11-26T19:48:48Z
```

```text
No admitted public probe shows a Jira item's start or closed date; decided 2026-11-26
```

```text
JiraPartialHistoryDatingTest oracle on Data Center and Cloud (8 failed before the fix, 10 passed after); the reporter's changelog.total vs histories discriminator and the maintainer's check in the real app after the push
```

#### E2

```text
Benjamin Huser-Berta (maintainer)
```

```text
2026-11-26T19:48:49Z
```

```text
No admitted public probe shows a Jira item's start or closed date; decided 2026-11-26
```

```text
JiraPartialHistoryDatingTest oracle on Data Center and Cloud (8 failed before the fix, 10 passed after); the reporter's changelog.total vs histories discriminator and the maintainer's check in the real app after the push
```

## Charter

None.

## Examination

### E1

unmet

#### Observations

```text
Public read of workspace W with no PATH listed public/ and scratch/ as empty; no public document supplies the Jira partial-history behaviour for P1, E1 or E2 (read exit 0, zero entries).
```

```text
Probe of 'nwdes --help' was refused (ProbeRefused, no output from the candidate); the admitted console-script list holds 23 candidate commands and none is a Jira partial-history or Jira connector capture.
```

```text
Probe of 'lighthouse-jira-probe --help' was refused (ProbeRefused) with the same admitted list; the examiner cannot capture a team's Jira search answer, changelog.total or whole-history route (rest/api/2/issue/KEY?expand=changelog on Data Center; paged issue changelog on Cloud) from the public surface, and the packet supplies no fixture for either deployment.
```

observation_gap

#### Waiver

```text
Benjamin Huser-Berta (maintainer)
```

```text
2026-11-26T19:48:48Z
```

```text
No admitted public probe shows a Jira item's start or closed date; decided 2026-11-26
```

```text
JiraPartialHistoryDatingTest oracle on Data Center and Cloud (8 failed before the fix, 10 passed after); the reporter's changelog.total vs histories discriminator and the maintainer's check in the real app after the push
```

### E2

unmet

#### Observations

```text
Public read of workspace W with no PATH listed public/ and scratch/ as empty; no public document supplies the Jira partial-history behaviour for P1, E1 or E2 (read exit 0, zero entries).
```

```text
Probe of 'nwdes --help' was refused (ProbeRefused, no output from the candidate); the admitted console-script list holds 23 candidate commands and none is a Jira partial-history or Jira connector capture.
```

```text
Probe of 'lighthouse-jira-probe --help' was refused (ProbeRefused) with the same admitted list; the examiner cannot capture a team's Jira search answer, changelog.total or whole-history route (rest/api/2/issue/KEY?expand=changelog on Data Center; paged issue changelog on Cloud) from the public surface, and the packet supplies no fixture for either deployment.
```

observation_gap

#### Waiver

```text
Benjamin Huser-Berta (maintainer)
```

```text
2026-11-26T19:48:49Z
```

```text
No admitted public probe shows a Jira item's start or closed date; decided 2026-11-26
```

```text
JiraPartialHistoryDatingTest oracle on Data Center and Cloud (8 failed before the fix, 10 passed after); the reporter's changelog.total vs histories discriminator and the maintainer's check in the real app after the push
```
