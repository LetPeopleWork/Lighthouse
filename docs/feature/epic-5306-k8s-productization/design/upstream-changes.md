# Upstream Changes — epic-5306-k8s-productization (DESIGN → product owner review)

DESIGN decisions that change assumptions in the DISCUSS slice docs. None invalidate a story or acceptance criterion; they refine slice *scope wording* and one mechanism. Product owner to confirm and (optionally) touch the slice docs.

## 1. Slice-01 walking skeleton now includes bundled Postgres (ADR-080)

- **Original** (`slices/slice-01-walking-skeleton-one-command-install.md`, *Out of scope*): "Postgres / MCP / OIDC values (slice 03)." The walking skeleton was framed as API-pod-only.
- **Change**: the chart is **Postgres-only with no SQLite path** (ADR-080), so the API needs a database from the first install. Slice-01 therefore renders the **bundled Postgres StatefulSet** (default `postgresql.enabled: on`) alongside the API. OIDC, MCP, and the full `values-enterprise.yaml` stay in slice-03.
- **Impact**: still one command (`helm install l8e ./chart`); the walking-skeleton hypothesis ("a pile of manifests → a running Lighthouse with one command") is unchanged and arguably stronger (a real DB comes up too). The slice-01 *Out of scope* line should drop "Postgres" (keep MCP/OIDC deferred).
- **AC impact**: none — slice-01's embedded ACs ("all chart workloads reach Ready", "exactly one API workload", "NOTES.txt access URL") hold; "all workloads" now includes the Postgres pod.

## 2. Publish mechanism refined: docs-tree on existing Pages, not gh-pages/chart-releaser (ADR-083)

- **Original** (DISCUSS + `slices/slice-04`): "publish to a GitHub Pages Helm repo (LPW org)", implicitly via the usual `gh-pages`/chart-releaser pattern.
- **Change**: the repo already publishes docs via a **single artifact-based Pages deploy** (`pages.yml`). GitHub Pages allows one source per repo, so a `gh-pages` branch (chart-releaser's model) would conflict. The Helm repo instead lives at **`docs/charts/`**, packaged by `helm package` + `helm repo index --merge` **in the existing release stage**, and shipped by the existing `pages.yml`. The no-silent-overwrite gate becomes an explicit version-present check.
- **Impact**: same outcome (a public GitHub Pages Helm repo; `helm repo add` + install works from a no-source machine). Slice-04's *In scope* "Helm repo index generation + push to the GitHub Pages branch" should read "…to `docs/charts/` served by the existing Pages deploy" (no branch push). All slice-04 ACs hold verbatim.

## 3. "Single-container shape" clarified (ADR-080/081)

- **Original**: US-01 edge case "default install preserves the single-container shape" (Marco, defaults only).
- **Clarification**: "single-container shape" / the standalone gate means **`frontend.mode: embedded` — one *app* workload serving the SPA in-process**, NOT "no database workload". The chart always renders a database (bundled Postgres by default, or external). The standalone *image* is unchanged (keeps SQLite). The chart's "simple shape" = embedded frontend + bundled Postgres + MCP off.
- **Impact**: no AC change; the standalone-gate guard test asserts "default values → embedded, exactly one *API* workload" (the Postgres pod is a separate, expected workload). Avoids a misread that "single container" forbids the bundled DB pod.

---

**Recommendation**: accept all three as scope-wording refinements; optionally edit slice-01 and slice-04 *In/Out of scope* lines to match. No re-estimation needed (still ≤1 day each; the bundled-DB template is ~4 small files folded into slice-01).
