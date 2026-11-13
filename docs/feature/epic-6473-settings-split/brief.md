# Product brief: epic\-6473\-settings\-split

## Request

```text
Epic 6473 Settings split: split today's one System Settings page into three areas by who decides (Organization, Access, System), split the Configuration tab into Terminology, Calendar and Refresh & ordering, give the license and the connections a home under System, and route every way in to its new place, as decided in docs/feature/epic-6473-settings-split/feature-delta.md.
```

## Work

L

```text
Each person finds a setting where its owner looks for it, and every old link still opens the same setting.
```

### Rework

None.

## Promises

### P1

Outcome

```text
Settings opens three areas, Organization, Access and System, each holding today's tabs whole, and every old settings link opens the same tab
```

### P2

Outcome

```text
Organization shows Terminology, Calendar and Refresh & ordering tabs, and System shows a Usage Data tab, each with its own loading and failure look
```

### P3

Outcome

```text
System shows a License tab with the license status, import and clear, and the license links in the connection forms open it
```

### P4

Outcome

```text
System shows a Connections tab, first, with the list, reconnect banners, empty state and a Connect button, and the Overview no longer holds the connections section
```

### P5

Outcome

```text
Every way to a connection (stepper, Task Manager, the no-connection hint, old URLs) leads to System, Connections, and the connection pages lead back there
```

## Scope

```text
Frontend only: settings routes, area/tab definitions, header menu, tab moves, License and Connections tabs, entry points and redirects, E2E page objects
```

```text
Any change to what a setting does or stores; any backend change
```

```text
Anything level-related (Epic 6474) and connection-mapping UI (Epic 6455 vision)
```

```text
Docs prose and screenshots: done at finalize, not in the slices
```

## Decisions

```text
Header: text trigger 'Settings' with a down arrow opens a menu of Organization (how we work), Access (who may do what), System (running this server); mobile drawer: non-link caption 'Settings' with the three areas indented (D1-D3)
```

#### Status

Confirmed


```text
Area page h1 is 'Settings › {Area}'; bare /settings lands on the first area the person may see, Organization for a System Admin (D4, D7)
```

#### Status

Confirmed


```text
Access tab 'System Admins' is renamed 'System-wide access'; the old slug keeps working (D5)
```

#### Status

Confirmed


```text
System tab order after all slices: Connections, Database, Encryption, Demo Data, Usage Data, System Info, License (D6)
```

#### Status

Confirmed


```text
Organization tab icons: Terminology Translate, Calendar EventBusy, Refresh & ordering Sync; license failure on Calendar reads 'Could not load the license.' (D8, D9)
```

#### Status

Confirmed


```text
System → Usage Data shows the switch row and the docs link only, no sentence about the footer choice (D10)
```

#### Status

Confirmed


```text
System → License shows the license content at natural width in one 'License' section; connection forms keep the link text 'View license options', only its target changes (D11, D12)
```

#### Status

Confirmed


```text
Connections tab keeps the '{Work Tracking Systems}' title with the button right of it; the button and the stepper action say 'Connect {Work Tracking System}' (D13a, D13b)
```

#### Status

Confirmed


```text
Task Manager connections section gains a 'Manage {Work Tracking Systems}' link, a link in its empty text, and the configured term in its words (D13)
```

#### Status

Confirmed


```text
With no connection the Overview shows a text line under the disabled Add buttons linking to Settings › System › Connections; nothing extra when the stepper is dismissed (D15, D12b)
```

#### Status

Confirmed


```text
Connection pages show a 'Back to {Work Tracking Systems}' link to System → Connections above the title (D14)
```

#### Status

Confirmed


## Journey

None.

## Slices

### S1

```text
Settings opens three areas, Organization, Access and System, each holding today's tabs whole, and every old settings link opens the same tab
```

#### Promises

P1

#### Depends on

None.

#### Slice work

None.

### S2

```text
Organization shows Terminology, Calendar and Refresh & ordering tabs, and System shows a Usage Data tab, each with its own loading and failure look
```

#### Promises

P2

#### Depends on

S1

#### Slice work

None.

### S3

```text
System shows a License tab with the license status, import and clear, and the license links in the connection forms open it
```

#### Promises

P3

#### Depends on

S1

#### Slice work

None.

### S4

```text
System shows a Connections tab, first, with the list, reconnect banners, empty state and a Connect button, and the Overview no longer holds the connections section
```

#### Promises

P4

#### Depends on

S1

#### Slice work

None.

### S5

```text
Every way to a connection (stepper, Task Manager, the no-connection hint, old URLs) leads to System, Connections, and the connection pages lead back there
```

#### Promises

P5

#### Depends on

S4

#### Slice work

None.
