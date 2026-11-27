# Spike: how the frontend talks to the API today (2026-11-27)

**Question.** Does the UI make so many API calls, or calls so dependent on each other, that one endpoint
(GraphQL, or another shape that fetches "what the page needs" at once) would make Lighthouse noticeably faster?

**Evidence that would change the decision.** Pages whose time to data is dominated by the number of calls or by
chains of calls that wait on each other. If time goes elsewhere (bytes, JavaScript, server work), an API change
does not buy speed, and the case for it must rest on something else (developer experience, contracts).

**Result in one line.** On a fast line every page has its data in 0.1–1.2 s. On a slow line, 75–90 % of the wait is
**JavaScript bytes, sent uncompressed**; the API calls after that take 0.2–1.4 s. The call count and the chains
are real but small; GraphQL would save about 0.5 s on the slowest pages on slow 4G, compression about 4–10 s.

## Set-up

- Worktree build at `5c26f30ee`: frontend `vite build` (production bundle), backend Debug build, SQLite, no auth
  (RBAC off), free licence, demo scenario 2 "Product Launch" (2 Teams, 1 Portfolio).
- Chromium via Playwright, new browser context per open (empty cache). Three network profiles via CDP:
  fast (local, no throttle), **slow 4G** (150 ms latency, 1.6 Mb/s down), **slow 3G** (400 ms, 400 kb/s).
- **Cold**: open the URL directly (first visit, or a reload). **In-app**: load the Overview, then navigate to the
  page inside the app (what a click does; JavaScript of the shell already loaded, page chunks not).
- 16 pages cold (3 runs fast and slow 4G, 1 run slow 3G), 15 pages in-app (3 runs fast and slow 4G). Medians.
- Probes: `measure-calls.mjs`, `measure-bytes.mjs`, `analyse.py`, `timeline.py`, `gzip_ratio.py`.
  Raw data: `results.json`, `results-warm.json`, `results-bytes.json`; tables: `summary-cold.md`, `summary-warm.md`.

**Limits.** Demo data is small (2 Teams), so payloads and server times are at the low end; a large organisation
has bigger answers and more Teams in the shell's lists, which makes bytes matter *more*, not less. Throttling is
emulated, one machine, Debug backend. "Chain depth" is inferred from timing (a call that starts within 60 ms of
another's end is counted as waiting on it); at fast speed this is noisy, so only slow 4G depths are quoted.
Two rows of the bytes probe (Portfolio pages over HTTP/2) recorded only 10 KB of API data and are left out.

## Numbers

### Cold load (first visit or reload)

| | Typical page | Metrics pages (Team, Portfolio) |
|---|---|---|
| API calls | 25–42 | 52–53 |
| … of which the app shell (same on every page) | ~26 | ~26 |
| … of which the page itself | 0–11 | 26–27 |
| Calls repeated within the load | 4–14 | 8 |
| Data complete, fast | 0.4–1.2 s | 0.7 s |
| Data complete, slow 4G | 8–20 s | 12–20 s |
| Data complete, slow 3G | 27–80 s | 32–82 s |
| JavaScript downloaded per cold load | 1.7–3.0 MB | 2.7–3.0 MB |
| API data per cold load | ~380–520 KB | ~500–520 KB |

### In-app navigation (the common case)

| Page | Calls | Data complete, fast | Slow 4G: wait for page JS | Slow 4G: data complete | Chain depth |
|---|---|---|---|---|---|
| Team tabs (Features, Forecasts, Refinement, Reports) | 6–8 | 0.07–0.4 s | 5.3 s | 5.6–5.9 s | 2 |
| Team › Metrics | 32 | 0.8 s | 5.3 s | 6.7 s | 2 |
| Team › Settings | 14 | 0.2 s | 5.3 s | 6.2 s | 2 |
| Portfolio tabs (Features, Deliveries, Reports) | 9–11 | 0.1–0.15 s | 7.4 s | 8.3–8.5 s | 3–4 |
| Portfolio › Metrics | 31 | 0.2 s | 7.4 s | 9.0 s | 4 |
| Portfolio › Settings | 21 | 0.27 s | 7.4 s | 8.9 s | 5 |
| Features, Reports, Settings pages | 4–5 | 0.03–0.36 s | 0–0.9 s | 0.2–1.1 s | 1 |

### Where a slow cold load goes (slow 4G, Team › Metrics)

```
0 s ────────── 5.9 s ─────────────────────────── 16–17 s ── 18.3 s
  main bundle      shell calls start, but queue        page JS      27 metric calls
  (no API yet)     behind ~2.7 MB of page JavaScript   arrives      all done in ~1.5 s
```

Twelve shell calls of 12 bytes to 4 KB each took ~10 s *each*, because they share the line with uncompressed
JavaScript. Once the JavaScript was in, the 27 metric calls finished within ~1.5 s, in parallel.
HTTP/2 (https) and HTTP/1.1 (http) gave the same times on the four pages measured on both, so the six-connection
limit of HTTP/1.1 is not what costs the time either.

## Findings

1. **Bytes, not calls, decide a slow page.** The server compresses nothing. `Program.cs:194` serves the
   frontend with `UseStaticFiles`, which ignores the `.gz` files the build already makes, and there is no response
   compression for the API. All JavaScript is 3.4 MB raw against 1.0 MB gzipped (3.2×). On slow 4G that is the
   difference between ~5–7 s and ~2 s of waiting for page JavaScript on *every* in-app navigation to a Team or
   Portfolio, and ~10 s on a cold load.
2. **The call count is high on cold load, but most of it is the app shell, and a quarter is duplicates.**
   About 26 calls run on every cold load whatever the page shows (version, licence, RBAC, terminology, teams,
   portfolios, update status, …). `GET /authorization/my-summary` alone is repeated 72 times over the 16 pages:
   `hooks/useRbac.ts:44` fetches it in a `useEffect` per component that uses the hook, with no shared cache.
   `teams`, `portfolios`, `license` and `teams/{id}/settings` repeat for similar reasons. About half the data
   hooks (37 files) fetch through `useEffect`, the other half (35 files) through TanStack Query.
3. **The chains are short.** The longest chains on a cold load are all in the app shell: the version footer
   awaits five independent calls one after another (`LighthouseVersion.tsx:91-105`), and licence → teams. The
   only chain in page data is Portfolio's in-app load: portfolio → portfolio settings → team settings →
   feature list, 4 steps, ~0.7 s on slow 4G. Team pages have 2 steps.
4. **One answer is far too big.** `GET /version/new` sends 368 KB of release notes on every cold load, to show
   whether an update exists. That is most of the cold load's API bytes. The Features tabs also load full Team
   settings (32 KB) to render a list.
5. **Metrics pages are the one place where many calls meet.** 27–28 parallel calls, one per widget. On a fast
   line they finish in 0.2–0.8 s; on slow 4G in ~1.4 s after the page JavaScript has arrived.

## What GraphQL (or any single-request read) would and would not change

| Effect | Estimate on slow 4G | Also reachable without GraphQL? |
|---|---|---|
| Metrics pages: 27 calls → 1 | saves ~0.5–1 s of ~6.7–9 s in-app | Yes: one composite read per page, or HTTP/2 already multiplexes |
| Portfolio chain: 4 steps → 1 | saves ~0.5 s of ~8.5 s in-app | Yes: one page read, or fetch in parallel |
| Duplicates gone | fewer calls, little time (they run in parallel) | Yes: a shared query cache (TanStack Query) |
| Over-fetching gone (`version/new`, Team settings for a list) | ~368 KB less per cold load | Yes: a slimmer endpoint |
| Uncompressed JavaScript | none | Only by compression; GraphQL does not touch it |

So **speed is not a reason to move to GraphQL** in Lighthouse today. The reasons that remain open are not
measured by this spike: developer experience (a typed schema, the UI picks fields, fewer service classes),
one contract for UI and clients, and whether 265 REST actions and 65 client endpoints are a cost worth reducing.

## Decision still needed (maintainer)

1. Is the case for one endpoint *only* about speed? If yes, this spike answers it: no, and the ADR records that,
   with the cheaper fixes above as the alternatives. If no, the open drivers (developer experience, contract
   shape) need step 2 (research) and step 3 (compare directions) before the ADR.
2. Whether the cheap fixes (compression, shared RBAC/teams/portfolios cache, slimmer `version/new`, parallel
   version checks) become improvement items now, independent of the GraphQL question.
