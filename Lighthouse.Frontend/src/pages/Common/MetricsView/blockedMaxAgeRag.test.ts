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
		expect(computeBlockedMaxAgeRag(12, threshold, terms).ragStatus).toBe("red");
	});

	it("is AMBER when the oldest blocker is aging toward the threshold", () => {
		expect(computeBlockedMaxAgeRag(8, threshold, terms).ragStatus).toBe(
			"amber",
		);
	});

	it("is GREEN when nothing is aging", () => {
		expect(computeBlockedMaxAgeRag(2, threshold, terms).ragStatus).toBe(
			"green",
		);
	});

	it("is GREEN when there are no blocked items (max age is null)", () => {
		expect(computeBlockedMaxAgeRag(null, threshold, terms).ragStatus).toBe(
			"green",
		);
	});

	it("is none (disabled) when the threshold is 0", () => {
		expect(computeBlockedMaxAgeRag(20, 0, terms).ragStatus).toBe("none");
	});
});
