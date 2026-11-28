# Acceptance brief: story\-6712\-faster\-page\-loads

## Slices

None.

## Expectations

### Rows

#### E1

##### Promises

P1

```text
With a browser that accepts compression, a cold load of the spike's pages transfers the frontend JavaScript at about a third of today's bytes (about 1 MB instead of 3.4 MB).
```

```text
A byte measure on one page set does not show every JS file is compressed or that the figure holds on other pages.
```

None.

None.

None.

#### E2

##### Promises

P1

```text
A browser that does not accept compression still loads the same files, and the pages work.
```

```text
A bytes-on-the-line oracle does not see that the uncompressed path still serves complete, working files.
```

None.

None.

None.

#### E3

##### Promises

P2

```text
A browser that accepts compression receives the large API answers judged safe (metrics, feature lists, settings) compressed, in fewer bytes than today.
```

```text
The oracle does not see which answers were judged safe or whether any large safe answer was left uncompressed.
```

None.

None.

None.

#### E4

##### Promises

P3

```text
A page load or move to another page asks GET my-summary once in total, not once per gating component, and every gate still shows the right actions.
```

```text
A call count does not show that gating results are unchanged or that nothing reads my-summary outside useRbac().
```

None.

None.

None.

#### E5

##### Promises

P4

```text
When an administrator grants or removes the user's right while a page is open, the next page the user moves to shows the new actions and hides lost ones, without a reload.
```

```text
A single-user simulation does not see a real second session or the timing of when the change is picked up.
```

None.

None.

None.

#### E6

##### Promises

P5

```text
After the user creates a Team or Portfolio or changes a grant, the follow-on actions (such as admin actions on the new Team) show at once, without a reload.
```

```text
The oracle does not see every kind of own change or a stale gate shown briefly before the refresh.
```

None.

None.

None.

#### E7

##### Promises

P6

```text
When the permissions answer cannot be loaded, every gating component shows the same could-not-load state and fallback as today, and one Retry asks again once for all of them.
```

```text
Failure observed on one page does not show all components agree, or that Retry asks exactly once and recovers every component.
```

None.

None.

None.

#### E8

##### Promises

P7

```text
The Team list, Portfolio list and licence status used by the app shell and pages are each fetched once per load or move, not once per component.
```

```text
A call count does not show the shared lists and licence status still render the same content everywhere.
```

None.

None.

None.

#### E9

##### Promises

P8

```text
After the user creates, renames or deletes a Team or Portfolio, the menus, overview and lists show it at once, and a change by someone else shows on the next move to another page.
```

```text
The oracle does not see all surfaces showing the change or the other-user case in a real second session.
```

None.

None.

None.

#### E10

##### Promises

P9

```text
A Features tab loads without the full Team settings (32 KB today) and shows the same rows and columns as today.
```

```text
A byte or call check does not show rows and columns are unchanged for all Team kinds and data.
```

None.

None.

None.

#### E11

##### Promises

P10

```text
On page load the update check carries only whether an update exists and the newest version, not the release notes (368 KB today); the footer's version checks run at the same time, and if any fails the footer shows its error as today.
```

```text
The oracle does not see real concurrency timing or each individual failure case in the footer.
```

None.

None.

None.

#### E12

##### Promises

P11

```text
When the user opens the new-version dialog with an update available, it shows a loading look and then the same release notes as today.
```

```text
The oracle does not see the loading look on a slow connection or that the notes are identical to today's.
```

None.

None.

None.

#### E13

##### Promises

P12

```text
When release notes cannot load, the dialog still shows the newest version number, says the notes could not be loaded, and its Retry button asks again and shows the notes on success.
```

```text
The oracle does not see repeated failures or Retry after a recovered connection.
```

None.

None.

None.

### Waivers

None.

## Charter

None.

## Examination

None.
