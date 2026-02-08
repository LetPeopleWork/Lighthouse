---
title: Create/Edit Portfolios
layout: home
parent: Portfolios
nav_order: 2
---

Whether you want to create a new portfolio or edit an existing one, you can use this page to specify all the details that make up your portfolio.

- TOC
{:toc}

# Validation and Save
Before you can save a new or modified portfolio, you'll have to *Validate* the changes. Lighthouse will run a query against your specified work tracking system and make sure that the query is successfully executing. Only after this you will be able to save.

The validation checks the following things:
- The connection to the work tracking system is valid (the connection settings are ok)
- The query can be executed (the query is having a correct syntax)
- The query returns at least one feature

If the validation is ok, you are good to save the changes.

# General Configuration
The general information contains the name of your portfolio. This can be anything that helps you identify it, for example the name of a specific release, customer project, or iteration you want to track.

## Work Tracking System
In order for Lighthouse to get the data it needs for forecasting, it needs to connect to your Work Tracking System. Work Tracking Systems are stored in the Lighthouse Settings and can be reused across Teams and Portfolios.

When creating or modifying both Teams or Portfolios, you can either choose an existing connection or create a new one.

Each connection has a specific name and a type. Depending on the type, different configuration options have to be specified. Check the detailed pages on [Jira](../../concepts/worktrackingsystems/jira.html#work-tracking-system-connection), [Azure DevOps](../../concepts/worktrackingsystems/azuredevops.html#work-tracking-system-connection) or [Csv](../../concepts/worktrackingsystems/csv.html) for details.

## Work Item Query
The Work Item Query is the query that is executed against your [Work Tracking System](../../concepts/concepts.html#work-tracking-system) to get the features relevant for the portfolio. The specific syntax depends on the Work Tracking System you are using.

See the [Jira](../../concepts/worktrackingsystems/jira.html#portfolios) and [Azure DevOps](../../concepts/worktrackingsystems/azuredevops.html#portfolios) specific pages for details on the query.

If you chose a file-based Work Tracking system (like CSV), you will see an upload dialog instead of a query, which will allow you to upload the datasource.

## Cut Off Days
The *Cut Off Days* setting controls how far back in time Lighthouse will look when fetching features. Features that were closed *before* this date will be ignored. This helps to speed up the update and forecast process.

- Portfolio default: **365 days**
- Pick a value (in days) that makes sense for your portfolio — longer windows smooth variability, shorter windows emphasise recent behaviour.

{: .note}
This only applies to items that are considered done. If you have old items in your backlog, they will still appear. 

# Work Item Types
Independent of the [Work Item Query](#work-item-query), Lighthouse needs to know which item types your Features have. Thus you can define the item types that should be taken into account for this specific portfolio.

{: .recommendation}
Common examples for item types on portfolio level are "Epic" in Jira and Azure DevOps, and additionally "Feature" for Azure DevOps.

You can remove types by hitting the remove icon, and add new ones by typing them in and hit *Add Work Item Type*.

{: .note}
You have to type the exact type name as it's used in your Work Tracking System. Make sure to use the exact spelling and casing. Spaces (for example in 'User Story') are supported.  
While you can add multiple types, usually it makes sense to focus on a single one. Either use "Epics" or "Features", but dont mix them up.

# Involved Teams
Lighthouse will look for child items of the features for this portfolio in all team backlogs of the involved teams. Here you can specify which teams are involved in this portfolio. It will list all teams that are defined already and you can select as many as you want. You must select at least one team.

{: .note}
You can also select teams that do not have concrete work for any feature of this portfolio yet. That will allow you to define this team as a [Feature Owner](#ownership-settings) and will save you having to go modify the portfolio later on.

# States
In order for Lighthouse to judge whether an item is *done*, *in progress*, or not even started, you must specify which *states* map to which *category*.

| State Category | Description |
|-------|-------------|
| To Do | Items that are in this state are discovered as *pending* for this team. It is important to have all those states mapped as so Lighthouse can discover pending work for features in a portfolio. |
| Doing | Items that are in this state are actively being worked on. Lighthouse will mark features as *In Progress* based on these states. |
| Done | Items that are done contribute to the Throughput. Based on this value forecasts are made. |

You don't have to add every state to one of the categories. For example you might have a *Removed* or *Canceled* state, which is not mapping to any of the categories. You don't have to specify it, then the item will not be existing for Lighthouse (only items that are in any of the mentioned states are discovered by Lighthouse).

{: .recommendation}
For Azure DevOps, the common states are: *New* or *Backlog* (To Do), *Active*, *Resolved*, *Comitted* (Doing), and *Closed* or *Done* (Done).  
For Jira, the common states are: *To Do* (To Do), *In Progress* (Doing), *Done* (Done).

{: .important}
While Azure DevOps can handle if you specify states that don't exist, Jira will not execute a query with a state that is not in its system. That means for Jira you have make sure everything you mention does exist exactly as specified, as otherwise the [Validation](#validation-and-save) will fail.

# Tags
Tags allow you to add any kind of additional information that may be helpful for you to identify this portfolio. This may be a specific customer, a department, business unit, or tribe, or anything else that somehow might be useful. You can add as many tags as you want. Existing tags will be shown as proposal.

Tags are checked when you use the search functionality.

# Default Feature Size
Not every Feature will be broken down already. This is great, because it might not be worth the effort. However, when we run a forecast, we need to know the approximate size.
Lighthouse offers a functionality to define a *Default Feature Size* that is applied to features that don't have any child items yet.

The default feature size can be defined either as a [fixed number](#default-feature-size), using [historical data](#historical-feature-size), or using an [estimate](#estimated-size).

Lighthouse will determine the size in this order:
1. Number of Child Items read from your Work Tracking System
2. Estimation of Size per Feature
3. Default/Historical Feature Size

If no child items are defined, it will fall back to the estimation. If no field is defined, or the field is defined but no estimate is found for a feature, it will fall back to the default or historical value (depending on your configuration).

## Default Feature Size
The easiest way is to define a fix number, for example 10 items. You can base this number on your historical data or just go with your gut.

## Historical Feature Size
Instead of a default size, you can also chose to let Lighthouse calculate your historical feature size. If you do so, you have to define a percentile and a time range.

The percentile is a number between 50 and 100. If you chose for example 80, it means that it uses the size that 80% of your Features had. If you go for 50, it uses the size that at least 50% of your features had.
The features that are used for this calculation are based on the closed features within the specified time range. So this will be a sliding window, that adjusts over time.

## Estimated Size
On top of the default size, which will be similar for any feature, you can also specify a field that would include an estimate. This allows you to use the default size for features you have not looked at at all, while providing more details for some things that you may have started to refine, but have no child items yet.

{: .note}
The Size Estimate Field is configured by selecting an Additional Field that has been defined on your Work Tracking System connection. Go to **Settings > Connections** to define Additional Fields, then return here to select the appropriate field.

If the field is empty or 0 for a feature, it will fall back to the default/historical feature size.

## State Override
Sometimes we may have child items already, but we are in the process of still refining. So instead of using the 3 child items that are already there, we may still want to use the estimate (which may say 12 for a feature) for some time.
In order to achieve this, you can define which states should ignore the real child items. For those states, the real number will **always** be ignored, and the estimate or default size will be used.

## Visual Indication
In the [Portfolio Detail](./detail.html) and [Team Detail](../teams/detail.html) pages you will see a visual indication for every item that is using the default/estimated size.
That helps you identifying where you have pending work to refine the features.

# Ownership Settings
Lighthouse will forecast the portion of work for each team individually. If we have child items, they belong to a team and it's clear how this is done.
However, if we use the default size, this does not work like that (as the work does not yet exist). In order to forecast, Lighthouse will assume that the work will be evenly split between all involved teams.

{: .note}
If your portfolio is only having one involved team, these settings can safely be ignored, as nothing will effectively change. Only if there are two or more teams this may be relevant.

Usually a split between all involved teams equally will not reflect your reality. So what you can do is to specify more details.

## Owning Team
You can pick the *Owning Team* from all teams that are involved in the portfolio. The owning team will get *all* the default work assigned to them, instead of it being distributed among the teams.
This makes sense if you have one team that is driving most topics, while some other team(s) are involved occasionally only.

If defined, the owning team will be used unless a [Feature Owner](#feature-owner-field) is defined.

## Feature Owner field
You can specify a Feature Owner field that contains information about a team that owns a Feature.

{: .note}
The Feature Owner Field is configured by selecting an Additional Field that has been defined on your Work Tracking System connection. Go to **Settings > Connections** to define Additional Fields, then return here to select the appropriate field.

If defined, Lighthouse will get the data in this field for every feature. It will then check if the name of any team is included in the data (so it does not need to **match**, it just needs to be **contained**).

{: .note}
This means you may have to specify your teams exactly as you do it in your work tracking system.

Lighthouse will use the **Feature Owner** to assign the work to a specific team. If not defined, it will fall back to the **Owning Team** of this portfolio. If this is neither defined, it will distribute the work across all the teams.

# Flow Metrics Configuration
Under this section, you'll find all configuration options related to Flow Metrics.

## System WIP Limit
If you want to work with WIP limits, Lighthouse allows you to specify one for the overall system you are working in. This configuration has **no** impact on any forecasting, but it will show up in your metrics section and helps you identify if you are above or below your limit. If set, the limit will show up:
- In the header of a team
- In the Work In Progress Widget as a *Goal*
- In the WIP Run Chart as a horizontal line

Lighthouse is not allowing you to set state specific limits, it will check everything that is in progress, independent of the states (but based on your [state configuration](#states)).

{: .note}
A WIP limit is not just a maximum you should not exceed. It is what we think is our optimum capacity. Meaning that, while we should not exceed it, we should also not be below it, as this means, we're not running at our optimum. While we should take natural variability into account, we should aim to be **at** the limit we set, and neither below nor above.


## Service Level Expectation
You can enable a Service Level Expectation (SLE) for your teams. If you do, you must define a *probability* and a range in days. You can use this for forecast single item delivery and to communicate with your stakeholders, as well as to inspect your delivery performance.

If you enable the SLE, you will get additional information in the [Metrics View](../metrics/metrics.html).

## Blocked Items
You can configure how you identify blocked items in your process. Lighthouse offers two options for this.

### Tags
If you use tags (*labels* in Jira), you can specify which tag mean that an item is blocked. You are free to define whatever and how many tags you like.

{: .note}
Jira note: If you use Jira's built-in `Flag` functionality, add `Flagged` to the blocked tags in Lighthouse and flagged issues will be detected and visualized as blocked.

### States
You can also specify states that indicate a blockage of your work. You can pick any state that you defined as [doing state](#states).

{: .important}
We do not recommend using states for identifying blocked work. Tags work better from a Flow perspective. [More Details here](https://www.prokanban.org/blog/whats-wrong-with-having-a-blocked-column).

## Process Behaviour Chart Baseline
Lighthouse can show **Process Behaviour Charts (PBC)** in the Metrics view. To enable them, configure a **baseline start** and **baseline end** date.

{: .important}
If no baseline is set, Lighthouse will **not** show Process Behaviour Charts.

The baseline is used as the reference period to calculate the average and natural process limits and to highlight special-cause signals.

See [Process Behaviour Charts](../metrics/widgets.html#process-behaviour-charts) for examples and screenshots.

# Advanced Configuration
There are a few options that are optional. This means that they have an impact, but you can save a portfolio without bothering.

## Parent Override Field
By default, Lighthouse uses the native parent-child relationships from your work tracking system to determine which work items belong to which features.

If you need to override this behavior (for example, to group work items under a custom field instead of the native parent link), you can select a **Parent Override Field**. This field must be defined as an Additional Field on your Work Tracking System connection first.

{: .note}
Go to **Settings > Connections** to define Additional Fields, then return here to select the appropriate field for parent override.