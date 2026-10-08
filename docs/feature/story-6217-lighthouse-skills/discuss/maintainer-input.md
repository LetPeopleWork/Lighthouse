# Maintainer input — Lighthouse skills (#6217 and the wider skill review)

Captured 2026-10-08 from the maintainer, in conversation, before DISCUSS.

## Shape

- One **general Lighthouse skill** (how to use Lighthouse: connect, read metrics, forecast, coach) —
  the existing `lighthouse-clients/skill/`, reviewed and updated against everything shipped since
  2026-06-06.
- **Specific skills** that build on the general one (may reference it rather than repeat it):
  - **Refinement**
  - **Daily Flow Review**
- Distribution: same as today — lives in the lighthouse-clients repo, shipped by its CI pipeline.

## Refinement skill

- Users:
  - a **PO** preparing the next Refinement session — goal: see at a glance whether the Team is ready;
  - **individual developers** checking whether they need to prepare or vote on something — goal:
    very easy to prep.
- The question at the heart of it: "Can we do ABC in x days or less?" — Yes / Yes, if… / No.
- Getting more detail on a Work Item is NOT the skill's job: it only says *where* to look (e.g. the
  link to the item in the work tracking system), because that depends heavily on the system.
- **Humans always decide.** Agents may fetch details and propose; they never vote on anyone's behalf
  or decide readiness.

## Daily Flow Review skill

- User: a **facilitator**, just before or during the daily.
- From the data, show what is most important for the Team to **1) decide** and **2) discuss** today.
- Purpose: remove the status round where everybody prepares a statement and justifies what they
  did; focus the daily on getting work done.

## Stance

- Somewhat opinionated, ProKanban-aligned, but **not dogmatic** — must not put people off.

## Sources to analyse (and extract more from, where useful)

- ProKanban's own skill: https://github.com/ProKanban/professional-kanban
- Liz Rettig (collaborated with LetPeopleWork on many things):
  - https://prokanban.org/blog/ai-kanban-and-a-restructure-you-can-prove-something-moved-not-what-moved-it
  - https://prokanban.org/blog/ai-boosted-your-throughput-your-strategy-didn-t-move

## Follow-up answers (same conversation)

- Three ADO stories, all shipped together in the next release (sequenced, not separately released):
  #6245 general skill update, #6217 Refinement skill, #6246 Daily Flow Review skill.
- Daily Flow Review uses what Lighthouse already exposes (in-progress Work Items with age against the
  SLE, blocked items, WIP against its limit) **plus Process Behaviour Charts**.
- Liz Rettig covered these topics at a LetPeopleWork meetup: https://www.youtube.com/watch?v=YBdlyT00u8k
- Order: after #6193, before #6202 — #6245 first (both specific skills reference it), then #6217,
  then #6246.
