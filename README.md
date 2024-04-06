# Lighthouse
Lighthouse is a tool that helps you run probabilistic forecasts using Monte Carlo Simulations in a continuous and simple way.
It connects to your Work Tracking Tool (currently Jira and Azure DevOps are supported) and will automatically update your teams Throughput and your projects forecasted delivery dates.

You can use it with a single team for doing manual "When" and "How Many" forecasts, as well as for tracking projects with one or multiple teams.

Lighthouse is provided free of charge as open source software by [LetPeopleWork](https://letpeople.work). If you want to learn more about the tool, what we can offer you and your company, or just want to chat, please reach out.

<Image Here>

# Disclaimer
The current version is a very early version, that is used to collect feedback. Functionality and User Experience are not final yet and will likely change going forward.
Do you want to try it out? Reach out to us, we're happy to support you and get your feedback.

# Installation
Lighthouse is a web application, that is foreseen to run on a server where multiple people have access to it. You can however run it also on your local machine. This might be the preferred option for now, as there is no User Management, nor any authentication/authorization at this point in time.

## Prerequisites
The application is based on dotnet. Thus you must have installed the [ASP.NET Core Runtime 8.0.3](https://dotnet.microsoft.com/en-us/download/dotnet/8.0). Select the version that matches your system (Windows, macOS, Linux).
<Image Here>

After you installed it, open a terminal, and type `dotnet --version`. It should show 8.0.3:
<Image Here>

## Download Lighthouse
Download the latest version of Lighthouse from the [Releases](https://github.com/LetPeopleWork/Lighthouse/releases/latest).
Download the zip file, and extract it to the location you want to run the application from.

## Start Lighthouse
Once extracted, you can open a terminal on this location. Then you can start the application by running `dotnet Lighthouse.dll`
<Image Here>

This will start the application running on the system on port 5000. If everything worked as expected, you can open the app now in your browser via [http://localhost:5000](http://localhost:5000)
<Image Here>

# Usage
When you browse to the Lighthouse Landing Page, you'll see an empty page (unless you've configured teams and projects already). The overview page will show you a summary of all configured projects and their estimated delivery dates in various probabilites.

<Image Here>

## Configuration
Before you can do anything with Lighthouse, you have to configure it. There are two things you can configure:
1. Teams
2. Projects

In order to add a project, you need at least one team that contributes to this project.

### Work Tracking System Configuration
When you create Teams and Projects, you need to specify against which Work Tracking System you are working. Currently supported are Azure DevOps and Jira.

#### Azure DevOps
When you connect to Azure DevOps, your queries have to follow the Work Item Query Language (WIQL) as described by Microsoft: [Work Item Query Language (WIQL) syntax reference](https://learn.microsoft.com/en-us/azure/devops/boards/queries/wiql-syntax?view=azure-devops).

In order to connect, you need to have the Url of your Azure DevOps organization. If you work in the cloud, this looks something like this: `https://dev.azure.com/letpeoplework` where `letpeoplework` would be your organization name. You don't need to specify any Team Project, this will be part of the query.
On top of that, you need to specify a [Personal Access Token](https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows). You need read permissions for the Work Items scope.

If you have this, you are all set.

#### Jira
When you connect to Jira, your queries have to follow the Jira Query Language (JQL), as described here: [JQL: the most flexible way to search Jira](https://www.atlassian.com/blog/jira-software/jql-the-most-flexible-way-to-search-jira-14).

In order to connect, you need also to have the Url of your Jira instance. This looks something like this: `https://letpeoplework.atlassian.net` where `letpeoplework` is your instance name.
On top of that, you need to create an Api Token for a dedicated user, and supply both the username as well as the access token. You can find more information on how to create an Access Token here: [Manage API tokens for your Atlassian account](https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/)

### Create Teams
Go to the "Teams" section via the banner and click on "Create new Team". You'll be presented with the following dialog:
<Image Here>

Following are the configuration options:

| Name                   | Description                                                                                         |
|------------------------|-----------------------------------------------------------------------------------------------------|
| Team Name              | Name of your team                                                                                   |
| Feature WIP            | How many features the team works on in parallel                                                     |
| History in Day         | How much historical data should be looked at for this team. Don't use too high values, the suggested value is 30 |
| Work Item Query        | The JQL or WIQL query to get the team's backlog. This query should return the items of the specified types, both closed and still open |
| Work Item Types        | Which Work Item Types you care about for this team. Dependent on your work tracking system and way of working |
| Additional Related Field | Work In Progress field, you can ignore for now                                                    |
| Work Tracking System   | Choose if you work against Jira or Azure DevOps. Based on this selection, you'll see more detailed options to connect to your respective work tracking system. |

#### Example Queries
Following are a few example queries for both Azure DevOps and Jira.

This WIQL query will select all items in the Team Project called "Lighthouse Demo" and that are within the Area Path "Lighthouse Demo\Binary Blazers":
```
[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\Binary Blazers"
```

JQL Query:
```
TODO
```

#### Save Team and Update Throughput
After you've set everything up for your team, you can hit "Edit Team" and then "Update Throughput". If your configuration was correct, the Throughput will update and show in a graph at the bottom of the page.
Once you are able to get a teams throughput, you can make Team Level forecasts. See below for more information.

<Image Here>

### Create Projects
Once you have created one or more teams, you can add projects those teams are contributing to. A project is a set of work items (usually "higher-level items" like Epics) for which you want to forecast completion dates.
As part of the project creation, you need to specify how to collect all the work items you are interested in. Then the remaining work will automatically collected by the existing teams. This works via the relationships to those items you have set up in your work tracking system. So if you have an Epic with 20 child items, of which 8 are already done, the remaining work will be 12 items.

Projects can involve one or many teams. The remaining work for each work item can also be either for a single team or there might be work for many teams. This is all taken into account by the forecast.

When you create project, you can see the following dialog:

<Image Here>

These are your configuration options:

| Name                      | Description                                                                                         |
|---------------------------|-----------------------------------------------------------------------------------------------------|
| Project Name              | The name of your project                                                                            |
| Work Item Types           | Which work item types your projects consist of                                                        |
| Milestones                | Optionally add milestones for your project. If you add a milestone, you'll get a probability on how likely it is that you manage to complete each of your work items till this date. |
| Work Item Query           | The JQL or WIQL query to get the project related items. This query should return the items of the specified type(s) |
| Unparented Items Query    | Optionally you can specify a query on how to identify items that belong to the project in each team's backlog, but don't have a relationship to the higher level items |
| Default Work Items per Feature | If the work items are still open but have no remaining work, it could mean that they are not broken down. In that case, the following number of items is assumed as remaining work |
| Work Tracking System      | Choose if you work against Jira or Azure DevOps. Based on this selection, you'll see more detailed options to connect to your respective work tracking system. |

#### Example Queries
Following are a few example queries for both Azure DevOps and Jira.

This WIQL query will select all items in the Team Project called "Lighthouse Demo" and that contain the tag "Release 1.33.7":
```
[System.TeamProject] = "Lighthouse Demo" AND [System.Tags] CONTAINS "Release 1.33.7"
```

JQL Query:
```
TODO
```

#### Save Project and Refresh Features & Forecasts
After you're done with your configuration, you can save your project. On the following page, you can Refresh your features and forecasts for the projects.
If everything was configured correct, you will see all features of your project, together with the forecasted completion dates and likelihoods (if you configured milestones).

<Image Here>

## Continuous Updates
To make most use of the tool, it will automatically refresh the Throughput from each team as well as the features for a project and the forecasts.
Every x minutes it will check whether it's time to update the values. It will update them, if the last update was longer ago than configured.

Following are the default values:

| What       | Interval | RefreshAfter |
|------------|----------|--------------|
| Throughput | 60       | 360          |
| WorkItems  | 60       | 360          |
| Forecasts  | 20       | 120          |

This means that:
- Throughput is checked every 60 minutes, and will update automatically if the last update was more than 360 minutes ago
- Work Items for a Project will be checked every 60 minutes, and will update automatically if the last update was more than 360 minutes ago
- Forecasts will be checked every 20 minutes and will update if the last update was more than 120 minutes ago

You can change those values by modifying the file *appsettings.json* in the Lighthouse folder. After a restart of the application, these values will be taken into account.

## Team Details
You can see all your configured teams if you hit the Teams link on the top bar. You can either edit them, or see the details.
<Image Here>

If you check the details, you can see all the features the team is contributing to in any of the configured projects. On top of that, you can also run "Team Level Forecasts".

<Image Here>

### When Forecasts
If you want to know when a specific amount of items will be done for this team, you can enter this in "Remaining Items" and click Forecast. This will show you various probabilities on when you can expect this amount of items to be completed by the selected team.

<Image Here>

### How Many Forecast
If you want to know how many items will be doable till a specific date (for example for your Sprint-, PI-, or Quarterly Planning), you can specify this date and you'll see various probabilities on how many items this team might complete:

<Image Here>

## Project Details
Similar to the Teams, you can also see all your configured Projects by simply clicking on the Projects link in the top bar. You can either edit them, or see the details.

<Image Here>

If you check the details, you can see all the features that are left for this project, together with the completion dates of different probabilities. If you have milestones configured, you'll also see the probability of hitting those milestones.
Furthermore you'll see the all involved teams in this project and a visual representation of your feature completion dates (85% probability) and your milestones.

<Image here>