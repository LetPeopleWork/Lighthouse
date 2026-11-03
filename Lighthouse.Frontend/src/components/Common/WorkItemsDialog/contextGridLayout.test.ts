import { describe, expect, it } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { IWorkItem } from "../../../models/WorkItem";
import {
	aFeature,
	aWorkItem,
	day,
	estimateOf,
	noUsableEstimate,
} from "../../../tests/WorkItemsDialogTestKit";
import type { AgeBandColumnDescriptor } from "../../../utils/charts/paceBands";
import type { SleRiskColumnDescriptor } from "../../../utils/charts/sleRisk";
import {
	type CatalogueColumnSettings,
	type ContextGridLayout,
	contextGridLayout,
} from "./workItemColumns";
import type { WorkItemsDialogContextId } from "./workItemsDialogContexts";

const productWords: Record<string, string> = {
	cycleTime: "Cycle Time",
	workItemAge: "Work Item Age",
	blocked: "Blocked",
};
const getTerm = (key: string) => productWords[key] ?? key;

type Settings = Partial<CatalogueColumnSettings>;

const layoutOf = (
	context: WorkItemsDialogContextId,
	items: readonly IWorkItem[],
	settings: Settings = {},
): ContextGridLayout =>
	contextGridLayout(context, items, { getTerm, ...settings });

const fieldsOf = (layout: ContextGridLayout): string[] =>
	Object.keys(layout.visibilityModel);

const shownIn = (layout: ContextGridLayout): string[] =>
	Object.entries(layout.visibilityModel)
		.filter(([, shown]) => shown)
		.map(([field]) => field);

const columnOf = (layout: ContextGridLayout, field: string) => {
	const column = layout.columns.find((candidate) => candidate.field === field);
	if (column === undefined) throw new Error(`no ${field} column`);
	return column;
};

const valueIn = (layout: ContextGridLayout, field: string, row: IWorkItem) =>
	columnOf(layout, field).valueGetter?.(undefined, { ...row });

const threePointerThatTookLong = aWorkItem({
	referenceId: "ST-1",
	startedDate: day("2026-09-01"),
	closedDate: day("2026-09-20"),
	cycleTime: 20,
	estimate: estimateOf(3),
	parentWorkItemReference: "FTR-1",
});
const eightPointerThatFlew = aWorkItem({
	referenceId: "ST-2",
	startedDate: day("2026-09-27"),
	closedDate: day("2026-09-30"),
	cycleTime: 4,
	estimate: estimateOf(8),
	parentWorkItemReference: "FTR-2",
});
const storyWithoutUsableEstimate = aWorkItem({
	referenceId: "ST-3",
	startedDate: day("2026-09-14"),
	closedDate: day("2026-09-25"),
	cycleTime: 12,
	estimate: noUsableEstimate(),
	parentWorkItemReference: "FTR-1",
});
const finishedStories = [
	threePointerThatTookLong,
	eightPointerThatFlew,
	storyWithoutUsableEstimate,
];

const NOW = day("2026-10-10");
const oldestButJustMoved = aWorkItem({
	referenceId: "WIP-1",
	state: "In Progress",
	stateCategory: "Doing",
	startedDate: day("2026-09-11"),
	closedDate: new Date(0),
	cycleTime: 0,
	workItemAge: 30,
	currentStateEnteredAt: day("2026-10-09"),
	isBlocked: true,
	blockedSince: "2026-10-01T12:00:00Z",
});
const longestInItsState = aWorkItem({
	referenceId: "WIP-2",
	state: "Review",
	stateCategory: "Doing",
	startedDate: day("2026-10-01"),
	closedDate: new Date(0),
	cycleTime: 0,
	workItemAge: 10,
	currentStateEnteredAt: day("2026-10-01"),
});
const justStarted = aWorkItem({
	referenceId: "WIP-3",
	state: "In Progress",
	stateCategory: "Doing",
	startedDate: day("2026-10-08"),
	closedDate: new Date(0),
	cycleTime: 0,
	workItemAge: 3,
	currentStateEnteredAt: day("2026-10-08"),
});
const inFlight = [justStarted, oldestButJustMoved, longestInItsState];
const finishedAndInFlight = [
	threePointerThatTookLong,
	oldestButJustMoved,
	justStarted,
];

const ageBand: AgeBandColumnDescriptor = {
	headerName: "Work Item Age Band",
	description: "Where this age sits",
	optionLabels: ["Below 50th", "Above 95th"],
	bandFor: (item) => (item.workItemAge > 20 ? "Above 95th" : "Below 50th"),
	colorForBand: () => undefined,
};
const sleRisk: SleRiskColumnDescriptor = {
	headerName: "SLE Risk",
	description: "Share that went on to miss the target",
	riskFor: (item) => item.workItemAge * 2,
	labelFor: (item) => `${item.workItemAge * 2}%`,
	colorForRisk: () => undefined,
	disclosureFor: () => undefined,
};
const timeInState = { now: NOW };
const warnings = {
	headerName: "Warnings",
	description: "What is worth checking",
	warningsFor: () => ["Starts after its Delivery date."],
};

const bigFeature = aFeature({
	referenceId: "FTR-2",
	size: 40,
	cycleTime: 30,
	closedDate: day("2026-09-28"),
});
const mediumFeature = aFeature({
	referenceId: "FTR-1",
	size: 13,
	cycleTime: 60,
	closedDate: day("2026-09-15"),
	owningTeam: "Team Voyager",
});
const smallFeature = aFeature({
	referenceId: "FTR-3",
	size: 5,
	cycleTime: 8,
	closedDate: day("2026-09-30"),
});
const portfolioFeatures: IFeature[] = [mediumFeature, smallFeature, bigFeature];

type ContextCase = {
	context: WorkItemsDialogContextId;
	items: IWorkItem[];
	settings?: Settings;
	/** The columns the context opens with, which Owned by is not one of: the dialog adds that itself. */
	columns: string[];
	sortedBy: string;
	/** Everything Manage columns lists for these rows: the defaults, then the one fixed order. */
	offered: string[];
};

const everyContext: ContextCase[] = [
	{
		context: "closedItems",
		items: finishedStories,
		columns: ["closedDate", "cycleTime"],
		sortedBy: "cycleTime",
		offered: [
			"closedDate",
			"cycleTime",
			"startedDate",
			"workItemAge",
			"estimate",
			"parent",
			"blockedSince",
		],
	},
	{
		context: "estimation",
		items: finishedStories,
		columns: ["estimate", "cycleTime", "closedDate"],
		sortedBy: "cycleTime",
		offered: [
			"estimate",
			"cycleTime",
			"closedDate",
			"startedDate",
			"workItemAge",
			"parent",
			"blockedSince",
		],
	},
	{
		context: "arrivals",
		items: finishedAndInFlight,
		columns: ["startedDate", "workItemAge", "cycleTime"],
		sortedBy: "workItemAge",
		offered: [
			"startedDate",
			"workItemAge",
			"cycleTime",
			"closedDate",
			"estimate",
			"parent",
			"blockedSince",
		],
	},
	{
		context: "inProgress",
		items: inFlight,
		settings: { sleRisk, timeInState },
		columns: ["startedDate", "workItemAge", "timeInState", "sleRisk"],
		sortedBy: "workItemAge",
		offered: [
			"startedDate",
			"workItemAge",
			"timeInState",
			"sleRisk",
			"closedDate",
			"cycleTime",
			"parent",
			"blockedSince",
		],
	},
	{
		context: "aging",
		items: inFlight,
		settings: { ageBand, sleRisk, timeInState },
		columns: ["workItemAge", "ageBand", "sleRisk", "timeInState"],
		sortedBy: "workItemAge",
		offered: [
			"workItemAge",
			"ageBand",
			"sleRisk",
			"timeInState",
			"startedDate",
			"closedDate",
			"cycleTime",
			"parent",
			"blockedSince",
		],
	},
	{
		context: "blocked",
		items: inFlight,
		columns: ["workItemAge", "blockedSince"],
		sortedBy: "workItemAge",
		offered: [
			"workItemAge",
			"blockedSince",
			"startedDate",
			"closedDate",
			"cycleTime",
			"parent",
		],
	},
	{
		context: "stale",
		items: inFlight,
		settings: { timeInState },
		columns: ["workItemAge", "timeInState"],
		sortedBy: "workItemAge",
		offered: [
			"workItemAge",
			"timeInState",
			"startedDate",
			"closedDate",
			"cycleTime",
			"parent",
			"blockedSince",
		],
	},
	{
		context: "workDistribution",
		items: finishedAndInFlight,
		columns: ["parent", "workItemAge", "cycleTime"],
		sortedBy: "workItemAge",
		offered: [
			"parent",
			"workItemAge",
			"cycleTime",
			"startedDate",
			"closedDate",
			"estimate",
			"blockedSince",
		],
	},
	{
		context: "featureSize",
		items: portfolioFeatures,
		// Feature rows never carry a blocked-since date, and add Size and the Features grid's
		// Forecasted Completion, whose grid field is "forecasts".
		columns: ["size", "workItemAge", "cycleTime", "closedDate"],
		sortedBy: "size",
		offered: [
			"size",
			"workItemAge",
			"cycleTime",
			"closedDate",
			"startedDate",
			"parent",
			"forecasts",
		],
	},
	{
		context: "startedAndClosed",
		items: finishedAndInFlight,
		columns: ["startedDate", "closedDate", "workItemAge", "cycleTime"],
		sortedBy: "workItemAge",
		offered: [
			"startedDate",
			"closedDate",
			"workItemAge",
			"cycleTime",
			"estimate",
			"parent",
			"blockedSince",
		],
	},
	{
		context: "featureChildren",
		items: finishedAndInFlight,
		columns: [
			"startedDate",
			"closedDate",
			"workItemAge",
			"cycleTime",
			"estimate",
		],
		sortedBy: "workItemAge",
		offered: [
			"startedDate",
			"closedDate",
			"workItemAge",
			"cycleTime",
			"estimate",
			"parent",
			"blockedSince",
		],
	},
	{
		context: "deliveryTimeline",
		items: [mediumFeature],
		settings: { warnings },
		columns: ["warnings"],
		sortedBy: "warnings",
		offered: [
			"warnings",
			"startedDate",
			"closedDate",
			"cycleTime",
			"workItemAge",
			"parent",
			"size",
			"forecasts",
		],
	},
];

const withoutEstimation = (items: IWorkItem[]): IWorkItem[] =>
	items.map((item) => ({ ...item, estimate: null }));

const exceptEstimate = (fields: string[]) =>
	fields.filter((field) => field !== "estimate");

describe("what each context opens with", () => {
	it.each(everyContext)(
		"$context opens with exactly its default columns, in order, sorted by $sortedBy",
		({ context, items, settings, columns, sortedBy }) => {
			const layout = layoutOf(context, items, settings);

			expect(shownIn(layout)).toEqual(columns);
			expect(layout.sortBy).toBe(sortedBy);
		},
	);

	it.each(everyContext)(
		"$context hands the grid one column for each field it lists, in the order it lists them",
		({ context, items, settings }) => {
			const layout = layoutOf(context, items, settings);

			expect(layout.columns.map(({ field }) => field)).toEqual(
				fieldsOf(layout),
			);
		},
	);

	it.each(everyContext)(
		"$context neither shows nor offers an Estimate where estimation is not set up",
		({ context, items, settings, columns, offered }) => {
			const layout = layoutOf(context, withoutEstimation(items), settings);

			expect(shownIn(layout)).toEqual(exceptEstimate(columns));
			expect(fieldsOf(layout)).toEqual(exceptEstimate(offered));
		},
	);

	it.each(everyContext)(
		"$context lists its defaults, then every other column the rows can fill in the one fixed order",
		({ context, items, settings, columns, offered }) => {
			const layout = layoutOf(context, items, settings);

			expect(fieldsOf(layout)).toEqual(offered);
			expect(Object.values(layout.visibilityModel)).toEqual(
				offered.map((_, index) => index < columns.length),
			);
		},
	);

	it("Cumulative Time per State opens with Days Contributed only and offers nothing else", () => {
		const layout = layoutOf(
			"cumulativeStateTime",
			[threePointerThatTookLong, eightPointerThatFlew],
			{ daysContributed: { daysFor: () => 2 } },
		);

		expect(layout.visibilityModel).toEqual({ daysContributed: true });
		expect(layout.sortBy).toBe("daysContributed");
	});
});

describe("Estimate", () => {
	it("a category estimate has its category name as its value", () => {
		const sized = {
			...threePointerThatTookLong,
			estimate: estimateOf(2, null, "M"),
		};

		const layout = layoutOf("estimation", [sized]);

		expect(valueIn(layout, "estimate", sized)).toBe("M");
	});

	it("an estimate with nothing the chart could place has an empty value", () => {
		const layout = layoutOf("estimation", finishedStories);

		expect(valueIn(layout, "estimate", threePointerThatTookLong)).toBe("3");
		expect(valueIn(layout, "estimate", storyWithoutUsableEstimate)).toBe("");
	});

	it.each([
		["every estimated row shares a unit", finishedStories],
		[
			"no unit is set",
			finishedStories.map((story) => ({
				...story,
				estimate: story.estimate && { ...story.estimate, unit: null },
			})),
		],
		[
			"the rows carry different units",
			[
				threePointerThatTookLong,
				{ ...eightPointerThatFlew, estimate: estimateOf(8, "Hours") },
			],
		],
	])("the Estimate header reads plain Estimate when %s", (_, items) => {
		expect(columnOf(layoutOf("estimation", items), "estimate").headerName).toBe(
			"Estimate",
		);
	});

	it("rows from a backend that sends no estimate at all offer no Estimate", () => {
		const fromAnOlderBackend = finishedStories.map(
			({ estimate: _, ...story }) => story,
		);

		const layout = layoutOf("estimation", fromAnOlderBackend);

		expect(shownIn(layout)).toEqual(["cycleTime", "closedDate"]);
		expect(fieldsOf(layout)).toEqual([
			"cycleTime",
			"closedDate",
			"startedDate",
			"workItemAge",
			"parent",
			"blockedSince",
		]);
	});
});

describe("dates", () => {
	it("a date shows the local day as YYYY-MM-DD, and a date the item does not have stays empty", () => {
		const layout = layoutOf("startedAndClosed", [
			eightPointerThatFlew,
			justStarted,
		]);

		expect(valueIn(layout, "closedDate", eightPointerThatFlew)).toBe(
			"2026-09-30",
		);
		expect(valueIn(layout, "startedDate", eightPointerThatFlew)).toBe(
			"2026-09-27",
		);
		expect(valueIn(layout, "closedDate", justStarted)).toBe("");
	});
});

describe("Time in State", () => {
	it("inProgress neither shows nor offers Time in State when the caller says nothing about how to measure it", () => {
		const layout = layoutOf("inProgress", inFlight, { ageBand, sleRisk });

		expect(shownIn(layout)).not.toContain("timeInState");
		expect(fieldsOf(layout)).not.toContain("timeInState");
	});
});

describe("Work Item Age and Cycle Time", () => {
	it.each([
		"arrivals",
		"workDistribution",
		"startedAndClosed",
		"featureChildren",
	] as const)(
		"%s leaves a finished item's Work Item Age and an unfinished item's Cycle Time empty, never 0",
		(context) => {
			const layout = layoutOf(context, finishedAndInFlight);

			expect(
				valueIn(layout, "workItemAge", threePointerThatTookLong),
			).toBeNull();
			expect(valueIn(layout, "cycleTime", threePointerThatTookLong)).toBe(20);
			expect(valueIn(layout, "workItemAge", oldestButJustMoved)).toBe(30);
			expect(valueIn(layout, "cycleTime", oldestButJustMoved)).toBeNull();
		},
	);

	it("an item not started yet leaves its Work Item Age empty, never 0", () => {
		const notStarted = aWorkItem({
			referenceId: "TODO-1",
			state: "New",
			stateCategory: "ToDo",
			startedDate: new Date(0),
			closedDate: new Date(0),
			cycleTime: 0,
			workItemAge: 0,
		});

		const layout = layoutOf("featureChildren", [
			notStarted,
			oldestButJustMoved,
			justStarted,
		]);

		expect(valueIn(layout, "workItemAge", notStarted)).toBeNull();
		expect(valueIn(layout, "workItemAge", oldestButJustMoved)).toBe(30);
		expect(valueIn(layout, "workItemAge", justStarted)).toBe(3);
	});

	it("Age on a past day's point is the age the item had on that day", () => {
		const layout = layoutOf("inProgress", [oldestButJustMoved], {
			ageOn: day("2026-09-20"),
		});

		expect(valueIn(layout, "workItemAge", oldestButJustMoved)).toBe(10);
	});

	it("Age on a past day's point still shows for an item finished since, because it was unfinished on that day", () => {
		const layout = layoutOf("startedAndClosed", [threePointerThatTookLong], {
			ageOn: day("2026-09-10"),
		});

		expect(valueIn(layout, "workItemAge", threePointerThatTookLong)).toBe(10);
	});

	it("Age on a day given as its first moment, as a chosen range end is, is the age on that calendar day", () => {
		const layout = layoutOf(
			"inProgress",
			[oldestButJustMoved, threePointerThatTookLong],
			{ ageOn: new Date(2026, 8, 20) },
		);

		expect(valueIn(layout, "workItemAge", oldestButJustMoved)).toBe(10);
		expect(valueIn(layout, "workItemAge", threePointerThatTookLong)).toBeNull();
	});

	it("Age on a past day stays empty for an item that had finished by that day", () => {
		const layout = layoutOf("inProgress", [threePointerThatTookLong], {
			ageOn: day("2026-09-25"),
		});

		expect(valueIn(layout, "workItemAge", threePointerThatTookLong)).toBeNull();
	});

	it("Age on a past day's point stays empty for an item that had not started by that day", () => {
		const layout = layoutOf("inProgress", [justStarted], {
			ageOn: day("2026-09-20"),
		});

		expect(valueIn(layout, "workItemAge", justStarted)).toBeNull();
	});

	it("Age on a day counts an item started later that day, and not one finished that day or never started", () => {
		const startedThatAfternoon = aWorkItem({
			referenceId: "WIP-S",
			state: "In Progress",
			stateCategory: "Doing",
			startedDate: new Date("2026-10-08T15:00:00Z"),
			closedDate: new Date(0),
			workItemAge: 3,
		});
		const finishedThatAfternoon = aWorkItem({
			referenceId: "ST-F",
			startedDate: day("2026-10-01"),
			closedDate: new Date("2026-10-08T15:00:00Z"),
		});
		const neverStarted = aWorkItem({
			referenceId: "ST-N",
			state: "New",
			stateCategory: "ToDo",
			startedDate: new Date(0),
			closedDate: new Date(0),
		});

		const layout = layoutOf(
			"inProgress",
			[startedThatAfternoon, finishedThatAfternoon, neverStarted],
			{ ageOn: day("2026-10-08") },
		);

		expect(valueIn(layout, "workItemAge", startedThatAfternoon)).toBe(1);
		expect(valueIn(layout, "workItemAge", finishedThatAfternoon)).toBeNull();
		expect(valueIn(layout, "workItemAge", neverStarted)).toBeNull();
	});

	it("Cycle Time reads the named cycle time it is scoped to, and each named column reads its own", () => {
		const namedCycleTimeDefinitions = [
			{ id: 7, name: "Dev Cycle" },
			{ id: 9, name: "Review Cycle" },
		];
		const throughBoth = aWorkItem({
			referenceId: "ST-A",
			namedCycleTimes: [
				{ definitionId: 7, days: 2 },
				{ definitionId: 9, days: 5 },
			],
		});
		const throughDevOnly = aWorkItem({
			referenceId: "ST-B",
			namedCycleTimes: [{ definitionId: 7, days: 4 }],
		});
		const throughNeither = aWorkItem({ referenceId: "ST-C" });
		const rows = [throughBoth, throughDevOnly, throughNeither];

		const layout = layoutOf("closedItems", rows, {
			namedCycleTimeDefinitions,
			cycleTimeScope: 9,
		});

		expect(columnOf(layout, "cycleTime").headerName).toBe("Review Cycle");
		expect(rows.map((row) => valueIn(layout, "cycleTime", row))).toEqual([
			5,
			null,
			null,
		]);
		expect(layout.visibilityModel["namedCycleTime:7"]).toBe(false);
		expect(rows.map((row) => valueIn(layout, "namedCycleTime:7", row))).toEqual(
			[2, 4, null],
		);
	});

	it("a named percentile's dialog reads that named cycle time under its name, and offers each named cycle time", () => {
		const withNamedCycleTimes = finishedStories.map((story, index) => ({
			...story,
			namedCycleTimes: [
				{ definitionId: 7, days: [2, 15, 6][index] },
				{ definitionId: 9, days: 1 },
			],
		}));

		const layout = layoutOf("closedItems", withNamedCycleTimes, {
			namedCycleTimeDefinitions: [
				{ id: 7, name: "Dev Cycle" },
				{ id: 9, name: "Review Cycle" },
			],
			cycleTimeScope: 7,
		});

		expect(columnOf(layout, "cycleTime").headerName).toBe("Dev Cycle");
		expect(
			withNamedCycleTimes.map((row) => valueIn(layout, "cycleTime", row)),
		).toEqual([2, 15, 6]);
		expect(
			fieldsOf(layout).filter((field) => field.startsWith("namedCycleTime:")),
		).toEqual(["namedCycleTime:7", "namedCycleTime:9"]);
	});

	it("the configured word for Cycle Time heads its column", () => {
		const layout = layoutOf("closedItems", finishedStories, {
			getTerm: (key) => (key === "cycleTime" ? "Lead Time" : key),
		});

		expect(columnOf(layout, "cycleTime").headerName).toBe("Lead Time");
	});
});

describe("what each catalogue column says", () => {
	it("heads every column the catalogue offers with its name", () => {
		const layout = layoutOf("closedItems", finishedStories);

		expect(layout.columns.map(({ headerName }) => headerName)).toEqual([
			"Closed",
			"Cycle Time",
			"Started",
			"Work Item Age",
			"Estimate",
			"Parent",
			"Blocked since",
		]);
		expect(
			columnOf(layoutOf("featureSize", portfolioFeatures), "size").headerName,
		).toBe("Size");
		expect(
			columnOf(
				layoutOf("cumulativeStateTime", [threePointerThatTookLong], {
					daysContributed: { daysFor: () => 2 },
				}),
				"daysContributed",
			).headerName,
		).toBe("Days Contributed");
	});

	it("heads the columns the caller describes, the forecast and each named cycle time with their names", () => {
		const aged = layoutOf("aging", inFlight, { ageBand, sleRisk, timeInState });
		const featureLayout = layoutOf("featureSize", portfolioFeatures);
		const timeline = layoutOf("deliveryTimeline", [mediumFeature], {
			warnings,
		});
		const named = layoutOf("closedItems", finishedStories, {
			namedCycleTimeDefinitions: [
				{ id: 7, name: "Dev Cycle" },
				{ id: 9, name: "Review Cycle" },
			],
		});

		expect(aged.columns.map(({ headerName }) => headerName)).toEqual([
			"Work Item Age",
			"Work Item Age Band",
			"SLE Risk",
			"Time in State",
			"Started",
			"Closed",
			"Cycle Time",
			"Parent",
			"Blocked since",
		]);
		expect(columnOf(featureLayout, "forecasts").headerName).toBe(
			"Forecasted Completion",
		);
		expect(columnOf(timeline, "warnings").headerName).toBe("Warnings");
		expect(columnOf(named, "namedCycleTime:7").headerName).toBe("Dev Cycle");
		expect(columnOf(named, "namedCycleTime:9").headerName).toBe("Review Cycle");
	});

	it("Feature rows add Size and Forecasted Completion, last, to what Manage columns offers", () => {
		const layout = layoutOf("closedItems", portfolioFeatures);

		expect(fieldsOf(layout).slice(-2)).toEqual(["size", "forecasts"]);
		expect(fieldsOf(layout)).not.toContain("blockedSince");
	});

	it("the Delivery timeline keeps Warnings as its only extra column and never offers Estimate or Blocked since", () => {
		const layout = layoutOf(
			"deliveryTimeline",
			[{ ...mediumFeature, estimate: estimateOf(5) }],
			{ warnings },
		);

		expect(shownIn(layout)).toEqual(["warnings"]);
		expect(fieldsOf(layout)).toContain("startedDate");
		expect(fieldsOf(layout)).not.toContain("estimate");
		expect(fieldsOf(layout)).not.toContain("blockedSince");
	});

	it("lists every warning a row has, one sentence after another", () => {
		const layout = layoutOf("deliveryTimeline", [mediumFeature], {
			warnings: {
				...warnings,
				warningsFor: () => [
					"Starts after its Delivery date.",
					"Has no forecast.",
				],
			},
		});

		expect(valueIn(layout, "warnings", mediumFeature)).toBe(
			"Starts after its Delivery date. Has no forecast.",
		);
	});
});
