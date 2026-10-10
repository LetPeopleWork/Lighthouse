import { screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import type { IWorkItem } from "../../../models/WorkItem";
import {
	aFeature,
	aWorkItem,
	catalogueOffered,
	cellIn,
	cellText,
	columnHeader,
	columnsShown,
	contextColumnsShown,
	day,
	estimateOf,
	headerText,
	noUsableEstimate,
	openManageColumns,
	openTheDialog,
	rowOrder,
	sortedDescendingBy,
	turnColumn,
} from "../../../tests/WorkItemsDialogTestKit";
import type { AgeBandColumnDescriptor } from "../../../utils/charts/paceBands";
import type { SleRiskColumnDescriptor } from "../../../utils/charts/sleRisk";
import { riskyColor } from "../../../utils/theme/colors";
import type { WorkItemsDialogContextId } from "./workItemsDialogContexts";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		licenseStatus: { canUsePremiumFeatures: false },
		isLoading: false,
	}),
}));

const productWords: Record<string, string> = {
	workItem: "Work Item",
	workItems: "Work Items",
	cycleTime: "Cycle Time",
	workItemAge: "Work Item Age",
	blocked: "Blocked",
	team: "Team",
	feature: "Feature",
	features: "Features",
};
const instanceWords: Record<string, string> = {};

vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => instanceWords[key] ?? productWords[key] ?? key,
	}),
}));

beforeEach(() => {
	localStorage.clear();
	for (const key of Object.keys(instanceWords)) delete instanceWords[key];
});

const RENDER_HEAVY = 20000;

// Three finished stories of one Team whose estimation field is Story Points: one took 20 days at
// 3 points, one 4 days at 8 points, and one has nothing in the field the chart could place.
const threePointerThatTookLong = aWorkItem({
	referenceId: "ST-1",
	startedDate: day("2026-09-01"),
	closedDate: day("2026-09-20"),
	cycleTime: 20,
	estimate: estimateOf(3),
	parentWorkItemReference: "FTR-1",
	currentStateEnteredAt: day("2026-09-20"),
});
const eightPointerThatFlew = aWorkItem({
	referenceId: "ST-2",
	startedDate: day("2026-09-27"),
	closedDate: day("2026-09-30"),
	cycleTime: 4,
	estimate: estimateOf(8),
	parentWorkItemReference: "FTR-2",
	currentStateEnteredAt: day("2026-09-30"),
});
const storyWithoutUsableEstimate = aWorkItem({
	referenceId: "ST-3",
	startedDate: day("2026-09-14"),
	closedDate: day("2026-09-25"),
	cycleTime: 12,
	estimate: noUsableEstimate(),
	parentWorkItemReference: "FTR-1",
	currentStateEnteredAt: day("2026-09-25"),
});
const finishedStories = [
	threePointerThatTookLong,
	eightPointerThatFlew,
	storyWithoutUsableEstimate,
];
const finishedStoriesWithoutEstimation = finishedStories.map((story) => ({
	...story,
	estimate: null,
}));

// Three items in flight on 2026-10-10. Oldest first is WIP-1, but WIP-2 has sat longest in its
// state, which is what today's dialogs sort the aging and stale lists by.
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

const ageBandColumn: AgeBandColumnDescriptor = {
	headerName: "Work Item Age Band",
	description: "Where this age sits",
	optionLabels: ["Below 50th", "Above 95th"],
	bandFor: (item) => (item.workItemAge > 20 ? "Above 95th" : "Below 50th"),
	colorForBand: () => undefined,
};
const sleRiskColumn: SleRiskColumnDescriptor = {
	headerName: "SLE Risk",
	description: "Share that went on to miss the target",
	riskFor: (item) => item.workItemAge * 2,
	labelFor: (item) => `${item.workItemAge * 2}%`,
	colorForRisk: () => undefined,
	disclosureFor: () => undefined,
};
const timeInStateColumn = { now: NOW };

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

const warningsColumn = {
	headerName: "Warnings",
	description: "What is worth checking",
	warningsFor: () => ["Starts after its Delivery date."],
};

type ContextCase = {
	context: WorkItemsDialogContextId;
	items: IWorkItem[];
	extra?: Record<string, unknown>;
	columns: string[];
	sortedBy: string;
	firstRow: string;
	/** Everything Manage columns lists for these rows: the defaults, then the one fixed order. */
	offered: string[];
};

const everyChartContext: ContextCase[] = [
	{
		context: "closedItems",
		items: finishedStories,
		columns: ["closedDate", "cycleTime"],
		sortedBy: "cycleTime",
		firstRow: "ST-1",
		offered: [
			"closedDate",
			"cycleTime",
			"startedDate",
			"workItemAge",
			"ageOrCycleTime",
			"estimate",
			"parent",
			"blockedSince",
			"timeInState",
		],
	},
	{
		context: "estimation",
		items: finishedStories,
		columns: ["estimate", "cycleTime", "closedDate"],
		sortedBy: "cycleTime",
		firstRow: "ST-1",
		offered: [
			"estimate",
			"cycleTime",
			"closedDate",
			"startedDate",
			"workItemAge",
			"ageOrCycleTime",
			"parent",
			"blockedSince",
			"timeInState",
		],
	},
	{
		context: "arrivals",
		items: [threePointerThatTookLong, oldestButJustMoved, justStarted],
		columns: ["startedDate", "ageOrCycleTime"],
		sortedBy: "ageOrCycleTime",
		firstRow: "WIP-1",
		offered: [
			"startedDate",
			"ageOrCycleTime",
			"closedDate",
			"cycleTime",
			"workItemAge",
			"estimate",
			"parent",
			"blockedSince",
			"timeInState",
		],
	},
	{
		context: "inProgress",
		items: inFlight,
		extra: { sleRiskColumn, timeInStateColumn },
		columns: ["startedDate", "workItemAge", "timeInState", "sleRisk"],
		sortedBy: "workItemAge",
		firstRow: "WIP-1",
		offered: [
			"startedDate",
			"workItemAge",
			"timeInState",
			"sleRisk",
			"closedDate",
			"cycleTime",
			"ageOrCycleTime",
			"parent",
			"blockedSince",
		],
	},
	{
		context: "aging",
		items: inFlight,
		extra: { ageBandColumn, sleRiskColumn, timeInStateColumn },
		columns: ["workItemAge", "ageBand", "sleRisk", "timeInState"],
		sortedBy: "workItemAge",
		firstRow: "WIP-1",
		offered: [
			"workItemAge",
			"ageBand",
			"sleRisk",
			"timeInState",
			"startedDate",
			"closedDate",
			"cycleTime",
			"ageOrCycleTime",
			"parent",
			"blockedSince",
		],
	},
	{
		context: "blocked",
		items: inFlight,
		columns: ["workItemAge", "blockedSince"],
		sortedBy: "workItemAge",
		firstRow: "WIP-1",
		offered: [
			"workItemAge",
			"blockedSince",
			"startedDate",
			"closedDate",
			"cycleTime",
			"ageOrCycleTime",
			"parent",
			"timeInState",
		],
	},
	{
		context: "stale",
		items: inFlight,
		extra: { timeInStateColumn },
		columns: ["workItemAge", "timeInState"],
		sortedBy: "workItemAge",
		firstRow: "WIP-1",
		offered: [
			"workItemAge",
			"timeInState",
			"startedDate",
			"closedDate",
			"cycleTime",
			"ageOrCycleTime",
			"parent",
			"blockedSince",
		],
	},
	{
		context: "workDistribution",
		items: [threePointerThatTookLong, oldestButJustMoved, justStarted],
		columns: ["parent", "ageOrCycleTime"],
		sortedBy: "ageOrCycleTime",
		firstRow: "WIP-1",
		offered: [
			"parent",
			"ageOrCycleTime",
			"startedDate",
			"closedDate",
			"cycleTime",
			"workItemAge",
			"estimate",
			"blockedSince",
			"timeInState",
		],
	},
	{
		context: "featureSize",
		items: portfolioFeatures,
		columns: ["owningTeam", "size", "ageOrCycleTime", "closedDate"],
		sortedBy: "size",
		firstRow: "FTR-2",
		// Feature rows keep Owned by beside the fixed columns, never carry a blocked-since date, and
		// add Size and the Features grid's Forecasted Completion, whose grid field is "forecasts".
		offered: [
			"owningTeam",
			"size",
			"ageOrCycleTime",
			"closedDate",
			"startedDate",
			"cycleTime",
			"workItemAge",
			"parent",
			"timeInState",
			"forecasts",
		],
	},
	{
		context: "startedAndClosed",
		items: [threePointerThatTookLong, oldestButJustMoved, justStarted],
		columns: ["startedDate", "closedDate", "ageOrCycleTime"],
		sortedBy: "ageOrCycleTime",
		firstRow: "WIP-1",
		offered: [
			"startedDate",
			"closedDate",
			"ageOrCycleTime",
			"cycleTime",
			"workItemAge",
			"estimate",
			"parent",
			"blockedSince",
			"timeInState",
		],
	},
];

const theFeatureListContexts: ContextCase[] = [
	{
		context: "featureChildren",
		items: [threePointerThatTookLong, oldestButJustMoved, justStarted],
		columns: ["startedDate", "closedDate", "ageOrCycleTime", "estimate"],
		sortedBy: "ageOrCycleTime",
		firstRow: "WIP-1",
		offered: [
			"startedDate",
			"closedDate",
			"ageOrCycleTime",
			"estimate",
			"cycleTime",
			"workItemAge",
			"parent",
			"blockedSince",
			"timeInState",
		],
	},
	{
		context: "deliveryTimeline",
		items: [mediumFeature],
		extra: { warningsColumn },
		columns: ["owningTeam", "warnings"],
		sortedBy: "warnings",
		firstRow: "FTR-1",
		offered: [
			"owningTeam",
			"warnings",
			"startedDate",
			"closedDate",
			"cycleTime",
			"workItemAge",
			"ageOrCycleTime",
			"parent",
			"timeInState",
			"size",
			"forecasts",
		],
	},
];

const withoutEstimation = (items: IWorkItem[]): IWorkItem[] =>
	items.map((item) => ({ ...item, estimate: null }));

async function estimateIsNeitherShownNorOffered({
	context,
	items,
	extra,
	columns,
	offered,
}: ContextCase) {
	const user = userEvent.setup();
	const exceptEstimate = (fields: string[]) =>
		fields.filter((field) => field !== "estimate");
	openTheDialog({ context, items: withoutEstimation(items), ...extra });

	expect(contextColumnsShown()).toEqual(exceptEstimate(columns));

	await openManageColumns(user);

	expect(catalogueOffered().map(({ field }) => field)).toEqual(
		exceptEstimate(offered),
	);
}

async function manageColumnsListsTheOneFixedOrder({
	context,
	items,
	extra,
	offered,
}: ContextCase) {
	const user = userEvent.setup();
	openTheDialog({ context, items, ...extra });

	await openManageColumns(user);

	expect(catalogueOffered().map(({ field }) => field)).toEqual(offered);
}

async function theReadersLayoutComesBackOnReopen({
	context,
	items,
	extra,
	columns,
	offered,
}: ContextCase) {
	const user = userEvent.setup();
	const hidden = columns[columns.length - 1];
	const turnedOn = offered[columns.length];
	const first = openTheDialog({ context, items, ...extra });
	await turnColumn(user, hidden, false);
	await turnColumn(user, turnedOn, true);
	first.unmount();

	openTheDialog({ context, items, ...extra });

	expect(contextColumnsShown()).toEqual([...columns.slice(0, -1), turnedOn]);
}

describe("US-01 — the Estimation dialog shows each item's estimate", () => {
	// @us-01 @slice-01b @driving_port @contract-shape:pure-function
	it.skip(
		"a bubble on a Team's Estimation vs. Cycle Time opens with Estimate, Cycle Time and Closed, longest Cycle Time first",
		() => {
			openTheDialog({
				context: "estimation",
				ownerKind: "team",
				items: finishedStories,
			});

			expect(contextColumnsShown()).toEqual([
				"estimate",
				"cycleTime",
				"closedDate",
			]);
			expect(sortedDescendingBy()).toBe("cycleTime");
			expect(rowOrder()).toEqual(["ST-1", "ST-3", "ST-2"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @driving_port @contract-shape:pure-function
	it.skip(
		"a bubble on a Portfolio's Estimation vs. Cycle Time opens the same way, with Owned by still beside the Features",
		() => {
			const estimatedFeatures = portfolioFeatures.map((feature, index) => ({
				...feature,
				estimate: estimateOf(index + 1, "T-shirt", ["S", "M", "L"][index]),
			}));

			openTheDialog({
				context: "estimation",
				ownerKind: "portfolio",
				items: estimatedFeatures,
			});

			expect(contextColumnsShown()).toEqual([
				"owningTeam",
				"estimate",
				"cycleTime",
				"closedDate",
			]);
			expect(columnHeader("owningTeam")).toBeInTheDocument();
			expect(rowOrder()).toEqual(["FTR-1", "FTR-2", "FTR-3"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @driving_port @contract-shape:pure-function
	it.skip(
		"each row shows the estimate the chart plots it at, and an empty cell where there is nothing the chart could place",
		() => {
			openTheDialog({ context: "estimation", items: finishedStories });

			expect(cellText("estimate", "ST-1")).toBe("3");
			expect(cellText("estimate", "ST-2")).toBe("8");
			expect(cellText("estimate", "ST-3")).toBe("");
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @contract-shape:pure-function
	it.skip(
		"a category estimate shows its category name",
		() => {
			const sized = [
				{ ...threePointerThatTookLong, estimate: estimateOf(2, null, "M") },
			];

			openTheDialog({ context: "estimation", items: sized });

			expect(cellText("estimate", "ST-1")).toBe("M");
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @boundary @contract-shape:pure-function
	it.skip.each([
		[
			"names the unit when every estimated row shares it",
			finishedStories,
			"Estimate (Story Points)",
		],
		[
			"reads plain Estimate when no unit is set",
			finishedStories.map((story) => ({
				...story,
				estimate: story.estimate && { ...story.estimate, unit: null },
			})),
			"Estimate",
		],
		[
			"reads plain Estimate when the rows carry different units",
			[
				threePointerThatTookLong,
				{ ...eightPointerThatFlew, estimate: estimateOf(8, "Hours") },
			],
			"Estimate",
		],
	])(
		"the Estimate header %s",
		(_, items, header) => {
			openTheDialog({ context: "estimation", items });

			expect(headerText("estimate")).toBe(header);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @contract-shape:pure-function
	it.skip(
		"sorting by Estimate follows the configured category order, not the alphabet",
		async () => {
			const user = userEvent.setup();
			const sized = [
				{ ...threePointerThatTookLong, estimate: estimateOf(2, null, "M") },
				{ ...eightPointerThatFlew, estimate: estimateOf(0, null, "XS") },
				{ ...storyWithoutUsableEstimate, estimate: estimateOf(1, null, "S") },
			];
			openTheDialog({ context: "estimation", items: sized });

			await user.click(columnHeader("estimate"));

			expect(rowOrder()).toEqual(["ST-2", "ST-3", "ST-1"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @driving_port @contract-shape:pure-function
	it.skip(
		"Manage columns offers every other column the rows can fill, all of them off, after the defaults",
		async () => {
			const user = userEvent.setup();
			openTheDialog({ context: "estimation", items: finishedStories });

			await openManageColumns(user);

			expect(catalogueOffered()).toEqual([
				{ field: "estimate", shown: true },
				{ field: "cycleTime", shown: true },
				{ field: "closedDate", shown: true },
				{ field: "startedDate", shown: false },
				{ field: "workItemAge", shown: false },
				{ field: "ageOrCycleTime", shown: false },
				{ field: "parent", shown: false },
				{ field: "blockedSince", shown: false },
				{ field: "timeInState", shown: false },
			]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @contract-shape:bounded-change
	it.skip(
		"a column turned on appears in its place in the fixed order",
		async () => {
			const user = userEvent.setup();
			openTheDialog({ context: "estimation", items: finishedStories });

			await turnColumn(user, "parent", true);
			await turnColumn(user, "startedDate", true);

			expect(contextColumnsShown()).toEqual([
				"estimate",
				"cycleTime",
				"closedDate",
				"startedDate",
				"parent",
			]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @error @contract-shape:pure-function
	it.skip(
		"rows from a backend that sends no estimate at all read as estimation not set up",
		async () => {
			const user = userEvent.setup();
			const fromAnOlderBackend = finishedStories.map(
				({ estimate: _, ...story }) => story,
			);
			openTheDialog({ context: "estimation", items: fromAnOlderBackend });

			expect(contextColumnsShown()).toEqual(["cycleTime", "closedDate"]);

			await openManageColumns(user);

			expect(catalogueOffered().map(({ field }) => field)).toEqual([
				"cycleTime",
				"closedDate",
				"startedDate",
				"workItemAge",
				"ageOrCycleTime",
				"parent",
				"blockedSince",
				"timeInState",
			]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @boundary @contract-shape:pure-function
	it.skip(
		"dates show the local day as YYYY-MM-DD, and a date the item does not have stays empty",
		() => {
			openTheDialog({
				context: "startedAndClosed",
				items: [eightPointerThatFlew, justStarted],
			});

			expect(cellText("closedDate", "ST-2")).toBe("2026-09-30");
			expect(cellText("startedDate", "ST-2")).toBe("2026-09-27");
			expect(cellText("closedDate", "WIP-3")).toBe("");
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @contract-shape:pure-function
	it.skip(
		"the configured word for Cycle Time heads its column",
		() => {
			instanceWords.cycleTime = "Lead Time";

			openTheDialog({ context: "closedItems", items: finishedStories });

			expect(headerText("cycleTime")).toBe("Lead Time");
		},
		RENDER_HEAVY,
	);
});

describe("US-02 — every chart's dialog brings the columns that explain its point", () => {
	// @us-02 @slice-02 @driving_port @contract-shape:pure-function
	it.skip.each(everyChartContext)(
		"$context opens with exactly its default columns, in order, sorted by $sortedBy",
		({ context, items, extra, columns, sortedBy, firstRow }) => {
			openTheDialog({ context, items, ...extra });

			expect(contextColumnsShown()).toEqual(columns);
			expect(sortedDescendingBy()).toBe(sortedBy);
			expect(rowOrder()[0]).toBe(firstRow);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @driving_port @contract-shape:pure-function
	it.skip(
		"a Feature Size dot lists the largest Feature first",
		() => {
			openTheDialog({
				context: "featureSize",
				ownerKind: "portfolio",
				items: portfolioFeatures,
			});

			expect(rowOrder()).toEqual(["FTR-2", "FTR-1", "FTR-3"]);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @contract-shape:pure-function
	it.skip.each(["aging", "stale", "inProgress"] as const)(
		"%s lists the oldest item first, not the one longest in its state",
		(context) => {
			openTheDialog({
				context,
				items: inFlight,
				timeInStateColumn,
				ageBandColumn,
				sleRiskColumn,
			});

			expect(rowOrder()).toEqual(["WIP-1", "WIP-2", "WIP-3"]);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @regression @contract-shape:pure-function
	it.skip(
		"Cycle Time keeps its SLE colouring and bold where the caller passes the SLE",
		() => {
			openTheDialog({
				context: "closedItems",
				items: finishedStories,
				sle: 10,
			});

			const longest = within(cellIn("cycleTime", "ST-1")).getByTestId(
				"additionalColumnContent",
			);
			expect(longest).toHaveStyle(`color: ${riskyColor}`);
			expect(longest).toHaveStyle("font-weight: 700");
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @regression @contract-shape:pure-function
	it.skip(
		"a blocked item still carries the blocked marker in the column it is sorted by",
		() => {
			openTheDialog({ context: "blocked", items: inFlight });

			expect(
				within(cellIn("workItemAge", "WIP-1")).getByTestId("BlockIcon"),
			).toBeInTheDocument();
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @regression @contract-shape:pure-function
	it.skip(
		"Time in State keeps its own badge when the list is sorted by Age",
		() => {
			openTheDialog({ context: "stale", items: inFlight, timeInStateColumn });

			expect(sortedDescendingBy()).toBe("workItemAge");
			expect(cellText("timeInState", "WIP-2")).toMatch(/^\d+d in Review$/);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it.skip(
		"Age on a past day's point is the age the item had on that day",
		() => {
			openTheDialog({
				context: "inProgress",
				items: [oldestButJustMoved],
				ageOn: day("2026-09-20"),
			});

			expect(cellText("workItemAge", "WIP-1")).toBe("10");
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @driving_port @contract-shape:pure-function
	it.skip(
		"a named percentile's dialog reads that named cycle time under its name, and offers each named cycle time",
		async () => {
			const user = userEvent.setup();
			const namedCycleTimeDefinitions = [
				{ id: 7, name: "Dev Cycle" },
				{ id: 9, name: "Review Cycle" },
			];
			const withNamedCycleTimes = finishedStories.map((story, index) => ({
				...story,
				namedCycleTimes: [
					{ definitionId: 7, days: [2, 15, 6][index] },
					{ definitionId: 9, days: 1 },
				],
			}));
			openTheDialog({
				context: "closedItems",
				items: withNamedCycleTimes,
				namedCycleTimeDefinitions,
				cycleTimeScope: 7,
			});

			expect(headerText("cycleTime")).toBe("Dev Cycle");
			expect(rowOrder()).toEqual(["ST-2", "ST-3", "ST-1"]);

			await openManageColumns(user);

			expect(
				catalogueOffered()
					.map(({ field }) => field)
					.filter((field) => field.startsWith("namedCycleTime:")),
			).toEqual(["namedCycleTime:7", "namedCycleTime:9"]);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it.skip(
		"Cumulative Time per State keeps Days Contributed only and offers nothing else",
		async () => {
			const user = userEvent.setup();
			const daysById = new Map([
				[threePointerThatTookLong.id, 2.5],
				[eightPointerThatFlew.id, 7],
			]);
			openTheDialog({
				context: "cumulativeStateTime",
				items: [threePointerThatTookLong, eightPointerThatFlew],
				daysContributedColumn: {
					daysFor: (item) => daysById.get(item.id) ?? 0,
				},
			});

			expect(contextColumnsShown()).toEqual(["daysContributed"]);
			expect(rowOrder()).toEqual(["ST-2", "ST-1"]);

			await openManageColumns(user);

			expect(catalogueOffered()).toEqual([
				{ field: "daysContributed", shown: true },
			]);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @contract-shape:pure-function
	it.skip(
		"Feature rows add Size and Forecasted Completion, last, to what Manage columns offers",
		async () => {
			const user = userEvent.setup();
			openTheDialog({
				context: "closedItems",
				ownerKind: "portfolio",
				items: portfolioFeatures,
			});

			await openManageColumns(user);

			expect(
				catalogueOffered()
					.map(({ field }) => field)
					.slice(-2),
			).toEqual(["size", "forecasts"]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @error @contract-shape:pure-function
	it.skip(
		"Work Item rows neither show nor offer Owned by",
		async () => {
			const user = userEvent.setup();
			openTheDialog({ context: "closedItems", items: finishedStories });

			expect(columnsShown()).toEqual([
				"referenceId",
				"name",
				"type",
				"state",
				"closedDate",
				"cycleTime",
			]);

			await openManageColumns(user);

			expect(catalogueOffered().map(({ field }) => field)).toEqual([
				"closedDate",
				"cycleTime",
				"startedDate",
				"workItemAge",
				"ageOrCycleTime",
				"estimate",
				"parent",
				"blockedSince",
				"timeInState",
			]);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @driving_port @contract-shape:bounded-change
	it.skip(
		"a Feature's forecast is offered as the Features grid's Forecasted Completion column, right after Size",
		async () => {
			const user = userEvent.setup();
			const forecast = new WhenForecast();
			forecast.probability = 85;
			forecast.expectedDate = day("2026-11-20");
			const forecastFeatures = portfolioFeatures.map((feature) => ({
				...feature,
				forecasts: [forecast],
			}));
			openTheDialog({
				context: "closedItems",
				ownerKind: "portfolio",
				items: forecastFeatures,
			});

			await openManageColumns(user);

			const boxes = screen.getAllByRole("checkbox");
			const sizeAt = boxes.findIndex(
				(box) => box.getAttribute("name") === "size",
			);
			const forecastAt = boxes.findIndex(
				(box) =>
					box.closest("label")?.textContent?.trim() === "Forecasted Completion",
			);
			expect(sizeAt).toBeGreaterThan(-1);
			expect(forecastAt).toBe(sizeAt + 1);
			await user.keyboard("{Escape}");

			await turnColumn(user, "forecasts", true);

			expect(
				screen.getByRole("columnheader", { name: "Forecasted Completion" }),
			).toBeInTheDocument();
			expect(screen.getAllByTestId("feature-forecast-cell")).toHaveLength(3);
		},
		RENDER_HEAVY,
	);
});

describe("US-02 — every chart context that offers more columns follows the same rules", () => {
	// @us-02 @slice-02 @error @contract-shape:pure-function
	it.skip.each(everyChartContext)(
		"$context neither shows nor offers an Estimate where estimation is not set up",
		estimateIsNeitherShownNorOffered,
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @contract-shape:pure-function
	it.skip.each(everyChartContext)(
		"$context lists its defaults, then every other column the rows can fill in the one fixed order",
		manageColumnsListsTheOneFixedOrder,
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @contract-shape:bounded-change
	it.skip.each(everyChartContext)(
		"$context comes back with the reader's own layout, a default hidden and another column on, the next time it opens",
		theReadersLayoutComesBackOnReopen,
		RENDER_HEAVY,
	);
});

describe("US-03 — a Feature's child items show their progress at a glance", () => {
	// @us-03 @slice-03 @driving_port @contract-shape:pure-function
	it.skip(
		"a Feature's child items open with Started, Closed, Age / Cycle Time and Estimate",
		() => {
			openTheDialog({
				context: "featureChildren",
				items: [threePointerThatTookLong, oldestButJustMoved, justStarted],
			});

			expect(contextColumnsShown()).toEqual([
				"startedDate",
				"closedDate",
				"ageOrCycleTime",
				"estimate",
			]);
			expect(sortedDescendingBy()).toBe("ageOrCycleTime");
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @error @contract-shape:pure-function
	it.skip(
		"without estimation on the child items' Team, the child items open without an Estimate",
		() => {
			openTheDialog({
				context: "featureChildren",
				items: finishedStoriesWithoutEstimation,
			});

			expect(contextColumnsShown()).toEqual([
				"startedDate",
				"closedDate",
				"ageOrCycleTime",
			]);
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @error @contract-shape:pure-function
	it.skip(
		"the Delivery timeline keeps Warnings as its only extra column and never offers Estimate or Blocked since",
		async () => {
			const user = userEvent.setup();
			openTheDialog({
				context: "deliveryTimeline",
				ownerKind: "portfolio",
				items: [{ ...mediumFeature, estimate: estimateOf(5) }],
				warningsColumn,
			});

			expect(contextColumnsShown()).toEqual(["owningTeam", "warnings"]);

			await openManageColumns(user);

			const offered = catalogueOffered().map(({ field }) => field);
			expect(offered).toContain("startedDate");
			expect(offered).not.toContain("estimate");
			expect(offered).not.toContain("blockedSince");
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @error @contract-shape:pure-function
	it.skip.each(theFeatureListContexts)(
		"$context neither shows nor offers an Estimate where estimation is not set up",
		estimateIsNeitherShownNorOffered,
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @contract-shape:pure-function
	it.skip.each(theFeatureListContexts)(
		"$context lists its defaults, then every other column the rows can fill in the one fixed order",
		manageColumnsListsTheOneFixedOrder,
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @contract-shape:bounded-change
	it.skip.each(theFeatureListContexts)(
		"$context comes back with the reader's own layout, a default hidden and another column on, the next time it opens",
		theReadersLayoutComesBackOnReopen,
		RENDER_HEAVY,
	);
});

describe("guards that hold today and must keep holding", () => {
	// @us-01 @slice-01b @regression @contract-shape:unbounded-preservation
	it(
		"Feature rows keep Owned by beside the fixed columns",
		() => {
			openTheDialog({ items: portfolioFeatures });

			expect(columnHeader("owningTeam")).toBeInTheDocument();
			expect(cellText("owningTeam", "FTR-1")).toBe("Team Voyager");
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @regression @contract-shape:unbounded-preservation
	it(
		"a dialog opened without a context keeps today's columns",
		() => {
			openTheDialog({
				items: finishedStories,
				highlightColumn: {
					title: "Cycle Time",
					description: "days",
					valueGetter: (item) => item.cycleTime,
				},
			});

			expect(contextColumnsShown()).toEqual(["additionalColumn"]);
			expect(rowOrder()).toEqual(["ST-1", "ST-3", "ST-2"]);
		},
		RENDER_HEAVY,
	);
});
