---
title: Usage Data
layout: home
parent: System Settings
nav_order: 8
---

# Usage Data

Lighthouse can send a small daily signal about **the instance itself** — its version, how it is
deployed, and which licence tier it runs on. Nothing is sent unless somebody using that instance
agrees to it first, and anyone can stop it again with one click.

This page is the full account: what is sent, what is never sent, who holds it, how long they hold it,
and what Lighthouse stores on your own server to remember your answer. If the consent dialog and this
page ever disagree, that is a bug — they are checked against each other, and against the message
actually sent, on every build.

**It is off until somebody turns it on.** A Lighthouse instance that nobody has answered on sends
nothing at all, and has no identifier to send.

## Why this exists

Lighthouse has no idea how many instances are running, which versions people are actually on, or
whether an upgrade reached anyone. Download counts and whoever happens to post in Slack are the whole
picture today. That makes "is it safe to stop supporting this version" a guess, and it leaves several
questions about whether shipped features landed permanently unanswerable.

## What is sent

Exactly five fields, once per day, per instance:

| Field | What it is | Example |
|---|---|---|
| Instance identifier | A random value, generated on this instance the first time somebody agrees. Derived from nothing — not your hostname, not your licence key, not your database name | `f4c1…` |
| Lighthouse version | The version this instance is running | `v26.9.9.9` |
| Deployment mode | How it is deployed | `Docker`, `Kubernetes`, `Standalone` |
| Licence tier | Which tier this instance runs on | `Community`, `Premium` |
| Timestamp | When the signal was sent | `2026-09-12T04:00:00Z` |

That is the complete list. There is no sixth field, and the message that goes out is checked against
this list automatically rather than by anyone remembering to look.

## What is never sent

Nothing about your work, and nothing about you:

- No work item titles, identifiers, queries or descriptions
- No team, portfolio or delivery names
- No user names, email addresses or account identifiers
- No URLs, no free text of any kind
- **No IP address.** The message explicitly carries an instruction not to record one, and the
  collector is configured to discard it as well
- **No location.** Location lookup is off, and the message carries an instruction to skip it

The signal describes a *deployment*, not a person and not a project.

## Where it goes, and who holds it

The collector is **PostHog Cloud EU**, operated by PostHog. Data rests on servers in **Frankfurt**.

One thing worth stating plainly rather than leaving you to assume it: *resting in Frankfurt is not the
same as never leaving the EU.* PostHog's data-processing agreement says processing may happen outside
that area, including in the US — which in practice covers their own staff and internal tooling under
standard contractual clauses. The sub-processors holding the data are EU-located; the content delivery
network that carries it in transit is global, as it is for any web request. We would rather say this
than let the word "Frankfurt" imply something stronger than it means.

Lighthouse sends this **from the server**, not from your browser. Your browser never contacts the
collector, which is also why nothing here can be blocked or seen by a browser extension.

### How long it is kept

**One year.** This is not a dial we can turn down — the collector sets retention by plan and does not
allow a shorter period, so one year is what the plan we are on provides. It comfortably covers every
question we ask of this data, all of which look back days or weeks rather than years.

If that ever changes, this page and the consent dialog change with it, in the same release.

## What Lighthouse stores on your own server

Answering the dialog writes a small amount of data to **your** Lighthouse database. None of it is ever
sent anywhere — not to the collector, not to us.

| What | Where | Why |
|---|---|---|
| One consent row per browser | Your Lighthouse database | So your answer survives a page reload, and so a browser that already answered is not asked again |
| A random identifier for this instance | Your Lighthouse database, in application settings | So the daily signal can be counted as one instance rather than many. Created **only** when somebody agrees — an instance nobody has agreed on never has one |
| An opaque token | Your browser's local storage | How this browser proves which consent row is its own |

The consent row holds the **hash** of that token, never the token itself, along with your decision,
when you made it, and when this browser was last seen. The server therefore cannot reproduce your
token — it can only recognise one when a browser presents it.

Two things people are often surprised by, so they are said here:

- **Saying no is also recorded.** Otherwise the instance has no way to know it already asked you, and
  would keep asking.
- **Consent is per browser, not per account.** A different browser, or a cleared browser storage, is a
  browser that has not answered yet.

## Deciding, and changing your mind

An indicator sits in the footer next to the version number, showing whether this instance is sending
anything. Clicking it opens the dialog, which lists the fields above and offers two buttons. You can
open it any time; opening it and closing it without choosing stores nothing at all.

Revoking takes effect on the **next** send — there is nothing cached to go stale.

Two honest limits:

- **Revoking stops future sends. It does not erase what was already sent**, which ages out on the
  retention period above.
- **If you simply clear your browser storage, that browser stops counting after about 30 days**, not
  immediately. Clearing storage happens entirely on your machine and produces no request, so the
  server has no way to learn about it — it notices only when the browser stops showing up. If that was
  the only browser that had agreed, the instance keeps sending for up to a month.

An instance is counted in the total while **at least one browser holds live consent**, so "instances
reporting" means "instances with at least one recently active consenting browser" rather than
"instances installed".

## Something Lighthouse already sends, which this does not cover

Lighthouse checks GitHub for a newer release, and has always done so. That check is a request to
GitHub from your server, so GitHub sees your server's IP address, and it happens whatever you answer
here. It is named on this page because a page about what leaves your instance should not mention only
the part that asks permission.

## Running without an outbound connection

The collector address is configurable. Point it somewhere else, or somewhere that does not resolve,
and nothing reaches PostHog. Be aware that the message format is PostHog's own capture API, so a
substitute has to speak that protocol — this is not "point it at any URL and it works".

On Kubernetes this is `app.usageData.collectorBaseUrl` in the Helm chart.

## How the collector is configured

These settings are ours, not yours, and they live in a vendor's web console where no test of ours can
reach them. They are listed so the claims above are checkable rather than merely asserted, and they
are re-verified at each release rather than trusted to stay put.

| Setting | Expected state |
|---|---|
| Region | EU Cloud (Frankfurt) |
| Discard client IP data | On |
| Location (GeoIP) enrichment | No such transformation present |
| Session recording | Off |
| Autocapture, heatmaps, web vitals, dead clicks | All off |
| Third-party AI services | Off — enabling them would add four further processors |
| Training on anonymised data | Off |
| Event retention | One year |

Two of these do not depend on trusting that list at all: every message carries its own instruction to
discard the IP and skip location lookup, and that is checked against the actual outgoing message on
every build. A scheduled job also reads the collector back and fails if any stored event carries an
address or a location, or if a location-shaped field appears at all.
