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
	FIXED_COLUMNS,
	headerText,
	noUsableEstimate,
	openManageColumns,
	openTheDialog,
	rowOrder,
	sortedDescendingBy,
	turnColumn,
	turnColumns,
} from "../../../tests/WorkItemsDialogTestKit";
import type { AgeBandColumnDescriptor } from "../../../utils/charts/paceBands";
import type { SleRiskColumnDescriptor } from "../../../utils/charts/sleRisk";
import { hexToRgba, riskyColor } from "../../../utils/theme/colors";
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
		firstRow: "ST-1",
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
		items: [threePointerThatTookLong, oldestButJustMoved, justStarted],
		columns: ["startedDate", "workItemAge", "cycleTime"],
		sortedBy: "workItemAge",
		firstRow: "WIP-1",
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
			"parent",
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
			"parent",
			"blockedSince",
		],
	},
	{
		context: "workDistribution",
		items: [threePointerThatTookLong, oldestButJustMoved, justStarted],
		columns: ["parent", "workItemAge", "cycleTime"],
		sortedBy: "workItemAge",
		firstRow: "WIP-1",
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
		columns: ["owningTeam", "size", "workItemAge", "cycleTime", "closedDate"],
		sortedBy: "size",
		firstRow: "FTR-2",
		// Feature rows keep Owned by beside the fixed columns, never carry a blocked-since date, and
		// add Size and the Features grid's Forecasted Completion, whose grid field is "forecasts".
		offered: [
			"owningTeam",
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
		items: [threePointerThatTookLong, oldestButJustMoved, justStarted],
		columns: ["startedDate", "closedDate", "workItemAge", "cycleTime"],
		sortedBy: "workItemAge",
		firstRow: "WIP-1",
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
];

const theFeatureListContexts: ContextCase[] = [
	{
		context: "featureChildren",
		items: [threePointerThatTookLong, oldestButJustMoved, justStarted],
		columns: [
			"startedDate",
			"closedDate",
			"workItemAge",
			"cycleTime",
			"estimate",
		],
		sortedBy: "workItemAge",
		firstRow: "WIP-1",
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
			"parent",
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
	await turnColumns(user, [
		[hidden, false],
		[turnedOn, true],
	]);
	first.unmount();

	openTheDialog({ context, items, ...extra });

	expect(contextColumnsShown()).toEqual([...columns.slice(0, -1), turnedOn]);
}

describe("US-01 — the Estimation dialog shows each item's estimate", () => {
	// @us-01 @slice-01b @driving_port @contract-shape:pure-function
	it(
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
	it(
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
	it(
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
	it(
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
	it.each([
		[
			"reads plain Estimate even when every estimated row shares a unit",
			finishedStories,
			"Estimate",
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
	it(
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
	it(
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
				{ field: "parent", shown: false },
				{ field: "blockedSince", shown: false },
			]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @contract-shape:bounded-change
	it(
		"a column turned on appears in its place in the fixed order",
		async () => {
			const user = userEvent.setup();
			openTheDialog({ context: "estimation", items: finishedStories });

			await turnColumns(user, [
				["parent", true],
				["startedDate", true],
			]);

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
	it(
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
				"parent",
				"blockedSince",
			]);
		},
		RENDER_HEAVY,
	);

	// @us-01 @slice-01b @boundary @contract-shape:pure-function
	it(
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
	it(
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
	it.each(everyChartContext)(
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
	it(
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
	it.each(["aging", "stale", "inProgress"] as const)(
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
	it(
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
	it(
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
	it(
		"Time in State keeps its own badge when the list is sorted by Age",
		() => {
			openTheDialog({ context: "stale", items: inFlight, timeInStateColumn });

			expect(sortedDescendingBy()).toBe("workItemAge");
			expect(cellText("timeInState", "WIP-2")).toMatch(/^\d+d in Review$/);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it.each(["aging", "inProgress"] as const)(
		"%s neither shows nor offers Time in State when the caller says nothing about how to measure it",
		async (context) => {
			const user = userEvent.setup();
			openTheDialog({ context, items: inFlight, ageBandColumn, sleRiskColumn });

			expect(columnsShown()).not.toContain("timeInState");

			await openManageColumns(user);

			expect(catalogueOffered().map(({ field }) => field)).not.toContain(
				"timeInState",
			);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it(
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

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it.each([
		"arrivals",
		"workDistribution",
		"startedAndClosed",
		"featureChildren",
	] as const)(
		"%s leaves a finished item's Work Item Age and an unfinished item's Cycle Time empty, never 0, and lists the empty ages last",
		(context) => {
			openTheDialog({
				context,
				items: [threePointerThatTookLong, oldestButJustMoved, justStarted],
			});

			expect(cellText("workItemAge", "ST-1")).toBe("");
			expect(cellText("cycleTime", "ST-1")).toBe("20");
			expect(cellText("workItemAge", "WIP-1")).toBe("30");
			expect(cellText("cycleTime", "WIP-1")).toBe("");
			expect(rowOrder()).toEqual(["WIP-1", "WIP-3", "ST-1"]);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it(
		"Age on a past day's point still shows for an item finished since, because it was unfinished on that day",
		() => {
			openTheDialog({
				context: "startedAndClosed",
				items: [threePointerThatTookLong],
				ageOn: day("2026-09-10"),
			});

			expect(cellText("workItemAge", "ST-1")).toBe("10");
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it(
		"an item not started yet leaves its Work Item Age empty, never 0, and lists it below the started ones",
		() => {
			const notStarted = aWorkItem({
				referenceId: "TODO-1",
				state: "New",
				stateCategory: "ToDo",
				startedDate: new Date(0),
				closedDate: new Date(0),
				cycleTime: 0,
				workItemAge: 0,
			});
			openTheDialog({
				context: "featureChildren",
				items: [notStarted, oldestButJustMoved, justStarted],
			});

			expect(cellText("workItemAge", "TODO-1")).toBe("");
			expect(rowOrder()).toEqual(["WIP-1", "WIP-3", "TODO-1"]);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it(
		"Age on a day given as its first moment, as a chosen range end is, is the age on that calendar day",
		() => {
			openTheDialog({
				context: "inProgress",
				items: [oldestButJustMoved, threePointerThatTookLong],
				ageOn: new Date(2026, 8, 20),
			});

			expect(cellText("workItemAge", "WIP-1")).toBe("10");
			expect(cellText("workItemAge", "ST-1")).toBe("");
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it(
		"Age on a past day stays empty for an item that had finished by that day",
		() => {
			openTheDialog({
				context: "inProgress",
				items: [threePointerThatTookLong],
				ageOn: day("2026-09-25"),
			});

			expect(cellText("workItemAge", "ST-1")).toBe("");
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @boundary @contract-shape:pure-function
	it(
		"Age on a past day's point stays empty for an item that had not started by that day",
		() => {
			openTheDialog({
				context: "inProgress",
				items: [justStarted],
				ageOn: day("2026-09-20"),
			});

			expect(cellText("workItemAge", "WIP-3")).toBe("");
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @driving_port @contract-shape:pure-function
	it(
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
	it(
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
	it(
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
	it(
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
				"estimate",
				"parent",
				"blockedSince",
			]);
		},
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @driving_port @contract-shape:bounded-change
	it(
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
	it.each(everyChartContext)(
		"$context neither shows nor offers an Estimate where estimation is not set up",
		estimateIsNeitherShownNorOffered,
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @contract-shape:pure-function
	it.each(everyChartContext)(
		"$context lists its defaults, then every other column the rows can fill in the one fixed order",
		manageColumnsListsTheOneFixedOrder,
		RENDER_HEAVY,
	);

	// @us-02 @slice-02 @contract-shape:bounded-change
	it.each(everyChartContext)(
		"$context comes back with the reader's own layout, a default hidden and another column on, the next time it opens",
		theReadersLayoutComesBackOnReopen,
		RENDER_HEAVY,
	);
});

describe("US-03 — a Feature's child items show their progress at a glance", () => {
	// @us-03 @slice-03 @driving_port @contract-shape:pure-function
	it(
		"a Feature's child items open with Started, Closed, Work Item Age, Cycle Time and Estimate, oldest first",
		() => {
			openTheDialog({
				context: "featureChildren",
				items: [threePointerThatTookLong, oldestButJustMoved, justStarted],
			});

			expect(contextColumnsShown()).toEqual([
				"startedDate",
				"closedDate",
				"workItemAge",
				"cycleTime",
				"estimate",
			]);
			expect(sortedDescendingBy()).toBe("workItemAge");
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @error @contract-shape:pure-function
	it(
		"without estimation on the child items' Team, the child items open without an Estimate",
		() => {
			openTheDialog({
				context: "featureChildren",
				items: finishedStoriesWithoutEstimation,
			});

			expect(contextColumnsShown()).toEqual([
				"startedDate",
				"closedDate",
				"workItemAge",
				"cycleTime",
			]);
		},
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @error @contract-shape:pure-function
	it(
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
	it.each(theFeatureListContexts)(
		"$context neither shows nor offers an Estimate where estimation is not set up",
		estimateIsNeitherShownNorOffered,
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @contract-shape:pure-function
	it.each(theFeatureListContexts)(
		"$context lists its defaults, then every other column the rows can fill in the one fixed order",
		manageColumnsListsTheOneFixedOrder,
		RENDER_HEAVY,
	);

	// @us-03 @slice-03 @contract-shape:bounded-change
	it.each(theFeatureListContexts)(
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
			openTheDialog({ items: portfolioFeatures, context: "featureSize" });

			expect(columnHeader("owningTeam")).toBeInTheDocument();
			expect(cellText("owningTeam", "FTR-1")).toBe("Team Voyager");
		},
		RENDER_HEAVY,
	);
});

const headingsOffered = (): string[] =>
	screen
		.getAllByRole("checkbox")
		.filter((box) => {
			const field = box.getAttribute("name") ?? "";
			return (
				field !== "" &&
				field !== "Show/Hide All" &&
				!FIXED_COLUMNS.includes(field)
			);
		})
		.map((box) => box.closest("label")?.textContent?.trim() ?? "");

const inFlightSince = (
	referenceId: string,
	currentStateEnteredAt: Date,
): IWorkItem =>
	aWorkItem({
		referenceId,
		state: "In Progress",
		stateCategory: "Doing",
		startedDate: day("2026-10-01"),
		closedDate: new Date(0),
		cycleTime: 0,
		workItemAge: 10,
		currentStateEnteredAt,
	});

describe("what each catalogue column says, and how it sorts", () => {
	it(
		"heads every column the catalogue offers with its name",
		async () => {
			const user = userEvent.setup();
			const storyList = openTheDialog({
				context: "closedItems",
				items: finishedStories,
			});
			await openManageColumns(user);

			expect(headingsOffered()).toEqual([
				"Closed",
				"Cycle Time",
				"Started",
				"Work Item Age",
				"Estimate",
				"Parent",
				"Blocked since",
			]);
			storyList.unmount();

			const featureList = openTheDialog({
				context: "featureSize",
				ownerKind: "portfolio",
				items: portfolioFeatures,
			});

			expect(headerText("size")).toBe("Size");
			featureList.unmount();

			openTheDialog({
				context: "cumulativeStateTime",
				items: [threePointerThatTookLong],
				daysContributedColumn: { daysFor: () => 2 },
			});

			expect(headerText("daysContributed")).toBe("Days Contributed");
		},
		RENDER_HEAVY,
	);

	it.each([
		{
			context: "inProgress" as const,
			items: inFlight,
			extra: { sleRiskColumn, timeInStateColumn },
			turnOn: ["cycleTime"],
			fields: ["startedDate", "timeInState", "sleRisk", "cycleTime"],
		},
		{
			context: "workDistribution" as const,
			items: [threePointerThatTookLong, eightPointerThatFlew],
			extra: {},
			turnOn: [],
			fields: ["parent"],
		},
		{
			context: "deliveryTimeline" as const,
			items: [mediumFeature],
			extra: { warningsColumn },
			turnOn: [],
			fields: ["warnings"],
		},
	])(
		"$context: every column named here sorts on a header click",
		async ({ context, items, extra, turnOn, fields }) => {
			const user = userEvent.setup();
			openTheDialog({ context, items, ...extra });
			if (turnOn.length > 0) {
				await turnColumns(
					user,
					turnOn.map((field) => [field, true] as const),
				);
			}

			for (const field of fields) {
				const before = columnHeader(field).getAttribute("aria-sort");
				await user.click(columnHeader(field));

				expect(columnHeader(field).getAttribute("aria-sort")).not.toBe(before);
			}
		},
		RENDER_HEAVY,
	);

	it(
		"orders by parent on a Parent header click",
		async () => {
			const user = userEvent.setup();
			const underTheSecondFeature = aWorkItem({
				referenceId: "ST-A",
				state: "In Progress",
				stateCategory: "Doing",
				closedDate: new Date(0),
				workItemAge: 30,
				parentWorkItemReference: "FTR-2",
			});
			const underTheFirstFeature = aWorkItem({
				referenceId: "ST-B",
				state: "In Progress",
				stateCategory: "Doing",
				closedDate: new Date(0),
				workItemAge: 3,
				parentWorkItemReference: "FTR-1",
			});
			openTheDialog({
				context: "workDistribution",
				items: [underTheSecondFeature, underTheFirstFeature],
			});

			await user.click(columnHeader("parent"));

			expect(rowOrder()).toEqual(["ST-B", "ST-A"]);
		},
		RENDER_HEAVY,
	);

	it(
		"orders by time in state on a Time in State header click",
		async () => {
			const user = userEvent.setup();
			openTheDialog({ context: "aging", items: inFlight, timeInStateColumn });

			await user.click(columnHeader("timeInState"));

			expect(rowOrder()).toEqual(["WIP-1", "WIP-3", "WIP-2"]);
		},
		RENDER_HEAVY,
	);

	it(
		"of two items equally old, lists the one longer in its state first",
		() => {
			const movedRecently = inFlightSince("WIP-A", day("2026-10-08"));
			const stuck = inFlightSince("WIP-B", day("2026-10-02"));
			openTheDialog({
				context: "aging",
				items: [movedRecently, stuck],
				timeInStateColumn,
			});

			expect(rowOrder()).toEqual(["WIP-B", "WIP-A"]);
		},
		RENDER_HEAVY,
	);

	it(
		"orders by estimate on an Estimate header click, a row an older server sent none for included",
		async () => {
			const user = userEvent.setup();
			const sentNoEstimate = aWorkItem({ referenceId: "ST-4", cycleTime: 7 });
			openTheDialog({
				context: "estimation",
				items: [eightPointerThatFlew, sentNoEstimate, threePointerThatTookLong],
			});

			await user.click(columnHeader("estimate"));

			expect(rowOrder()).toEqual(["ST-4", "ST-1", "ST-2"]);
		},
		RENDER_HEAVY,
	);

	it(
		"Blocked since shows the day an item was blocked, and nothing for one that is not",
		() => {
			openTheDialog({ context: "blocked", items: inFlight });

			expect(cellText("blockedSince", "WIP-1")).toBe("2026-10-01");
			expect(cellText("blockedSince", "WIP-2")).toBe("");
		},
		RENDER_HEAVY,
	);

	it(
		"judges only the column the rows are sorted by",
		() => {
			const blockedFeature = aFeature({
				referenceId: "FTR-9",
				state: "In Progress",
				stateCategory: "Doing",
				closedDate: new Date(0),
				workItemAge: 12,
				isBlocked: true,
				size: 20,
			});
			openTheDialog({
				context: "featureSize",
				ownerKind: "portfolio",
				items: [blockedFeature],
				sle: 10,
			});

			expect(
				within(cellIn("size", "FTR-9")).getByTestId("additionalColumnContent"),
			).toBeInTheDocument();
			expect(
				within(cellIn("workItemAge", "FTR-9")).queryByTestId(
					"additionalColumnContent",
				),
			).toBeNull();
			expect(
				within(cellIn("workItemAge", "FTR-9")).queryByTestId("BlockIcon"),
			).toBeNull();
		},
		RENDER_HEAVY,
	);

	it(
		"leaves an empty cell of the sort column unjudged",
		() => {
			openTheDialog({
				context: "inProgress",
				items: [threePointerThatTookLong],
				ageOn: day("2026-09-25"),
				sle: 10,
			});

			expect(
				within(cellIn("workItemAge", "ST-1")).queryByTestId(
					"additionalColumnContent",
				),
			).toBeNull();
		},
		RENDER_HEAVY,
	);

	it(
		"Age on a day counts an item started later that day, and not one finished that day or never started",
		() => {
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
			openTheDialog({
				context: "inProgress",
				items: [startedThatAfternoon, finishedThatAfternoon, neverStarted],
				ageOn: day("2026-10-08"),
			});

			expect(cellText("workItemAge", "WIP-S")).toBe("1");
			expect(cellText("workItemAge", "ST-F")).toBe("");
			expect(cellText("workItemAge", "ST-N")).toBe("");
		},
		RENDER_HEAVY,
	);

	it(
		"Cycle Time reads the named cycle time it is scoped to, and each named column reads its own",
		async () => {
			const user = userEvent.setup();
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
			const references = ["ST-A", "ST-B", "ST-C"];
			openTheDialog({
				context: "closedItems",
				items: [throughBoth, throughDevOnly, throughNeither],
				namedCycleTimeDefinitions,
				cycleTimeScope: 9,
			});

			expect(headerText("cycleTime")).toBe("Review Cycle");
			expect(references.map((ref) => cellText("cycleTime", ref))).toEqual([
				"5",
				"",
				"",
			]);

			await turnColumn(user, "namedCycleTime:7", true);

			expect(
				references.map((ref) => cellText("namedCycleTime:7", ref)),
			).toEqual(["2", "4", ""]);
		},
		RENDER_HEAVY,
	);

	it(
		"washes a judged cell in a tenth of its judgement's colour",
		() => {
			openTheDialog({
				context: "inProgress",
				items: inFlight,
				sleRiskColumn: { ...sleRiskColumn, colorForRisk: () => riskyColor },
			});

			expect(
				within(cellIn("sleRisk", "WIP-1")).getByTestId("sleRiskColumnContent"),
			).toHaveStyle({ backgroundColor: hexToRgba(riskyColor, 0.1) });
		},
		RENDER_HEAVY,
	);

	it(
		"orders SLE Risk lowest first, then highest first, with a row it cannot place last both ways",
		async () => {
			const user = userEvent.setup();
			openTheDialog({
				context: "inProgress",
				items: [longestInItsState, oldestButJustMoved, justStarted],
				sleRiskColumn: {
					...sleRiskColumn,
					labelFor: (item) =>
						item.referenceId === "WIP-2"
							? "No history"
							: `${item.workItemAge * 2}%`,
				},
			});

			await user.click(columnHeader("sleRisk"));

			expect(rowOrder()).toEqual(["WIP-3", "WIP-1", "WIP-2"]);

			await user.click(columnHeader("sleRisk"));

			expect(rowOrder()).toEqual(["WIP-1", "WIP-3", "WIP-2"]);
		},
		RENDER_HEAVY,
	);

	it(
		"keeps rows SLE Risk cannot place in the order they came, below the rest",
		async () => {
			const user = userEvent.setup();
			openTheDialog({
				context: "inProgress",
				items: [longestInItsState, justStarted, oldestButJustMoved],
				sleRiskColumn: {
					...sleRiskColumn,
					labelFor: (item) =>
						item.referenceId === "WIP-1"
							? `${item.workItemAge * 2}%`
							: "No history",
				},
			});

			await user.click(columnHeader("sleRisk"));

			expect(rowOrder()).toEqual(["WIP-1", "WIP-2", "WIP-3"]);
		},
		RENDER_HEAVY,
	);

	it(
		"shows a row's SLE Risk disclosure when the reader hovers over it",
		async () => {
			const user = userEvent.setup();
			openTheDialog({
				context: "inProgress",
				items: inFlight,
				sleRiskColumn: {
					...sleRiskColumn,
					disclosureFor: (item) =>
						item.referenceId === "WIP-1"
							? "Judged on 3 finished items."
							: undefined,
				},
			});

			await user.hover(
				within(cellIn("sleRisk", "WIP-1")).getByTestId("sleRiskColumnContent"),
			);

			expect(await screen.findByRole("tooltip")).toHaveTextContent(
				"Judged on 3 finished items.",
			);
		},
		RENDER_HEAVY,
	);

	it(
		"lists every warning a row has, one sentence after another",
		() => {
			openTheDialog({
				context: "deliveryTimeline",
				items: [mediumFeature],
				warningsColumn: {
					...warningsColumn,
					warningsFor: () => [
						"Starts after its Delivery date.",
						"Has no forecast.",
					],
				},
			});

			expect(cellText("warnings", "FTR-1")).toBe(
				"Starts after its Delivery date. Has no forecast.",
			);
		},
		RENDER_HEAVY,
	);

	it(
		"Time in State counts a blocked item as stale from the day it was blocked",
		() => {
			openTheDialog({
				context: "aging",
				items: inFlight,
				timeInStateColumn: {
					now: NOW,
					stalenessThresholdDays: 100,
					blockedStalenessThresholdDays: 5,
				},
			});

			expect(
				within(cellIn("timeInState", "WIP-1")).getByTestId(
					"time-in-state-stale",
				),
			).toHaveAttribute("aria-label", expect.stringContaining("Blocked 10d"));
		},
		RENDER_HEAVY,
	);

	it(
		"Feature rows that name no owning team leave Owned by out",
		() => {
			openTheDialog({
				context: "featureSize",
				ownerKind: "portfolio",
				items: portfolioFeatures.map((feature) => ({
					...feature,
					owningTeam: "  ",
				})),
			});

			expect(columnsShown()).not.toContain("owningTeam");
		},
		RENDER_HEAVY,
	);
});

// On a Team, every size is the Team's own part of the Feature. That number comes from
// the Team's answer, so the dialog only has to show the size it is handed, unchanged.
describe("a Team's Features worked on", () => {
	// @story-6251 @us-01 @contract-shape:unbounded-preservation
	it(
		"show in the Size column the size the Team's answer carries",
		async () => {
			const user = userEvent.setup();
			openTheDialog({
				context: "inProgress",
				ownerKind: "team",
				items: [aFeature({ referenceId: "OE-002", size: 4 })],
			});

			await turnColumn(user, "size", true);

			expect(cellText("size", "OE-002")).toBe("4");
		},
		RENDER_HEAVY,
	);
});
