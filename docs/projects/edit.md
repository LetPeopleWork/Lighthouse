---
title: Create/Edit Projects
layout: home
parent: Projects
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

See the [Jira](../../concepts/worktrackingsystems/jira.html#projects) and [Azure DevOps](../../concepts/worktrackingsystems/azuredevops.html#projects) specific pages for details on the query.

# Work Item Types
Independent of the [Work Item Query](#work-item-query), Lighthouse needs to know which item types your Features have. Thus you can define the item types that should be taken into account for this specific project.

{: .recommendation}
Common examples for item types on project level are "Epic" in Jira and Azure DevOps, and additionally "Feature" for Azure DevOps.
Check [Default Project Settings](../settings/defaultprojectsettings.html) to see how to adjust default values for every newly created project.

You can remove types by hitting the remove icon, and add new ones by typing them in and hit *Add Work Item Type*.
