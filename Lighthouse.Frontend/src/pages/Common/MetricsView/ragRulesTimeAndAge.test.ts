import { describe, expect, it } from "vitest";
import {
	computeCumulativeStateTimeRag,
	computeCycleTimePercentilesRag,
	computeCycleTimeScatterplotRag,
	computeSleRiskRag,
	computeTotalWorkItemAgeOverTimeRag,
	computeTotalWorkItemAgeRag,
	computeWorkItemAgeChartRag,
	computeWorkItemAgePercentilesRag,
} from "./ragRulesTimeAndAge";
import type { RagTerms } from "./ragTypes";

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

describe("ragRules", () => {
	describe("computeSleRiskRag", () => {
		const sleAt85 = { percentile: 85, value: 4 };
		const sleAt70 = { percentile: 70, value: 10 };

		it("tells a team with no target to publish one", () => {
			const result = computeSleRiskRag(0, 9, null, terms);

			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define a SLE in settings");
		});

		it("asks for the target even when nothing is in progress", () => {
			// The missing setting outlives today's board, so it is checked first.
			const result = computeSleRiskRag(0, 0, null, terms);

			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define a SLE in settings");
		});

		it("is green with nothing in progress, and says so rather than dividing", () => {
			const result = computeSleRiskRag(0, 0, sleAt85, terms);

			expect(result.ragStatus).toBe("green");
			// An empty board and a board with nothing at risk are both green and are not the same
			// thing, so the tip has to tell them apart. Removing the guard entirely also lands on
			// green — zero divided by zero compares false against everything — so the status alone
			// cannot say whether the guard is still there.
			expect(result.tipText).toBe(
				"No Work Items in progress, so none are at risk.",
			);
		});

		it("is green when nothing is at risk, and says that instead", () => {
			const result = computeSleRiskRag(0, 9, sleAt85, terms);

			expect(result.ragStatus).toBe("green");
			expect(result.tipText).toBe(
				"No Work Items are at risk of missing the SLE.",
			);
		});

		it("is amber when something is at risk but the share is within the allowance", () => {
			// One of twenty is 5%, under the 15% a team promising 85% has accepted.
			const result = computeSleRiskRag(1, 20, sleAt85, terms);

			expect(result.ragStatus).toBe("amber");
		});

		it("is red when the share reaches the allowance the target implies", () => {
			// Three of twenty is exactly 15%. At the line, not merely past it.
			const result = computeSleRiskRag(3, 20, sleAt85, terms);

			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toBe(
				"3 of 20 Work Items are at risk, at or above the 15% this SLE allows. Focus on them before starting new work.",
			);
		});

		// The one test here that is about the absence of a bug rather than the presence of a rule.
		// Six of forty-one is 14.63%, which is under a 15% allowance and must read amber. An
		// implementation that rounds the share to a whole percentage before comparing gets 15,
		// finds 15 >= 15, and reads red.
		//
		// The numbers are chosen so the two implementations disagree: forty items would give
		// exactly 15% and fifty would give 12%, and a test written on either passes against the bug.
		it("does not act on a share that only rounds up to the allowance", () => {
			const result = computeSleRiskRag(6, 41, sleAt85, terms);

			expect(result.ragStatus).toBe("amber");
		});

		it("lets a looser target absorb what a tighter one cannot", () => {
			// The same board read by two teams. Three of twenty is 15%: at the line for a team
			// promising 85%, comfortably inside the 30% a team promising 70% has accepted.
			expect(computeSleRiskRag(3, 20, sleAt85, terms).ragStatus).toBe("red");
			expect(computeSleRiskRag(3, 20, sleAt70, terms).ragStatus).toBe("amber");
		});

		it("names the team's own allowance in its status tip", () => {
			expect(computeSleRiskRag(1, 20, sleAt85, terms).tipText).toContain("15%");
			expect(computeSleRiskRag(1, 20, sleAt70, terms).tipText).toContain("30%");
		});

		// The other half of the split, and the half that does the work: the fixed threshold belongs
		// to the info icon, and a reader who meets both numbers in one tooltip has two percentages
		// and no way to tell which is the rule and which is their team.
		//
		// Only the 85% team is asked. A team promising 30% has a 70% allowance, so "70%" would
		// appear in its tip legitimately and the assertion could not tell the two meanings apart.
		it("keeps the rule's own threshold out of the status tip", () => {
			for (const result of [
				computeSleRiskRag(1, 20, sleAt85, terms),
				computeSleRiskRag(6, 20, sleAt85, terms),
				computeSleRiskRag(0, 20, sleAt85, terms),
			]) {
				expect(result.tipText).not.toContain("70%");
			}
		});
	});

	describe("computeCycleTimePercentilesRag", () => {
		it("returns red when no SLE is defined (null)", () => {
			const result = computeCycleTimePercentilesRag(null, [], terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define a SLE");
		});

		it("returns green when percentage meets SLE target", () => {
			// SLE: 85% within 10 days. 9 of 10 items <= 10 → 90% >= 85%
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [1, 2, 3, 4, 5, 6, 7, 8, 9, 11];
			const result = computeCycleTimePercentilesRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when percentage is slightly below SLE target (within 20pp)", () => {
			// SLE: 85% within 10 days. 8 of 10 = 80%. Gap = 5pp < 20
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [1, 2, 3, 4, 5, 6, 7, 8, 11, 12];
			const result = computeCycleTimePercentilesRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red when percentage is far below SLE target (>20pp)", () => {
			// SLE: 85% within 10 days. 4 of 10 = 40%. Gap = 45pp > 20
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [1, 2, 3, 4, 11, 12, 13, 14, 15, 16];
			const result = computeCycleTimePercentilesRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns red when no cycle time data exists", () => {
			const sle = { percentile: 85, value: 10 };
			const result = computeCycleTimePercentilesRag(sle, [], terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green when all cycle times are within SLE", () => {
			const sle = { percentile: 85, value: 10 };
			// 20 items, 17 ≤ 10, 3 > 10 → 85% >= 85%
			const cycleTimes = [...new Array(17).fill(5), 11, 12, 13];
			const result = computeCycleTimePercentilesRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when gap is exactly 1pp below target", () => {
			// SLE: 85% within 100. 84 of 100 within → 84%. Gap = 1pp < 20
			const sle = { percentile: 85, value: 100 };
			const cycleTimes = [
				...new Array(84).fill(50),
				...new Array(16).fill(150),
			];
			const result = computeCycleTimePercentilesRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("amber");
		});
	});

	describe("computeTotalWorkItemAgeRag", () => {
		it("returns red when no WIP limit is defined", () => {
			const result = computeTotalWorkItemAgeRag(100, 5, undefined, 10, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define System WIP Limit and SLE");
		});

		it("returns red when no SLE is defined", () => {
			const result = computeTotalWorkItemAgeRag(100, 5, 5, undefined, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define System WIP Limit and SLE");
		});

		it("returns red when total age exceeds reference value", () => {
			// ref = WIP(5) * SLE(10) = 50. totalAge=60 > 50
			const result = computeTotalWorkItemAgeRag(60, 5, 5, 10, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when tomorrow projection exceeds reference", () => {
			// ref = 5*10=50. totalAge=46, tomorrow = 46+5=51 > 50
			const result = computeTotalWorkItemAgeRag(46, 5, 5, 10, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when total age and tomorrow are within range", () => {
			// ref = 5*10=50. totalAge=40, tomorrow=40+5=45 <= 50
			const result = computeTotalWorkItemAgeRag(40, 5, 5, 10, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when total age is exactly at reference", () => {
			// ref = 5*10=50. totalAge=50, tomorrow=50+5=55 but today is exactly at limit
			const result = computeTotalWorkItemAgeRag(50, 5, 5, 10, terms);
			// totalAge = ref, but tomorrow > ref → amber
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when well below reference", () => {
			// ref =5*10=50. totalAge=20, tomorrow=20+5=25 <= 50
			const result = computeTotalWorkItemAgeRag(20, 5, 5, 10, terms);
			expect(result.ragStatus).toBe("green");
		});
	});

	describe("computeCycleTimeScatterplotRag", () => {
		it("returns red when no SLE is defined", () => {
			const result = computeCycleTimeScatterplotRag(null, [], terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Define a SLE");
		});

		it("returns green when within SLE", () => {
			// SLE: 85% within 10 days. Items: 9 under 10, 1 over
			// 10% above → within X(85%) tolerance
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [2, 3, 4, 5, 6, 7, 8, 9, 9, 11];
			// 1 out of 10 = 10% above, SLE says 15% allowed
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns amber when between X% and (X+10)% above SLE days", () => {
			// SLE: 85% within 10 days → 15% allowed above
			// If 18% above → between 15% and 25% → amber
			const sle = { percentile: 85, value: 10 };
			// 18 of 100 items above 10 days
			const cycleTimes = [...new Array(82).fill(5), ...new Array(18).fill(15)];
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red when (X+10)%+ items above SLE days", () => {
			// SLE: 85% within 10 days → 15% allowed, red at 25%+
			// 30% above
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [...new Array(70).fill(5), ...new Array(30).fill(15)];
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns green with empty cycle time data", () => {
			const sle = { percentile: 85, value: 10 };
			const result = computeCycleTimeScatterplotRag(sle, [], terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when all items within SLE", () => {
			const sle = { percentile: 85, value: 10 };
			const cycleTimes = [1, 2, 3, 4, 5];
			const result = computeCycleTimeScatterplotRag(sle, cycleTimes, terms);
			expect(result.ragStatus).toBe("green");
		});
	});

	describe("computeWorkItemAgeChartRag", () => {
		it("returns red when no SLE is set", () => {
			const result = computeWorkItemAgeChartRag(null, true, [], terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("SLE");
		});

		it("returns red when no blocked config is set", () => {
			const sle = { percentile: 85, value: 14 };
			const result = computeWorkItemAgeChartRag(sle, false, [], terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Blocked");
		});

		it("returns red when any item is above SLE", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 10, isBlocked: false, isStale: false },
				{ workItemAge: 15, isBlocked: false, isStale: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when some items are above SLE within allowed percentage", () => {
			const sle = { percentile: 85, value: 14 };
			// 1 of 10 above SLE → 10% <= allowedAbove(15%) → anyAbove=true → amber
			const items = [
				{ workItemAge: 15, isBlocked: false, isStale: false },
				...new Array(9).fill({
					workItemAge: 5,
					isBlocked: false,
					isStale: false,
				}),
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns amber when any item is blocked", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 5, isBlocked: true, isStale: false },
				{ workItemAge: 3, isBlocked: false, isStale: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns amber when any item is stale, mirroring blocked", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 5, isBlocked: false, isStale: true },
				{ workItemAge: 3, isBlocked: false, isStale: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns red when items are both above SLE and stale, mirroring blocked", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 15, isBlocked: false, isStale: true },
				{ workItemAge: 3, isBlocked: false, isStale: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("escalates to red when a stale item is above SLE even though the above-percentage stays within tolerance", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 15, isBlocked: false, isStale: true },
				...new Array(9).fill({
					workItemAge: 5,
					isBlocked: false,
					isStale: false,
				}),
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Blocked or stale");
			expect(result.tipText).toContain("Resolve immediately");
		});

		it("stays amber and omits the flagged suffix when items exceed the SLE without any blocked or stale flag", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 15, isBlocked: false, isStale: false },
				...new Array(9).fill({
					workItemAge: 5,
					isBlocked: false,
					isStale: false,
				}),
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).not.toContain("Blocked or stale");
			expect(result.tipText).toContain("14 days. Monitor closely.");
		});

		it("describes blocked-or-stale items in the amber tip when a stale item is within the SLE", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 5, isBlocked: false, isStale: true },
				{ workItemAge: 3, isBlocked: false, isStale: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).toContain("Blocked or stale");
		});

		it("stays amber when the above-SLE percentage exactly equals the allowed tolerance", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				...new Array(3).fill({
					workItemAge: 15,
					isBlocked: false,
					isStale: false,
				}),
				...new Array(17).fill({
					workItemAge: 5,
					isBlocked: false,
					isStale: false,
				}),
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when no blockers and all below SLE", () => {
			const sle = { percentile: 85, value: 14 };
			const items = [
				{ workItemAge: 5, isBlocked: false, isStale: false },
				{ workItemAge: 8, isBlocked: false, isStale: false },
			];
			const result = computeWorkItemAgeChartRag(sle, true, items, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green with empty items", () => {
			const sle = { percentile: 85, value: 14 };
			const result = computeWorkItemAgeChartRag(sle, true, [], terms);
			expect(result.ragStatus).toBe("green");
		});
	});

	describe("computeTotalWorkItemAgeOverTimeRag", () => {
		it("returns red when end is higher than start by >10%", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(100, 115, terms);
			expect(result.ragStatus).toBe("red");
		});

		it("returns amber when start is higher than end by >10%", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(100, 85, terms);
			expect(result.ragStatus).toBe("amber");
		});

		it("returns green when start and end are within 10% margin", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(100, 105, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when start and end are equal", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(100, 100, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns green when both are zero", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(0, 0, terms);
			expect(result.ragStatus).toBe("green");
		});

		it("returns red when start is zero and end is positive", () => {
			const result = computeTotalWorkItemAgeOverTimeRag(0, 10, terms);
			expect(result.ragStatus).toBe("red");
		});
	});

	describe("computeCumulativeStateTimeRag", () => {
		it.each([
			{
				scenario: "no state reaches 40% of the total",
				dominant: 30,
				expected: "green",
			},
			{
				scenario: "one state holds exactly 40%",
				dominant: 40,
				expected: "amber",
			},
			{ scenario: "one state holds 55%", dominant: 55, expected: "amber" },
			{
				scenario: "one state holds exactly 60%",
				dominant: 60,
				expected: "amber",
			},
			{
				scenario: "one state holds more than 60%",
				dominant: 80,
				expected: "red",
			},
		])("returns $expected when $scenario", ({ dominant, expected }) => {
			const remainder = 100 - dominant;
			const states = [
				{ state: "Doing", totalDays: dominant },
				{ state: "Review", totalDays: remainder / 2 },
				{ state: "Test", totalDays: remainder / 2 },
			];

			const result = computeCumulativeStateTimeRag(states, terms);

			expect(result.ragStatus).toBe(expected);
		});

		it("returns red with a filter-adjustment tip when there are no states", () => {
			const result = computeCumulativeStateTimeRag([], terms);

			expect(result.ragStatus).toBe("red");
			expect(result.tipText.toLowerCase()).toContain("filter");
		});

		it("returns red when a single state holds 100% of the total time", () => {
			const result = computeCumulativeStateTimeRag(
				[{ state: "Doing", totalDays: 42 }],
				terms,
			);

			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Doing");
		});

		it("names the dominant state and its exact percentage in the red tip", () => {
			const result = computeCumulativeStateTimeRag(
				[
					{ state: "Doing", totalDays: 80 },
					{ state: "Review", totalDays: 10 },
					{ state: "Test", totalDays: 10 },
				],
				terms,
			);

			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Doing");
			expect(result.tipText).toContain("80.0%");
		});

		it("names the dominant state and its exact percentage in the amber tip", () => {
			const result = computeCumulativeStateTimeRag(
				[
					{ state: "Review", totalDays: 50 },
					{ state: "Doing", totalDays: 30 },
					{ state: "Test", totalDays: 20 },
				],
				terms,
			);

			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).toContain("Review");
			expect(result.tipText).toContain("50.0%");
		});

		it("selects the largest state as dominant even when it is not first in the array", () => {
			const result = computeCumulativeStateTimeRag(
				[
					{ state: "First", totalDays: 5 },
					{ state: "Biggest", totalDays: 90 },
					{ state: "Last", totalDays: 5 },
				],
				terms,
			);

			expect(result.ragStatus).toBe("red");
			expect(result.tipText).toContain("Biggest");
			expect(result.tipText).toContain("90.0%");
		});

		it("returns the no-contribution red tip when every state has zero recorded time", () => {
			const result = computeCumulativeStateTimeRag(
				[
					{ state: "Doing", totalDays: 0 },
					{ state: "Review", totalDays: 0 },
				],
				terms,
			);

			expect(result.ragStatus).toBe("red");
			expect(result.tipText.toLowerCase()).toContain("contribute");
		});

		it("names the first of two equally-dominant states deterministically", () => {
			const result = computeCumulativeStateTimeRag(
				[
					{ state: "First", totalDays: 50 },
					{ state: "Second", totalDays: 50 },
				],
				terms,
			);

			expect(result.ragStatus).toBe("amber");
			expect(result.tipText).toContain("First");
			expect(result.tipText).not.toContain("Second");
		});

		it("describes balanced states in the green tip", () => {
			const result = computeCumulativeStateTimeRag(
				[
					{ state: "Doing", totalDays: 30 },
					{ state: "Review", totalDays: 35 },
					{ state: "Test", totalDays: 35 },
				],
				terms,
			);

			expect(result.ragStatus).toBe("green");
			expect(result.tipText.toLowerCase()).toContain("balanced");
		});
	});
});

/**
 * Story 5508 (widget-loose-ends) slice 04, US-05 — RE-AUTHORED against D6-REVISED.
 *
 * These specs were originally authored in DISTILL against the share-based D6 design (bands on the
 * PERCENTAGE of in-progress items within the SLE, reusing `computeCycleTimePercentilesRag`'s 20pp
 * bands). That design was WITHDRAWN by the product owner on 2026-07-19 after its human validation
 * gate fired: it read GREEN on a period the coach had nominated as deteriorated, because a
 * percentage over a population of 1-4 items is noise. See
 * `docs/feature/widget-loose-ends/feature-delta.md` → "D6 gate RESULT and D6-REVISED" (commit
 * b1a92b70) for the full record. DELIVER does not normally re-author ATs — the acceptance criteria
 * themselves changed, which is why this is recorded rather than silently edited.
 *
 * D6-REVISED — absolute counts over in-progress ages as of `endDate`, `V` = `sle.value` only:
 *   no SLE                                          → RED (define-an-SLE tip)
 *   count(age > V) > 1                              → RED (Act)
 *   count(age > V) === 1 || count(age === V) >= 1   → AMBER (Observe)
 *   otherwise (all ages < V, or empty population)   → GREEN (Sustain)
 *
 * The SLE percentile is deliberately discarded; absolute counts deliberately do not scale with WIP
 * size; an empty population is deliberately GREEN (the WIP RAG already signals an empty board).
 *
 * WITHDRAWN with the old design: scenario 33 ("bands identically to the cycle-time rule") and DESIGN
 * D19 (extract a shared band helper). The two rules no longer share band knowledge — one is
 * share-based, one is count-based — so there is no single piece of knowledge to extract, and a
 * shared helper would couple two rules that deliberately diverged.
 */
describe("computeWorkItemAgePercentilesRag — count-based SLE bands over in-progress ages (Story 5508 slice 04)", () => {
	const sle = { percentile: 85, value: 14 };

	it("renders red when more than one item is older than the SLE value (AC1 — Act)", () => {
		const result = computeWorkItemAgePercentilesRag(sle, [3, 15, 21], terms);

		expect(result.ragStatus).toBe("red");
		expect(result.tipText).toContain("2");
		expect(result.tipText).toContain("14");
	});

	it("renders amber when exactly one item is older than the SLE value (AC2 — Observe)", () => {
		const result = computeWorkItemAgePercentilesRag(sle, [3, 7, 21], terms);

		expect(result.ragStatus).toBe("amber");
		expect(result.tipText).toContain("14");
	});

	it("moves from amber to red at the second breaching item (boundary: one above vs two above)", () => {
		expect(
			computeWorkItemAgePercentilesRag(sle, [1, 2, 99], terms).ragStatus,
		).toBe("amber");
		expect(
			computeWorkItemAgePercentilesRag(sle, [1, 99, 99], terms).ragStatus,
		).toBe("red");
	});

	/**
	 * INVERTED relative to the withdrawn design, which counted an age of exactly `V` as within the
	 * SLE and therefore green. Under D6-REVISED an item sitting exactly ON the SLE value has used up
	 * its whole allowance and is the thing to look at next — it triggers AMBER.
	 */
	it("treats an age exactly equal to the SLE value as an amber trigger, not a pass (boundary)", () => {
		const result = computeWorkItemAgePercentilesRag(sle, [2, 5, 14], terms);

		expect(result.ragStatus).toBe("amber");
	});

	it("stays amber when several items sit exactly on the SLE value and none exceed it", () => {
		expect(
			computeWorkItemAgePercentilesRag(sle, [14, 14, 1], terms).ragStatus,
		).toBe("amber");
	});

	it("agrees with itself about how many items it is talking about", () => {
		// The tip is the whole of what the chip says once you hover it; a count that reads
		// "1 Work Items" or "2 Work Item sits" is the kind of thing nobody reports and
		// everybody notices. Mutation testing found both singular branches unguarded.
		const oneAtLimit = computeWorkItemAgePercentilesRag(sle, [14, 2], terms);
		expect(oneAtLimit.tipText).toContain("1 in-progress Work Item sits");

		const twoAtLimit = computeWorkItemAgePercentilesRag(sle, [14, 14], terms);
		expect(twoAtLimit.tipText).toContain("2 in-progress Work Items sit");

		const oneBelow = computeWorkItemAgePercentilesRag(sle, [2], terms);
		expect(oneBelow.tipText).toContain("The in-progress Work Item is younger");

		const severalBelow = computeWorkItemAgePercentilesRag(
			sle,
			[2, 3, 4],
			terms,
		);
		expect(severalBelow.tipText).toContain(
			"All 3 in-progress Work Items are younger",
		);
	});

	it("renders green when every in-progress age is below the SLE value (AC2 — Sustain)", () => {
		const result = computeWorkItemAgePercentilesRag(sle, [1, 5, 13], terms);

		expect(result.ragStatus).toBe("green");
		expect(result.tipText).toContain("14");
	});

	it("lets red win over amber when items both exceed and sit on the SLE value (evaluation order)", () => {
		expect(
			computeWorkItemAgePercentilesRag(sle, [14, 15, 20], terms).ragStatus,
		).toBe("red");
	});

	it("renders red with the define-an-SLE tip when no SLE is configured (AC3)", () => {
		const result = computeWorkItemAgePercentilesRag(null, [3, 5, 9], terms);

		expect(result.ragStatus).toBe("red");
		expect(result.tipText).toContain(terms.sle);
	});

	/**
	 * SUPERSEDES the earlier AC3b `"none"` expectation. D6-REVISED: nothing in progress is not a bad
	 * state, and the WIP RAG already signals an empty board — this widget must not say it twice.
	 * The no-SLE case stays RED, so the two remain distinguishable, for a different reason than before.
	 */
	it("renders green on an empty population, and never mentions defining an SLE (AC3b, revised)", () => {
		const result = computeWorkItemAgePercentilesRag(sle, [], terms);

		expect(result.ragStatus).toBe("green");
		expect(result.tipText).not.toContain(terms.sle);
		expect(result.tipText).not.toContain("NaN");
		expect(result.tipText.length).toBeGreaterThan(0);
	});

	it("keeps the two empty-ish cases distinct: no SLE reds, an empty population does not (AC3 vs AC3b)", () => {
		const noSle = computeWorkItemAgePercentilesRag(null, [3, 5, 9], terms);
		const noItems = computeWorkItemAgePercentilesRag(sle, [], terms);

		expect(noSle.ragStatus).toBe("red");
		expect(noItems.ragStatus).toBe("green");
		expect(noSle.tipText).not.toBe(noItems.tipText);
	});

	it("ignores the configured percentile entirely, banding on the day value alone (D6-REVISED)", () => {
		const ages = [3, 7, 21];

		for (const percentile of [50, 70, 85, 99]) {
			expect(
				computeWorkItemAgePercentilesRag({ percentile, value: 14 }, ages, terms)
					.ragStatus,
			).toBe("amber");
		}
	});

	it("does not scale the bands with WIP size: two breaching items read red at any WIP (D6-REVISED)", () => {
		const smallWip = [1, 20, 20];
		const largeWip = [...Array.from({ length: 38 }, () => 1), 20, 20];

		expect(
			computeWorkItemAgePercentilesRag(sle, smallWip, terms).ragStatus,
		).toBe("red");
		expect(
			computeWorkItemAgePercentilesRag(sle, largeWip, terms).ragStatus,
		).toBe("red");
	});

	it("carries the status in the tip text for every band, so colour is never the only signal (AC5, CI3)", () => {
		const populations = [[], [1, 5, 13], [3, 7, 21], [3, 15, 21], [2, 5, 14]];

		for (const ages of populations) {
			const result = computeWorkItemAgePercentilesRag(sle, ages, terms);
			expect(result.tipText.length).toBeGreaterThan(0);
			expect(result.tipText).not.toContain("NaN");
		}

		expect(
			computeWorkItemAgePercentilesRag(null, [1], terms).tipText.length,
		).toBeGreaterThan(0);
	});

	/**
	 * Scenario 33 ("bands identically to the shipped cycle-time rule") is DELETED, not re-authored.
	 * It asserted the equivalence that DESIGN D19's shared-band-helper refactor was built on. Both
	 * are withdrawn: the cycle-time rule bands on a share against the percentile, this one bands on
	 * absolute counts against the day value. There is no longer a shared band to assert or extract.
	 */
});
