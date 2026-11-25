import { describe, expect, it } from "vitest";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem, StateCategory } from "../../../models/WorkItem";
import { buildViewData } from "./widgetViewData";

const OVER_TIME_CHARTS = [
	"wipOverTime",
	"totalWorkItemAgeOverTime",
	"wipPbc",
	"totalWorkItemAgePbc",
];

const workItem = (id: number): IWorkItem => ({
	id,
	name: `Item ${id}`,
	referenceId: `ZEN-${id}`,
	url: null,
	state: "In Progress",
	stateCategory: "Doing" as StateCategory,
	type: "User Story",
	startedDate: new Date("2026-09-01"),
	closedDate: new Date("2026-09-01"),
	cycleTime: 0,
	workItemAge: 6,
	parentWorkItemReference: "",
	isBlocked: false,
});

// Item 1 is in progress on every day of the range; items 2 and 3 on one day each.
const firstRange = () =>
	new RunChartData(
		{
			0: [workItem(1), workItem(2)],
			1: [workItem(1)],
			2: [workItem(1)],
			3: [workItem(1), workItem(3)],
		},
		4,
		0,
	);

// A later range: item 7 spans three days and item 8 is there on one.
const secondRange = () =>
	new RunChartData(
		{
			0: [workItem(7)],
			1: [workItem(7), workItem(8)],
			2: [workItem(7)],
		},
		3,
		0,
	);

// Items 90 and 91 were finished or are in progress today, but on no day of the range.
const NEVER_IN_PROGRESS_DURING_THE_RANGE = [workItem(90), workItem(91)];

const viewDataFor = (ownerKind: "team" | "portfolio", range: RunChartData) =>
	buildViewData({
		title: "Work Items",
		endDate: new Date("2026-10-01T12:00:00"),
		ownerKind,
		inProgressItems: [NEVER_IN_PROGRESS_DURING_THE_RANGE[0]],
		blockedItems: [],
		staleItems: [],
		featuresInProgress: undefined,
		cycleTimeData: NEVER_IN_PROGRESS_DURING_THE_RANGE,
		throughputData: null,
		wipOverTimeData: range,
		allFeaturesForSizeChart: [],
		serviceLevelExpectation: null,
		sleRangeInDays: undefined,
		percentilesScopeDefinitionId: null,
		scopedPercentileItems: null,
		namedCycleTimeDefinitions: [],
		estimationVsCycleTimeData: null,
		arrivalsData: null,
		workItemLookup: new Map(
			NEVER_IN_PROGRESS_DURING_THE_RANGE.map((item) => [item.id, item]),
		),
		perStatePercentileValues: [],
		sleRiskValues: [],
		doingStates: ["In Progress"],
		terms: {
			workItem: "Work Item",
			workItems: "Work Items",
			features: "Features",
			cycleTime: "Cycle Time",
			workItemAge: "Work Item Age",
			blocked: "Blocked",
			sle: "SLE",
		},
	});

describe("data list of the over-time charts", () => {
	const cases = OVER_TIME_CHARTS.flatMap((chart) =>
		(["team", "portfolio"] as const).flatMap((owner) => [
			{ chart, owner, range: firstRange, expectedIds: [1, 2, 3] },
			{ chart, owner, range: secondRange, expectedIds: [7, 8] },
		]),
	);

	it.each(cases)(
		"lists each Work Item in progress during the range once, and nothing else: $chart on a $owner, ids $expectedIds",
		({ chart, owner, range, expectedIds }) => {
			const ids = (viewDataFor(owner, range())[chart]?.items ?? []).map(
				(item) => item.id,
			);

			expect(
				[...ids].sort((a, b) => a - b),
				"The list must hold each Work Item in progress during the range exactly once: no repeat per day, no Work Item from another day missing, none from outside the range. Build the list from the range's Work Items without repeats.",
			).toEqual(expectedIds);
		},
	);
});
