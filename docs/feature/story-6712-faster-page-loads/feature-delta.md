# Story 6712 — Faster page loads on slow connections

ADO User Story 6712 "Faster page loads on slow connections: compression, shared cache, smaller update check" (no
parent Epic, tag `Release Notes`). Product facts live in `handoff.json` (nwdes); this file holds Lighthouse's own
records: the maintainer decisions and the checklists.

## Waves

- DISCOVER: covered by the measuring spike of 2026-11-27, `docs/feature/api-shape-evaluation/spike/findings.md`
  (the problem and its cause, measured on 16 pages at three network speeds).
- DIVERGE: covered by the API-shape evaluation (`docs/feature/api-shape-evaluation/diverge/`) and ADR-288, which
  kept REST and named these fixes as the cheaper way to speed.
- DISCUSS: 2026-11-28.

## Maintainer decisions (DISCUSS, 2026-11-28)

- **How "faster" is proven:** bytes and calls, measured with the spike's probes (`measure-bytes.mjs`,
  `measure-calls.mjs`) before and after. Slow-4G times are reported as evidence, not promised.
- **Permission freshness:** like today. One shared answer, asked again when the user moves to another page, and at
  once after the user's own create or grant actions.
- **Team, Portfolio and licence lists:** the same rule. The user's own create, rename or delete shows at once;
  a change by someone else shows on the next page move.
- **Update check:** release notes load when the new-version dialog opens. The pop-up and footer need only the
  newest version number. If the notes cannot be loaded, the dialog shows a message and a Retry button.
- **Parallel version checks** keep today's error rule: any failure shows the footer error.
- **In scope beyond permissions:** Teams and Portfolios lists, licence, and Team settings on the Features tabs.
  **Out:** moving the other `useEffect` data hooks, and the Portfolio page's four-step load chain.

## Found during DISCUSS (for DESIGN)

- The Story says "the `.gz` files the build already makes". None exist: `vite.config.ts` has no compression step,
  and the pipeline makes none. The spike's 3.2× came from its own `gzip_ratio.py`. So compression is new work, at
  build or at serve time. Open, owner Design.
- `/version/new` is called only when an update exists (`LighthouseVersion.tsx:97-101`), so the 368 KB reach users
  who run an older version, which is most self-hosted instances between releases.
- The CLI and MCP clients call only `/version/current`.

## Slices (product, independent of each other)

| Slice | What it delivers |
|---|---|
| S1 | Walking skeleton: the server sends the frontend's JavaScript, and safe API answers, compressed |
| S2 | One shared permissions answer behind `useRbac()`, as fresh as today |
| S3 | One shared Teams list, Portfolios list and licence status, the user's own changes at once |
| S4 | Features tabs stop loading full Team settings |
| S5 | No release notes on page load; notes load in the dialog; footer checks run in parallel |

S5 changes something a user sees (the dialog's loading and could-not-load looks), so it needs a sketch at the
start of DELIVER. S1–S4 change nothing a user sees.

### DISCUSS checklist

| Item | Answer |
|---|---|
| RBAC impact | No new action, page or data, so no new permission. But S2 changes **how** `useRbac()` gets its answer: one shared answer instead of one per component. All gating still comes from `useRbac()`, and freshness stays as today (P4, P5). The could-not-load fallback and retry stay as today (P6). No backend change to `IRbacAdministrationService`. |
| Lighthouse-Clients CLI / MCP | N/A, because the clients call only `/version/current`, which stays. If DESIGN slims or adds a version route, or compression changes what a client receives, DESIGN checks again. A client that sends no `Accept-Encoding` gets answers as today (P1). |
| Website marketing surface | N/A, because faster pages on a slow connection are a quality fix, not a new capability worth a page. The release note carries it (tag `Release Notes` on the Story). |
| Context map | Touches no domain context. S1 is the **platform lane** (how the host serves files and answers). S2 reads **Access** (the authorization summary). S3 reads **Organisation set-up** (Teams and Portfolios as units) and **Licensing**. S4 reads Team settings (Organisation set-up). S5 is the platform lane (the version and update check). No new glossary term or concept, no domain event published or reacted to: these are read caches and transport, not domain behaviour. UI surface after ADR-271: nothing moves; the new-version dialog stays where it is (the footer). ADR-270 and `ARCHITECTURE.md` §4 need no change. |
| Improvement backlog (`/continuous-improvement before`) | N/A, because 6712 is a Story with no parent Epic, not a new feature Epic. It is itself the speed follow-up of the API-shape evaluation. |

## DESIGN (2026-11-28)

Design facts live in `handoff.json` (rendered to `architecture.md`); the compression rule is
`docs/product/architecture/ADR-004.md` (Proposed). Choices:

- **Compression at serve time:** ASP.NET Core response compression (Brotli and Gzip, `Optimal`), after
  `UseForwardedHeaders` and before the static files. No build step, no new package. Measured on today's bundle:
  3.22× gzip, 3.23× Brotli; `Fastest` gives only 2.32×. Rejected: `MapStaticAssets` (needs a manifest built after
  `pnpm build`, empty in dev, replaces the no-cache static path) and a Vite precompression plugin.
- **BREACH rule:** compress only GET/HEAD, compressible types, when the browser asks, never the SignalR hub. Owner:
  `BreachSafeResponseCompressionProvider` in `Platform/Host`. No GET answer carries a secret today; the API key
  comes back from a POST. Residual risk: a future GET that returns a secret beside reflected text.
- **Features tabs:** `PortfolioDetail.fetchPortfolio` loads Team settings per involved Team into a state nothing
  reads; the fetch goes. Team pages never loaded them.
- **S2/S3:** a shared query whose answer carries the page it was asked on (`usePageFreshQuery`), a
  `PageMoveRefresh` inside the Router, and refresh hooks after the user's own changes. `useRbac` keeps its
  `RbacState` shape. Quint model `design/shared_page_reads.qnt` for the freshness rules.
- **S5:** new `GET version/newest` (the version of the first new release); `/version/new` stays for the dialog.
  Footer: current, distribution and hasupdate together, then newest and updateSupported. The dialog has loading,
  loaded and could-not-load (message + Retry) states.
- **Order:** S3 builds on S2's freshness owner, so S2 lands first.
