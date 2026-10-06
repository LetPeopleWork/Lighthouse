import { describe, expect, it } from "vitest";
import type { RefinementVerdict } from "../../../../models/Refinement/Refinement";
import {
	describeNeed,
	describeNeedOrigin,
	type NeedFacts,
	type NeedOriginFacts,
} from "./needWording";

const THURSDAY_THE_EIGHTH = new Date(2026, 9, 8);
const THURSDAY_THE_FIFTEENTH = new Date(2026, 9, 15);

const gravitysNeed = (
	verdict: RefinementVerdict,
	readyCount: number,
	low: number,
	high: number,
): NeedFacts => ({
	verdict,
	readyCount,
	low,
	high,
	teamName: "Team Gravity",
	workItemTerm: "Work Item",
	workItemsTerm: "Work Items",
	refinementTerm: "Refinement",
});

// The cycle from Gravity's next Refinement to the one after it, read at the default band.
const gravitysCycle = (
	overrides: Partial<NeedOriginFacts> = {},
): NeedOriginFacts => ({
	teamName: "Team Gravity",
	horizonWorkingDays: 5,
	refinementDay: THURSDAY_THE_EIGHTH,
	cycleStart: THURSDAY_THE_EIGHTH,
	cycleEnd: THURSDAY_THE_FIFTEENTH,
	lowPercentile: 50,
	highPercentile: 85,
	teamTerm: "Team",
	throughputTerm: "Throughput",
	refinementsTerm: "Refinements",
	...overrides,
});

const LIKELIHOODS_AT_50_AND_85 =
	"The Team pulls at least the low end with 50% likelihood, and more than the high end with only 15% likelihood.";

describe("describeNeed", () => {
	// @need-over-one-cycle @contract-shape:pure-function
	// Below the range the number is what the Team pulls until the Refinement after the next one.
	it.skip.each<[number, number, number, string]>([
		[
			3,
			5,
			8,
			"3 ready — below the range of 5–8 Work Items Team Gravity is likely to pull until the Refinement after. Refine 2 to 5 more.",
		],
		[
			0,
			1,
			2,
			"0 ready — below the range of 1–2 Work Items Team Gravity is likely to pull until the Refinement after. Refine 1 to 2 more.",
		],
		[
			3,
			5,
			5,
			"3 ready — below the 5 Work Items Team Gravity is likely to pull until the Refinement after. Refine 2 more.",
		],
		[
			0,
			1,
			1,
			"0 ready — below the 1 Work Item Team Gravity is likely to pull until the Refinement after. Refine 1 more.",
		],
	])(
		"Below with %s ready against %s–%s reads %s",
		(readyCount, low, high, expected) => {
			expect(describeNeed(gravitysNeed("Below", readyCount, low, high))).toBe(
				expected,
			);
		},
	);

	it.each<[RefinementVerdict, number, number, number, string]>([
		[
			"In",
			6,
			5,
			8,
			"6 ready — in the range of 5–8. Nothing more needs refining by then.",
		],
		[
			"In",
			5,
			5,
			8,
			"5 ready — in the range of 5–8. Nothing more needs refining by then.",
		],
		[
			"In",
			8,
			5,
			8,
			"8 ready — in the range of 5–8. Nothing more needs refining by then.",
		],
		[
			"In",
			5,
			5,
			5,
			"5 ready — exactly the 5 likely to be pulled. Nothing more needs refining by then.",
		],
		[
			"Above",
			11,
			5,
			8,
			"11 ready — above the range of 5–8. Stop refining: nothing more is needed by then.",
		],
		[
			"Above",
			2,
			0,
			0,
			"2 ready — above the 0 likely to be pulled. Stop refining: nothing more is needed by then.",
		],
	])(
		"%s with %s ready against %s–%s reads %s",
		(verdict, readyCount, low, high, expected) => {
			expect(describeNeed(gravitysNeed(verdict, readyCount, low, high))).toBe(
				expected,
			);
		},
	);

	it("says the Team's own word for Work Items and names the Team as it is", () => {
		expect(
			describeNeed({
				...gravitysNeed("Below", 3, 5, 8),
				workItemTerm: "Ticket",
				workItemsTerm: "Tickets",
			}),
		).toContain(
			"below the range of 5–8 Tickets Team Gravity is likely to pull",
		);
	});

	// @need-over-one-cycle @contract-shape:pure-function
	it.skip("says the Team's own word for Refinement", () => {
		expect(
			describeNeed({
				...gravitysNeed("Below", 3, 5, 8),
				refinementTerm: "Grooming",
			}),
		).toContain("is likely to pull until the Grooming after. Refine");
	});
});

describe("describeNeedOrigin", () => {
	// @need-over-one-cycle @contract-shape:pure-function
	// The forecast covers the working days between two Refinements in a row; on a Refinement day the first
	// of them is today, so the dates read the same way whichever day it is.
	it.skip.each<[string, Partial<NeedOriginFacts>, string]>([
		[
			"five working days from the next Refinement",
			{},
			`Based on Team Gravity's Throughput: a How Many forecast for the 5 working days between the Refinements on Thu 8 Oct and Thu 15 Oct. ${LIKELIHOODS_AT_50_AND_85} Same forecast as on the Forecasts page.`,
		],
		[
			"one working day",
			{
				horizonWorkingDays: 1,
				cycleStart: new Date(2026, 9, 12),
				refinementDay: new Date(2026, 9, 12),
				cycleEnd: new Date(2026, 9, 13),
			},
			`Based on Team Gravity's Throughput: a How Many forecast for the 1 working day between the Refinements on Mon 12 Oct and Tue 13 Oct. ${LIKELIHOODS_AT_50_AND_85} Same forecast as on the Forecasts page.`,
		],
		[
			"a Team's own words and band",
			{
				horizonWorkingDays: 4,
				lowPercentile: 30,
				highPercentile: 95,
				teamTerm: "Squad",
				throughputTerm: "Velocity",
				refinementsTerm: "Groomings",
			},
			"Based on Team Gravity's Velocity: a How Many forecast for the 4 working days between the Groomings on Thu 8 Oct and Thu 15 Oct. The Squad pulls at least the low end with 70% likelihood, and more than the high end with only 5% likelihood. Same forecast as on the Forecasts page.",
		],
	])("names the cycle for %s", (_case, overrides, expected) => {
		expect(describeNeedOrigin(gravitysCycle(overrides))).toBe(expected);
	});
});
