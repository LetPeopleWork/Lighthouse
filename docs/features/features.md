---
title: Features
layout: home
nav_order: 25
---

# Features

The Features page lists every Feature Lighthouse knows about, across all your Portfolios, in the order it forecasts them. You reach it from the top navigation, between *Overview* and *System Settings*.

Every other list in Lighthouse shows you a slice: the Features of one Portfolio, or the Features one Team contributes to. This page shows the whole backlog in one place, which is the only view that matches how forecasting actually works — Lighthouse always takes all Features of all Portfolios into account. See [Why the Backlog Order Matters](../concepts/howlighthouseforecasts.html#why-the-backlog-order-matters).

## The position column

The `#` column is the Feature's place in the order Lighthouse simulates, counted across the whole instance.

It is **not** the row number. Two rows shown one after the other can read `26` and `77`, and that is correct — the numbers in between belong to Features that are filtered out of your current view. Positions count:

- completed Features, which keep their place even while hidden
- Features in Portfolios you cannot see

So the position tells you where a Feature sits in the real forecast sequence, not where it sits on your screen. A Feature ordered above another gets your Teams' throughput first, which is what moves the forecasted dates.

{: .note}
The order comes from your work tracking system — the rank or backlog order your Features already carry there. Lighthouse reads it, it does not write it back.

## What you see

- **Completed Features are hidden by default.** The *Hide Completed Features* toggle reveals them. Turning it off does not renumber anything: every position stays exactly as it was, and the hidden Features simply reappear in their places.
- **Portfolio membership** is shown per row. A Feature that belongs to several Portfolios appears once and lists all of them.
- **Sorting** the grid by any other column leaves the positions untouched, so you can sort by name or state and still read where each Feature really sits.

## Who sees what

The page is available on every Lighthouse instance, including the community edition.

When authorization is enabled, the list is filtered to the Portfolios you are allowed to read. Positions are still the instance-wide ones, so the numbers you see are the same numbers a colleague with wider access would see for those Features — you simply see fewer rows.

## Feedback

We'd love to hear from you! Reach out at [contact@letpeople.work](mailto:contact@letpeople.work) or through our [Slack Channel](https://join.slack.com/t/let-people-work/shared_invite/zt-38df4z4sy-iqJEo6S8kmIgIfsgsV0J1A).
