---
title: Release Notes
layout: home
nav_order: 95
---

<!--
# Lighthouse vNext
{: .d-inline-block }
Preview
{: .label .label-blue }
 -->

# Lighthouse v25.8.31.1120
## CSV Support
With this version it's possible to use CSV files as a base for Teams and Projects. While the recommendation is to use direct connections to your systems, this allows you to:
- Try out Lighthouse with static data
- Work around constraints in your environment if you don't have permissions for direct connections
- Use Lighthouse even though your work tracking system is not yet supported

You can find how to connect data with CSV as well as some examples on how to export CSV from common systems in the [documentation](https://docs.lighthouse.letpeople.work/concepts/worktrackingsystems/csv.html).

![CSV Connection](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/concepts/worktrackingsystem_CSV.png).

In the free version, you can add one team that is based on CSV data. In the premium version, you get unlimited teams and projects through CSV.

# Update Jira Endpoints
Atlassian is retiring some of their API endpoints that were used by Lighthouse. This release changes to the still supported APIs and ensures Lighthouse keeps working as expected.

**Important:** If you are using Jira, please make sure to upgrade to this version to ensure that Lighthouse will still be able to connect to your Jira Instances in future!

## Other Improvements
- Update of various third-party libraries

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Gonzalo Mendez](https://www.linkedin.com/in/gonzalo-mendez-nz/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.8.18.910...v25.8.31.1120)

# Lighthouse v25.8.18.910

## Customizable Dashboard
The Metrics Dashboard now allows you to customize it to your needs:
- Show/Hide Widgets
- Resize
- Rearrange

![Customizable Dashboard](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/releasenotes/ConfigurableDashboard.png)

See the documentation for more information.

## Performance Improvements
Various adjustements/fine tunigs for increasing performance. A significant increase in speed should be observable if you are running the containerized version. The standard install should also profit from the improvements.

## Other Improvements
- Date Range selection for Metrics has been adjusted to fit the new style and is now in the "Dashboard Header"
- Update of various third party libraries

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.8.13.702...v25.8.18.910)

# Lighthouse v25.8.13.702

## Licensing
With this version, Lighthouse has integrated [Licensing](https://docs.lighthouse.letpeople.work/licensing/licensing.html). Licenses are needed for various premium features, and in order to:
- Have more than 3 teams
- Have more than 1 project
- Be able to export and import a configuration.

You can find more details on the licensing on our [Website](https://letpeople.work/lighthouse#lighthouse-premium).

![License Info](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/licensing/licenseinformation.png)

## Feature Size Chart
On project level, you can now see how big your features are in size (in terms of child items) and see this on a scatterplot together with the Cycle Time of those respective features:

![Feature Size Chart](https://raw.githubusercontent.com/LetPeopleWork/Lighthouse/refs/heads/main/docs/assets/features/metrics/featuresize.png)

**This feature will be available in the community edition of Lighthouse**

### ⚠️ Breaking Changes ⚠️
As part of this change, we also simplified the configuration of the default feature size. For this, we **removed** the possibility to add your own query to filter for Features that should be used in the calculation. Instead, you can now specify a time (in days) that should looked back to fetch historical data that then is used for the percentile calculation.

## Improvements
- New Releases are now highlighted with a dialog as soon as it is detected (don't worry, you can disable that behaviour if it's annoying)
- Update of various third party libraries

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Mihajlo Vilajić](https://www.linkedin.com/in/mihajlo-v-6804ba162/)

A huge thank you goes out also to the [Let People Work Slack Community](https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A) that was actively providing feedback on our licensing plans. Your input was immensly helpful and supported us finding a path forward that should lead to a win-win situation, so that both we as a company and the broader community can profit!
 
[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.7.27.1729...v25.8.13.702)

# Lighthouse v25.7.27.1729

## Terminology Configuration
Lighthouse now allows you to customize terminology throughout the application to match your organization's language and workflow:

![Terminology Configuration](https://github.com/LetPeopleWork/Lighthouse/blob/main/docs/releasenotes/Terminology_Config.png?raw=true)

- **Configurable Terms**: Customize how Lighthouse displays common terminology including:
  - Work Item/Work Items (can be customized to Stories, Tasks, Issues, etc.)
  - Feature/Features (can be changed to Epic, Initiative, etc.)
  - Cycle Time, Throughput, Work in Progress (WIP)
  - Work Item Age, Service Level Expectation (SLE)
  - Teams, Work Tracking Systems, Queries, Tags, and Blocked items
- **System Settings Integration**: Access terminology configuration through *Settings* → *System Settings* → *Terminology*
- **Dynamic Updates**: Changes are applied immediately across the entire application interface
- **Default Values**: Each term shows its default value and description to help with configuration
- **Consistent Language**: Eliminates confusion by harmonizing terminology that was previously inconsistent (e.g., "Items" vs "Work Items")

![Terminology Applied](https://github.com/LetPeopleWork/Lighthouse/blob/main/docs/releasenotes/Terminology_Adjusted.png?raw=true)

This feature addresses feedback about terminology inconsistencies and enables organizations to use familiar language that aligns with their existing processes and tools.

## Bug Fixes
- Fixed issue that caused Work Items not to appear correctly if the state was in a different case than on the work tracking system

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Mihajlo Vilajić](https://www.linkedin.com/in/mihajlo-v-6804ba162/)
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)
- [Gabor Bittera](https://www.linkedin.com/in/gaborbittera/)
 
[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.7.26.915...v25.7.27.1729)

# Lighthouse v25.7.26.915
{: .d-inline-block }
Latest
{: .label .label-green }

## Improved Visual Indicators
We've added visual icons to make configuration status more apparent:
- Teams and Projects now display dedicated icons for System WIP Limits, Forecast Configuration, and Service Level Expectations
- Icons only appear when the respective configuration is enabled
- Hover over icons to see detailed tooltip information about the current configuration
- This replaces the previous dashed border visual indicators with cleaner, more intuitive icons

## Donation Support
We've added support for community donations through Ko-fi:
- A donation button is now available in the application footer
- Donation option is also included in the bug reporting and feedback dialog
- This provides an easy way for users who want to support the product's development

## Improved Feedback Collection
We've enhanced how users can provide feedback and report issues. Instead of redirecting to GitHub Issues, Lighthouse now displays a dedicated feedback dialog that:
- Provides clear guidance on how to submit feature requests and bug reports
- Directs users to our preferred Slack community for real-time feedback and discussions
- Offers email support as an alternative contact method
- Includes information about our review processes and custom development options

This change makes it easier for users to get help and ensures feedback reaches us through the most effective channels.

## Bug Fixes
- Fixed handling of work items with 0 days Age/Cycle Time to prevent calculation errors
- Log Level change was not reflecting in UI when using docker

## Other Improvements
- Updated various third party dependencies

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Gabor Bittera](https://www.linkedin.com/in/gaborbittera/)
 
[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.7.14.809...v25.7.26.915)

# Lighthouse v25.7.14.809

## Predictability Score
The Throughput Run Chart now includes a *Predictability Score*:

![Predictability Score in Throughput Run Chart](https://github.com/LetPeopleWork/Lighthouse/blob/main/docs/assets/features/metrics/throughputRunChart.png?raw=true)

The score tries to give you an indication of the predictability of the selected Throughput range when it's used for a forecast. It's showing how *close together* the 95% chance and 50% chance values are. The higher the score, the better the predictability.

When you click on the predictability chip in the Throughput Run Chart, you'll see more details:

![Predictability Score](https://github.com/LetPeopleWork/Lighthouse/blob/main/docs/assets/features/metrics/predictabilityscore.png?raw=true)

Please check the [docs](https://docs.lighthouse.letpeople.work/metrics/metrics.html#predictability-score) for more information on how this works and how you can potentially use it!

 
[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.7.7.1834...v25.7.14.809)

# Lighthouse v25.7.7.1834

## Visualize Blocked Items

![Blocked Items Widget](https://github.com/LetPeopleWork/Lighthouse/blob/main/docs/assets/features/metrics/blockedItems.png?raw=true)

Lighthouse now can visualize items that are blocked. Following changes were made:
- New widget that shows the total of blocked items
- The *Work Item Aging Chart* displays blocked items with a red dot
- Blocked items have a dedicated icon in the Work Item Dialog when you

![Blocked Items Dialog](https://github.com/LetPeopleWork/Lighthouse/blob/main/docs/assets/features/metrics/blockedItems_dialog.png?raw=true)

In order to configure blocked items, both Teams and Projects offer a new option under *Flow Metrics Configuration*.
You can specify tags (*labels* in Jira) or states that mark an item as blocked.

## Bug Fixes
- In certain cases, the validation for Projects was never completing. This should be fixed now.
- Some of the metrics in a project were cached but never invalidated, thus the value never updated. This is fixed.

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Lars Henning](https://www.linkedin.com/in/larshenning42/)
 
[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.7.6...v25.7.7.1834)

# Lighthouse v25.7.6

## Fixed disappearing states in Work Item Aging Chart
If you have many states, some of them would not show up in the x-axis of the Work Item Aging Chart.
This is adjusted now, and all states will always show, independent of available space.

**Note:** This may mean that some of the text is overlapping. This is known and accepted for the time being.

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Gonzalo Mendez](https://www.linkedin.com/in/gonzalo-mendez-nz/)
 
[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.7.5.1158...v25.7.6)

# Lighthouse v25.7.5.1158

## Work Item Aging Chart
We added a new chart to visualize *In Progress* work. In the *Work Item Aging Chart*, you'll see all ongoing work, where:
- The x-axis is showing the state the item is in currently
- The y-axis indicates how long it's in progress already (*Work Item Age*)

The states on the x-axis are in the order you defined them in the settings for the respective Team/Project. If you want to adjust the order, you can do so by drag and dropping states left/right when editing.

The chart behaves the same way as the Cycle Time Scatterplot:
- If multiple items are at the same location, the dot appears bigger
- You can click on each dot to get more details
- You can selectively show the percentiles from the Cycle Time and the SLE (if configured)

![Aging Chart](./WorkItemAgingChart.png)

## Automatic Download and Installation of New Versions
In order to simplify updating Lighthouse, you can now simply click a button which will automatically:
- Download the latest version for your operating system (Windows, Linux, MacOS)
  - **Note:** This is not supported if you're running docker
- Extracting the files in the directory you run Lighthouse from
- Restarting Lighthouse

This should help keeping up with the frequent releases, and make it easier also for less techy users to update (no need to fiddle with scripts or GitHub).

![Auto Update](./AutoUpdate.png)

**Note:** *If you experience issues, please reach out to us via [Slack](https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A), as it was not easy to test this in all potential configurations.*

## Other Improvements
- States can now be re-ordered when editing Teams and Projects through drag and drop. The order of the states is affecting the [Work Item Aging Chart](#work-item-aging-chart).
- When editing a team/project, it will **not** trigger an automatic update anymore after saving.
- Updated various third party dependencies

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)
 
[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.6.29.1252...v25.7.5.1158)

# Lighthouse v25.6.29.1252

## Allow to Group Features by Parent
So far, the Features listed for Teams/Projects was always a flat list. However, many teams have another layer "on top" of their feature that they track in Lighthouse.
With this release, it's now possible to visualize the Parent in this list. If the "Group Features by Parent" toggle is switched on, the flat list changes to a hierarchy, where items are sorted under their parent.

The parent is using the default parent field from your system. If you can't use this, you can now also define a *Parent Override Field* for your project (similar) to the teams.
*Note:* The field must contain an ID to an item in your Work Tracking System so Lighthouse can fetch more information for it.

![Features Grouped By Parent](./GroupByFeatures.png)

*As this is the first version of this feature - we're eager to get your feedback. Join our [Slack](https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A) and let us know how we can improve it!*

## Other Improvements
- The Footer now contains a link to the LetPeopleWork Offering Obeya
- Improved the speed of the Project Validation on Jira
- Renamed "Custom Related Field" to "Parent Override Field"

## Bug Fixes
- Opening Work Items from Lighthouse sometimes added a double "/" which made the url invalid (observed on Jira Data Center) - this should be fixed now.
- The legend for show/hide the System WIP Limit was sometimes overlapping the points in the chart. Aligned Legend now with other charts.

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Nina Wagen](https://www.linkedin.com/in/nina-wagen-04a9756a/)
- [Gonzalo Mendez](https://www.linkedin.com/in/gonzalo-mendez-nz/)
- [Agnieszka Reginek](https://www.linkedin.com/in/agnieszka-reginek/)
 
[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.6.16.1514...v25.6.29.1252)

# Lighthouse v25.6.16.1514

This release focused on many smaller improvements and bug fixes, many of which came in through our Slack community.

## Context For Charts and Widgets
Visualization of our data is very useful, but at times it's also great to see the underlying data in a simple, table-like format. In this release, we added more context for many widgets and charts, so that you can see the individual items that make up the values Lighthouse is visualizing:

- Started vs. Closed Widget: Click on the widget to see which items were started and/or closed in the specific time range
- Throughput Run Chart: Click on a specific day to see which item(s) were closed
- WIP Run Chart: Click on a specific day to see which items were open

Clicking will always open a dialog with more detailed information. We also aligned this behaviour on the Cycle Time Scatterplot. Instead of opening the link directly, a click on a bubble will now open the dialog as well. This makes it a lot easier if you have a single bubble that represents more than one item.

![Closed Items Dialog](https://github.com/LetPeopleWork/Lighthouse/blob/main/docs/assets/features/metrics/closeditemsdialog.png?raw=true)

The tooltip for all the charts will mention how many items are affected. If it's just a single one, you'll also see more details directly in the tooltip, like the name, and the reference/ID/key for all the teams and organizations that are operating like Rain Man and talk in numbers instead of meaningful words.

## System WIP Limits
For teams and projects, you can now define a *System WIP Limit*. If done, the value shows up:
- In the header of the team/project details, similar to the Service Level Expectation
- As a *Goal* in the Work In Progress widget
- As an optional, horizontal line, in the WIP Run Chart

![WIP Goal](https://github.com/LetPeopleWork/Lighthouse/blob/main/docs/assets/features/metrics/workitemsinprogress.png?raw=true)

## Contrast
We got feedback from several people that not everything is easy to read. We tried to improve this with this release, increasing the contrast and trying to implement the [WCAG guidelines](https://developer.mozilla.org/en-US/docs/Web/Accessibility/Guides/Understanding_WCAG/Perceivable/Color_contrast) for contrast ratios. This has an effect mainly (but not only) on dark mode, and especially within the metrics section.

## Docker
The docker container uses internally now the default ports 80 (http) and 443 (https) instead of 5000/5001. Also the http port is now exposed, so if you want to bind directly to this one, you are free to do so.

### ⚠️ Breaking Changes ⚠️
Be aware that this may break your existing scripts/commands to startup the docker container. Please adjust the ports accordingly in case you are updating.

## Other Improvements
- In the header of the team, it's now showing which timespan is used for forecasting (*Forecast Configuration*)
- The default time range preselected when looking at their metrics is now based on the *Forecast Configuration* (unless they use a fixed date Throughput)
- You can now override the default *request timeout* to cope with slow endpoints and/or large queries that time out
- The System WIP, Feature WIP (for teams only), and Service Level Expectation configuration are now grouped under *Flow Metrics Configuration* in the Create/Edit page

## Bug Fixes
- If a tag for a project or team is empty, it will not be shown
- Tooltip for the Cycle Time Scatterplot should not expand endlessly in width now
- Update of various third-party libraries

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Gabor Bittera](https://www.linkedin.com/in/gaborbittera/)
- [Frank Barner](https://www.linkedin.com/in/frankbarner/)
- [Nina Wagen](https://www.linkedin.com/in/nina-wagen-04a9756a/)
- [Gonzalo Mendez](https://www.linkedin.com/in/gonzalo-mendez-nz/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.6.6.1614...v25.6.16.1514)

# Lighthouse v25.6.6.1614

## Export and Import of Configuration
The main focus of this release was the introduction of the functionality to export and import your Lighthouse Configuration.

![Summary](../assets/settings/import/summary.png)

In *Settings* --> *System Settings* you will see two new buttons:
- Export Configuration
- Import Configuration

Export will download a *.json* file that contains all the settings from your Work Tracking Systems, Teams, and Projects, **excluding** any secret information to connect to your Work Tracking System.

If you click on Import, you'll get an import dialog that will lead you through the process step by step.

You can find the full documentation for the features in the [docs](https://docs.lighthouse.letpeople.work/settings/settings.html#lighthouse-configuration).
 
## Bug Fixes
- Items that haved an Age/Cycle Time exactly at SLE are displayed red instead of orange
- Last Updated Date was wrong (or not updated anymore) for Projects

## Other Improvements
- Restructured the Settings Page, grouping various settings under the "System Settings" Tab
- Scatterplot is now showing bigger bubbles if multiple items were closed at the same day with the same age
- The CFD has no tooltip anymore (as it was not useful)
- Update of various third-party libraries

## Contributions ❤️ 
Special Thanks to everyone who contributed their feedback to this release:
- [Chris Graves](https://www.linkedin.com/in/chris-graves-23455ab8/)
- [Agnieszka Reginek](https://www.linkedin.com/in/agnieszka-reginek/)
- [Hendra Gunawan](https://www.linkedin.com/in/hendragunawan823/)

[**Full Changelog**](https://github.com/LetPeopleWork/Lighthouse/compare/v25.5.18.1752...v25.6.6.1614)