import { screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import type { IWorkItem } from "../../../models/WorkItem";
import { inSequence } from "../../../tests/inSequence";
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

type ReopenCase = {
	context: WorkItemsDialogContextId;
	items: IWorkItem[];
	columns: string[];
	/** Everything Manage columns lists for these rows: the defaults, then the one fixed order. */
	offered: string[];
};

const readersLayoutCases: ReopenCase[] = [
	{
		context: "estimation",
		items: finishedStories,
		columns: ["estimate", "cycleTime", "closedDate"],
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
		context: "featureSize",
		items: portfolioFeatures,
		columns: ["owningTeam", "size", "workItemAge", "cycleTime", "closedDate"],
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
];

async function theReadersLayoutComesBackOnReopen({
	context,
	items,
	columns,
	offered,
}: ReopenCase) {
	const user = userEvent.setup();
	const hidden = columns[columns.length - 1];
	const turnedOn = offered[columns.length];
	const first = openTheDialog({ context, items });
	await turnColumns(user, [
		[hidden, false],
		[turnedOn, true],
	]);
	first.unmount();

	openTheDialog({ context, items });

	expect(contextColumnsShown()).toEqual([...columns.slice(0, -1), turnedOn]);
}

describe("US-01 — the Estimation dialog shows each item's estimate", () => {
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

	it(
		"Time in State keeps its own badge when the list is sorted by Age",
		() => {
			openTheDialog({ context: "stale", items: inFlight, timeInStateColumn });

			expect(sortedDescendingBy()).toBe("workItemAge");
			expect(cellText("timeInState", "WIP-2")).toMatch(/^\d+d in Review$/);
		},
		RENDER_HEAVY,
	);

	it(
		"aging neither shows nor offers Time in State when the caller says nothing about how to measure it",
		async () => {
			const user = userEvent.setup();
			openTheDialog({
				context: "aging",
				items: inFlight,
				ageBandColumn,
				sleRiskColumn,
			});

			expect(columnsShown()).not.toContain("timeInState");

			await openManageColumns(user);

			expect(catalogueOffered().map(({ field }) => field)).not.toContain(
				"timeInState",
			);
		},
		RENDER_HEAVY,
	);

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

	it(
		"arrivals leaves a finished item's Work Item Age and an unfinished item's Cycle Time empty, never 0, and lists the empty ages last",
		() => {
			openTheDialog({
				context: "arrivals",
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
	it.each(readersLayoutCases)(
		"$context comes back with the reader's own layout, a default hidden and another column on, the next time it opens",
		theReadersLayoutComesBackOnReopen,
		RENDER_HEAVY,
	);
});

describe("US-03 — a Feature's child items show their progress at a glance", () => {
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
});

describe("guards that hold today and must keep holding", () => {
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

			await inSequence(fields, async (field) => {
				const before = columnHeader(field).getAttribute("aria-sort");
				await user.click(columnHeader(field));

				expect(columnHeader(field).getAttribute("aria-sort")).not.toBe(before);
			});
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
		"Time in State gives the blocked time Lighthouse names as the reason a blocked item is stale",
		() => {
			openTheDialog({
				context: "aging",
				items: inFlight.map((item) =>
					item === oldestButJustMoved
						? {
								...item,
								isStale: true,
								staleReasons: [{ kind: "BlockedDuration" as const, days: 10 }],
							}
						: item,
				),
				timeInStateColumn: { now: NOW },
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
