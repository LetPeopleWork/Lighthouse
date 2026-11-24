import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { useNamedPercentilesScope } from "./useNamedPercentilesScope";
import { buildViewData } from "./widgetViewData";

const readyForRelease: INamedCycleTimeDefinition = {
	id: 7,
	name: "Analysis to Ready for Release",
	endState: "Ready for Release",
};

const rangeStart = new Date(2026, 10, 1);
const rangeEnd = new Date(2026, 10, 30);

const workItem = (
	id: number,
	name: string,
	closedOn: Date | null,
	namedDays: number | null,
): IWorkItem => ({
	id,
	referenceId: name,
	name,
	url: null,
	state: closedOn ? "Done" : "Ready for Release",
	stateCategory: closedOn ? "Done" : "Doing",
	type: "Story",
	startedDate: new Date(2026, 9, 20),
	// The server sends no closed date for an item that is not closed.
	closedDate: closedOn as Date,
	cycleTime: closedOn ? 15 : 0,
	namedCycleTimes:
		namedDays === null
			? []
			: [
					{
						definitionId: readyForRelease.id,
						days: namedDays,
						endDate: new Date(2026, 10, 5),
					},
				],
	workItemAge: 15,
	parentWorkItemReference: "",
	isBlocked: false,
});

// A reached Ready for Release in the range and was closed later in it.
const closedA = workItem(1, "A", new Date(2026, 10, 20), 6);
// B reached Ready for Release in the range and is still there.
const waitingB = workItem(2, "B", null, 4);
// C was closed in the range but reached Ready for Release before it, so the server leaves it out
// of the measure's own Work Items while it still sends C's value on the default closed list.
const closedEarlierC = workItem(3, "C", new Date(2026, 10, 3), 8);

const theMeasuresOwnItems = [closedA, waitingB];
const theDefaultClosedList = [closedA, closedEarlierC];

const serviceAnsweringTheMeasure = () => {
	const getCycleTimeData = vi.fn(
		(_id: number, _start: Date, _end: Date, definitionId?: number) =>
			Promise.resolve(
				definitionId === readyForRelease.id
					? theMeasuresOwnItems
					: theDefaultClosedList,
			),
	);
	const service = {
		getCycleTimeData,
		getCycleTimePercentiles: vi.fn().mockResolvedValue([]),
		getCycleTimePercentilesInfo: vi.fn().mockResolvedValue(null),
	} as unknown as IMetricsService<IWorkItem>;
	return { service, getCycleTimeData };
};

const percentilesListWith = (scope: {
	definitionId: number | null;
	itemsOfTheMeasure: IWorkItem[] | null;
}) =>
	buildViewData({
		title: "Work Items",
		endDate: rangeEnd,
		ownerKind: "team",
		inProgressItems: [],
		blockedItems: [],
		staleItems: [],
		featuresInProgress: undefined,
		cycleTimeData: theDefaultClosedList,
		throughputData: null,
		wipOverTimeData: null,
		allFeaturesForSizeChart: [],
		serviceLevelExpectation: null,
		sleRangeInDays: undefined,
		percentilesScopeDefinitionId: scope.definitionId,
		scopedPercentileItems: scope.itemsOfTheMeasure,
		namedCycleTimeDefinitions: [readyForRelease],
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
	} as Parameters<typeof buildViewData>[0]).percentiles;

const namesIn = (list: { items: IWorkItem[] } | undefined) =>
	(list?.items ?? []).map((item) => item.name);

describe("View Data of the Cycle Time percentiles with a named Cycle Time", () => {
	it("fetches the named measure's own Work Items for the range when the measure is selected, and drops them on Default", async () => {
		const { service, getCycleTimeData } = serviceAnsweringTheMeasure();
		const { result } = renderHook(() =>
			useNamedPercentilesScope({
				metricsService: service,
				entityId: 5,
				startDate: rangeStart,
				endDate: rangeEnd,
			}),
		);

		act(() => {
			result.current.handlePercentilesScopeChange(readyForRelease.id);
		});

		await waitFor(() => {
			expect(
				result.current.scopedPercentileItems?.map((item) => item.name),
				"With a named measure selected the percentiles must list the Work Items the server counts for that measure (A, and B which is not closed), fetched for the same team, range and measure as the percentile numbers. Fetch the Cycle Time data with the measure's id and hand it back as the scoped Work Items.",
			).toEqual(["A", "B"]);
		});
		expect(getCycleTimeData).toHaveBeenCalledWith(
			5,
			rangeStart,
			rangeEnd,
			readyForRelease.id,
		);

		act(() => {
			result.current.handlePercentilesScopeChange(null);
		});

		expect(
			result.current.scopedPercentileItems,
			"Back on Default the named measure's Work Items must no longer be offered as the list.",
		).toBeNull();
	});

	it("lists the named measure's own Work Items, also one that is not closed, and none that only the default closed list holds", () => {
		const list = percentilesListWith({
			definitionId: readyForRelease.id,
			itemsOfTheMeasure: theMeasuresOwnItems,
		});

		expect(
			namesIn(list),
			"The View Data list must hold the same Work Items as the named percentiles: B, which reached Ready for Release but is not closed, must be listed, and C, which only the default closed list holds, must not. Build the list from the measure's own Work Items, not from the default closed list.",
		).toEqual(["A", "B"]);
		expect(list).toMatchObject({
			context: "closedItems",
			cycleTimeScope: readyForRelease.id,
			namedCycleTimeDefinitions: [readyForRelease],
		});
		expect(
			namesIn(
				percentilesListWith({ definitionId: null, itemsOfTheMeasure: null }),
			),
			"On Default the list stays the closed Work Items of the range.",
		).toEqual(["A", "C"]);
	});
});
