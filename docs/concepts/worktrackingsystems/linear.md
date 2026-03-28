---
title: Linear
layout: home
nav_order: 20
parent: Work Tracking Systems
grand_parent: Concepts
---

This page will give you an overview of the specifics to Linear when using Lighthouse. In detail, it will cover:  

- TOC
{:toc}

# Work Tracking System Connection
To create a connection to a Linear Workspace, you need a single thing: A Linear API Key
  
<!-- TODO: Add automatically generated picture once the integration is fully implemented -->
![Create Linear Connection](../assets/concepts/worktrackingsystem_Linear.png)

You can find more information on how to create an API Key in the [Linear Documentation](https://linear.app/docs/api-and-webhooks#create-an-api-key)

{: .important}
The API Key shall be treated like a password. Do not share this with anyone or store it in plaintext. Lighthouse is storing it encrypted in its database (see [Encryption Key](../installation/configuration.html#encryption-key) for more details) and will not send it to any client in the frontend.

# Team Backlog
When you create a new team in Lighthouse that uses a Linear connection, you can select a Linear team from a wizard that lists all teams available in the connected workspace.

Lighthouse will automatically fetch all issues for the selected team. Work item types are fixed to *Issue* — you do not need to configure item types manually.

# Portfolios
Linear portfolios retrieve all **projects** from the authenticated workspace as Lighthouse features. No query or work item type configuration is required.

Each Linear project becomes a Lighthouse feature, and its issues roll up as work items. If a project is linked to a Linear **initiative**, Lighthouse will resolve that initiative as the parent feature.

# Hierarchy
Lighthouse maps the full Linear hierarchy:

| Linear Concept | Lighthouse Concept |
|---|---|
| Issue | Work Item (team level) |
| Project | Feature (portfolio level) |
| Initiative | Parent Feature |

Issues are associated with the project they belong to. If an issue does not have a direct project association, Lighthouse checks the issue's parent chain to find the project. Projects linked to initiatives will display the initiative as a parent feature with its name, status, and URL fetched from the Linear API.

# States
The states correspond to **Issue statuses** in Linear. Make sure to specify all statuses you care about. As an example, if you have the following states:
- Backlog
- Planned
- Development
- Done
- Canceled

You can configure them as follows:
- *To Do*
  - Backlog
  - Planned
- *Doing*
  - Development
- *Done*
  - Done

States you don't need (e.g. *Canceled*) can be left out. Items in unmapped states will not be tracked by Lighthouse and will not affect your metrics.

# Feature Order
The order of features is based on the ordering you set in Linear. To change this, you can [manually reorder](https://linear.app/docs/display-options#manual-ordering).