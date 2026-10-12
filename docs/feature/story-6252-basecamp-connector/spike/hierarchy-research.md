# Basecamp connector spike: how to map Features and Work Items

**Date**: 2026-10-12 | **Status**: desk research only. No account was created and no authenticated call was made for this document. It builds on `research.md` in this folder, which holds the API basics and the first probe results. | **Overall confidence**: Medium

## Question

Lighthouse needs two levels: **Work Items**, which flow through a team's board and give cycle time, throughput, WIP and age, and **Features**, which are bigger pieces of value made of Work Items. Lighthouse forecasts a Feature from its remaining child Work Items and the team's throughput. Features can also have parents.

Basecamp has no parent link between cards, and none between cards and to-dos. So which Basecamp objects should be the Work Items, which should be the Features, and what would the user configure? Four sub-questions:

1. How does 37signals' own method, Shape Up, use Basecamp, and what does it say about estimates and forecasting?
2. How do real teams model "a big thing made of small things" in Basecamp, both Shape Up style and Card Table (Kanban) style?
3. Which mappings are candidates, and what does each one break?
4. Which mapping should a first connector use, and what must the hands-on probe check for it?

## How Lighthouse connectors map hierarchy today

The Linear connector is the closest precedent, because Linear has no query language either. All paths are under `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/WorkTrackingConnectors/Linear/LinearWorkTrackingConnector.cs`.

- **Team.** The user configures one value per team, `DataRetrievalValue`, and for Linear it is the **Linear team's name** (`:59`). The connector resolves the name to an id (`:67`), fetches every issue of that team (`:76`) and keeps those whose state is in the team's configured states (`:77`). Validation fails with a pointer to `DataRetrievalValue` when the name is empty or cannot be resolved (`:291-312`). The team wizard fills that value with the team's name, and it also pre-fills the To Do / Doing / Done lists from Linear's own state *types*: triage/backlog/unstarted to To Do, started to Doing, completed to Done (`:939-967`). A Basecamp column's `type` could pre-fill the categories the same way.
- **Portfolio (Features).** For a Linear portfolio, `DataRetrievalValue` is **not read at all**. The portfolio fetch takes **every Linear project in the workspace** (`:122`) and keeps those whose status is in the portfolio's configured states (`:137`). Validation only checks that the workspace has at least one project (`:262-273`). So "which Features belong to this portfolio" is answered by "all of them, filtered by state".
- **Work Item to Feature.** Each issue carries its project's id as the parent reference. If the issue has no project, the connector walks up the issue's parent chain until it finds an ancestor with a project (`:389-421`). An issue with no resolvable project still becomes a Work Item, just without a Feature (`:94-102`).
- **Feature to parent.** A Linear project's first initiative becomes the Feature's parent (`:428`, `:438`). Those parents are fetched one by one as separate items with their own state and dates (`:164-222`).

Takeaway (interpretation): Lighthouse needs no explicit link object. It needs (a) a way to list a team's Work Items, (b) a way to list Features, and (c) a parent id on each Work Item that matches a Feature's id. Containment (issue in project) is enough, and "all Features in the workspace" is an accepted portfolio scope.

## Shape Up model

Shape Up is 37signals' published method and it is written around Basecamp. Source: the free book by Ryan Singer, basecamp.com/shapeup.

| Shape Up concept | What the book says | Basecamp object |
|---|---|---|
| Cycle | "First, we work in six-week cycles." (Introduction). Six weeks is "long enough to finish something meaningful and still short enough to see the end from the beginning" (Bets, Not Backlogs) | None. The appendix prepends the cycle to the project name: "Cycle 4: Autopay" |
| Cool-down | "after each six-week cycle, we schedule two weeks for cool-down", with "no scheduled work" (Bets, Not Backlogs) | None |
| Pitch, betting table | The betting table is "a meeting held during cool-down where stakeholders decide what to do in the next cycle" (Bets, Not Backlogs) | A Basecamp Team for shaping, kept small (appendix) |
| Bet = project | "Create a Basecamp Project for each project in the six-week cycle" (appendix). "We start the project by creating a new Basecamp project and adding the team to it" (Hand Over Responsibility) | **Project** (bucket) |
| Team | "one designer and two programmers or one designer and one programmer" (Bets, Not Backlogs). "Assign projects, not tasks": "We don't start by assigning tasks to anyone" (Hand Over Responsibility) | The project's people |
| Scope | "In practice, we define and track the scopes as to-do lists." "Each scope corresponds to a list name" (Map the Scopes). "the team creates a To-Do List for each scope" (appendix) | **To-do list** |
| Task | "Add design and programming tasks to each scope as To-Do Items" (appendix). Teams start with "imagined tasks" and "discover tasks by doing real work" (Hand Over Responsibility) | **To-do** |
| Loose tasks | "a 'Chowder' list for loose tasks that don't fit anywhere"; more than "three to five items" means a scope is missing (Map the Scopes) | A to-do list named Chowder |
| Nice-to-haves | Recorded as tasks on the scope, marked "with a ~ in front" (Map the Scopes) | A to-do whose title starts with `~` |
| Hill chart | Uphill is "uncertainty, unknowns, and problem solving"; downhill is "certainty, confidence". The team members "intuitively drag the scopes into position" (Show Progress) | One dot per **tracked to-do list** ("Track this on the Hill Chart", appendix) |
| Get one piece done | Build one slice end to end early: "make something tangible and demoable early—in the first week or so" (Get One Piece Done) | No Basecamp object |
| Card Tables | Not mentioned in the book or its Basecamp appendix | None |

What Shape Up says about estimates, backlogs and forecasting:

- "No backlogs, no Kanban, no velocity tracking, none of that." (Foreword, Jason Fried). "Backlogs are a big weight we don't need to carry" (Bets, Not Backlogs).
- Appetite replaces estimates: "Estimates start with a design and end with a number. Appetites start with a number and end with a design." This is "fixed time, variable scope" (Set Boundaries).
- Counting tasks does not show progress. Scopes are "discovered by doing the real work" (Map the Scopes), and "From an outsider's perspective, there's no way to know whether the number of outstanding tasks will go down or up" (Show Progress, under "The tasks that aren't there").
- The deadline is fixed by the cycle: "If they don't finish, by default the project doesn't get an extension" (the circuit breaker, Bets, Not Backlogs).

Confidence: **High**. All of it is quoted from the primary source.

Implications for Lighthouse (interpretation, Medium confidence):

- In a Shape Up project, a Feature's remaining Work Items are **not a stable number**: to-dos are added as they are discovered. A forecast of "remaining to-dos divided by throughput" would therefore look too optimistic early in the cycle, and the book's own argument says it cannot be trusted. Lighthouse should not present such a forecast as if it were meaningful. At most it can show throughput and how far the work is from the cycle's fixed end.
- The question a Shape Up team asks is not "when will this be done?" (the answer is fixed: by the end of the cycle) but "is this scope still uphill?". Lighthouse has no concept for that today.
- Shape Up teams are not the natural customers of a flow-metrics tool. The book explicitly rejects Kanban and velocity tracking. The people who would ask for a Basecamp connector are more likely Basecamp users who run Card Tables as a Kanban board.

## How teams model it in practice

**Shape Up style (to-do lists as scopes).** The book's appendix is the documented practice (above), and Basecamp's Hill Charts page confirms that "the status is attached to lists instead of individual To-dos" and that teams "turn on Hill Chart tracking for each list". Every hill chart update "is saved to the project's history" in the web app. Confidence **High** (book plus product page).

**Card Table style.**
- Basecamp introduces Card Tables as "for working in a different way" and says they "aren't a replacement for To-dos or another way to visualize them. Mix and match To-dos and Card Tables however works best for you." Triage is for things "you *might* do", Not Now is for things "you decide not to prioritize, but don't want to lose", and there is an On Hold section "to put stuff that's blocked or paused for the moment" (37signals update post). Confidence **High** for the product intent.
- An agency write-up (Viget) uses one Card Table for monthly planning with columns such as "Client To Do/Review", "With Design", two "Priority Backlogs", "In Progress" and "This Week's Deploy". It describes cards as "limited to title, assigning someone to the card, due date, and 'Steps'" and wishes for "a tagging system". Confidence **Medium** (one practitioner).
- Agencies commonly use one Basecamp project per client (secondary vendor reviews). Confidence **Low to Medium**.

**How cards are grouped into bigger things.** I found no 37signals guidance and no practitioner write-up describing a convention for grouping cards into a larger piece of work (title prefixes, a separate roadmap card table, linking a card to a to-do list). What the product offers:

- **Project = container.** Each card's `bucket` is its project. Single items (a card, a to-do list) can be moved between projects, and since early 2024 whole tools, card tables included, can be moved too (37signals "Move Tools Between Projects").
- **Several card tables per project** on paid plans ("UNLIMITED tools per project", pricing page). The free plan allows one of each tool.
- **Wormholes.** A card table can have up to four wormholes, each forwarding cards to a column on another card table, and "Passing a wormhole's id as the column_id moves the card to another card table, including one in another project" (API docs). This supports a hand-off pipeline across boards, not a parent-child relation.
- **Steps** on a card are a checklist (`Kanban::Step`, the same thing as a "subtask"), and in Basecamp 5 to-dos can have subtasks too (basecamp.com/5).
- **Backlinks.** `GET /recordings/:id/backlinks.json` lists recordings that reference a given recording "in its rich text content". A team could mention a to-do list in a card. Nothing suggests teams do this systematically.
- **Lineup markers** are account-wide named dates, not containers.

**Basecamp 4 to 5 changes that matter here.** Basecamp 5 (2026) added "loose to-dos" that sit "unattached to any list" and subtasks on to-dos, and it "cleaned up Hill Charts" (basecamp.com/5, basecamp.com/new). Card Tables can now be shared by public link. I found no new grouping or linking feature for cards, no hill chart on card tables, and nothing named "Doors" or "Bubble up" that affects hierarchy. Confidence **Medium** (absence of evidence on two product pages).

## API facts behind the mappings

All from the official `bc3-api` docs, confidence **High** unless marked.

- **Card**: `parent` is its column (for example `Kanban::Triage`), `bucket` is its project, plus `completed` and `subtasks_count`. No `completed_at`. Move with `POST /card_tables/cards/:id/moves.json` and `column_id`.
- **Card step**: `completed`, `due_on`, `assignees`, `parent` = the card. No completion timestamp. Subtasks list at `GET /recordings/:id/subtasks.json`, with `completed` but no completion time.
- **To-do**: `parent` is a `Todolist`, `bucket` is the project, plus `completed`, `completion.created_at` (single view), `starts_on`, `due_on`, `assignees`, `subtasks_count`. A to-do can be created "outside of any to-do list" via `POST /buckets/:project/todosets/:id/todos.json` (a loose to-do; its `parent` type is not shown). A to-do can be moved to another list with `parent_id`.
- **To-do list group**: itself `type: Todolist`, with `parent` = the to-do list it sits in, plus `completed_ratio`. Nesting deeper than one level is not documented.
- **Per-recording events**: documented actions are `created`, `completed`, `uncompleted`, `assignment_changed`. The probe found an undocumented `adopted` action for card column moves (`research.md`, probe results).
- **Account event feed**: has `todo.moved`, but only `card.moved` and `boost.created` carry `details`, so a to-do moved between lists says nothing about the lists. There is **no event for hill chart updates and none for step or subtask completion**.
- **Hill chart**: `GET /todosets/:id/hill.json` returns `enabled`, `stale`, `updated_at` and `dots`, each with `id`, `label`, `color`, `position`, `url`. The example's dot `id` and `url` point at a to-do list. `PUT /todosets/:id/hills/settings.json` only tracks or untracks lists. **No history of dot positions is exposed**, and positions cannot be written. The range of `position` is not documented.
- **Gauge**: one per project, a needle `position` 0 to 100 plus a colour, and "Each time progress is recorded, a new gauge needle is created, preserving a history of updates" (`GET /projects/:id/gauge/needles.json`, `GET /reports/gauges.json`).
- **Reports**: `GET /reports/todos/assigned/:person.json` returns "all active, pending to-dos assigned to the person", to-dos only, no cards, no completed items.

## Candidate mappings

| | M1 Single board | M2 Shape Up, project = Feature | M3 Scope = Feature | M4 Cards, project = Feature |
|---|---|---|---|---|
| Team configures | One card table (pick project, then its card table) | Unclear: a set of projects, or a set of people | One project's to-do set | One or more card tables, across projects |
| Work Item | Card | To-do | To-do | Card |
| Feature | None (or the whole board) | Project (`bucket`) | To-do list (`parent`; walk up from a group to its list) | Project (`bucket`) |
| Feature parent | None | None | Project (`bucket`) | None (no container above a project) |
| Portfolio configures | Nothing useful | All projects in the account, optionally a name filter such as "Cycle 4:" | One or more projects | All projects in the account (Linear precedent), optionally a picked set |
| Raw state, category | Column title; column `type` gives the category | Open / completed only | Open / completed only | Column title; column `type` gives the category |
| Retroactive history | Yes, per-card `adopted` events (confirmed) | Done date only (`completion.created_at`, `completed` event) | Done date only | Yes, per-card `adopted` events (confirmed) |
| Cycle time, WIP, aging | Yes | No (no started state) | No | Yes |
| Throughput | Yes | Yes | Yes | Yes |
| Feature forecast | Not possible | Possible but misleading (scope is discovered) | Possible but misleading; a scope is small, so it ends quickly | Yes, count-based |

Notes per mapping:

- **M1, Team = one Card Table, no Feature.** The simplest connector and it delivers every team-level metric (cycle time, throughput, WIP, aging, blocked through the On Hold section). It breaks the portfolio side entirely: every card of the team lives in the same project, so "Feature = project" would give one Feature containing the whole board. Usable as a team-only integration.
- **M2, Shape Up: Feature = project, Work Item = to-do.** Matches the book. It breaks in three places. First, to-dos have no started state. The only stand-ins are `assignment_changed` (but the book says "Assign projects, not tasks", so assignment is not a reliable start signal), a to-do's `starts_on` date (a plan, not a fact), or a first subtask completion (no timestamp). Second, the team is ill-defined: a Shape Up team is formed per bet, so a stable Lighthouse team would have to be "to-dos in these projects" or "to-dos assigned to these people". The assignment report only lists pending to-dos, so the people route needs the account-wide recordings listing filtered by `assignees`. Third, the forecast contradicts the method it serves (see Shape Up model). Throughput and closed Feature counts still work.
- **M3, Feature = to-do list (scope), parent = project.** This is the cleanest use of Lighthouse's two levels plus a parent: scope, then bet. The hill chart dot of a scope could be shown next to the Feature as the team's own judgement. But it inherits every M2 history gap. Loose to-dos (Basecamp 5) and the Chowder list have no real Feature. Only the current hill position is readable, so Lighthouse would have to record positions itself to show a trend.
- **M4, Work Item = card, Feature = project.** The card's `bucket` gives the parent reference directly, the way a Linear issue's project does, and the portfolio can be "all projects in the account", as in the Linear connector. It fits teams that run one project per client, customer or initiative, each with its own card table, and work across several of them. Breaks: a team that works in one project gets one Feature (it falls back to M1). Projects have no workflow state of their own (only active / archived / trashed is documented on recordings, and the project JSON's status fields were not checked here), so Feature states and dates need a rule, for example "archived = done", or an inference from the child cards. There is no parent level above a project. Several card tables mean several sets of column titles to map to states. A card moved to another project through a wormhole changes its Feature.
- **Rejected: card = Feature, steps = Work Items.** Steps have only `completed`, with no timestamp and no feed event, so they give no throughput or cycle time.
- **Rejected for a first connector: card table = Feature** (on paid plans with several card tables per project). Possible, but I found no evidence that teams use one card table per big piece of work, and it splits the team's flow across boards.

## Recommendation

**Primary: M4, with M1 as its natural degenerate case.** Interpretation, Medium confidence.

- Work Items should be **cards**. Cards are the only Basecamp object with a multi-state workflow, a category hint (column `type`), a blocked signal (On Hold) and confirmed retroactive move history. Lighthouse's team metrics are its core, and only cards feed them.
- The Feature should be the card's **project** (`bucket`), and the portfolio should be all projects in the account, filtered by the configured states. That copies the Linear precedent (Linear project = Feature, portfolio = every project in the workspace) and needs no link Basecamp lacks. A team whose cards all live in one project still gets full team metrics. Its portfolio is simply one Feature, which is honest.
- The team configures **one or more card tables**, picked from a list (project, then card table), instead of a name. The wizard pre-fills To Do / Doing / Done from the column types, the way the Linear wizard uses Linear's state types.
- Leave **to-dos, Shape Up and hill charts out of the first connector**. Without a started state they give throughput only, and forecasting Shape Up scopes would contradict the method's own reasoning. If the maintainer wants a second step, M3 (Feature = to-do list, parent = project, Work Item = to-do, throughput only, no Feature forecast) is the cleanest optional mode. It should come only after a real Shape Up user asks for it.

What should not be pretended: no Feature size (no estimate field), no forecast for to-do based Features, and no started dates for to-dos.

## Probe additions

The existing plan in `research.md` (H0 to H10) stays. For the recommended mapping, add:

1. **Cards across projects.** Does `GET /projects/recordings.json?type=Kanban::Card` (no `bucket`) return cards from every project with `bucket` and `parent` set? The free plan has only one project, so this needs the 30-day Freelancer trial (3 projects).
2. **Card table from a column.** Does a column's `parent` point at its card table, so a card can be traced card → column → card table → project with no extra calls? Needed when a team picks a card table and the sweep is account-wide.
3. **Moving a card to another project** through a wormhole or the move endpoint. Does `bucket` change? Do the per-card events show `adopted` with the old and new columns from different card tables? (Paid trial.)
4. **Project as Feature.** What do `GET /projects.json` and a single project return for status, `created_at`, and archiving (is there an archived date, and are archived projects listed with `status=archived`)? That decides Feature state and closed date.
5. **Several card tables in one project** (paid trial). Do the column titles collide across tables, and does the dock list each table?
6. **On Hold on a fetched card** (H6 already) and whether `adopted` events show a move into and out of On Hold.
7. Only if M3 stays on the table: the `parent` type of a loose to-do (expected the to-do set); whether per-to-do events record `adopted` with list ids when a to-do moves between lists; whether `assignment_changed` appears retroactively for to-dos; the `position` range and `updated_at` in `GET /todosets/:id/hill.json`; and the gauge needle history for the one free project.

## Unverified

- That teams group cards into bigger things by project rather than by some other convention. No practitioner source was found either way. Searched: 37signals help and update posts, Shape Up, Reddit-oriented web searches, consultancy guides. Results were vendor reviews and integration docs.
- That an agency-style "one project per client, one card table each" setup is common. Only secondary review sites say so.
- The exact range of hill chart `position` and that a dot `id` always equals a to-do list id (inferred from the example's URL).
- Project status and archive dates (not checked in the projects docs).
- Whether "loose to-dos" have a to-do set as `parent`.
- Whether Basecamp 5 changed card tables in ways not listed on basecamp.com/5 and basecamp.com/new. "Doors" and "Bubble up" turned up nothing relevant to hierarchy.
- The Lighthouse behaviour for a Feature with no state source (inferring Feature state from child Work Items) was not checked in the code for this document.

## Sources

1. Ryan Singer. *Shape Up*. Foreword by Jason Fried. https://basecamp.com/shapeup/0.1-foreword. Accessed 2026-10-12. (primary)
2. Shape Up, Introduction. https://basecamp.com/shapeup/0.3-chapter-01. Accessed 2026-10-12.
3. Shape Up, Set Boundaries. https://basecamp.com/shapeup/1.2-chapter-03. Accessed 2026-10-12.
4. Shape Up, Bets, Not Backlogs. https://basecamp.com/shapeup/2.1-chapter-07. Accessed 2026-10-12.
5. Shape Up, The Betting Table. https://basecamp.com/shapeup/2.2-chapter-08. Accessed 2026-10-12.
6. Shape Up, Hand Over Responsibility. https://basecamp.com/shapeup/3.1-chapter-10. Accessed 2026-10-12.
7. Shape Up, Get One Piece Done. https://basecamp.com/shapeup/3.2-chapter-11. Accessed 2026-10-12.
8. Shape Up, Map the Scopes. https://basecamp.com/shapeup/3.3-chapter-12. Accessed 2026-10-12.
9. Shape Up, Show Progress. https://basecamp.com/shapeup/3.4-chapter-13. Accessed 2026-10-12.
10. Shape Up, Decide When to Stop (glossary for uphill/downhill, imagined/discovered tasks). https://basecamp.com/shapeup/3.5-chapter-14. Accessed 2026-10-12.
11. Shape Up, Appendix: How to Implement Shape Up in Basecamp. https://basecamp.com/shapeup/4.0-appendix-01. Accessed 2026-10-12.
12. Basecamp. Hill Charts. https://basecamp.com/hill-charts. Accessed 2026-10-12. (official, product page)
13. 37signals. "New in Basecamp: Card Tables". https://updates.37signals.com/post/new-in-basecamp-card-tables. Accessed 2026-10-12. (official)
14. 37signals. "New in Basecamp: Move Tools Between Projects". https://updates.37signals.com/post/new-in-basecamp-move-tools. Accessed 2026-10-12. (official; content seen only through a search summary)
15. Basecamp. Basecamp 5. https://basecamp.com/5. Accessed 2026-10-12. (official)
16. Basecamp. New in Basecamp. https://basecamp.com/new. Accessed 2026-10-12. (official)
17. Viget. "Project Managers, Pull Up a Chair to Basecamp's New Card Table Feature". https://viget.com/articles/project-manager-basecamp-card-table-feature. Accessed 2026-10-12. (practitioner, Medium)
18. Basecamp API. Hill charts. https://github.com/basecamp/bc3-api/blob/master/sections/hill_charts.md. Accessed 2026-10-12.
19. Basecamp API. Card table cards. https://github.com/basecamp/bc3-api/blob/master/sections/card_table_cards.md. Accessed 2026-10-12.
20. Basecamp API. Card table steps. https://github.com/basecamp/bc3-api/blob/master/sections/card_table_steps.md. Accessed 2026-10-12.
21. Basecamp API. Subtasks. https://github.com/basecamp/bc3-api/blob/master/sections/subtasks.md. Accessed 2026-10-12.
22. Basecamp API. Card table wormholes. https://github.com/basecamp/bc3-api/blob/master/sections/card_table_wormholes.md. Accessed 2026-10-12.
23. Basecamp API. To-dos. https://github.com/basecamp/bc3-api/blob/master/sections/todos.md. Accessed 2026-10-12.
24. Basecamp API. To-do list groups. https://github.com/basecamp/bc3-api/blob/master/sections/todolist_groups.md. Accessed 2026-10-12.
25. Basecamp API. Events. https://github.com/basecamp/bc3-api/blob/master/sections/events.md. Accessed 2026-10-12.
26. Basecamp API. Event feed. https://github.com/basecamp/bc3-api/blob/master/sections/event_feed.md. Accessed 2026-10-12.
27. Basecamp API. Gauges. https://github.com/basecamp/bc3-api/blob/master/sections/gauges.md. Accessed 2026-10-12.
28. Basecamp API. Lineup markers. https://github.com/basecamp/bc3-api/blob/master/sections/lineup_markers.md. Accessed 2026-10-12.
29. Basecamp API. Backlinks. https://github.com/basecamp/bc3-api/blob/master/sections/backlinks.md. Accessed 2026-10-12.
30. Basecamp API. Reports. https://github.com/basecamp/bc3-api/blob/master/sections/reports.md. Accessed 2026-10-12.
31. Basecamp API. README (endpoint index, incl. Hill charts, Gauges, Lineup Markers, Subtasks, Card table wormholes). https://github.com/basecamp/bc3-api/blob/master/README.md. Accessed 2026-10-12.
32. Basecamp. Pricing ("UNLIMITED tools per project" on paid plans). https://basecamp.com/pricing. Accessed 2026-10-12 (via `research.md`).
