import { describe, expect, it } from "vitest";
import type { BlockedCountSnapshot } from "../../../models/BlockedCountSnapshot";
import { computeBlockedTrend } from "./blockedTrend";

/**
 * DISTILL RED-pending specs (Epic 5074, slice-06 / B3). Job:
 * job-delivery-lead-tell-blocked-trend-vs-last-period. describe.skip = RED scaffold enabled in DELIVER
 * one at a time (ADR-025). The trend feeds the EXISTING WidgetShell trend chrome (direction up/down/flat),
 * asserted end-to-end against the widget-trend-* test-ids in BaseMetricsView.test.tsx during DELIVER.
 */
describe("computeBlockedTrend — previous-period trend (B3)", () => {
	const snap = (
		recordedAt: string,
		blockedCount: number,
	): BlockedCountSnapshot =>
		({ recordedAt, blockedCount }) as BlockedCountSnapshot;

	const start = new Date("2026-06-08");
	const end = new Date("2026-06-14");
	// Previous-period boundary = day before the selected range start (2026-06-07).

	it("reports an up/worse direction when current exceeds the prior-period boundary", () => {
		const history = [snap("2026-06-07", 3), snap("2026-06-14", 9)];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend?.direction).toBe("up");
	});

	it("reports a down/better direction when current is below the prior-period boundary", () => {
		const history = [snap("2026-06-07", 9), snap("2026-06-14", 3)];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend?.direction).toBe("down");
	});

	it("reports flat when current equals the prior-period boundary", () => {
		const history = [snap("2026-06-07", 5), snap("2026-06-14", 5)];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend?.direction).toBe("flat");
	});

	// Two cases were DELETED here by Story 5508 slice 02, superseded by design (D2), not regressed:
	//   - "marks no-baseline (not a direction) when no snapshot exists at or before the prior-period
	//     boundary" — that path now yields a zero baseline and a real direction (AC2).
	//   - "marks no-baseline for empty or null history" — now flat (AC3).
	// The block below replaces both. Note the inline marker in the slice-02 docstring named only the
	// second; the first was superseded too. The `noBaseline` field stays on TrendPayload for other
	// widgets, and the marker path itself survives for AC2b.
});

/**
 * DISTILL RED-pending specs — Story 5508 (widget-loose-ends) slice 02, US-03.
 *
 * DISCUSS D2 (re-decided 2026-07-19 after UPSTREAM-4). TWO changes, and the order matters:
 *
 * 1. THE FETCH WINDOW (US-03 AC0, asserted in `useMetricsData.test.ts` — NOT here). The history was
 *    fetched with the dashboard's own [startDate, endDate] while the baseline is looked up at
 *    `startDate − 1 day`, one day outside it. `latestAtOrBefore(history, boundary)` was therefore
 *    undefined on EVERY instance and every range, and the widget has never rendered a comparison.
 *    DELIVER widens the fetch to start at `startDate − 1 day` FIRST. Without that, everything below
 *    is untestable in the real app: these cases build a pre-boundary snapshot by hand, which the
 *    shipped wiring could never supply. That gap is exactly why a green suite hid a live defect.
 *
 * 2. THE ZERO BASELINE (AC2-AC4, below). With a real baseline now reachable, an absent one means what
 *    the docstring on `noBaselineTrend` always claimed: a forward-only history that genuinely
 *    predates the boundary. Only in that residual case does the count fall back to 0, so a day-one
 *    instance reads "+N since we started recording" rather than a dash that looks like breakage.
 *
 * Read together: the fallback is now a rare young-instance path, not the everyday one. Shipping (2)
 * without (1) would have made every instance read "+N" forever and hidden the real comparison — a
 * visibly broken widget traded for an invisibly wrong one.
 *
 * SUPERSEDES the "marks no-baseline for empty or null history" case above — DELIVER removes it when
 * un-skipping this block. The `noBaseline` field itself stays on TrendPayload for other widgets.
 *
 * describe.skip = RED scaffold; DELIVER enables it (ADR-025).
 */
describe("computeBlockedTrend — absent baseline counts as zero (Story 5508 slice 02)", () => {
	const snap = (
		recordedAt: string,
		blockedCount: number,
	): BlockedCountSnapshot =>
		({ recordedAt, blockedCount }) as BlockedCountSnapshot;

	const start = new Date("2026-07-01");
	const end = new Date("2026-07-14");
	// Previous-period boundary = 2026-06-30.

	it("keeps comparing against a real snapshot at or before the boundary (AC1, unchanged)", () => {
		const history = [snap("2026-06-30", 3), snap("2026-07-14", 5)];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend?.direction).toBe("up");
		expect(trend?.percentageDelta).toBe("+66.7%");
	});

	it("labels each side with the day it was actually measured on, not the range edges (AC5)", () => {
		// The labels ARE the assumed-vs-measured signal since the tooltip was dropped
		// (US-03 AC5b): a measured current must name its own recordedAt, and an assumed
		// baseline must name the boundary day the zero stands for — never a fabricated date.
		const measured = computeBlockedTrend(
			[snap("2026-06-30", 3), snap("2026-07-10", 5)],
			start,
			end,
		);
		expect(measured?.currentLabel).toBe("2026-07-10");
		expect(measured?.previousLabel).toBe("2026-06-30");

		const assumedBaseline = computeBlockedTrend(
			[snap("2026-07-10", 5)],
			start,
			end,
		);
		expect(assumedBaseline?.currentLabel).toBe("2026-07-10");
		expect(assumedBaseline?.previousLabel).toBe("2026-06-30");
	});

	it("still picks the LATEST snapshot at or before the boundary (AC1, unchanged)", () => {
		const history = [
			snap("2026-06-20", 9),
			snap("2026-06-28", 5),
			snap("2026-07-14", 2),
		];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend?.direction).toBe("down");
	});

	it("treats a missing boundary snapshot as a baseline of zero and renders a direction (AC2)", () => {
		const history = [snap("2026-07-14", 4)];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend?.direction).toBe("up");
		expect(trend?.previousValue).toBe("0");
		expect(trend?.currentValue).toBe("4");
		expect(trend?.noBaseline).toBeFalsy();
	});

	it("renders flat — never a false arrow — when the baseline and the current count are both zero (AC3)", () => {
		const history = [snap("2026-07-14", 0)];

		expect(computeBlockedTrend(history, start, end)?.direction).toBe("flat");
	});

	it("renders flat on an entirely empty history rather than a no-baseline placeholder (AC3)", () => {
		for (const empty of [
			computeBlockedTrend([], start, end),
			computeBlockedTrend(null, start, end),
		]) {
			expect(empty?.direction).toBe("flat");
			expect(empty?.noBaseline).toBeFalsy();
		}
	});

	it("omits the percentage delta when the baseline is zero, keeping the absolute values (AC4)", () => {
		const trend = computeBlockedTrend([snap("2026-07-14", 4)], start, end);

		expect(trend?.percentageDelta).toBeUndefined();
		expect(trend?.previousValue).toBe("0");
	});

	it("does not present a fabricated snapshot date for the synthetic zero baseline (AC5)", () => {
		const history = [snap("2026-07-14", 4)];

		const trend = computeBlockedTrend(history, start, end);

		// The label must be PRESENT — the widget needs something to render against the zero baseline —
		// but must never claim a recordedAt that never existed. Asserting only the second half would
		// pass vacuously against today's undefined label, so presence is asserted first.
		expect(trend?.previousLabel).toBeDefined();
		expect(history.map((s) => s.recordedAt)).not.toContain(
			trend?.previousLabel,
		);
	});

	/**
	 * AC5b — added 2026-07-19 by the second-pass review gate, SUPERSEDED the same day by user
	 * decision during manual verification on a running instance. AC5b originally required the
	 * assumed baseline to announce itself through an explanatory `hintText` tooltip; the product
	 * owner reviewed it live and removed the tooltip as UI bloat ("no need to bloat this, just show
	 * bare numbers"). The trend now renders bare numbers in both cases.
	 *
	 * What survives is `previousLabel` (AC5), which is now the SOLE signal separating an assumed
	 * baseline from a measured one — hence this test, rewritten to pin that distinction rather than
	 * deleted. Measured: the label is a real `recordedAt` drawn from the history. Assumed: the label
	 * is the previous-period boundary DAY, which appears nowhere in the history and is never a
	 * fabricated `recordedAt`.
	 *
	 * Known residual: when a measured baseline's `recordedAt` happens to fall exactly ON the boundary
	 * day, the two labels are byte-identical and the distinction is invisible. The tooltip used to
	 * cover that overlap; nothing does now. Recorded here rather than silently dropped.
	 */
	it("distinguishes an assumed baseline from a measured one through previousLabel alone (AC5, ex-AC5b)", () => {
		const assumedHistory = [snap("2026-07-14", 4)];
		const measuredHistory = [snap("2026-06-28", 3), snap("2026-07-14", 4)];

		const assumed = computeBlockedTrend(assumedHistory, start, end);
		const measured = computeBlockedTrend(measuredHistory, start, end);

		// Measured: the label IS one of the history's own recordedAt values.
		expect(measuredHistory.map((s) => s.recordedAt)).toContain(
			measured?.previousLabel,
		);
		// Assumed: the label is the boundary day, present nowhere in the history.
		expect(assumed?.previousLabel).toBe("2026-06-30");
		expect(assumedHistory.map((s) => s.recordedAt)).not.toContain(
			assumed?.previousLabel,
		);

		// Neither case carries a tooltip any more — bare numbers only (user decision, 2026-07-19).
		expect(assumed?.hintText).toBeUndefined();
		expect(measured?.hintText).toBeUndefined();
	});

	/**
	 * AC2b — added 2026-07-19. The THIRD `noBaselineTrend()` return site (UPSTREAM-3 counted three;
	 * the scenario list covered only two). History exists but holds nothing at or before endDate —
	 * e.g. a range selected entirely before recording began. The zero-baseline rule must not silently
	 * invent a current value here: with no measurement at either end there is nothing to compare, and
	 * an arrow would be pure fabrication rather than the defensible day-one assumption of AC2.
	 */
	it("does not fabricate a direction when the history holds nothing at or before the range end (AC2b)", () => {
		const history = [snap("2026-08-20", 7)]; // after `end` (2026-07-14)

		const trend = computeBlockedTrend(history, start, end);

		// Pin the marker, not just the absence of an arrow: a zero-vs-zero comparison also
		// reads "none", so asserting only the direction let the whole AC2b branch be deleted
		// without a test noticing (found by mutation testing).
		expect(trend?.noBaseline).toBe(true);
		expect(trend?.hintText).toBeTruthy();
		expect(trend?.currentValue).toBeUndefined();
		expect(trend?.previousValue).toBeUndefined();
		expect(trend?.direction).toBe("none");
	});
});
