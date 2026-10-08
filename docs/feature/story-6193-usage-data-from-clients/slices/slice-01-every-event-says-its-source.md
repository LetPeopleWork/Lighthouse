# Slice 01 — Every usage event says where it came from

**Story** #6193 / US-01 · **Job** `job-maintainer-know-if-a-shipped-feature-landed` · **Repo** Lighthouse ·
**Estimate** ~5h

## Goal

Every event forwarded to the collector carries `source` (`Browser`, `Cli` or `Mcp`), and a client can tell
that this Lighthouse labels sources before it sends anything.

## IN scope

- Optional batch-level `source` on `POST /usagedata/events`; absent = `Browser`; other values `400` (D2).
- `source` attached to every forwarded event, beside the instance properties (M10).
- The "understands source" signal on `GET /usagedata/state` (D3); shape chosen in DESIGN.
- `docs/settings/usagedata.md`: `Source` row in "Every event carries these"; docs-parity test.
- Lighthouse `ARCHITECTURE.md`: the usage-data concept names three sources.

## OUT of scope

- Any client change (slices 02–05). Any web change: the browser keeps sending no `source`.
- Rewriting the page's browser-only wording (slice 02, when it first becomes false).

## Learning hypothesis

**Disproves that a declared, unverified source is enough to split the data** — if the existing shape check or
the drain cannot carry a batch-level value to each event without a second string path, the label belongs
somewhere else and DESIGN revisits D2.

## Acceptance criteria

AC-01.1 … AC-01.6. Risk carrier: **AC-01.6**, a real web forecast on the dev instance arriving at a capture
stub with `source: Browser`.

## Dependencies

None (the pipe is in `main`).

## Reference class

Epic #5733 slice 04 — adding `RefinementVerdict` to the DTO and the publisher (#5980).

## Pre-slice SPIKE

None.
