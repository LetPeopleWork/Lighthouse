---
title: Jira
layout: home
nav_order: 2
parent: Concepts
---

This page will give you an overview of the specifics to Jira when using Lighthouse. In detail, it will cover:  

- TOC
{:toc}

# Work Tracking System Connection
To create a connection to a Jira system, you need three things:
- The URL of your Jira Instance
- The username for the user that will be used to connect to the Jira Instance
- The API token for this user

The URL will look something like this: `https://letpeoplework.atlassian.net` where *letpeoplework* is your instance name.  

You can find more information on how to create an Access Token in the [Atlassian Documentation](https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/)

{: .important}
The API Token shall be treated like a password. Do not share this with anyone or store it in plaintext. Lighthouse is storing it encrypted in its database (see [Encryption Key](../installation/configuration.html#encryption-key) for more details) and will not send it to any client in the frontend.

## Connecting to Jira Server/Data Center
The above description is true if you are working against a Jira Cloud instance. In case you are connecting to an on-premise Jira version (*Server* or *Data Center*), there are small differences.

Instead of an *API Token*, you have to provide a *Personal Access Token*. See the [Atlassian Documentation](https://confluence.atlassian.com/enterprise/using-personal-access-tokens-1026032365.html) for more details.

A *Personal Access Token* will not require you to specify a username, as it's part of the token itself. Thus please leave the username empty when you want to connect to a Jira Server/Data Center instance and provide a *Personal Access Token*.

{: .note}
If you do not leave the username field empty, Lighthouse will assume you try to connect to a Jira Cloud instance with an *API Token*, therefore the validation will fail!

{: .important}
As with the *API Token*, the *Personal Access Token* is treated like a password from Lighthouse.

# Query
Queries for Jira are written in [Jira Query Language (JQL)](https://www.atlassian.com/blog/jira/jql-the-most-flexible-way-to-search-jira-14). An example Query for a Team called "Lagunitas", where all issues for this Team are labeled with their team name, could look like this:

```
project = "LGHTHSDMO" AND labels = "Lagunitas"
```

You can use any kind of filtering you'd like and that is valid according to the JQL specification. An extended query that would exclude certain states would look like this:

```
project = "LGHTHSDMO" AND labels = "Lagunitas" AND status NOT IN (Canceled)
```

# Team Backlog
When you create a new team, you will have to define a query that will get the items that belong to the specific team backlog. The query should **not** specify *Work Item Types* (for example Story, Bug, etc.) nor specific *Work Item States* (like In Progress, Canceled), as those things will be specified outside the query.

{: .definition}
The work items we look for on team level are the ones that you plan with on that level. Often this would be *Stories* and *Bugs*. They should be delivering value and you should be able to consistently close them. *Subtasks* tend to be too detailed and technical (so they do not deliver value), while *Epics* may be too big (see [Projects](#projects) for more details on how to handle this). This is the general guidance, but your context might be different, so adjust this as needed.

What should be in there is everything else that defines whether an item is belonging to a team or not, like:
- Project (via *project*)
- Label (via *label*)
- Components (via *component*)
- Anything else that is needed to identify an item for your team, including custom fields if you have them

```bash
project = "LGHTHSDMO" AND labels in (Team B)
component = "Team A"
YourCustomField = "crypticValueThatIdentifesYourTeam"
```

{: .note}
The whole syntax of JQL is at your disposal. Remember, with great power comes great responsibility. Lighthouse will not be able to validate if what you write is making sense or not. There is a minimal verification on saving of a team, that makes sure that at least one item is found by the query. As long as that's the case, Lighthouse will assume it's correct.

# Projects
Projects are made up of items that have *child items* - in Lighthouse this is called a *Feature*. In a Jira context, this often means *Epics*. But it could be other (custom) types as well.

When creating a project, you need to specify a query that will fetch the features that are relevant for this project. This may be via:
- Release (*fixVersion*)
- Label (*label*)
- Whatever else may identify a feature to belong to a specific project

As with the [Teams](#team-backlog), you do **not** have to specify work item type and state in the query itself when defining the project.

Example JQL Queries for Projects could look like these:

```bash
project = "LGHTHSDMO" AND fixVersion = "Release GCZ 1886"
project = "LGHTHSDMO" AND labels = "Version 1.33.7"
```