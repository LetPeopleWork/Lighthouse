import { describe, expect, it } from "vitest";
import { computeBlockedMaxAgeRag } from "./blockedMaxAgeRag";
import type { RagTerms } from "./ragRules";

/**
 * DISTILL RED-pending specs (Epic 5074, slice-07 / B2). Job:
 * job-flow-coach-read-blocked-health-at-a-glance. describe.skip = RED scaffold enabled in DELIVER one at
 * a time (ADR-025). The result drives the EXISTING Blocked widget RAG chip (rag-status test-id) — DELIVER
 * wires it into the computeBlockedOverviewRag call site and asserts the chip end-to-end in
 * BaseMetricsView.test.tsx.
 */
const terms: RagTerms = {
	workItem: "Work Item",
	workItems: "Work Items",
	feature: "Feature",
	features: "Features",
	cycleTime: "Cycle Time",
	throughput: "Throughput",
	wip: "WIP",
	workItemAge: "Work Item Age",
	blocked: "Blocked",
	sle: "SLE",
};

describe("computeBlockedMaxAgeRag — max-blocked-age RAG (B2)", () => {
	const threshold = 10;

	it("is RED when an item is blocked past the threshold", () => {
		const result = computeBlockedMaxAgeRag(12, threshold, terms);
		expect(result.ragStatus).toBe("red");
		expect(result.tipText).toContain("blocked 12 days");
		expect(result.tipText).toContain("past the 10-day threshold");
	});

	it("is RED at exactly the threshold (at-threshold counts as past)", () => {
		// Pins the `>=` boundary: at maxAge === threshold the oldest blocker is already stale.
		expect(computeBlockedMaxAgeRag(10, threshold, terms).ragStatus).toBe("red");
	});

	it("is AMBER when the oldest blocker is aging toward the threshold", () => {
		const result = computeBlockedMaxAgeRag(8, threshold, terms);
		expect(result.ragStatus).toBe("amber");
		expect(result.tipText).toContain("aging toward the 10-day threshold");
	});

	it("is AMBER at exactly the aging-band boundary (0.75 x threshold)", () => {
		// threshold 8 -> band 6.0; pins both the `>=` boundary and the 0.75 band fraction.
		const result = computeBlockedMaxAgeRag(6, 8, terms);
		expect(result.ragStatus).toBe("amber");
	});

	it("is GREEN when nothing is aging", () => {
		const result = computeBlockedMaxAgeRag(2, threshold, terms);
		expect(result.ragStatus).toBe("green");
		expect(result.tipText).toContain("well within the 10-day threshold");
	});

	it("is just below the aging band and still GREEN", () => {
		// threshold 8 -> band 6.0; 5 sits below the band, distinguishing amber from green.
		expect(computeBlockedMaxAgeRag(5, 8, terms).ragStatus).toBe("green");
	});

	it("is GREEN when there are no blocked items (max age is null)", () => {
		const result = computeBlockedMaxAgeRag(null, threshold, terms);
		expect(result.ragStatus).toBe("green");
		expect(result.tipText).toContain("No Blocked");
	});

	it("is red (prompt to configure) when the threshold is 0", () => {
		const result = computeBlockedMaxAgeRag(20, 0, terms);
		expect(result.ragStatus).toBe("red");
		expect(result.tipText).toContain("staleness threshold");
	});
});
