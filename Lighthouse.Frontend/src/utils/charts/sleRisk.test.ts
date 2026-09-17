import { describe, expect, test } from "vitest";
import type { IWorkItem } from "../../models/WorkItem";
import { PACE_BAND_COLORS_LOW_TO_HIGH } from "./paceBands";
import {
	buildSleRiskColumnDescriptor,
	SLE_RISK_BEYOND_HISTORY_LABEL,
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

const descriptorFor = (risks: [string, number | null][]) =>
	buildSleRiskColumnDescriptor({
		riskByReferenceId: new Map(risks),
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

describe("building the risk column", () => {
	test("offers nothing at all when no item was answered for", () => {
		// A team with no published target is the only way this arrives empty, and there is then no
		// promise for anything to be at risk of breaking.
		expect(descriptorFor([])).toBeUndefined();
	});

	test("carries the percentage as the column's value, so the export reads as the column does", () => {
		const descriptor = descriptorFor([["ZEN-412", 86]]);

		expect(descriptor?.labelFor(zenithItem("ZEN-412"))).toBe("86%");
	});

	test("says beyond history for an item the history cannot answer for", () => {
		const descriptor = descriptorFor([
			["ZEN-412", 86],
			["ZEN-455", null],
		]);

		expect(descriptor?.labelFor(zenithItem("ZEN-455"))).toBe(
			SLE_RISK_BEYOND_HISTORY_LABEL,
		);
		expect(descriptor?.riskFor(zenithItem("ZEN-455"))).toBeUndefined();
	});

	test("says the same about an item the answer never mentioned", () => {
		// An item the endpoint did not list — it entered the board after the answer was computed.
		// Inventing a risk for it would be worse than admitting there is none.
		const descriptor = descriptorFor([["ZEN-412", 86]]);

		expect(descriptor?.labelFor(zenithItem("ZEN-999"))).toBe(
			SLE_RISK_BEYOND_HISTORY_LABEL,
		);
	});

	test("keeps the risk itself available for ordering, not only its spelling", () => {
		const descriptor = descriptorFor([["ZEN-412", 86]]);

		expect(descriptor?.riskFor(zenithItem("ZEN-412"))).toBe(86);
	});
});

describe("painting the risk", () => {
	const descriptor = descriptorFor([["ZEN-412", 86]]);

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
			expect(descriptor?.colorForRisk(risk)).toBe(
				PACE_BAND_COLORS_LOW_TO_HIGH[band],
			);
		},
	);

	test("leaves an item with no answer unpainted", () => {
		// Not the calmest colour. An absence of evidence must not be dressed as good news.
		expect(descriptor?.colorForRisk(undefined)).toBeUndefined();
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
