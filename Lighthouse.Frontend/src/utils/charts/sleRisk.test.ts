import { describe, expect, test } from "vitest";
import type { ISleRisk } from "../../models/Metrics/SleRisk";
import type { IWorkItem } from "../../models/WorkItem";
import { PACE_BAND_COLORS_LOW_TO_HIGH } from "./paceBands";
import {
	buildSleRiskColumnDescriptor,
	SLE_RISK_BEYOND_HISTORY_LABEL,
	SLE_RISK_NOT_ENOUGH_HISTORY_LABEL,
	sleRiskAtRiskSummary,
	sleRiskColumnDescription,
	sleRiskColumnHeaderName,
	sleRiskSortValue,
} from "./sleRisk";

const zenithItem = (referenceId: string): IWorkItem => ({
	id: Number.parseInt(referenceId.replace("ZEN-", ""), 10),
	name: `Item ${referenceId}`,
	state: "In Progress",
	stateCategory: "Doing",
	type: "User Story",
	referenceId,
	url: `https://example.com/work/${referenceId}`,
	startedDate: new Date("2026-09-01"),
	closedDate: new Date("2026-09-01"),
	cycleTime: 0,
	workItemAge: 9,
	parentWorkItemReference: "",
	isBlocked: false,
});

/** An answered item, or one of the two silences: `null` with evidence, or `null` with none. */
const answer = (
	referenceId: string,
	risk: number | null,
	comparableItems = risk === null ? 0 : 30,
): ISleRisk => ({ referenceId, risk, comparableItems });

const descriptorFor = (answers: ISleRisk[]) =>
	buildSleRiskColumnDescriptor({
		answers,
		headerName: "SLE Risk",
		description: "how likely this one is to miss",
	});

describe("the risk column's wording", () => {
	test("names the target with whatever the team calls it", () => {
		expect(sleRiskColumnHeaderName("Service Level Expectation")).toBe(
			"Service Level Expectation Risk",
		);
	});

	test("says what the number is a share of, in the team's own word for one piece of work", () => {
		expect(sleRiskColumnDescription("Ticket")).toBe(
			"Of every Ticket still open at this age, the share that went on to miss the target",
		);
	});
});

describe("what the two silences are called", () => {
	// Pinned against the words themselves. Every other assertion here compares a label to the
	// constant it came from, which holds just as well when the constant is blank.
	test("an item nothing ran as long as", () => {
		expect(SLE_RISK_BEYOND_HISTORY_LABEL).toBe("Beyond history");
	});

	test("an item too little ran as long as", () => {
		expect(SLE_RISK_NOT_ENOUGH_HISTORY_LABEL).toBe("Not enough history");
	});
});

describe("building the risk column", () => {
	test("offers nothing at all when no item was answered for", () => {
		// A team with no published target is the only way this arrives empty, and there is then no
		// promise for anything to be at risk of breaking.
		expect(descriptorFor([])).toBeUndefined();
	});

	test("carries the percentage as the column's value, so the export reads as the column does", () => {
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.labelFor(zenithItem("ZEN-412"))).toBe("86%");
	});

	test("says beyond history for an item the history cannot answer for", () => {
		const descriptor = descriptorFor([
			answer("ZEN-412", 86),
			answer("ZEN-455", null),
		]);

		expect(descriptor?.labelFor(zenithItem("ZEN-455"))).toBe(
			SLE_RISK_BEYOND_HISTORY_LABEL,
		);
		expect(descriptor?.riskFor(zenithItem("ZEN-455"))).toBeUndefined();
	});

	test("tells a thinly-evidenced item apart from one nothing can be compared against", () => {
		// Both have no number, and they are not the same thing. Saying nothing ran this long, when
		// nine items did, is a false claim about the team's history rather than a softer one.
		const descriptor = descriptorFor([
			answer("ZEN-455", null, 0),
			answer("ZEN-470", null, 9),
		]);

		expect(descriptor?.labelFor(zenithItem("ZEN-455"))).toBe(
			SLE_RISK_BEYOND_HISTORY_LABEL,
		);
		expect(descriptor?.labelFor(zenithItem("ZEN-470"))).toBe(
			SLE_RISK_NOT_ENOUGH_HISTORY_LABEL,
		);
	});

	test("leaves a thinly-evidenced item out of the ordering as well", () => {
		// It has no number, so it cannot be placed among the ones that do.
		const descriptor = descriptorFor([answer("ZEN-470", null, 9)]);

		expect(descriptor?.riskFor(zenithItem("ZEN-470"))).toBeUndefined();
		expect(sleRiskSortValue(SLE_RISK_NOT_ENOUGH_HISTORY_LABEL)).toBeUndefined();
	});

	test("says the same about an item the answer never mentioned", () => {
		// An item the endpoint did not list — it entered the board after the answer was computed.
		// Inventing a risk for it would be worse than admitting there is none.
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.labelFor(zenithItem("ZEN-999"))).toBe(
			SLE_RISK_BEYOND_HISTORY_LABEL,
		);
	});

	test("has no risk to order by for an item the answer never mentioned", () => {
		// The lookup misses, and a miss must read as "no answer" rather than throw on the way to
		// drawing the column.
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.riskFor(zenithItem("ZEN-999"))).toBeUndefined();
	});

	test("keeps the risk itself available for ordering, not only its spelling", () => {
		const descriptor = descriptorFor([answer("ZEN-412", 86)]);

		expect(descriptor?.riskFor(zenithItem("ZEN-412"))).toBe(86);
	});
});

describe("painting the risk", () => {
	const paintedBy = (risk: number) =>
		descriptorFor([answer("ZEN-412", 86)])?.colorForRisk(risk);

	test.each([
		[0, 0],
		[24, 0],
		[25, 1],
		[49, 1],
		[50, 2],
		[74, 2],
		[75, 3],
		[99, 3],
		[100, 4],
	])(
		"paints %i in the same colour the chart paints its band %i",
		(risk, band) => {
			expect(paintedBy(risk)).toBe(PACE_BAND_COLORS_LOW_TO_HIGH[band]);
		},
	);

	test("leaves an item with no answer unpainted", () => {
		// Not the calmest colour. An absence of evidence must not be dressed as good news.
		expect(
			descriptorFor([answer("ZEN-412", 86)])?.colorForRisk(undefined),
		).toBeUndefined();
	});
});

describe("reading the number back out of a rendered label", () => {
	test("recovers the risk from the percentage the column carries", () => {
		expect(sleRiskSortValue("86%")).toBe(86);
	});

	test("has no number for the beyond-history label", () => {
		expect(sleRiskSortValue(SLE_RISK_BEYOND_HISTORY_LABEL)).toBeUndefined();
	});

	test("has no number for a label that was never a percentage", () => {
		expect(sleRiskSortValue("")).toBeUndefined();
	});
});

// --- Epic #4127 slice 02: how many of the open items are in trouble ---
describe("counting what is at risk", () => {
	test("counts the items more likely than not to miss the target", () => {
		const summary = sleRiskAtRiskSummary([
			answer("ZEN-1", 86),
			answer("ZEN-2", 50),
			answer("ZEN-3", 49),
			answer("ZEN-4", 12),
		]);

		// Fifty is the line and sits on the at-risk side of it: more likely than not.
		expect(summary.count).toBe(2);
	});

	test("counts an item that has outlasted everything the team ever finished", () => {
		// It has no number and it is not a safe item - that is the whole reason it has no number.
		const summary = sleRiskAtRiskSummary([answer("ZEN-5", null, 0)]);

		expect(summary.count).toBe(1);
	});

	test("does not count an item too little history can speak for", () => {
		// The absence of a signal is not a signal. Counting it would put items nobody can act on
		// into the one number a coach uses to decide whether to act.
		const summary = sleRiskAtRiskSummary([answer("ZEN-6", null, 9)]);

		expect(summary.count).toBe(0);
	});

	test("takes its colour from the worst item it counted", () => {
		const summary = sleRiskAtRiskSummary([
			answer("ZEN-1", 60),
			answer("ZEN-2", 99),
		]);

		expect(summary.color).toBe(PACE_BAND_COLORS_LOW_TO_HIGH[3]);
	});

	test("reads an item beyond all history as the worst band there is", () => {
		// Nothing the team finished ran this long, so nothing places it below the top band.
		const summary = sleRiskAtRiskSummary([
			answer("ZEN-1", 60),
			answer("ZEN-5", null, 0),
		]);

		expect(summary.color).toBe(
			PACE_BAND_COLORS_LOW_TO_HIGH[PACE_BAND_COLORS_LOW_TO_HIGH.length - 1],
		);
	});

	test("has no colour to give when it counted nothing", () => {
		const summary = sleRiskAtRiskSummary([answer("ZEN-3", 12)]);

		expect(summary.count).toBe(0);
		expect(summary.color).toBeUndefined();
	});

	test("counts nothing for a team that published no target", () => {
		// An empty answer is how that arrives, and it is the same shape as having no items.
		expect(sleRiskAtRiskSummary([]).count).toBe(0);
	});
});
