import { describe, expect, it } from "vitest";
import type { BlockedCountSnapshot } from "../../../models/BlockedCountSnapshot";
import { computeBlockedTrend } from "./blockedTrend";

const snap = (recordedAt: string, blockedCount: number): BlockedCountSnapshot =>
	({ recordedAt, blockedCount }) as BlockedCountSnapshot;

/**
 * The trend feeds the widget's existing up/down/flat chrome. That it reaches the
 * screen is asserted against the widget-trend-* test ids in BaseMetricsView.test.tsx;
 * what the arrow says is decided here.
 */
describe("computeBlockedTrend — previous-period trend", () => {
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
});

/**
 * An absent baseline counts as a blocked count of zero rather than a neutral
 * placeholder, so a day-one instance reads "+N since we started recording" rather
 * than a dash that looks like breakage.
 *
 * That is only defensible because the fetch window was widened first. The history
 * used to be fetched over the dashboard's own selected range while the baseline is
 * looked up one day before its start — one day outside the fetched window — so the
 * lookup came back empty on every instance and every range, and the widget had never
 * once rendered a real comparison. The cases below build a pre-boundary snapshot by
 * hand, which the shipped wiring could not supply; that is exactly how a green suite
 * hid a live defect. Assuming zero without widening the window first would have made
 * every instance read "+N" forever — a visibly broken widget traded for an invisibly
 * wrong one.
 *
 * The `noBaseline` marker itself survives for the one case further down where nothing
 * was measured at either end.
 */
describe("computeBlockedTrend — absent baseline counts as zero", () => {
	const start = new Date("2026-07-01");
	const end = new Date("2026-07-14");
	// Previous-period boundary = 2026-06-30.

	it("keeps comparing against a real snapshot at or before the boundary", () => {
		const history = [snap("2026-06-30", 3), snap("2026-07-14", 5)];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend?.direction).toBe("up");
		expect(trend?.percentageDelta).toBe("+66.7%");
	});

	it("labels each side with the day it was actually measured on, not the range edges", () => {
		// With no tooltip to explain itself, the labels are the only signal telling an
		// assumed baseline from a measured one: a measured current names its own
		// recordedAt, an assumed baseline names the boundary day the zero stands for.
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

	it("still picks the LATEST snapshot at or before the boundary", () => {
		const history = [
			snap("2026-06-20", 9),
			snap("2026-06-28", 5),
			snap("2026-07-14", 2),
		];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend?.direction).toBe("down");
	});

	it("treats a missing boundary snapshot as a baseline of zero and renders a direction", () => {
		const history = [snap("2026-07-14", 4)];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend?.direction).toBe("up");
		expect(trend?.previousValue).toBe("0");
		expect(trend?.currentValue).toBe("4");
		expect(trend?.noBaseline).toBeFalsy();
	});

	it("renders flat — never a false arrow — when the baseline and the current count are both zero", () => {
		const history = [snap("2026-07-14", 0)];

		expect(computeBlockedTrend(history, start, end)?.direction).toBe("flat");
	});

	it("renders flat on an entirely empty history rather than a no-baseline placeholder", () => {
		for (const empty of [
			computeBlockedTrend([], start, end),
			computeBlockedTrend(null, start, end),
		]) {
			expect(empty?.direction).toBe("flat");
			expect(empty?.noBaseline).toBeFalsy();
		}
	});

	it("omits the percentage delta when the baseline is zero, keeping the absolute values", () => {
		const trend = computeBlockedTrend([snap("2026-07-14", 4)], start, end);

		expect(trend?.percentageDelta).toBeUndefined();
		expect(trend?.previousValue).toBe("0");
	});

	it("does not present a fabricated snapshot date for the synthetic zero baseline", () => {
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
	 * An assumed baseline used to announce itself through an explanatory tooltip. That
	 * was removed as UI bloat after a live review — bare numbers only — which leaves
	 * `previousLabel` as the sole signal separating an assumed baseline from a measured
	 * one. Measured: the label is a real `recordedAt` drawn from the history. Assumed:
	 * the label is the previous-period boundary day, which appears nowhere in the
	 * history and is never a fabricated `recordedAt`.
	 *
	 * Known residual: when a measured baseline's `recordedAt` happens to fall exactly on
	 * the boundary day, the two labels are byte-identical and the distinction is
	 * invisible. The tooltip used to cover that overlap; nothing does now.
	 */
	it("distinguishes an assumed baseline from a measured one through previousLabel alone", () => {
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

		// Neither case carries a tooltip any more — bare numbers only.
		expect(assumed?.hintText).toBeUndefined();
		expect(measured?.hintText).toBeUndefined();
	});

	/**
	 * History exists but holds nothing at or before the range end — a range selected
	 * entirely before recording began. The zero-baseline rule must not silently invent a
	 * current value here: with no measurement at either end there is nothing to compare,
	 * so an arrow would be fabrication rather than a defensible day-one assumption.
	 */
	it("does not fabricate a direction when the history holds nothing at or before the range end", () => {
		const history = [snap("2026-08-20", 7)]; // after `end` (2026-07-14)

		const trend = computeBlockedTrend(history, start, end);

		// Pin the marker, not just the absence of an arrow: a zero-vs-zero comparison also
		// reads "none", so asserting only the direction let this whole branch be deleted
		// without a test noticing (found by mutation testing).
		expect(trend?.noBaseline).toBe(true);
		expect(trend?.hintText).toBeTruthy();
		expect(trend?.currentValue).toBeUndefined();
		expect(trend?.previousValue).toBeUndefined();
		expect(trend?.direction).toBe("none");
	});
});

/**
 * The backend serves a continuous daily series: days with no recorded snapshot carry
 * the last known count forward, so the boundary day itself is present in the fetched
 * history whenever recording has begun. These cases pin the selector against that
 * interpolated shape (a weekend gap Fri→Mon carried through Sat/Sun). They characterise
 * behaviour that was already correct — the selector never changed; what changed is that
 * the fetched history now guarantees the boundary-day entry they construct by hand.
 */
describe("computeBlockedTrend — interpolated-history contract (characterization)", () => {
	// Selected range starts Saturday (a carried-forward day); boundary = Friday 2026-06-26.
	const start = new Date("2026-06-27");
	const end = new Date("2026-06-29");

	it("uses the carried-forward snapshot on the boundary day as the baseline, not a zero", () => {
		// Interpolated series as the fixed backend serves it: Fri 3 (real), Sat/Sun 3 (carried), Mon 5 (real).
		const history = [
			snap("2026-06-26", 3),
			snap("2026-06-27", 3),
			snap("2026-06-28", 3),
			snap("2026-06-29", 5),
		];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend?.previousLabel).toBe("2026-06-26");
		expect(trend?.previousValue).toBe("3");
		expect(trend?.currentValue).toBe("5");
		expect(trend?.direction).toBe("up");
		expect(trend?.noBaseline).toBeFalsy();
	});

	it("renders flat against a carried-forward plateau instead of a zero baseline", () => {
		// Constant count 3 carried across the whole gap: trend must read flat-vs-3, never up-vs-0.
		const history = [
			snap("2026-06-26", 3),
			snap("2026-06-27", 3),
			snap("2026-06-28", 3),
			snap("2026-06-29", 3),
		];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend?.direction).toBe("flat");
		expect(trend?.previousValue).toBe("3");
		expect(trend?.currentValue).toBe("3");
		expect(trend?.noBaseline).toBeFalsy();
	});
});

/**
 * The selected range names calendar days, not instants. A label written through UTC
 * names the day before for every viewer at a positive offset, so the trend would
 * claim a boundary day the viewer never selected. These cases only bite at a
 * non-zero UTC offset — the suite pins one (see the `test` script).
 */
describe("computeBlockedTrend — range edges are local calendar days", () => {
	// Picked from the dashboard, so both edges sit at local midnight.
	const start = new Date(2026, 6, 1);
	const end = new Date(2026, 6, 20);

	it("states the local boundary day the assumed zero stands for", () => {
		const trend = computeBlockedTrend([snap("2026-07-14", 4)], start, end);

		expect(trend?.previousLabel).toBe("2026-06-30");
	});

	it("falls back to the local range end when nothing was ever recorded", () => {
		const trend = computeBlockedTrend([], start, end);

		expect(trend?.currentLabel).toBe("2026-07-20");
	});
});
