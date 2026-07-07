import { describe, expect, it } from "vitest";
import type { BlockedCountSnapshot } from "../../../models/BlockedCountSnapshot";
import { computeBlockedTrend } from "./blockedTrend";

/**
 * DISTILL RED-pending specs (Epic 5074, slice-06 / B3). Job:
 * job-delivery-lead-tell-blocked-trend-vs-last-period. describe.skip = RED scaffold enabled in DELIVER
 * one at a time (ADR-025). The trend feeds the EXISTING WidgetShell trend chrome (direction up/down/flat),
 * asserted end-to-end against the widget-trend-* test-ids in BaseMetricsView.test.tsx during DELIVER.
 */
describe.skip("computeBlockedTrend — previous-period trend (B3)", () => {
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

	it("shows no trend when no snapshot exists at or before the prior-period boundary", () => {
		const history = [snap("2026-06-14", 9)];

		const trend = computeBlockedTrend(history, start, end);

		expect(trend).toBeUndefined();
	});

	it("returns undefined for empty history", () => {
		expect(computeBlockedTrend([], start, end)).toBeUndefined();
		expect(computeBlockedTrend(null, start, end)).toBeUndefined();
	});
});
