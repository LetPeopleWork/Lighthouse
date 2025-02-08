---
title: Release Notes
layout: home
nav_order: 95
---

# Lighthouse v25.2.8.1238
{: .d-inline-block }

Latest
{: .label .label-green }
## New Features
- Allow to have a _fixed_ Throughput specified by Start and End Date, instead of only a rolling window of days. This may be useful for special periods where the full team is off for an extended period of time.

## Improvements and Bug Fixes
- Improved Documentation for how to start Lighthouse on MacOS
- Fixed issue with the configuration of the default certificate which caused Lighthouse to fail on startup on MacOS
- Updated Third Party Components
- General improvements and streamline of documentation - updated ReadMe on Github and point to official documentation instead
 
## Removed Features
- Removed Lighthouse Chart Preview Feature and related code as it was not used and could cause issues with loading. If you want such a chart back, please raise it via our [Slack Community](https://join.slack.com/t/let-people-work/shared_invite/zt-2y0zfim85-qhbgt8N0yw90G1P~JWXvlg)

## Contributors
- [**Agnieszka Reginek**](https://www.linkedin.com/in/agnieszka-reginek/)
- [**Hendra Gunawan**](https://www.linkedin.com/in/hendragunawan823/)
- [**Laurence Grantham-Clarke**](https://www.linkedin.com/in/laurence-grantham-clarke-a03316131/)
- [**Andy Normington**](https://www.linkedin.com/in/andynormington/)

[Full Changelog](https://github.com/LetPeopleWork/Lighthouse/compare/v25.2.1...v25.2.8.1238)

# Lighthouse v25.2.1

## Improvements
- Take all Features of a team into account when re-forecasting a project
- Make sure Features are returned in correct order
- Updated Third-Party Dependencies


**Full Changelog**: [v25.1.26.1015...v25.2.1](https://github.com/LetPeopleWork/Lighthouse/compare/v25.1.26.1015...v25.2.1)

# Lighthouse v25.1.26.1015

## New Features
- Support for Jira DataCenter/Server editions added (previously only Jira Cloud was supported)
- Added online documentation available under https://docs.lighthouse.letpeople.work
- Removed built-in tutorials as superseded by the new documentation

## Improvements
- Updated third-party dependencies

Full Changelog: [v25.1.2.1555...v25.1.26.1015](https://github.com/LetPeopleWork/Lighthouse/compare/v25.1.2.1555...v25.1.26.1015)

# Lighthouse v25.1.2.1555

## New Features
- Run Lighthouse with default certificate in HTTPS mode.
- Provide option to override default certificate via configuration options.

For configuration options, see the [Configuration](../installation/configuration.html#certificate).

## Improvements
- Improved download speed for log files, especially noticeable with large files.

Full Changelog: [v25.1.1.1225...v25.1.2.1555](https://github.com/LetPeopleWork/Lighthouse/compare/v25.1.1.1225...v25.1.2.1555)

# Lighthouse v25.1.1.1225

## New Features
- Added setting to Teams to automatically adjust Team Feature WIP based on real Feature WIP.
- Server-side updates are now queued; if multiple users request an update of a team, it will only be done once if it's already queued.
- Teams/Projects now auto-update if they are updated by any user or through a periodic update; manual reload is no longer necessary.
- REST Endpoints can now be viewed via `/api/swagger`.
- Docker container is now built against multiple architectures: `linux/amd64`, `linux/arm64`, `linux/arm/v7`.

## Improvements
- Updated system log clarity for multi-user scenarios.

Full Changelog: [v25.1.0.1100...v25.1.1.1225](https://github.com/LetPeopleWork/Lighthouse/compare/v25.1.0.1100...v25.1.1.1225)