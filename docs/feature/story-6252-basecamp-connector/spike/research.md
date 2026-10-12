# Basecamp connector spike: research

**Date**: 2026-10-12 | **Confidence overall**: Medium | **Status**: desk research only. No account was created and no authenticated call was made. Every claim about live API behaviour below is either quoted from the official docs or listed as a hypothesis for the hands-on probe.

## Question

We were asked whether Lighthouse could support Basecamp. Before anyone commits to a connector we want a gut feeling for:

1. Can Lighthouse get the state history of existing Work Items out of Basecamp (started date, closed date, time in each state), or would it have to record the transitions itself going forward, the way it already records blocked spells?
2. What does the rest of a connector need (identity, states, hierarchy, queries, auth) and how much of it Basecamp answers.
3. How hard would the connector be, and what must a hands-on probe confirm before we estimate it properly?

## Lighthouse connector needs

What any work tracking connector must hand back, read from the code in this worktree:

| Need | Where it is in Lighthouse |
|---|---|
| Connector contract: team fetch, portfolio (Feature) fetch, parent Feature fetch, connection/team/portfolio validation, optional write-back | `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/WorkTrackingConnectors/IWorkTrackingConnector.cs:12-72` |
| Declares whether the tracker supplies transition history | `IWorkTrackingConnector.cs:14` (`SupportsTransitionHistory`) |
| Optional cheap "identity plus last-changed" sweep for incremental sync; may be switched off | `IWorkTrackingConnector.cs:21`, `:36`, `:51`; Linear still returns `false` and throws on the sweep (`Linear/LinearWorkTrackingConnector.cs:40-48`) |
| Per Work Item: reference id, name, type, raw state mapped to a configured name, state category, URL, parent reference id, order, created / started / closed dates, synced transitions | `Linear/LinearWorkTrackingConnector.cs:353-367`; fields on `Models/WorkItemBase.cs:21-55` (also `Tags`, `CurrentStateEnteredAt`, `LastChangedRemote`, `AdditionalFieldValues`) |
| State category comes from the team/portfolio's configured To Do / Doing / Done state lists, so the connector only needs a raw state name per item | `Models/WorkTrackingSystemOptionsOwner.cs:27-31`, `MapStateToStateCategory` at `:78-90` |
| Transitions: a list of (from state, to state, when), mapped to configured state names, self-transitions dropped | `WorkItemStateTransitionMapper.cs:7-19`; Linear builds them from issue history at `LinearWorkTrackingConnector.cs:372-387` |
| **Fallback when the tracker has no history**: on every sync where the state differs from the stored state, Lighthouse appends a synthetic transition stamped with the sync time | `Services/Implementation/WorkItems/WorkItemService.cs:468-494` (`WithSyncDeltaTransition`). Linear already falls back to it when the history field is rejected (`LinearWorkTrackingConnector.cs:528-532`) |
| Another way to derive history: per-state "spans" (state label + entered-at), paired at their starts | `ServiceNow/ServiceNowStateSpan.cs`, `ServiceNow/ServiceNowHistoryQuery.cs` |
| Blocked: configured as a rule set on the team/portfolio, and the blocked spells are stored by Lighthouse itself (entered/left) | `WorkTrackingSystemOptionsOwner.cs:50` (`BlockedRuleSetJson`), `Models/WorkItemBlockedTransition.cs:5-11` |
| Estimation / Feature size: an additional field picked by the user | `WorkTrackingSystemOptionsOwner.cs:68-74`, `Models/Portfolio.cs:23` |
| Wait states, cycle-time definitions, state mappings | `WorkTrackingSystemOptionsOwner.cs:48-54` |
| Parent/child: a Work Item carries the parent Feature's reference id; Linear walks up the parent chain to the owning project | `LinearWorkTrackingConnector.cs:389-421` |
| Auth: pluggable strategies (PAT, API key, basic, OAuth bearer). OAuth providers implement authorize URL, code exchange and refresh; the client id and secret are stored per connection, so each customer brings their own app | `WorkTrackingConnectors/Auth/*`, `Auth/OAuthBearerAuthStrategy.cs:8-18`, `Services/Interfaces/OAuth/IOAuthProvider.cs:5-16`, `OAuth/OAuthWorkTrackingOptionNames.cs` |

Every path above is relative to the backend project `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/` unless it starts with `Lighthouse.Backend/` or `Models/`/`Services/`.

## Basecamp findings

Basecamp 5 replaced Basecamp 4 for every customer in 2026 [8]. The API is still documented in the `basecamp/bc3-api` repository, and its README now links to it as `bc-api` [1]. Base URL `https://3.basecampapi.com/<account_id>/` [1].

### a. Data model

- **Account → projects ("buckets") → tools.** Card Tables and To-do sets are tools inside a project. Recordings carry a `bucket` (the project) and a `parent` [3][4].
- **Card Tables.** A card table (`Kanban::Board`) has a `lists` array of columns. Column types are `Kanban::Triage`, `Kanban::Column`, `Kanban::DoneColumn` and `Kanban::NotNowColumn` [4]. A card (`Kanban::Card`) has `parent` = its column, `bucket` = its project, `completed` (boolean), `status` (active/archived/trashed), `due_on`, `assignees`, `steps`, `position`, `created_at`, `updated_at` [3]. **For a card, the state is its column.** The column *type* gives a free, stable To Do / Doing / Done hint (Triage and Not now → To Do, Column → Doing, DoneColumn → Done), and the column *title* is what Lighthouse would show as the raw state.
- **Columns can have an "on hold" section** (`POST/DELETE /card_tables/columns/:id/on_hold.json`) [5]. That is the nearest thing Basecamp has to "blocked".
- **To-dos.** To-do set → to-do lists (optionally in to-do list groups) → to-dos. A to-do has only `completed` (boolean). When it is completed, the single to-do view has a `completion` object with `created_at` and `creator`. No `completed_at` is documented [6]. **For a to-do there are only two states, open and completed.** Nothing means "started" unless a team agrees on a convention.
- **Hierarchy for Feature ↔ Work Item.** There are no parent links between cards. Candidates are project → card (Feature = project), card table → card, or to-do list → to-do (Feature = to-do list). Card steps are checklist items, not Work Items [3].
- Confidence: **High** for the shapes, which come from the official docs. **Medium** for the mapping proposal, which is my own interpretation.

### b. History: the core question

What the official docs say:

1. **Per-recording events** (`GET /recordings/:id/events.json`). "An event is created any time a recording changes". Each event has `action`, `details`, `created_at` and `creator`. Documented actions include `created`, `completed`, `uncompleted` and `assignment_changed` [2]. The page does **not** list a column-move action.
2. **Account event feed** (`GET /events.json`). This is a resumable, account-wide feed, oldest first, with a `since` id and an opaque `position`. It has 25 event types, including `card.moved`, `card.completed`, `todo.completed` and `todo.moved`. **`card.moved` carries `column_id` and `previous_column_id`**, and "going on hold within a column reports the same column twice" [7]. But: "history from before the feed shipped is never backfilled". The feed starts at "a fixed epoch, an operational fence that can be raised", and it is "not an audit log, and not guaranteed delivery" [7]. No `card.uncompleted` type is listed.
3. **Webhooks.** They are per project and HTTPS only, retry up to 10 times and then deactivate. Each webhook keeps its last 25 deliveries [9]. The documented kinds include `todo_completed`/`todo_uncompleted` and generic lifecycle kinds, but **no column-move kind** [9]. A third-party connector author found an **undocumented `kanban_card_adopted`** delivery for a column move, whose `details` hold `new_parent_id` and `parent_id_was` [10]. Zapier's staff say card tables are unsupported there, and partners recommend polling column by column and diffing [11].
4. **Project timeline / account progress report** (`/projects/:id/timeline.json`, `/reports/progress.json`) lists kinds such as `kanban_card_created` and `kanban_card_completed`. The list is labelled "common values" and does not include a move kind [12].

Interpretation (Medium confidence):

- **Going forward, Basecamp does give us column transitions**, through the account event feed (`card.moved` with both columns). Lighthouse can poll it from a self-hosted or desktop install, because polling needs **no public URL**, unlike webhooks. That fits the existing sync loop better than webhooks, which need a public HTTPS endpoint per project.
- **Retroactively, it is unproven.** The feed explicitly does not backfill. The only possible retroactive source is the per-recording events endpoint, if it records column moves as an undocumented `adopted` action (the webhook evidence in [10] suggests the same internal event exists). That would cost one call per card, and it is the probe's most important question.
- **If the probe refutes retroactive history**, Lighthouse would fall back to its existing sync-delta mechanism (`WithSyncDeltaTransition`), or better, record `card.moved` events from the feed as they arrive. Either way, cycle time and time-in-state only become trustworthy from the day the connection is created. Closed date for history could still be approximated from the `completed` event or the to-do's `completion.created_at`, and created date is always available.
- **To-dos**: done dates can be recovered retroactively from `completion.created_at` and the `completed`/`uncompleted` events. Start dates cannot, because no "started" state exists.

### c. Querying

- **No query language.** Listing is by container, for example cards in a column (`GET /card_tables/lists/:id/cards.json`) [3].
- **Account-wide listing exists**: `GET /projects/recordings.json?type=Kanban::Card` (or `Todo`), with no `bucket`, returns recordings across all active projects visible to the user. You can sort by `created_at`/`updated_at` but cannot filter by "updated since", so the cut-off is applied client-side [13]. That is a usable sweep for incremental sync: identity plus `updated_at`.
- **Pagination**: `Link: rel="next"` plus `X-Total-Count` [1]. The event feed pages up to 100 events with `next`/`position` [7].
- **Rate limit**: 429 with `Retry-After`. The commonly hit limit is **50 requests per 10 seconds per IP** [1]. Interpretation: per-card event calls for a retroactive backfill of a few thousand cards would take minutes, not seconds.
- **Conditional requests**: `ETag`/`Last-Modified` with `If-None-Match`/`If-Modified-Since` return 304 [1].
- Confidence: **High**, from the official docs.

### d. Auth

- **OAuth 2** is required, and basic auth is gone [1]. The legacy flow uses Launchpad: register an app at `launchpad.37signals.com/integrations`, get a client id and secret, authorize at `launchpad.37signals.com/authorization/new`, exchange tokens at `.../authorization/token`. **Access tokens expire after 2 weeks** and come with a refresh token. `GET /authorization.json` lists the user's accounts [14]. The redirect URI is registered with the app, and a placeholder is fine until the app goes live [14].
- **Newer options** (2026):
  - Basecamp now runs its own OAuth 2.1 issuer at `app.basecamp.com`, and the official CLI uses it with automatic refresh. A custom redirect URI must be an `http://` loopback with an explicit port [15].
  - **Personal access tokens** can be created at `app.basecamp.com/my/access_tokens`. They have no refresh, and expiry is whatever the server reports [15]. The Basecamp-served `/authorization.json` accepts them [14]. **Refuted in practice on 2026-10-12:** the maintainer's Free account offers no personal access token, only OAuth connections.
  - **Agents** use the `client_credentials` grant [1][15].
- The Free plan advertises "Bring your own AI agents" (CLI access) and MCP connectors [16]. Those run on this same API and accept personal tokens [15], so the personal-token route very likely exists on Free (see section f).
- Implications. With no personal access token available (at least on Free), a connector needs **OAuth via Launchpad**. That fits Lighthouse's existing OAuth bearer strategy, where each customer registers their own app and stores its client id and secret on the connection, with a redirect back to their own Lighthouse. Two costs follow: every customer must register a Launchpad app before connecting, and the 2-week access token means the refresh path is exercised in normal use, not only as an edge case.
- Confidence: **High** for OAuth (official docs). Personal tokens: **not available** in the maintainer's Free account, despite the CLI README.

### e. Custom fields, estimates, tags, blocked, assignees, due dates

- **Custom fields / estimates / tags**: none are documented on cards or to-dos [3][6]. Estimation and Feature size would have no field to point at, except perhaps the number of card steps or to-dos in a list as a proxy. Confidence **Medium**: this rests on absence from the docs.
- **Blocked**: there is no flag. The candidates are the column's **on hold** section [5] (visible in the feed as `card.moved` to the same column [7]), or a dedicated column the team configures as a blocked state. The on-hold placement of a card when fetched is **not documented** (probe).
- **Assignees, due dates**: yes, `assignees` and `due_on` on cards and to-dos [3][6]. `starts_on` exists on to-dos [6].

### f. Account for a hands-on probe

- **A free plan exists**: "One free project for personal use". It includes 1 project, 1 GB, up to 5 users, "Bring your own AI agents" and "ChatGPT and Claude AI connectors". It excludes project templates, "Multiple tools per project", time tracking and enhanced admin features. No credit card is needed. You can delete the one project and start another, but cannot keep more than one [16]. The maintainer confirmed the same list from a pricing-page screenshot on 2026-10-12. Paid plans run from $25/mo (Freelancer) with a 30-day trial, or 45 days for Unlimited. A paid account can never be downgraded to Free [16].
- **What "Multiple tools per project" means.** The paid plans' wording is "UNLIMITED tools per project: Add multiple message boards, chat rooms, card tables, or other tools to a single project" [16]. So the exclusion is about **several instances of the same kind of tool** (two card tables in one project), not about a project holding only one kind of tool. In the API, every project has a "dock" of tools (Message Board, To-dos, Docs & Files, Campfire, Schedule, …), each enabled or disabled [18]. The pricing page also says the core features, To-dos and Card Tables included, come with every package [16]. Interpretation, **Medium** confidence: a free project can hold **one To-do set and one Card Table at the same time**, but not a second card table. Nothing official says this in so many words, so it is probe hypothesis H0.
- **What "Bring your own AI agents" is**: per the pricing tooltip, "CLI access and skills for AI agents you run on your computer". The connectors item is "Official Basecamp support for ChatGPT, Claude, and AI agents with full MCP support" [16]. The official `basecamp` CLI authenticates by OAuth 2.1 against Basecamp's own issuer, by an imported **personal access token** (`BASECAMP_TOKEN`, issued at `app.basecamp.com/my/access_tokens`), or, for a Basecamp "agent" principal, by the OAuth `client_credentials` grant [15]. Community MCP servers likewise tell users to create a personal token with "all access" from their profile [19]. Interpretation: "Bring your own AI agents" on Free strongly suggests that the API, and with it the non-OAuth personal-token route, is open on the Free plan, which supports the auth answer in section d. That is still an inference from marketing copy plus the CLI docs, so it stays a probe item (H8, H9).
- **Card Tables are included on every plan, Free included** [16]. A new project may not show the Card Table in its dock until it is enabled under the project's tool settings [18] (unverified UI detail).
- **Probe sizing**: if H0 holds, one free project covers everything: one card table (Triage, two working columns, Done, Not now, an on-hold section) plus one to-do list. If H0 fails (only one tool), probe the **Card Table first**, because column moves are the history question. Then either delete and recreate the project with a To-do set, which the FAQ allows but which loses the card data, or open a 30-day Freelancer trial (3 projects, no card required) for the to-do half. A paid trial is **not** needed for the decisive question.

### g. Prior art

- Zapier does not support card tables. The suggested workaround is to poll each column and diff snapshots [11].
- Time trackers (Everhour) report on card tables but not on flow [17].
- An open-source "local agent connector" consumes the undocumented `kanban_card_adopted` webhook to react to column moves [10].
- I found no flow-metrics tool (cycle time, aging WIP, forecasting) that integrates Basecamp, which also means no reference for how others solved history. Confidence **Low to Medium** (absence of evidence from two searches).

## Gap analysis

| Lighthouse need | Basecamp | Workaround / note |
|---|---|---|
| Work Item identity, name, URL, created date | SUPPORTED | `id`, `title`, `app_url`, `created_at` |
| Type | PARTIAL | Only `Kanban::Card` vs `Todo`. There are no user-defined types, so a team picks cards, to-dos or both |
| Raw state + To Do / Doing / Done mapping | SUPPORTED (cards) / PARTIAL (to-dos) | Card = column title, and the column type suggests a category. To-do = open/completed only |
| Started date | PARTIAL | Cards: only from move history (going forward via the feed; retroactively only if per-recording events expose moves). To-dos: none |
| Closed date | SUPPORTED / PARTIAL | `completed` event timestamp; to-do `completion.created_at`; card moved to a Done column (history) |
| Transition history, retroactive | UNKNOWN, likely MISSING | Probe hypothesis H1. Otherwise accept that history starts at connection time |
| Transition history, going forward | SUPPORTED | Poll `GET /events.json` for `card.moved`, store the transitions as Lighthouse's own, or rely on the sync-delta fallback (sync-time precision only) |
| Parent Feature | PARTIAL | No item links. Use structure: project or card table → card, to-do list → to-do |
| Team / portfolio query | PARTIAL | No query language. Configure as project(s) + card table, or account-wide `recordings.json?type=` |
| Incremental sweep (identity + last changed) | SUPPORTED | `recordings.json` sorted by `updated_at`, plus ETag/304 |
| Blocked | PARTIAL | "On hold" section or a blocked column. Lighthouse already stores blocked spells itself |
| Estimation / Feature size field | MISSING | No custom fields. Use count-based forecasting only; a step count is a weak proxy |
| Tags | MISSING | None documented |
| Auth for self-hosted / desktop | PARTIAL | OAuth only (no personal access token on Free): each customer registers a Launchpad app, redirect back to their Lighthouse, 2-week tokens with refresh |
| Write-back | PARTIAL | Only standard fields can be written. Nothing to write estimates into |
| Webhooks | Not needed | A public URL is required. The polled feed covers the same need |

## Provisional Ease verdict

**Ease 3 of 5 (moderate).** The verdict drops to 2 if the probe shows that no retroactive column history exists and the maintainer wants usable cycle times on day one.

Rationale (interpretation):
- The plumbing is easy. It is REST with JSON, Link-header pagination, a PAT-style token that fits an existing auth strategy, an account-wide listing that doubles as the sweep, and a column type that pre-fills state categories.
- The modelling is new. There is no query language, so the team and portfolio settings UI needs a project/card-table picker instead of a query. There are no parent links, and no estimate field.
- History is the cost driver. Going forward, a feed consumer that persists `card.moved` transitions is a genuinely new kind of component: Lighthouse would become the system of record for state history, as it already is for blocked spells. It needs a durable feed `position` per connection and must tolerate gaps, because the feed is "not guaranteed delivery". Retroactively, a per-card events backfill (rate limited at about 5 requests per second) is needed only if H1 holds.

## Probe plan

Seed a free account: one project, one card table with columns Triage, "Doing A", "Doing B", Done and Not now, plus an on-hold section on "Doing A" and one to-do list with 3 to-dos. Register a Launchpad app and obtain an OAuth access token. Then **wait a day**, move cards and complete to-dos, and wait again so that timestamps are distinguishable. All calls use `Authorization: Bearer <token>` against `https://3.basecampapi.com/<account>/`.

Order: run H0 first, then the Card Table hypotheses (H1-H3, H5, H6), then the to-do ones (H4 for to-dos, H7) only if H0 allowed a To-do set in the same project.

| # | Hypothesis | Call | Confirms if | Refutes if |
|---|---|---|---|---|
| H0 | A free project can hold a To-do set and a Card Table at the same time (**confirmed in the UI 2026-10-12**; check the dock via the API too) | `GET /projects/<id>.json` and read `dock` | Both `todoset` and `kanban_board` entries, enabled | Only one can be enabled, in which case probe the Card Table first (see section f) |
| H1 | Per-card events record past column moves (retroactive history) | `GET /recordings/<card>/events.json` | Events with an action such as `adopted`/`moved`, whose details name old and new columns, timestamped at the move | Only `created`/`completed`/assignment events |
| H2 | The account feed reports moves with both columns | `GET /events.json?since=0&types=card.moved,card.completed,todo.completed` | `card.moved` with `column_id` + `previous_column_id` for each move made after the account was created | No moves, or no previous column |
| H3 | The feed's history reaches back to our seeding day, so a new connection can replay recent history | Same call as H2 | Events from before the token was created are present | Feed starts at token creation or at `now` |
| H4 | Uncompleting is visible | Uncomplete a card and a to-do, then call H2 and H1 | Events present | Only completion is recorded |
| H5 | Account-wide listing works as the sweep | `GET /projects/recordings.json?type=Kanban::Card&sort=updated_at&direction=desc` | Every card, with `parent` = column and `updated_at` | Missing cards or missing `parent` |
| H6 | On-hold placement is readable on a card | `GET /card_tables/cards/<card>.json` for a card on hold | A field or `parent` showing on-hold | Indistinguishable from the column, in which case blocked is detectable only from the feed |
| H7 | The to-do completion time is reliable | `GET /todos/<todo>.json` | `completion.created_at` equals the completion time | Absent or different |
| H8 | OAuth access token lifetime (expected 2 weeks) | `GET https://3.basecampapi.com/authorization.json` | `expires_at` about 2 weeks out, and a refresh with the refresh token returns a new one | Shorter lifetime, or refresh fails |
| H9 | API works on the Free plan | Any call above | 200 | 402/403 |
| H10 | Rate limit and conditional requests | Repeat `GET /card_tables/<id>.json` with `If-None-Match` | 304. Read any `RateLimit`/`Retry-After` headers | Always 200 |

## Account setup steps for the maintainer

1. Sign up for Basecamp Free at basecamp.com/pricing (no card; 1 project, 5 users) [16].
2. In the one project, open the tool settings and make sure both To-dos and Card Table are enabled. If the Free plan lets only one be on, pick Card Table. Add a Card Table. Rename the working columns to match the probe, and add an on-hold section to one column. Add a to-do list with 3 to-dos.
3. No personal access token exists on Free (checked 2026-10-12). Register an app at `https://launchpad.37signals.com/integrations` with a loopback redirect URI, authorize in the browser, and exchange the code for a token [14].
4. Note the account id from the browser URL or `/authorization.json`.
5. Make moves on two different days before running the probe, so that timestamps prove which source is retroactive.

## Unverified

- Whether per-recording events include column moves (H1). This is the deciding question. The only hint is an undocumented webhook kind [10].
- How far back the account event feed reaches, given "a fixed epoch … that can be raised" [7]. Whether personal tokens can poll it (the docs show bearer and agent tokens) [7].
- Personal token lifetime and whether the token page exists on the Free plan [15].
- API availability on the Free plan [16]. "Bring your own AI agents" makes it likely, but the page does not say so.
- Whether the Free plan's "no multiple tools per project" still allows one To-do set and one Card Table side by side (H0). The pricing wording points that way, but nothing states it [16][18].
- How "on hold" shows up on a fetched card [5].
- The rate limit figure (50 per 10 s per IP) is the "commonly hit" limit, not necessarily the only one [1].
- That there is no flow-metrics prior art is based on absence of evidence.

## Sources

1. Basecamp. bc3-api README. https://github.com/basecamp/bc3-api/blob/master/README.md. Accessed 2026-10-12. (official)
2. Basecamp. Events. https://github.com/basecamp/bc3-api/blob/master/sections/events.md. Accessed 2026-10-12. (official)
3. Basecamp. Card table cards. https://github.com/basecamp/bc3-api/blob/master/sections/card_table_cards.md. Accessed 2026-10-12. (official)
4. Basecamp. Card tables. https://github.com/basecamp/bc3-api/blob/master/sections/card_tables.md. Accessed 2026-10-12. (official)
5. Basecamp. Card table columns. https://github.com/basecamp/bc3-api/blob/master/sections/card_table_columns.md. Accessed 2026-10-12. (official)
6. Basecamp. To-dos. https://github.com/basecamp/bc3-api/blob/master/sections/todos.md. Accessed 2026-10-12. (official)
7. Basecamp. Event feed. https://github.com/basecamp/bc3-api/blob/master/sections/event_feed.md. Accessed 2026-10-12. (official)
8. 37signals podcast. "Launch details & decisions". https://37signals.com/podcast/launch-details-and-decisions/. Accessed 2026-10-12. (official, secondary for API purposes)
9. Basecamp. Webhooks. https://github.com/basecamp/bc3-api/blob/master/sections/webhooks.md. Accessed 2026-10-12. (official)
10. mferrerisaza. basecamp-local-agent-connector PR #4. https://github.com/mferrerisaza/basecamp-local-agent-connector/pull/4. Accessed 2026-10-12. (community, empirical, Medium)
11. Zapier Community. "How to update due dates in Asana based on Basecamp 3 card table changes". https://community.zapier.com/how-do-i-3/how-to-update-due-dates-in-asana-based-on-basecamp-3-card-table-changes-51149. Accessed 2026-10-12. (vendor community, Medium)
12. Basecamp. Timeline. https://github.com/basecamp/bc3-api/blob/master/sections/timeline.md. Accessed 2026-10-12. (official)
13. Basecamp. Recordings. https://github.com/basecamp/bc3-api/blob/master/sections/recordings.md. Accessed 2026-10-12. (official)
14. Basecamp. Authentication. https://github.com/basecamp/bc3-api/blob/master/sections/authentication.md. Accessed 2026-10-12. (official)
15. Basecamp. basecamp-cli README. https://github.com/basecamp/basecamp-cli. Accessed 2026-10-12. (official)
16. Basecamp. Pricing. https://basecamp.com/pricing. Accessed 2026-10-12. (official, commercial)
17. Everhour. Basecamp card table time report. https://everhour.com/integrations/basecamp-card-table-time-report. Accessed 2026-10-12. (vendor, Medium)
18. Basecamp. Tools. https://github.com/basecamp/bc3-api/blob/master/sections/tools.md. Accessed 2026-10-12. (official)
19. Community Basecamp MCP server README (kbhalerao/basecamp-mcp). https://glama.ai/mcp/servers/@kbhalerao/basecamp-mcp/blob/5490972e2788d7c9bcdf7a5aef2d21e2045a8b71/README.md. Accessed 2026-10-12. (community, Medium)
20. 37signals podcast. "Bring your AI agents to Basecamp". https://37signals.com/podcast/bring-your-ai-agents/. Accessed 2026-10-12. (official, describes the CLI for agents)

## Probe results so far (2026-10-12, account 6301506, Free plan)

- **H9 confirmed.** The API answers on the Free plan (200 on every call).
- **H8 confirmed.** The OAuth exchange returned `expires_in: 1209600` (14 days) with a refresh token; `/authorization.json` reports `expires_at` 14 days out. No personal access token is offered on Free.
- **H0 confirmed.** The project dock lists `kanban_board` and `todoset`, both enabled.
- **H1 confirmed: per-card history is retroactive.** The card "Teset" was moved Triage → Doing A at 10:54:45 UTC, about ten minutes **before** the Launchpad app and the token existed. `GET /buckets/<project>/recordings/<card>/events.json` returns that move as an `adopted` event whose `details` hold `parent_id_was` (Triage) and `new_parent_id` (Doing A), timestamped at the move, alongside `created` and `assignment_changed`. So a new connection can rebuild column history for existing cards, one card at a time.
- **Column types give the state categories for free.** Each column on the card table carries a `type`: `Kanban::Triage`, `Kanban::NotNowColumn`, `Kanban::Column` (user columns, with `color`), `Kanban::DoneColumn`. A column also carries an `on_hold` section when one is enabled.
- **Timestamps are UTC** (`Z` suffix); the move at 12:54 CEST reads `10:54:45Z`. The person record carries `time_zone` (here `Europe/Amsterdam`), which Lighthouse does not need for cycle time.
- **Refresh tokens do not rotate.** Two refreshes with the same refresh token each returned a new 14-day access token and no new refresh token, so an unattended job (and a connector) can keep one stored refresh token.
- **Moving a card into the Done column completes it** (`completed: true` with a `completion`), so Done is readable from the card itself as well as from its column.
- **Loose to-dos.** To-dos can sit directly in the to-do set with no list (`todos_count`, `completed_loose_todos_count` on the to-do set, and its own `todos_url`). A to-do list is therefore not a reliable parent for every to-do.
- **Writes work on Free:** create card, move card between columns, create to-do, complete to-do (the demo updater does all of these).
- **H6 confirmed, and better than hoped: on-hold history is retroactive too.** The on-hold section of a column has its own id (Doing A's is 10395174642). Putting a card on hold and releasing it show up in the card's events as `adopted` from the column into that id and back, with timestamps. So blocked spells can be rebuilt from the per-card history; Lighthouse does not have to snapshot them itself, unlike other systems.
- Still open: H2–H5, H7, H10, and a second-day check that older events stay put.
