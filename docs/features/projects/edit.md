---
title: Create/Edit Projects
parent: Projects
grand_parent: Features
layout: home
nav_order: 2
---

Whether you want to create a new project or edit an existing one, you can use this page to specify all the details that make up your project.

- TOC
{:toc}

# Validation and Save
Before you can save a new or modified project, you'll have to *Validate* the changes. Lighthouse will run a query against your specified work tracking system and make sure that the query is successfully executing. Only after this you will be able to save.

The validation checks the following things:
- The connection to the work tracking system is valid (the connection settings are ok)
- The query can be executed (the query is having a correct syntax)
- The query returns at least one feature

If the validation is ok, you are good to save the changes.

# General Configuration
The general information contains the name of your project. This can be anything that helps you identify it, for example the name of a specific release, customer project, or iteration you want to track.

## Work Item Query
The Work Item Query is the query that is executed against your [Work Tracking System](../../concepts/concepts.html#work-tracking-system) to get the features relevant for the project. The specific syntax depends on the Work Tracking System you are using.

See the [Jira](../../concepts/jira.html#projects) and [Azure DevOps](../../concepts/azuredevops.html#projects) specific pages for details on the query.

# Work Item Types
Independent of the [Work Item Query](#work-item-query), Lighthouse needs to know which item types your Features have. Thus you can define the item types that should be taken into account for this specific project.

{: .recommendation}
Common examples for item types on project level are "Epic" in Jira and Azure DevOps, and additionally "Feature" for Azure DevOps.
Check [Default Project Settings](../settings/defaultprojectsettings.html) to see how to adjust default values for every newly created project.

You can remove types by hitting the remove icon, and add new ones by typing them in and hit *Add Work Item Type*.

{: .note}
You have to type the exact type name as it's used in your Work Tracking System. Make sure to use the exact spelling and casing. Spaces (for example in 'User Story') are supported.  
While you can add multiple types, usually it makes sense to focus on a single one. Either use "Epics" or "Features", but dont mix them up.

# Involved Teams
Lighthouse will look for child items of the features for this project in all team backlogs of the involved teams. Here you can specify which teams are involved in this project. It will list all teams that are defined already and you can select as many as you want. You must select at least one team.

{: .note}
You can also select teams that do not have concrete work for any feature of this project yet. That will allow you to define this team as a [Feature Owner](#ownership-settings) and will save you having to go modify the project later on.

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

Each connection has a specific name and a type. Depending on the type, different configuration options have to be specified. Check the detailed pages on [Jira](../../concepts/jira.html#work-tracking-system-connection) and [Azure DevOps](../../concepts/azuredevops.html#work-tracking-system-connection) for details.

# Milestones
Milestones define important dates in your project. This could be special events (like a fair or a customer visit), milestones in your project like (like a deployment or code freeze date), or simply based on iterations (like your Sprint Reviews).
A milestone has a name and a date, and when you define it, the likelihood of getting this feature done by the milestone is showin in the overview.

This is beneficial for tracking your progress and helps with transparently communicating whether we can make certain dates or not.

{: .note}
Milestones are optional. You don't need to set any, and you can always add them later on in the [Project Detail View](./detail.html).

# Unparented Work Items
Sometimes (or shall we say "in the real world") there is work that is not belonging to a specific feature. Still it neeeds to get done.
These may be small improvements that were planned (or promised) or some bug fixes.

You could of course add some container feature (or even multiple) with the sole purpose of adding everything in this bucket.
However, if you use a dedicated workflow for features (which we highly encourage), and your feature should be scoped and provide value, that is often at odds with such "containers".

What Lighthouse offers instead is to collect all work items that are not having a feature parent into a "virtual" unparented feature that is also taken into the forecast.
All you need to do for this is to create a query that searches for the work items in the individual team backlogs and Lighthouse will group all of them together and show them as "Unparented Items" for your projects.

The query will only include items that are not already part of this project (under a 'regular feature').

An example could look like this:

```bash
[System.Tags] CONTAINS "My Release"
```
```bash
labels = "My Release"
```

That will go through all [Involved Teams](#involved-teams) backlog items, and check if any work item is matching the query.
If yes, and if those items are not yet added as part of a feature, it will be counted to the "Unparented Feature".

{: .note}
If you have multiple teams, all unparented work will be added to this Feature. Also the unparented Feature will be at the bottom of the priority, so it is always assumed that it will be done last.

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
Instead of a default size, you can also chose to let Lighthouse calculate your historical feature size. If you do so, you have to define a percentile and a query.

The percentile is a number between 50 and 100. If you chose for example 80, it means that it uses the size that 80% of your Features had. If you go for 50, it uses the size that at least 50% of your features had.
The features that are used for this calculation are based on the query you specify. The query will match the same [Work Item Types](#work-item-types) that you defined above (so no need to filter this again). Apart from that, you are free to filter however you want.

{: .recommendation}
We suggest that you filter based on a date. So for example you look at the last 180 days. That will make the system "self-learning" as you use a sliding window and not a fixed set of features.

## Estimated Size
On top of the default size, which will be similar for any feature, you can also specify a field that would include an estimate. This allows you to use the default size for features you have not looked at at all, while providing more details for some things that you may have started to refine, but have no child items yet.
Simply specify the name of the field that contains your estimate, and Lighthouse will extract the data from your work tracking system.

If the field is empty or 0 for a feature, it will fall back to the default/historical feature size.

## State Override
Sometimes we may have child items already, but we are in the process of still refining. So instead of using the 3 child items that are already there, we may still want to use the estimate (which may say 12 for a feature) for some time.
In order to achieve this, you can define which states should ignore the real child items. For those states, the real number will **always** be ignored, and the estimate or default size will be used.

## Visual Indication
In the [Project Detail](./detail.html) and [Team Detail](../teams/detail.html) pages you will see a visual indication for every item that is using the default/estimated size.
That helps you identifying where you have pending work to refine the features.

# Ownership Settings
Lighthouse will forecast the portion of work for each team individually. If we have child items, they belong to a team and it's clear how this is done.
However, if we use the default size, this does not work like that (as the work does not yet exist). In order to forecast, Lighthouse will assume that the work will be evenly split between all involved teams.

{: .note}
If your project is only having one involved team, these settings can safely be ignored, as nothing will effectively change. Only if there are two or more teams this may be relevant.

Usually a split between all involved teams equally will not reflect your reality. So what you can do is to specify more details.

## Owning Team
You can pick the *Owning Team* from all teams that are involved in the project. The owning team will get *all* the default work assigned to them, instead of it being distributed among the teams.
This makes sense if you have one team that is driving most topics, while some other team(s) are involved occasionally only.

If defined, the owning team will be used unless a [Feature Owner](#feature-owner-field) is defined.

## Feature Owner field
You can specify a feature owner field that contains information about a team that owns a Feature. Potential fields may be *[System.AreaPath]* and *[System.Tags]* for Azure DevOps, and *labels* for Jira.
If defined, Lighthouse will get the data in this field for every feature. It will then check if the name of any team is included in the data (so it does not need to **match**, it just needs to be **contained**).

{: .note}
This means you may have to specify your teams exactly as you do it in your work tracking system.

Lighthouse will use the **Feature Owner** to assign the work to a specific team. If not defined, it will fall back to the **Owning Team** of this project. If this is neither defined, it will distribute the work across all the teams.