# Product brief: story\-6504\-childless\-feature\-size

## Request

```text
ADO Story 6504: Choose whether a Feature without children gets an inferred size. Today a Feature with no child Work Items is sized by the Portfolio's Default Feature Size and that count is spread across the teams (e.g. '0 of 4'); some users read it as data from the tracker. Want a setting: Infer (today's behaviour, clearly labelled as an assumption wherever the count shows) or Disable (no inferred count; the Feature shows no remaining work and says it has no child Work Items yet). Open: what a forecast does for such a Feature when inference is off. Source: customer feedback 2026-11-02 ('No guessing when an item has no children ... I'd rather see nothing, or a clear label saying it's an assumption'). Maintainer skipped DISCOVER and DIVERGE explicitly on 2026-11-25; stand-alone Story, no parent Epic.
```

## Work

M

```text
A Portfolio owner decides whether Lighthouse invents a size for a Feature that has no child Work Items yet, and nobody mistakes an assumed count for tracker data.
```

### Rework

None.

## Promises

### P1

Outcome

```text
A Portfolio owner can switch default (inferred) Feature sizing off. When it is off, a Feature with no child Work Items gets size 0: Lighthouse invents no count for it and assigns nothing to any Team, and forecasts take that 0 as it comes. When it is on, Lighthouse behaves as today.
```

### P2

Scenario

```text
Default sizing on: a Feature without child Work Items is sized as today
```

```text
Portfolio Ocean has Default Feature Size 4, default sizing is switched on, and Feature F has no child Work Items in any Team
```

```text
a Portfolio owner opens the Feature list of Ocean
```

```text
F shows 4 remaining Work Items split across its Teams, with today's warning that the size comes from the default Feature size
```

### P3

Scenario

```text
Default sizing off: the forecast takes the 0 with no special handling
```

```text
default sizing of Portfolio Ocean is switched off and Feature F has no child Work Items
```

```text
a Portfolio owner looks at the forecast of F and of a Delivery that contains F
```

```text
F counts as 0 remaining Work Items in those forecasts, the same as any Feature with nothing remaining; Lighthouse adds no special forecast handling for F
```

### P4

Scenario

```text
Default sizing off: a Feature without child Work Items gets no invented count
```

```text
Portfolio Ocean has Default Feature Size 4, default sizing is switched off, and Feature F has no child Work Items in any Team
```

```text
a Portfolio owner opens the Feature list of Ocean
```

```text
F shows 0 remaining Work Items, no count such as '0 of 4' appears for F, and no Team carries remaining work for F
```

### P5

Scenario

```text
Default sizing off: the Feature says it has no child Work Items yet
```

```text
default sizing of Portfolio Ocean is switched off and Feature F has no child Work Items in any Team
```

```text
a Portfolio owner looks at F in the Feature list of Ocean's Portfolio page, a Team's page, and the Delivery timeline
```

```text
in each place F shows the note 'No child Work Items yet' where today's default-size warning icon sits, and today's sentence about the default Feature size does not appear
```

### P6

Scenario

```text
Default sizing off: a Feature in an override state keeps its real child count
```

```text
Portfolio Ocean has Default Feature Size 4 and lists state 'In Progress' under override real child count states, default sizing is switched off, and Feature G is In Progress with 1 child Work Item
```

```text
a Portfolio owner opens the Feature list of Ocean
```

```text
G shows its real child count of 1, not 4, and is not marked as using the default size
```

### P7

Scenario

```text
Default sizing off: settings without effect cannot be changed, and their values come back
```

```text
Portfolio Ocean has Default Feature Size 7, a size-estimate field mapping and override states set
```

```text
a Portfolio owner switches default sizing off in Ocean's advanced settings, and later switches it on again
```

```text
while off, Default Feature Size, the percentile option, the size-estimate field mapping and the override-states selection are greyed out and cannot be changed; after switching on, they show 7, the same mapping and the same states
```

### P8

Scenario

```text
Existing Portfolios keep today's behaviour after the upgrade
```

```text
Portfolio Ocean existed before this change, and Portfolio Reef is created after it
```

```text
a Portfolio owner opens the advanced settings of Ocean and of Reef
```

```text
default sizing is switched on for both, next to Default Feature Size, and their Feature counts are as before
```

## Scope

```text
With the switch off, a not-Done Feature with no child Work Items gets size 0: no inferred count, no remaining work assigned to any Team, and forecasts take the 0 as it comes.
```

```text
A per-Portfolio on/off switch for default Feature sizing in the advanced settings next to Default Feature Size, on for existing and new Portfolios.
```

```text
With the switch off, the Feature shows the note 'No child Work Items yet' in today's warning-icon place in the Feature lists on the Portfolio and Team pages and on the Delivery timeline.
```

```text
With the switch off, the override real child count states rule and a mapped size-estimate field have no effect: a Feature's size is its real child count, or 0.
```

```text
With the switch off, Default Feature Size, the percentile option, the size-estimate field mapping and the override-states selection are greyed out; their stored values are kept and come back when the switch is on again.
```

```text
Special forecast handling for a Feature sized 0 (skipping it, showing no date, warnings on the forecast): excluded because the maintainer decided the forecast takes the 0 as it comes.
```

```text
A new or stronger 'assumed' label on inferred counts when the switch is on: excluded because the maintainer did not ask for it; today's warning stays.
```

```text
Changing how Default Feature Size or its percentile is computed, and rewriting Delivery metrics history or archived Deliveries.
```

## Decisions

```text
No Epic, because 6504 is a single stand-alone Story (maintainer, 2026-11-25). DISCOVER and DIVERGE were skipped explicitly by the maintainer on 2026-11-25.
```

#### Status

Confirmed


```text
One general on/off switch for default (inferred) Feature sizing. On: today's behaviour, unchanged. Off: a Feature with no child Work Items gets size 0, so there is no inferred count and nothing is assigned to any Team (maintainer, 2026-11-25: 'a general switch to turn default size off, then we simply take 0 and live with the consequences').
```

#### Status

Confirmed


```text
The Story's open forecast question is answered: no special forecast handling. When the switch is off, When / How Many forecasts, Delivery likelihood and the Delivery timeline take the 0 as it comes, whatever that means for dates and likelihood (maintainer, 2026-11-25).
```

#### Status

Confirmed


```text
Old records stay as recorded: Delivery metrics history and archived Deliveries keep the 'using default size' flag they were captured with; switching off changes only what is computed from then on.
```

#### Status

Confirmed


```text
Done Features, Features that have child Work Items, and the percentile calculation of Default Feature Size are unaffected by the switch.
```

#### Status

Confirmed


```text
Every changed view that shows data (Feature lists on Portfolio and Team pages, Delivery timeline, Delivery Feature-size chart, Portfolio advanced settings): what it shows while loading and when loading fails.
```

#### Status

Open

Design

```text
Context map placement after ADR-270/ADR-271: which bounded contexts the switch touches (Portfolio configuration, Feature sizing, Forecasting), the glossary term for the switch (proposed 'default Feature sizing'), whether turning it off publishes or reacts to a domain event (proposed: a Portfolio settings-changed event triggers the existing Feature recalculation), and the UI surface (proposed: the Portfolio's settings tab).
```

#### Status

Open

Design

```text
End-of-DISCUSS checklist view: RBAC unchanged, the switch is a Portfolio setting edited by whoever edits Portfolio settings today (Portfolio admins). Lighthouse-Clients CLI/MCP: the Portfolio settings payload gains the switch and clients that read or write Portfolio settings must carry it; the Feature payload gains nothing new. Website: one line, e.g. 'Turn off default Feature sizing: a Feature without child Work Items counts as 0, never a guess'. Context map: see the Design open decision.
```

#### Status

Proposed


```text
No labelling change when the switch is on: the existing warning icon and sentence stay; no stronger 'assumed' mark (maintainer, 2026-11-25).
```

#### Status

Confirmed


```text
Switching default sizing off also turns off the 'override real child count states' rule. Off means Lighthouse never puts a default size in place of real children: a Feature's size is its real child count, or 0 if it has none (maintainer, 2026-11-25).
```

#### Status

Confirmed


```text
The switch lives per Portfolio in its advanced settings next to Default Feature Size. It is on for existing and new Portfolios, so nothing changes on upgrade (maintainer, 2026-11-25).
```

#### Status

Confirmed


```text
With the switch off, a Feature with no child Work Items shows 0 remaining Work Items plus the note 'No child Work Items yet' in today's warning-icon place: the Feature lists on the Portfolio and Team pages and the Delivery timeline (maintainer, 2026-11-25).
```

#### Status

Confirmed


```text
With the switch off, a mapped size-estimate field does not size a Feature: always 0 without children. While off, the Portfolio settings grey out Default Feature Size, the percentile option, the size-estimate field mapping and the override-states selection, because none has an effect; their stored values are kept, so switching on again restores them (maintainer, 2026-11-25: 'No, always 0. And we shouldn't even be able to set any other value/option in the default feature size').
```

#### Status

Confirmed


```text
Quint scenario exploration is not run. The forecast value (P3) was settled by the maintainer as 'no special handling'; the maintainer refused exploration of the switch x override-states x child-count combination on 2026-11-25 ('Skip it'). Those Quint-relevant values stay unexplored for that reason.
```

#### Status

Confirmed


## Journey

```text
A Portfolio owner sees a Feature with no child Work Items shown as, for example, '0 of 4' and learns the count is not from the tracker.
```

```text
Not supplied by the maintainer; the customer feedback of 2026-11-02 says 'No guessing when an item has no children ... I'd rather see nothing, or a clear label saying it's an assumption'.
```

```text
The owner opens the Portfolio's settings and switches default Feature sizing off.
```

```text
Not supplied.
```

```text
The owner returns to the Feature list: the Feature shows 0 remaining Work Items and says it has no child Work Items yet; no Team carries work for it.
```

```text
Not supplied.
```

```text
The owner looks at forecasts and Deliveries and accepts that the Feature counts as 0 there until it gets child Work Items.
```

```text
Not supplied; the maintainer said 'we simply take 0 and live with the consequences'.
```

## Slices

### S1

```text
A Portfolio owner switches default Feature sizing off in the Portfolio's advanced settings; the settings that then have no effect are greyed out and keep their values, a Feature with no child Work Items shows 0 remaining Work Items with no Team carrying work for it, and forecasts count it as 0. Existing and new Portfolios start with the switch on and nothing changes.
```

#### Promises

P1

P2

P3

P4

P7

P8

#### Depends on

None.

#### Slice work

M

```text
The Portfolio owner stops Lighthouse inventing work for Features without child Work Items.
```

##### Rework

```text
None.
```

```text
New behaviour; nothing delivered is revised.
```

### S2

```text
With default Feature sizing off, a Feature in an override real child count state keeps its real child count instead of the default size.
```

#### Promises

P6

#### Depends on

S1

#### Slice work

S

```text
The Portfolio owner sees real child counts for Features in override states, so switching off means no default size anywhere.
```

##### Rework

```text
None.
```

```text
Extends S1; nothing delivered is revised.
```

### S3

```text
With default Feature sizing off, a Feature with no child Work Items shows 'No child Work Items yet' in the Feature lists on the Portfolio and Team pages and on the Delivery timeline, instead of today's default-size warning.
```

#### Promises

P5

#### Depends on

S1

#### Slice work

S

```text
The Portfolio owner reads why a Feature shows 0 instead of mistaking it for finished.
```

##### Rework

```text
None.
```

```text
Extends S1; nothing delivered is revised.
```
