# Slice 10 — The SLE yardstick votes are cast against

**Feature**: epic-5510-5881-refinement · **Epic (proposed)**: E3 Sizing votes (#5510) · **Story**: US-10 ·
**Estimate**: ~½d · **Tier**: Community

## Goal

The tab asks one question with one number: "Doable within 7 days? (our SLE, 85%)". When the Team has no SLE, it uses
the 85th percentile of the Team's **default** cycle time, labelled as a fallback, with a hint to set an SLE (D7, D24).

## IN

- The question in the tab header, built from facts (the SLE range and probability, or the fallback value and a flag
  saying it is a fallback).
- The fallback computed over the Team's metrics window. There is no choice of cycle-time definition.
- The case with no SLE and no finished Work Items: the question appears without a number, and the hint explains why.

## OUT

Voting (11).

## Learning hypothesis

**This disproves "every Team has an SLE or accepts the fallback"** if, on the dev instance, the fallback values are 30
days or more. A question like "doable within 34 days?" is meaningless; the hint must then be louder, and DISCUSS of E3's
later slices revisits this.

## Data and dogfood moment

- Dev instance: list every Team's SLE, or its fallback value, before building. Also check how many Teams have no SLE.
- Demo: Teams carry 85% / 7 days (`DemoDataFactory.cs:55-56`). Clear one Team's SLE to cover the fallback.

## Acceptance criteria

AC-10.1 … AC-10.3 (US-10).

## Dependencies

Slice 02.
