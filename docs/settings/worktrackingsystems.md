---
title: Work Tracking Systems
layout: home
parent: System Settings
nav_order: 1
---

# Work Tracking Systems
Work Tracking System connections are managed from the [Overview](../index.html) page.

![Work Tracking Systems](../../assets/settings/worktrackingsystems.png)

## Adding New Work Tracking Systems
You can set up new Connections via the *Add Connection* button, and have to provide the details according to the selected Work Tracking System Type.
See [the concepts](../../concepts/concepts.html#work-tracking-system) for more details and how to specify it for your specific system.

## Modifying Existing Systems
It may happen that you want to adjust your existing connections. For example if the URL changes, you want to adjust your connection details (due to an updated token), or simply want to rename it.
You can do so by clicking on the 🖊️ icon on the right side of the work tracking system, and then adjust your settings as needed.

{: .important}
As the secret information (like API Tokens) are not available to the end user, you will **always** have to provide this information again on any change.

If your change touches the [Additional Fields](#additional-fields) of a connection that Teams or Portfolios use, saving asks how many of them it will update. **Save and update** updates them now and returns to the overview, **Save only** leaves them to their next scheduled update, and **Cancel** keeps your edits on the page. A connection nobody uses saves without asking.

## Deleting Systems
You can also delete Work Tracking Systems if they are not needed anymore. To do so, you can click on the 🗑️ icon on the right side of the work tracking system. This will permanently delete this work tracking system.

{: .note}
You can only delete a Work Tracking System if no team and portfolio is using it. If the connection is still referenced by any team or portfolio, Lighthouse will block the deletion and show an error message. Either remove or reassign those teams and portfolios first, then retry.

## Is this connection still working?
Lighthouse keeps a health state per connection — healthy, unreachable, or credentials refused — and the [Task Manager](taskmanager.html) in the header is where you see it, together with a **Test connection** button that asks your system right now. Use it after re-entering a token: you do not have to wait for the next refresh to find out whether it worked.

## Work Tracking System Settings
There are settings that apply to any configured Work Tracking System setting. These you can find below the available and configured systems. Those settings are *advanced* settings, and in general, you ideally never need to adjust them. However, there are situations where this may be coming in handy.

### Request Timeout
You can override the default timeout for the requests that are made to your Work Tracking System. If you do so, it means that it Lighthouse will wait potentially longer for an answer from your system.

This can be useful if you're using a query that returns many items, and your system is not very fast in responding. This is more likely to happen if you're using an internally hosted system (for example Jira Data Center) which may not run on the fastest hardware.

To override, simply toggle on the override button, and specify the desired timeout in seconds. The default timeout when no override is active is 100 seconds. So if you override, you most likely want to be above that.

## Additional Fields

Additional Fields let you surface or map custom fields from your work tracking system into Lighthouse. Typical uses include selecting a *Size Estimate* field, a *Parent Override* field for feature grouping, or a *Feature Owner* field for reporting and filters.

See details for [Jira](../concepts/worktrackingsystems/jira.html) and [Azure DevOps](../concepts/worktrackingsystems/azuredevops.html) in the respective sections.

On a Jira connection the **Field Reference** of an Additional Field may also name an issue link type, and on an Azure DevOps connection a relation type. A Portfolio then names that field as its Dependency Field to read its waits from that link or relation type. See [Jira](../concepts/worktrackingsystems/jira.html#naming-a-different-link-type) and [Azure DevOps](../concepts/worktrackingsystems/azuredevops.html#naming-a-different-relation-type).

Additional Fields are used by the UI and reporting features and can also be targeted by Data Sync Mappings. When changing an existing connection, secret options (such as API tokens) will always need to be re-entered.

A new or changed field only has values once the Teams and Portfolios on the connection have been updated. So when you change the Additional Fields of a connection they use, Lighthouse asks whether to update them now or leave it to their next scheduled update. A change saved through the API, the CLI or the MCP server updates them right away.


## Defaults for Teams and Portfolios

Some settings point at an Additional Field of this connection and are the same for every Team and Portfolio that uses it: the Parent Override Field, the Dependency Field, the Size Estimate Field, the Feature Owner Field and the Estimation. Set them once on the connection. Each Team or Portfolio then *follows* the connection's default, unless it *owns* its own choice.

The section sits below Additional Fields on the connection page, under *Defaults for Teams and Portfolios*. Only the settings the connection's tracker reads are listed:

| Connection type | Teams | Portfolios |
|---|---|---|
| Jira and Azure DevOps | Parent Override Field, Estimation | Parent Override Field, Dependency Field, Size Estimate Field, Feature Owner Field, Estimation |
| CSV | Estimation | Feature Owner Field, Estimation |
| Linear and ServiceNow | None, so the section is hidden | None |

A CSV file has no parent, dependency or size field to point at, so only the Feature Owner Field and the Estimation can come from a column.

![Connection defaults with the Estimation editor open](../../assets/settings/worktrackingsystems/connection-defaults.png)

### Following and owning

On a Team or Portfolio, the first entry of each of these settings reads **Connection default (*field*)**, with *Set on the Jira connection* (or the Azure DevOps or CSV connection) underneath. Leave it selected and the owner follows the connection, so a change to the connection's default changes what every follower reads. Pick any other entry, **None** included, and the owner owns the setting. An owner keeps its own setting when the connection's default changes.

Picking the same field as the default does not make an owner follow again. Choose **Connection default** for that.

An Estimation default is followed as one piece: its field, its unit and, for categories, the categories in their order.

### Saving a changed default

When a save changes a default that Teams or Portfolios follow, Lighthouse asks first, and counts the followers:

- A changed Parent Override Field, Dependency Field, Size Estimate Field or Feature Owner Field asks **Update 3 Teams and 0 Portfolios?**. The question also says what the followers read differently, for example *Their parents change only once they have been updated.* **Save and update** saves and updates the followers now. **Save only** saves and leaves them to their next scheduled update. **Cancel** keeps your edits on the page.
- A changed Estimation default asks **Change the Estimation default?**, with **Cancel** and **Save** only. Nothing is queued: the followers show the new estimation the next time their pages load.
- When nobody follows the default you changed, there is no question, and the connection saves as it always does.

### What the log records

Each changed default writes one Information entry to the [Logs](systeminfo.html#connection-default-changes). It names the connection, the default, the field before and after, the Teams or Portfolios that follow it, and how many updates the save queued:

```
Connection "Jira Cloud" changed the Teams' Parent Override Field default from "Caused by" to "Epic Link"; 3 Teams follow it: Payments, Platform, Checkout; 3 updates queued.
```

An Estimation change queues no update, so its entry says *no update queued*.

### Deleting a field in use

You cannot remove an Additional Field that a default or an owner still uses. The save is refused, and nothing is saved:

> "Business Value" cannot be removed. It is the Teams' Parent Override Field default, and Payments uses it as its own. Nothing was saved.

Point the default and the owners at another field before you remove it.

### Moving a Team or Portfolio to another connection

A Team or Portfolio that owns a setting with a field of its current connection cannot move to a connection that lacks that field. The save is refused, with a message such as *The Parent Override Field is not an Additional Field of this Team's connection.* Pick a field of the new connection, or choose **Connection default**, in the same save.

## Data Sync Mappings

{: .recommendation}
Data Sync Mappings require a valid [premium license](../licensing/licensing.html).

Lighthouse can write flow metrics and Forecasts back into your work tracking system, so that your team sees up-to-date operational signals directly in Jira or Azure DevOps without having to switch to Lighthouse. Data Sync is triggered automatically whenever a team, portfolio, or forecast refresh completes.

### How It Works
For each connection you can configure one or more *Mappings*. Each mapping defines:

| Setting | Description |
|---------|-------------|
| **Value Source** | The Lighthouse metric to write (see table below) |
| **Target Field Reference** | The [Additional Field](#additional-fields) the value should be written to |
| **Applies To** | Whether this mapping applies to *Team*-level work items or *Portfolio*-level features |
| **Target Value Type** | The format to write: `Date` (ISO 8601, e.g. `2026-03-15`) or `Formatted Text` (a custom date pattern, e.g. `dd MMM yyyy`) |
| **Date Format** | Required when *Target Value Type* is `Formatted Text`. Specify the pattern used to format the date value. |

### Available Value Sources

| Value Source | Applies To | Description |
|--------------|------------|-------------|
| `WorkItemAge/Cycle Time` | Team and Portfolio | Age (in days) of each in-progress work item respectively Cycle Time if the item is finished |
| `FeatureSize` | Portfolio | Remaining child item count for each feature |
| `ForecastPercentile50` | Portfolio | 50th percentile forecast completion date for each feature |
| `ForecastPercentile70` | Portfolio | 70th percentile forecast completion date for each feature |
| `ForecastPercentile85` | Portfolio | 85th percentile forecast completion date for each feature |
| `ForecastPercentile95` | Portfolio | 95th percentile forecast completion date for each feature |
| `ForecastedStartPercentile50` | Portfolio | 50th percentile forecast start date for each feature |
| `ForecastedStartPercentile70` | Portfolio | 70th percentile forecast start date for each feature |
| `ForecastedStartPercentile85` | Portfolio | 85th percentile forecast start date for each feature |
| `ForecastedStartPercentile95` | Portfolio | 95th percentile forecast start date for each feature |
| `SLE Risk` | Team | The chance each in-progress work item has of missing the team's SLE, as a whole number (`86`, not `86%`) |

#### About the Forecasted Start sources

These write the day work on a feature is forecast to *begin* — the same date the [Forecasted Start](../portfolios/detail.html#forecasted-start) column shows.

- **A feature that has already started writes the day it actually started**, whichever percentile the mapping names. A date that has happened is not a forecast, so all four say the same thing about it.
- **A feature nothing can be said about gets no write at all** — the field is left exactly as it was. Writing anything there would fill it with today's date, in the same shape a real answer arrives in.

#### About `SLE Risk`

It is written only for teams, because a feature can belong to several portfolios, each with its own
SLE and its own history — there would be no single number to write.

Three things follow from how the number is worked out, and they are worth knowing before you map it:

- **Only in-progress items get a value.** Finished items are never written to.
- **An item that gets no value gets no write at all** — the field is left exactly as it was, rather
  than cleared or set to zero. There is no way to write "no answer" into a tracker field, and
  clearing one you filter on would say something louder and less true than leaving it alone. In
  practice this covers a finished item and an item that has not started yet.
- **The evidence is the team's configured history, read as of today**, not whatever date range you
  last had open in the browser — and the screens now read it the same way, so the field and the
  column agree. See [SLE Risk Column](../metrics/flow-metrics.html#sle-risk-column).

The value is refreshed on every team update, so the field changes as an item ages. If your tracker
notifies on field changes, see [Notifications](#notifications) below.

{: .note}
Removing the mapping stops future writes immediately. It does **not** clear values already written —
those stay in your work tracking system until you remove them yourself.

### Adding a Mapping
1. Open the connection editor from the *Overview* page (🖊️ icon).
2. Navigate to the *Mappings* section.
3. Click *Add Mapping* and fill in the fields above.
4. Save the connection.


### Notifications

Lighthouse asks your work tracking system to keep quiet about the values it writes. A forecast Lighthouse
recalculated is not news anyone needs mailed, and without suppression a portfolio-wide refresh reaches
every watcher of every item it touches.

**Azure DevOps** suppresses notifications on every write, and needs no extra permission for it.

**Jira** only allows it if the account Lighthouse connects with has **Administer Jira** globally, or
**Administer Projects** on the project the work item lives in — and because that second one is granted
*per project*, the same connection can be silent in one project and noisy in another.

If the account does not have it, Lighthouse still writes the value — the write is never dropped — but the
watchers of that work item are emailed, and a warning naming the affected projects appears in the
[logs](systeminfo.html#view-logs). See
[Quiet Data Sync on Jira](../concepts/worktrackingsystems/jira.html#quiet-data-sync-notifications) for how
to check whether your account has the permission and how to grant it.

{: .important}
Suppression covers **email notifications only**. Every write still appears in the work item's history,
still bumps its *Updated* timestamp, and still triggers webhooks and automation rules. If an automation
rule of yours reacts to field changes, it will react to Lighthouse's writes whether notifications are
suppressed or not.