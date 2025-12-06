---
title: Teams
layout: home
has_children: true
nav_order: 10
---

The *Overview* page shows you all teams that are defined. As the name suggests, to give you an overview over everything that is going on. You can do the following things from this page:

- TOC
{:toc}

![Team Overview](../assets/features/overview.png)

# Gain an Overview
The overview, as the name suggests, shows you a rough overview over all the teams. You can see which teams are defined, and how many Features they are contributing to (based on the the features in all defined portfolios).

# Create a new Team
If you want to create a new team, click on the *Add New* button on top. That will bring you to the [Team Creation Page](./edit.html).

# Navigate to the Team Details
If you want to see more details for a specific team, either click on the ℹ️ icon on the right side of the team or the team name to navigate to the [Team Details](./detail.html).

# Modify an existing Team
If you want to modify the settings of an existing team, click on the 🖊️ icon on the right side of the team. This will bring you to the [Team Modification Page](./edit.html).

# Delete a Team
If you want to delete a team, you can click on the 🗑️ icon on the right side of the team. This will permantenly delete this team.

![Team Deletion](../assets/features/teams_delete.png)

## Cloning Teams

You can clone an existing team to create a new team with the same settings. When cloning a team:
* All team settings (work item query, work item types, states, throughput settings, etc.) are copied
* The new team's name is prefixed with "Copy of " to distinguish it from the original
* Only the settings are copied - relationships like portfolio assignments are not automatically copied
* The cloned team is created as a new, independent team

To clone a team, click the Clone button in the team overview page. This will open the team creation page pre-filled with the original team's settings.

{: .important}
Deleting a team will remove it from Lighthouse with no functionality to undo this change. This will also remove the team from all the involved portfolios. Ideally you make sure that this team is not involved in any portfolios before you delete it.

{: .note}
You can always add a team again using the same queries. Deleting a team has no effect at all on the data in your Work Tracking System.

# Constraints in the Free Version
If you are using the free version, you are restricted to three Teams on a single Lighthouse instance.


{: .important}
If you have more than the allowed number, Lighthouse will prevent any updates to all teams until you are not above the maximum number.
