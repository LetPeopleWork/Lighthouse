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


# Unparented Work Items

# Default Feature Size

# Ownership Settings
