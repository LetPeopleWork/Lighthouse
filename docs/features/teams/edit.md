---
title: Create/Edit Teams
parent: Teams
grand_parent: Features
layout: home
nav_order: 2
---

Whether you want to create a new team or edit an existing one, you can use this page to specify all the details that make up your team.

- TOC
{:toc}

# Validation and Save
Before you can save a new or modified team, you'll have to *Validate* the changes. Lighthouse will run a query against your specified work tracking system and make sure that the query is successfully executing. Only after this you will be able to save.

The validation checks the following things:
- The connection to the work tracking system is valid (the connection settings are ok)
- The query can be executed (the query is having a correct syntax)
- The query returns at least one closed item (we must have a *Throughput* in order to forecast, so we need at least one closed item)

If the validation is ok, you are good to save the changes.

# General Configuration
The general information contains the name of your team. This can be anything that helps you identify it.

{: .recommendation}
We suggest to use the same name as you use in your work tracking system to identify your team.

## Throughput
Throughput is the base of any forecast Lighthouse is making. For every team, you can decide whether you want to use *dynamic* throughput that looks at the last number of days and is every day updating, or one that is using a fixed period of time.

{: .recommendation}
We highly recommending using a dynamic Throughput over a fixed one. The fixed dates might help to overcome special situations *temporarily*. In general, you will get less accurate results with fixed dates, as your teams throughput will change over time, and Lighthouse will not take this into account if the Throughput will be always looking at the exact same period of time.

### Throughput History
This is the number of days of the past you want to include when running forecasts for this team.
In general this should be more than 10 days, and represent a period where this team was somewhat working in a stable fashion.

{: .recommendation}
We recommend using a value between 30 and 90 days. Fewer and it might be too sensitive to outliers. And more than three months is often too far away for being useful.

### Throughput Start and End Date
If you use a *fixed* Throughput, you must specify a start and end date. The end date cannot be in the future, and the start date must be at least 10 days before the selected end date. This is because we must have at least 10 data points to create a decent forecast.

{: .note}
As mentioned above, use a *fixed* Throughput with caution, and ideally only temporarily. Examples where it may be useful is if most of the team is off for some time (for example if the offices are closed for a week or more, like it happens for some companies in the Christmas period). As soon as you have enough data after this period again, we encourage you to switch back to the *dynamic* Throughput.

## Work Item Query
The Work Item Query is the query that is executed against your [Work Tracking System](../../concepts/concepts.html#work-tracking-system) to get the teams backlog.
The query should fetch all items that "belong" to this team and the specific syntax depends on the Work Tracking System you are using.

See the [Jira](../../concepts/worktrackingsystems/jira.html#team-backlog) and [Azure DevOps](../../concepts/worktrackingsystems/azuredevops.html#team-backlog) specific pages for details on the query.

# Work Item Types
In order to properly forecast, Lighthouse needs to know which items your team works on that are relevant for the forecast. Thus you can define the item types that should be taken into account for this specific team.

{: .recommendation}
Common examples for item types on team level are "User Story", "Bug", and "Product Backlog Item" for Azure DevOps, and "Story" as well as "Bug" for Jira.  
Check [Default Team Settings](../settings/defaultteamsettings.html) to see how to adjust default values for every newly created team.

You can remove types by hitting the remove icon, and add new ones by typing them in and hit *Add Work Item Type*.

{: .note}
You have to type the exact type name as it's used in your Work Tracking System. Make sure to use the exact spelling and casing. Spaces (for example in 'User Story') are supported.

# States
In order for Lighthouse to judge whether an item is *done*, *in progress*, or not even started, you must specify which *states* map to which *category*.

| State Category | Description |
|-------|-------------|
| To Do | Items that are in this state are discovered as *pending* for this team. It is important to have all those states mapped as so Lighthouse can discover pending work for features in a project. |
| Doing | Items that are in this state are actively being worked on. Lighthouse will mark features as *In Progress* based on these states. |
| Done | Items that are done contribute to the Throughput. Based on this value forecasts are made. |

You don't have to add every state to one of the categories. For example you might have a *Removed* or *Canceled* state, which is not mapping to any of the categories. You don't have to specify it, then the item will not be existing for Lighthouse (only items that are in any of the mentioned states are discovered by Lighthouse).

{: .recommendation}
For Azure DevOps, the common states are: *New* or *Backlog* (To Do), *Active*, *Resolved*, *Comitted* (Doing), and *Closed* or *Done* (Done).  
For Jira, the common states are: *To Do* (To Do), *In Progress* (Doing), *Done* (Done).

{: .important}
While Azure DevOps can handle if you specify states that don't exist, Jira will not execute a query with a state that is not in its system. That means for Jira you have make sure everything you mention does exist exactly as specified, as otherwise the [Validation](#validation-and-save) will fail.

# Work Tracking System
In order for Lighthouse to get the data it needs for forecasting, it needs to connect to your Work Tracking System. Work Tracking Systems are stored in the Lighthouse Settings and can be reused across Teams and Projects.

When creating or modifying both Teams or Projects, you can either choose an existing connection or create a new one.

Each connection has a specific name and a type. Depending on the type, different configuration options have to be specified. Check the detailed pages on [Jira](../../concepts/worktrackingsystems/jira.html#work-tracking-system-connection) and [Azure DevOps](../../concepts/worktrackingsystems/azuredevops.html#work-tracking-system-connection) for details.

# Tags
Tags allow you to add any kind of additional information that may be helpful for you to identify this team. This may be a specific project or initiative, a department, business unit, or tribe, or anything else that somehow might be useful. You can add as many tags as you want. Existing tags will be shown as proposal.

Tags are checked when you use the search functionality.

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

## Feature WIP
If your team is working on multiple Features at the same time, you want to adjust the Feature WIP to this number. This will impact your forecasts for projects, and will lead to different predicted delivery times.

{: .note}
Working on one Feature at a time does not mean only having one item in progress. It means that all items that are in progress belong to the same feature.

As an example, if you work on a single feature at a time, this feature will be done as fast as possible. If you work on two features, the first feature will finish later (as at least some of the teams effort goes to the second feature). In an ideal world, you have a Feature WIP of one. Your reality might look different, and that's ok. Just know that ideally you should strive to be as close as possible to a Feature WIP of one.

### Automatically Adjust Feature WIP
You can set your Feature WIP to anything you like, there does not need to be a correlation to what is actually happening (although obviously it would be good if the number reflects reality...). If you tick this box, Lighthouse will automatically adjust the Feature WIP with every [Team Data Update](./detail.html#update-team-data) to the number of Features that are right now being worked on by this team.

{: .recommendation}
Tick this box if you want to increase transparency. A higher Feature WIP will lead to *late delivery* of many features. Not everyone will like this. It may be a good way to show why we should use focus (and Lighthouse might give you the underlying data for that). Keep fighting the good fight!

## Service Level Expectation
You can enable a Service Level Expectation (SLE) for your teams. If you do, you must define a *probability* and a range in days. You can use this for forecast single item delivery and to communicate with your stakeholders, as well as to inspect your delivery performance.

If you enable the SLE, you will get additional information in the [Metrics View](../metrics/metrics.html).

# Advanced Configuration
There are a few options that are optional. This means that they have an impact, but you can save a team without bothering.

## Parent Override Field
In order to establish a relation between two work items, Lighthouse assumes that the Feature is set as a parent for the work item.
If this is not the case, you can specify an additional field that is containing the ID of the Feature in the Teams Work Items. That way, you can let Lighthouse know how the relation between Feature and Work Items are established.

{: .note}
For Jira, you need to figure out the [ID] of your customfield, check the [Atlassian Documentation](https://confluence.atlassian.com/jirakb/find-my-custom-field-id-number-in-jira-744522503.html) to understand how to do this. Once you know the id, you must specify the customfield like this: `cf[id]`.
For example, if your customfield id is 1886, you would specify `cf[1886]` as value.

You may wonder "Why would use another way of specifying a parent-child relationship than the built in ones?". Well, corporations sometimes do things for reasons beyond our understanding. We're not here to judge 🤷‍♂️  
In theory this would allow you to also *cross-link* Jira and Azure DevOps items (but please don't, we have not tested it, which is why we wrote *in theory*...).
It also turns out that on Jira Data Center versions you must have a link through a custom field (*Epic Link*).