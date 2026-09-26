---
title: Usage Data
layout: home
parent: System Settings
nav_order: 8
---

# Usage Data

Lighthouse can tell us **which parts of it get used**, and a little about the instance doing the
using — its version, how it is deployed, which licence tier it runs on. Nothing is sent unless
somebody using that instance agrees to it first, and anyone can stop it again with one click.

This page is the full account: what is sent, what is never sent, who holds it, how long they hold it,
and what Lighthouse stores on your own server to remember your answer. The dialog keeps no list of its
own, on purpose — a list inside a dialog goes stale quietly while still looking authoritative — so this
is the only place that list lives, and the build fails if it falls behind what the product can
actually send.

**It is off until somebody turns it on.** A Lighthouse instance that nobody has answered on sends
nothing at all.

## Why this exists

Lighthouse has no idea which versions people are actually on, or whether a feature that shipped is
being opened by anyone. Download counts and whoever happens to post in Slack are the whole picture
today. That makes "is it safe to stop supporting this version" a guess, and it leaves "did anyone
ever use the thing we spent a month on" unanswerable.

## What is sent

An **event** — a named thing that happened — plus a small, fixed set of facts about the instance. No
event is sent unless a browser on that instance holds live consent at the moment it happens, and that
check runs on your own server, against your own database, every single time.

The complete list of events:

| Event | When it is sent | What travels with it |
|---|---|---|
| A Team tab was opened | Somebody opened a tab on a Team page **and was still on it five seconds later** | Which of the five Team tabs it was. **Never which Team** |
| A Portfolio tab was opened | Somebody opened a tab on a Portfolio page **and was still on it five seconds later** | Which of the five Portfolio tabs it was. **Never which Portfolio** |
| A Team was created | Somebody finished creating a Team | Nothing. **Not its name, not its identifier** |
| A Team was deleted | Somebody confirmed deleting a Team | Nothing. **Not its name, not its identifier** |
| A Portfolio was created | Somebody finished creating a Portfolio | Nothing. **Not its name, not its identifier** |
| A Portfolio was deleted | Somebody confirmed deleting a Portfolio | Nothing. **Not its name, not its identifier** |
| A forecast was run by hand | Somebody asked for a forecast on a Team page. **Never the forecasts Lighthouse runs on its own** | Nothing. **Not what was asked, not what came back** |
| A work tracking system was connected | Somebody finished setting up a connection | Which kind it is — one of `Azure DevOps`, `Jira`, `Linear`, `CSV`, `ServiceNow`. **Never its address, never its name, never anything typed while setting it up** |
| A Team's data was refreshed by hand | Somebody pressed refresh on a Team rather than waiting for the next automatic one | Nothing |
| A Portfolio's data was refreshed by hand | Somebody pressed refresh on a Portfolio rather than waiting for the next automatic one | Nothing |
| A setting was switched | Somebody switched a setting under **Settings → System** and your server accepted the change. **Never a switch your server refused** | Which setting it was — only *Let Lighthouse own the order of your Features* — and whether it is now on or off. **Never the setting's key, its name or its description** |
| A forecast reality check was run | Somebody ran a forecast reality check on a Team page and got an answer back. **A check whose history was too thin to judge still counts; a check that failed to come back never does** | Nothing. **Not which Team, not its sampling window, not what the check found** |

That is the whole vocabulary. It is a closed list in the code — not a pattern that quietly matches new
things — and the build fails if anything outside it is sent.

**Eight of the twelve carry nothing but the fact that they happened.** That is not a courtesy; each
event in the code says what it is allowed to carry, and one arriving with anything else is refused
rather than trimmed. So the two tab openings are the only events that can name a page at all.

**Switching *Never send usage data* is never reported, in either direction.** Switching it on stops
everything from that moment, including the message that would say so. Switching it off could only
ever be counted one way round, and a count of the veto being lifted with no count of it being put in
place would tell us something untrue. So that switch is not on the list of settings at all, and a
message naming it is refused.

**A tab you pass through is not recorded.** Clicking through three tabs to find the one you want
records one opening, not three: a tab you leave within five seconds never counts. Nothing about how
long you stayed is measured or sent — the five seconds decides only whether an opening is recorded,
not what it carries.

Every event carries these, attached by **your** server rather than by your browser:

| Field | What it is | Example |
|---|---|---|
| Browser identifier | A random value your Lighthouse generates and stores **on your own server**, against the record of this browser's answer, the first time somebody agrees here. Derived from nothing — not your hostname, not your licence key, not your account. Your browser never sees it and never sends it | `a7f2…` |
| Which tab was opened | **Only on the two tab openings above.** One of ten addresses this product publishes about itself, listed in full below. Your browser never sends an address; it sends a label, and your server looks the published address up. On the other ten events this field is not empty — it is not there at all | `/teams/:id/metrics` |
| Which setting was switched | **Only on a setting being switched.** A fixed word this product publishes for the setting, and there is one: `FeatureOrder`, for *Let Lighthouse own the order of your Features*. **Never the key the setting is stored under, and never its name as you see it on screen.** On the other eleven events this field is not empty — it is not there at all | `FeatureOrder` |
| Which way it was switched | **Only on a setting being switched.** `true` when the setting is now on, `false` when it is now off. On the other eleven events this field is not `false` — it is not there at all | `true`, `false` |
| Lighthouse version | The version this instance runs, but only when it is a published release. Anything else is sent as the literal word `unreleased` | `v26.9.9.9`, `unreleased` |
| Deployment mode | How it is deployed, as one of `Standalone`, `Windows`, `Linux`, `MacOS`, `Docker`, `Kubernetes` | `Kubernetes` |
| Licence tier | Which tier this instance runs on | `Community`, `Premium` |
| Authentication | Whether somebody here signs in as themselves. `true` only when authentication is switched on and working — an instance where it is misconfigured, or where it refuses everybody, sends `false`, because nobody is signing in individually there either | `true`, `false` |
| Timestamp | When it happened. Your browser sends how long ago it was, never a reading of its own clock, so neither its clock nor its time zone travels; your server turns that into a time by its own | `2026-09-12T09:14:07Z` |

### What the page address never contains

Lighthouse pages have addresses like `/teams/42/metrics`, where `42` identifies one of *your* Teams.
That number never leaves your browser. What the browser records is not a shortened address — it is a
fixed label chosen from a closed list, so there is no address present to shorten and nothing to
accidentally get wrong.

Your server turns that label into the address this product publishes for it. There are ten of those,
and this is all of them:

`/teams/:id/features`, `/teams/:id/forecasts`, `/teams/:id/metrics`, `/teams/:id/settings`,
`/teams/:id/access`, `/portfolios/:id/features`, `/portfolios/:id/metrics`,
`/portfolios/:id/deliveries`, `/portfolios/:id/settings`, `/portfolios/:id/access`

The `:id` is written that way in Lighthouse's own source. It is not a real identifier that something
stripped on the way out — there was never a real one there to strip.

### There is a daily ceiling, and events past it are thrown away

One instance forwards at most **1000 events a day**. Past that the day's events are discarded rather
than refused: a refusal only tells a browser to try again, and trying again is the last thing an
exhausted allowance needs. A thousand is roughly twenty busy people's day on one instance, so a real
deployment does not reach it.

The allowance being protected is a single shared one, drawn on by every Lighthouse in the world, so
enough honest instances could empty it without any of them misbehaving. Operators can move the
ceiling with `UsageData:DailyEventBudget`. Either way the numbers are a floor rather than a count:
nothing extra is ever sent, and on a very large instance something may be missing.

## What is never sent

Nothing about your work, and nothing about you:

- No work item titles, identifiers, queries or descriptions
- No team, portfolio or delivery names, and no identifiers for any of them
- No user names, email addresses or account identifiers
- No free text of any kind, and no address your browser was at. What your browser posts to your own
  server has **no field capable of carrying free text** — only choices from closed lists and bounded
  numbers — so this is a property of its shape rather than a rule somebody has to remember. The only
  address-shaped thing that travels onward is one of the ten published above, which Lighthouse wrote
  down about itself
- **No IP address.** The message explicitly carries an instruction not to record one, and the
  collector is configured to discard it as well
- **No location.** Location lookup is off, and the message carries an instruction to skip it

## What this does reveal, which a once-a-day signal would not

Said plainly, because it is the real cost of counting features rather than installations:

**The times of the events describe when somebody was working.** Events arrive as they happen, so a
consenting browser leaves a rough trace of the hours it was in use, and therefore of a working day and
an approximate time zone. We cannot remove this from our side — a timestamp is what makes an event an
event.

**What it still cannot show** is which person, which Team, or what they were looking at. It is the
shape of activity, not its content.

## Counting browsers, not installations

**Lighthouse cannot count how many installations exist, and that is deliberate.**

The identifier above belongs to a browser. Nothing in the message says which instance sent it, so two
colleagues consenting on the same Lighthouse count as two, exactly as two people at different
companies would. There is no field that could join them.

This is a choice, and it costs us the number we would most like to have. An instance identifier
alongside a browser identifier would link colleagues to one another, which says more about a group of
people than either value does on its own. We would rather be unable to answer "how many installations"
than hold that.

So every number here counts *browsers*, and a large shared installation weighs more than a small one.
The `Authentication` field exists only so we can tell whether "one browser is roughly one person" is a
reasonable reading on that instance at all.

## Where it goes, and who holds it

The collector is **PostHog Cloud EU**, operated by PostHog, with data at rest in the EU. Processing
may also take place outside it, including in the US, under standard contractual clauses.

The authority on that is PostHog's own data processing agreement, not our summary of it:
<https://posthog.com/dpa>. Restating somebody else's terms here would leave you reading a copy that
can drift out of date while still sounding authoritative — and the terms that actually bind them are
the ones worth reading.

**Your browser never contacts the collector.** It tells your own Lighthouse server that something
happened; your server decides whether consent allows it, and only your server talks to PostHog. That
is also why nothing here is distorted by ad blockers — a blocked request would make the data quietly
wrong rather than absent, and we would have no way to tell which.

### How long it is kept

**One year.** This is not a dial we can turn down — the collector sets a retention floor by plan and
does not allow a shorter one, and one year is the floor on the plan we are on. It comfortably covers
every question we ask of this data, all of which look back days or weeks rather than years.

A paid plan would raise that floor to **seven years**. That is not a change we could make quietly: a
longer retention period counts as a material change, so it would mean updating this page, updating the
dialog, and **asking everyone who already agreed to agree again**. Staying inside the free plan's
limits is therefore a commitment about your data, not only about our costs.

## What Lighthouse stores on your own server

Answering the dialog writes a small amount of data to **your** Lighthouse database. None of it is ever
sent anywhere — not to the collector, not to us.

| What | Where | Why |
|---|---|---|
| One consent row per browser | Your Lighthouse database | So your answer survives a page reload, and so a browser that already answered is not asked again |
| An opaque token | Your browser's local storage | How this browser proves which consent row is its own |
| A browser identifier | Your Lighthouse database, on the same consent row | The random value that travels with events. It is **not** stored in your browser, and it is never sent to your browser or accepted from it — your browser presents its token, and the server looks the identifier up. Deliberately kept apart from the token: the token can revoke your consent, so it must never reach anybody else, while this value exists precisely to leave |

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

Revoking takes effect on the **next event**. Your server re-checks consent every single time, against
the database rather than anything remembered, so there is nothing cached to go stale and no window in
which a page left open keeps sending.

Two honest limits:

- **Revoking stops future sends. It does not erase what was already sent**, which ages out on the
  retention period above.
- **If you simply clear your browser storage, that browser stops sending immediately** — it no longer
  holds the token that proves which answer was its own, so there is nothing for your server to approve
  and it will be asked again as though it had never decided. Clearing storage happens entirely on your
  machine and produces no request, so your server cannot learn about it directly; the consent row it
  left behind lingers for about 30 days before ageing out, but it no longer permits anything.

## Something Lighthouse already sends, which this does not cover

Lighthouse checks GitHub for a newer release, and has always done so. That check is a request to
GitHub from your server, so GitHub sees your server's IP address, and it happens whatever you answer
here. It is named on this page because a page about what leaves your instance should not mention only
the part that asks permission.

## Running without an outbound connection

Lighthouse ships knowing where it would send, so a release you install needs nothing configured for
the footer switch to mean what it says. Nothing leaves until somebody on that instance agrees, and
the address is only ever reached once somebody has.

**A build nobody published sends nothing, whatever anybody agreed to.** If you are running Lighthouse
from source, or from your own build, it stays out of the shared figures — the people who compile
their own copy are not the population these numbers are meant to describe, and a version string from
a working tree is close to unique to whoever built it, which is the opposite of what the rest of this
page promises. An instance in that state says so in its log, once, at warning level. It has to say
it, because a usage event dropping quietly is normal here by design and looks exactly like working.

Where to send is `UsageData:CollectorBaseUrl`. Setting it does two things: it replaces the built-in
address, and it lifts the published-release rule, so an unreleased build with an address set will
send. That is how you would point an instance at a collector of your own, and how anybody checks what
this actually puts on the wire without waiting for a release. Be aware that the message format is
PostHog's own capture API, so a substitute has to speak that protocol — this is not "point it at any
URL and it works".

Blocking the address at your firewall works too, and needs no setting: sending fails, the failure is
dropped, and nothing is retried or queued.

## How the collector is configured

These settings are ours, not yours, and they live in a vendor's web console where no test of ours can
reach them. They are listed so the claims above are checkable rather than merely asserted, and they
are re-verified at each release rather than trusted to stay put.

| Setting | Expected state |
|---|---|
| Region | EU Cloud |
| Discard client IP data | On |
| Location (GeoIP) enrichment | No such transformation present |
| Session recording | Off |
| Autocapture, heatmaps, web vitals, dead clicks | All off |
| Third-party AI services | Off — enabling them would add four further processors |
| Training on anonymised data | Off |
| Event retention | One year |

Two of these do not depend on trusting that list at all: every message carries its own instruction to
discard the IP and skip location lookup, and that is checked against the actual outgoing message on
every build. The rest of the list lives where no test of ours can see it, so it is re-read by hand at
each release — written down here so that it can be.
