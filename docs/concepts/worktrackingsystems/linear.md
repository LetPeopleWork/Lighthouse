---
title: Linear
layout: home
nav_order: 20
parent: Work Tracking Systems
grand_parent: Concepts
---

This page will give you an overview of the specifics to Linear when using Lighthouse. In detail, it will cover:  

{: .d-inline-block }
Preview
{: .label .label-green }

- TOC
{:toc}

{: .note}
The integration with Linear is in preview and currently only very limited functionality is available. We're eager for feedback from users that work on a regular base with Linear so we can learn how to improve the integration. Please get in contact with us if you are interested.

# Work Tracking System Connection
To create a connection to a Linear Workspace, you need a single thing: A Linear API Key
  
<!-- TODO: Add automatically generated picture once the integration is fully implemented -->
![Create Linear Connection](../assets/concepts/worktrackingsystem_Linear.png)

You can find more information on how to create an API Key in the [Linear Documentation](https://linear.app/docs/api-and-webhooks#create-an-api-key)

{: .important}
The API Key shall be treated like a password. Do not share this with anyone or store it in plaintext. Lighthouse is storing it encrypted in its database (see [Encryption Key](../installation/configuration.html#encryption-key) for more details) and will not send it to any client in the frontend.

# Query
Right now no queries are supported for Linear. Instead, Lighthouse assumes you'll specify either the *Team Name* or the *Project Name* as defined in Linear itself in the respective query sections. We want to gain more insights on how the Linear integration may be used (by people that work with Linear) before putting more effort into querying.

# Team Backlog
When you create a new team, you will have to define a query that will get the items that belong to the specific team backlog. As mentioned in [Query](#query), for Linear, please put the **exact name of your team** in the *Work Item Query* field.

Lighthouse will then automatically fetch all issues for this team together that match the [Type](#types) and [State](#states) definitions.

# Projects
Projects are made up of items that have *child items* - in Lighthouse this is called a *Feature*. In a Linear context, you'll also have Projects. Please specify the **exact name of your project** in the *Work Item Query* field. Lighthouse will fetch all items that match the [Type](#types) and [State](#states) definitions and are part of this project.

# Types
You can specify which item types you care about. As in Linear, all items are issues, this translates best to *Templates* that are applied. Add here the names of the templates you care about. Items that don't have a template will be treated as type of *Default*.

{: .note}
You must specify *Default* as a type, as otherwise Lighthouse will ignore all issues without a template

# States
The states are the *Issue status* - please make sure to specify all status that you care about in Lighthouse. The ones you don't need, you can skip. As an example, if you have the following states:
- Backlog
- Planned
- Development
- Done
- Canceled

You can specify it as follows:
- *To Do*
  - Backlog
  - Planned
- *Doing*
  - Development
- *Done*
  - Done

For Lighthouse, the *Canceled* state is irrelevant, so you can leave it. No items in this state will be found by Lighthouse and will not affect any metrics.

# Feature Order
The Order of Features is based on the ordering you do in Linear. In order to change this, you can [manually reorder](https://linear.app/docs/display-options#manual-ordering).