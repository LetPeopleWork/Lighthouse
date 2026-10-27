---
title: Data Protection
layout: home
nav_order: 5
---

# Data Protection

This page is the answer to "what data does Lighthouse hold, where, and who else sees it?" — written for
the person filling in a data protection or software intake questionnaire. How stored credentials are
protected is on the [Security](security.html) page.

## The short answer

- **Lighthouse runs on your infrastructure.** You decide where it is hosted, in which country and on
  which machines. LetPeopleWork does not host it, cannot reach it and has no copy of your data.
- **Your data stays there.** Lighthouse reads from your work tracking systems and stores what it reads
  in your own database. None of it is sent to us or to anyone else.
- **LetPeopleWork is not a processor of your data.** We supply software you run yourself. We do not
  process personal data on your behalf, so there is no data processing agreement to sign with us.
- **One optional exception:** usage data, which is off until somebody agrees to it, carries no personal
  data and no content, and can be switched off for the whole instance. See [below](#usage-data).

## Where the data lives

| What | Where |
| --- | --- |
| The Lighthouse application | Wherever you install it: a server, a VM, Docker, Kubernetes, or a desktop for the Standalone edition. See [Installation](Installation/installation.html) |
| The database | PostgreSQL that you run, or a local SQLite file. Its location, region, backups and access are yours |
| The encryption key for stored credentials | Generated next to the database, or supplied by you or your secret store. See [Security](security.html#the-key-and-who-owns-it) |
| Backups you download | Wherever you put them, encrypted under the password you choose |

Hosting region, data centre and country are therefore whatever you choose for your own infrastructure.
There is no LetPeopleWork-operated cloud service involved.

## What Lighthouse stores

| Category | Examples | Personal data? |
| --- | --- | --- |
| Work item data read from your work tracking systems | ID, title, type, state, state changes with dates, parent and dependency links, and any Additional Fields you choose to read | Usually not. Titles are free text and can contain names, and an Additional Field you add (an assignee, for example) can be personal data. Which fields are read is your configuration |
| Configuration | Teams, Portfolios, queries, settings, connections | No |
| Credentials for your work tracking systems | Personal access tokens, OAuth tokens, client secrets | Encrypted. See [Security](security.html) |
| Forecasts and metrics | Forecast results, throughput, cycle times, history over time | No |
| Signed-in users | Display name and email address, from your identity provider, when [authentication](Installation/authentication.html) is switched on | Yes |
| Role assignments and API keys | Which user holds which role, API key owners and scopes | Yes, linked to the signed-in user |
| Sizing votes on the Refinement tab | Who voted, their answer and any comment | Yes, the voter's display name |

**Purpose:** forecasting delivery and showing flow metrics for your teams and portfolios. Lighthouse does
not profile individuals: its metrics are about teams and work items, not people.

## Retention and deletion

Lighthouse keeps what it stores until you remove it, and you control all of it: Teams, Portfolios and
connections can be deleted in the interface, user access is removed through your identity provider and
roles, and the database is yours to drop. Backup frequency and retention are part of how you run your database. See
[Database Management](settings/databasemanagement.html).

## Usage data

Lighthouse can tell us which parts of it get used. It is **off until a person on the instance agrees**,
and anyone can withdraw with one click.

- **What:** a closed list of events such as "a forecast was run" or "a Team tab was opened", plus the
  version, deployment mode, licence tier and whether authentication is on. No names, no email addresses,
  no work items, no Team or Portfolio names, no content.
- **Who receives it:** PostHog Cloud EU, with data at rest in the EU, kept for one year.
- **Switching it off for everybody:** with a licence, an administrator can switch on **Never send usage
  data** under Settings → System. Nothing is sent and nobody is asked.

The full list of events and fields is on the [Usage Data](settings/usagedata.html) page.

## AI

Lighthouse contains no AI and sends no data to any AI service. Its forecasts are Monte Carlo
simulations run on your instance. If you choose to connect an AI client of your own through the
[CLI or MCP server](aiintegration.html), that client reads Lighthouse with the user's own API key and
permissions, and what it does with the data is governed by your agreement with that AI provider. Nothing
is used to train models by LetPeopleWork.

## Third parties and transfers

| Party | When | Data |
| --- | --- | --- |
| Your work tracking systems (Jira, Azure DevOps, ServiceNow, Linear) | Always, as configured by you | Lighthouse reads from them with the credential you provide |
| Your identity provider | When authentication is on | Sign-in through OpenID Connect |
| GitHub | To check for a newer version | No data about your instance |
| PostHog Cloud EU | Only with consent, see [Usage data](#usage-data) | No personal data |

LetPeopleWork itself receives none of your data.

## Questions

For data protection questions, write to [contact@letpeople.work](mailto:contact@letpeople.work). For
security reports, see [Security](security.html#reporting-something).
