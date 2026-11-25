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
const runChartWithOneItemOnEveryDay = () =>
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

const viewDataFor = (ownerKind: "team" | "portfolio") =>
	buildViewData({
		title: "Work Items",
		endDate: new Date("2026-10-01T12:00:00"),
		ownerKind,
		inProgressItems: [],
		blockedItems: [],
		staleItems: [],
		featuresInProgress: undefined,
		cycleTimeData: [],
		throughputData: null,
		wipOverTimeData: runChartWithOneItemOnEveryDay(),
		allFeaturesForSizeChart: [],
		serviceLevelExpectation: null,
		sleRangeInDays: undefined,
		percentilesScopeDefinitionId: null,
		scopedPercentileItems: null,
		namedCycleTimeDefinitions: [],
		estimationVsCycleTimeData: null,
		arrivalsData: null,
		workItemLookup: new Map(),
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
	it.each(
		OVER_TIME_CHARTS.flatMap((chart) =>
			(["team", "portfolio"] as const).map((owner) => [chart, owner] as const),
		),
	)(
		"lists each Work Item in progress during the range once: %s on a %s",
		(chart, owner) => {
			const ids = (viewDataFor(owner)[chart]?.items ?? []).map(
				(item) => item.id,
			);

			expect(
				[...ids].sort((a, b) => a - b),
				"A Work Item in progress on several days must be listed once, and a Work Item in progress on one day must still be listed. Make the list hold each Work Item of the range exactly once.",
			).toEqual([1, 2, 3]);
		},
	);
});
