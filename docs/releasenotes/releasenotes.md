---
title: Release Notes
layout: home
nav_order: 95
---

# Lighthouse v25.2.1
{: .d-inline-block }

Latest
{: .label .label-green }

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