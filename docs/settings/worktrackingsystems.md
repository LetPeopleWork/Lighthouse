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

## Deleting Systems
You can also delete Work Tracking Systems if they are not needed anymore. To do so, you can click on the 🗑️ icon on the right side of the work tracking system. This will permanently delete this work tracking system.

{: .note}
You can only delete a Work Tracking System if no team and portfolio is using it. If the connection is still referenced by any team or portfolio, Lighthouse will block the deletion and show an error message. Either remove or reassign those teams and portfolios first, then retry.

## Work Tracking System Settings
There are settings that apply to any configured Work Tracking System setting. These you can find below the available and configured systems. Those settings are *advanced* settings, and in general, you ideally never need to adjust them. However, there are situations where this may be coming in handy.

### Request Timeout
You can override the default timeout for the requests that are made to your Work Tracking System. If you do so, it means that it Lighthouse will wait potentially longer for an answer from your system.

This can be useful if you're using a query that returns many items, and your system is not very fast in responding. This is more likely to happen if you're using an internally hosted system (for example Jira Data Center) which may not run on the fastest hardware.

To override, simply toggle on the override button, and specify the desired timeout in seconds. The default timeout when no override is active is 100 seconds. So if you override, you most likely want to be above that.

## Additional Fields

Additional Fields let you surface or map custom fields from your work tracking system into Lighthouse. Typical uses include selecting a *Size Estimate* field, a *Parent Override* field for feature grouping, or a *Feature Owner* field for reporting and filters.

- For **Azure DevOps** you typically select the field's reference name (for example `Custom.MyEstimate`).
- For **Jira** use the `customfield_XXXXX` key (for example `customfield_10234`).
- Multi-value fields (for example Jira `labels` or `fixVersions`) are stored as comma-separated values in Lighthouse.

Additional Fields are used by the UI and reporting features and can also be targeted by Data Sync Mappings. When changing an existing connection, secret options (such as API tokens) will always need to be re-entered.


## Data Sync Mappings

{: .recommendation}
Data Sync Mappings require a valid [premium license](../licensing/licensing.html).

Lighthouse can write flow metrics and Forecasts back into your work tracking system, so that your team sees up-to-date operational signals directly in Jira or Azure DevOps without having to switch to Lighthouse. Data Sync is triggered automatically whenever a team, portfolio, or forecast refresh completes.

### How It Works
For each connection you can configure one or more *Mappings*. Each mapping defines:

| Setting | Description |
|---------|-------------|
| **Value Source** | The Lighthouse metric to write (see table below) |
| **Target Field Reference** | The field identifier in your work tracking system where the value should be written (e.g. `Custom.FlowAge` in Azure DevOps, or `customfield_10200` in Jira) |
| **Applies To** | Whether this mapping applies to *Team*-level items or *Portfolio*-level features |
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

### Adding a Mapping
1. Open the connection editor from the *Overview* page (🖊️ icon).
2. Navigate to the *Mappings* section.
3. Click *Add Mapping* and fill in the fields above.
4. Save the connection.