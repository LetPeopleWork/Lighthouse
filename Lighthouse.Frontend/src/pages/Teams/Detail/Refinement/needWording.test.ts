import { describe, expect, it } from "vitest";
import type { RefinementVerdict } from "../../../../models/Refinement/Refinement";
import { describeNeed, describeNeedOrigin } from "./needWording";

const THURSDAY_THE_EIGHTH = new Date(2026, 9, 8);

describe("describeNeed", () => {
	it.each<[RefinementVerdict, number, number, number, string]>([
		[
			"Below",
			3,
			5,
			8,
			"3 ready — below the range of 5–8 Work Items Team Gravity is likely to pull by then. Refine 2 to 5 more.",
		],
		[
			"Below",
			0,
			1,
			2,
			"0 ready — below the range of 1–2 Work Items Team Gravity is likely to pull by then. Refine 1 to 2 more.",
		],
		[
			"Below",
			3,
			5,
			5,
			"3 ready — below the 5 Work Items Team Gravity is likely to pull by then. Refine 2 more.",
		],
		[
			"Below",
			0,
			1,
			1,
			"0 ready — below the 1 Work Item Team Gravity is likely to pull by then. Refine 1 more.",
		],
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
			expect(
				describeNeed({
					verdict,
					readyCount,
					low,
					high,
					teamName: "Team Gravity",
					workItemTerm: "Work Item",
					workItemsTerm: "Work Items",
				}),
			).toBe(expected);
		},
	);

	it("says the Team's own word for Work Items and names the Team as it is", () => {
		expect(
			describeNeed({
				verdict: "Below",
				readyCount: 3,
				low: 5,
				high: 8,
				teamName: "Team Gravity",
				workItemTerm: "Ticket",
				workItemsTerm: "Tickets",
			}),
		).toContain(
			"below the range of 5–8 Tickets Team Gravity is likely to pull",
		);
	});
});

describe("describeNeedOrigin", () => {
	it.each<[number, number, number, string, string, string]>([
		[
			6,
			50,
			85,
			"Team",
			"Throughput",
			"Based on Team Gravity's Throughput: a How Many forecast for the 6 working days until Thu 8 Oct. The Team pulls at least the low end with 50% likelihood, and more than the high end with only 15% likelihood. Same forecast as on the Forecasts page.",
		],
		[
			1,
			50,
			85,
			"Team",
			"Throughput",
			"Based on Team Gravity's Throughput: a How Many forecast for the 1 working day until Thu 8 Oct. The Team pulls at least the low end with 50% likelihood, and more than the high end with only 15% likelihood. Same forecast as on the Forecasts page.",
		],
		[
			4,
			30,
			95,
			"Squad",
			"Velocity",
			"Based on Team Gravity's Velocity: a How Many forecast for the 4 working days until Thu 8 Oct. The Squad pulls at least the low end with 70% likelihood, and more than the high end with only 5% likelihood. Same forecast as on the Forecasts page.",
		],
	])(
		"over %s working days, read at %s and %s percent, for a %s with %s",
		(horizonWorkingDays, lowPercentile, highPercentile, teamTerm, throughputTerm, expected) => {
			expect(
				describeNeedOrigin({
					teamName: "Team Gravity",
					horizonWorkingDays,
					refinementDay: THURSDAY_THE_EIGHTH,
					lowPercentile,
					highPercentile,
					teamTerm,
					throughputTerm,
				}),
			).toBe(expected);
		},
	);
});
