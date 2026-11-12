import { describe, expect, it } from "vitest";
import type { WorkItemsDialogContextId } from "../../../components/Common/WorkItemsDialog/workItemsDialogContexts";
import type { IFeature } from "../../../models/Feature";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem, StateCategory } from "../../../models/WorkItem";
import { buildViewData } from "./widgetViewData";

// The risk column is attached per payload, and a payload added without one is invisible: nothing
// fails, the list simply renders without the column. Two of the four lists of in-flight work went
// a release that way. So every payload the builder returns has to be classified here, and a new
// one that is in neither list fails until somebody decides which it is.
describe("which lists carry the risk column", () => {
	const LISTS_WHAT_IS_IN_FLIGHT_TODAY = [
		"wipOverview",
		"totalWorkItemAge",
		"workItemAgePercentiles",
		"aging",
		"sleRisk",
	];

	const DOES_NOT_LIST_WHAT_IS_IN_FLIGHT_TODAY = [
		"blockedOverview",
		"staleOverview",
		"featuresWorkedOnOverview",
		"percentiles",
		"throughput",
		"cycleScatter",
		"workDistribution",
		"wipOverTime",
		"totalWorkItemAgeOverTime",
		"stacked",
		"estimationVsCycleTime",
		"featureSize",
		"featureSizePercentiles",
		"throughputPbc",
		"wipPbc",
		"totalWorkItemAgePbc",
		"cycleTimePbc",
		"featureSizePbc",
		"arrivals",
		"arrivalsPbc",
		"totalThroughput",
		"totalArrivals",
	];

	const inFlightItem = (id: number, referenceId: string): IWorkItem => ({
		id,
		name: `Item ${id}`,
		referenceId,
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

	const viewDataWith = (
		sleRiskValues: {
			referenceId: string;
			risk: number;
			finishedItemsStillOpenAtThisAge: number;
			finishedItemsThatWentOnToMiss: number | null;
		}[],
		ownerKind: "team" | "portfolio" = "team",
		overrides: Partial<Parameters<typeof buildViewData>[0]> = {},
	) =>
		buildViewData({
			title: "Work Items",
			endDate: new Date("2026-10-01T12:00:00"),
			ownerKind,
			inProgressItems: [inFlightItem(1, "ZEN-1"), inFlightItem(2, "ZEN-2")],
			blockedItems: [],
			staleItems: [],
			featuresInProgress: undefined,
			cycleTimeData: [],
			throughputData: null,
			wipOverTimeData: null,
			allFeaturesForSizeChart: [],
			// A published target, because the risk column needs one to name. A team without one has
			// no promise for anything to be at risk of breaking, and gets no column at all.
			//
			// The range is passed separately from the percentile on purpose: a team can set a range
			// without a probability, and the endpoint answers on the range alone.
			serviceLevelExpectation: { percentile: 85, value: 10 },
			sleRangeInDays: 10,
			percentilesScopeDefinitionId: null,
			namedCycleTimeDefinitions: [],
			estimationVsCycleTimeData: null,
			arrivalsData: null,
			workItemLookup: new Map(),
			perStatePercentileValues: [],
			sleRiskValues,
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
			...overrides,
		});

	const builtWithRisk = () =>
		viewDataWith([
			{
				referenceId: "ZEN-1",
				risk: 86,
				finishedItemsStillOpenAtThisAge: 7,
				finishedItemsThatWentOnToMiss: 6,
			},
			{
				referenceId: "ZEN-2",
				risk: 12,
				finishedItemsStillOpenAtThisAge: 7,
				finishedItemsThatWentOnToMiss: 1,
			},
		]);

	it.each(LISTS_WHAT_IS_IN_FLIGHT_TODAY)(
		"every list of what is in flight today carries the risk: %s",
		(key) => {
			expect(builtWithRisk()[key]?.sleRiskColumn).toBeDefined();
		},
	);

	it.each(DOES_NOT_LIST_WHAT_IS_IN_FLIGHT_TODAY)(
		"no other list carries it: %s",
		(key) => {
			expect(builtWithRisk()[key]?.sleRiskColumn).toBeUndefined();
		},
	);

	it("every list is in one of the two lists above", () => {
		const unaccountedFor = Object.keys(builtWithRisk()).filter(
			(key) =>
				!LISTS_WHAT_IS_IN_FLIGHT_TODAY.includes(key) &&
				!DOES_NOT_LIST_WHAT_IS_IN_FLIGHT_TODAY.includes(key),
		);

		expect(
			unaccountedFor,
			"Put each of these in exactly one of the two lists at the top of this block: the first is for lists of what the team has in flight today, which carry the risk column; the second is for every other list, which deliberately does not. Whichever you choose, write the reason in the call-site table in the feature's DESIGN record.",
		).toEqual([]);
	});

	it.each(["team", "portfolio"] as const)(
		"the Estimation list opens the estimation context for a %s",
		(ownerKind) => {
			const estimation = viewDataWith([], ownerKind).estimationVsCycleTime;

			expect(estimation?.context).toBe("estimation");
			expect(estimation?.ownerKind).toBe(ownerKind);
		},
	);

	// Lighthouse judged staleness on the range's last day, so the Time in State label beside its
	// reason must count to that same day, not to today.
	it.each(["wipOverview", "staleOverview"])(
		"the %s list counts Time in State to the range's last day",
		(key) => {
			const rangeEnd = new Date(2026, 4, 25);

			const list = viewDataWith([], "team", { endDate: rangeEnd })[key];

			expect(list?.timeInStateColumn?.now).toBe(rangeEnd);
		},
	);

	const CONTEXT_OF_EVERY_LIST: Record<string, WorkItemsDialogContextId> = {
		wipOverview: "inProgress",
		blockedOverview: "blocked",
		staleOverview: "stale",
		featuresWorkedOnOverview: "inProgress",
		percentiles: "closedItems",
		totalWorkItemAge: "inProgress",
		workItemAgePercentiles: "inProgress",
		sleRisk: "inProgress",
		throughput: "closedItems",
		cycleScatter: "closedItems",
		workDistribution: "workDistribution",
		aging: "aging",
		wipOverTime: "inProgress",
		totalWorkItemAgeOverTime: "inProgress",
		stacked: "startedAndClosed",
		estimationVsCycleTime: "estimation",
		featureSize: "featureSize",
		featureSizePercentiles: "featureSize",
		throughputPbc: "closedItems",
		wipPbc: "inProgress",
		totalWorkItemAgePbc: "inProgress",
		cycleTimePbc: "closedItems",
		featureSizePbc: "featureSize",
		arrivals: "arrivals",
		arrivalsPbc: "arrivals",
		totalThroughput: "closedItems",
		totalArrivals: "arrivals",
	};

	it.each(["team", "portfolio"] as const)(
		"every View Data payload names a context for a %s",
		(ownerKind) => {
			const built = viewDataWith([], ownerKind, {
				featuresInProgress: [inFlightItem(3, "ZEN-3")],
			});

			const opensWith = Object.fromEntries(
				Object.entries(built).map(([key, payload]) => [
					key,
					{
						context: payload?.context,
						ownerKind: payload?.ownerKind,
					},
				]),
			);

			expect(opensWith).toEqual(
				Object.fromEntries(
					Object.entries(CONTEXT_OF_EVERY_LIST).map(([key, context]) => [
						key,
						{ context, ownerKind },
					]),
				),
			);
		},
	);

	const LISTS_IN_PROGRESS_OVER_THE_RANGE = [
		"wipOverTime",
		"totalWorkItemAgeOverTime",
		"wipPbc",
		"totalWorkItemAgePbc",
	];

	it("the lists of what was in progress over the range measure age on the range's last day, and no other list does", () => {
		const lastDay = new Date("2026-09-30T12:00:00");
		const built = viewDataWith([], "team", { endDate: lastDay });

		const ageOnOfEvery = Object.fromEntries(
			Object.entries(built).map(([key, payload]) => [key, payload?.ageOn]),
		);

		expect(ageOnOfEvery).toEqual(
			Object.fromEntries(
				Object.keys(built).map((key) => [
					key,
					LISTS_IN_PROGRESS_OVER_THE_RANGE.includes(key) ? lastDay : undefined,
				]),
			),
		);
	});

	it("no list is claimed that no longer exists", () => {
		const built = builtWithRisk();
		const named = [
			...LISTS_WHAT_IS_IN_FLIGHT_TODAY,
			...DOES_NOT_LIST_WHAT_IS_IN_FLIGHT_TODAY,
		].filter((key) => !(key in built));

		expect(
			named,
			"These payloads were renamed or removed. Update the list that still names them.",
		).toEqual([]);
	});

	// Without this, the first assertion would be satisfied by a descriptor that is always built,
	// and a team that has published no target would get a column of blanks on four lists.
	it("a team with no published target gets the column nowhere", () => {
		const built = viewDataWith([]);

		for (const key of Object.keys(built)) {
			expect(built[key]?.sleRiskColumn).toBeUndefined();
		}
	});

	const aWorkItem = (id: number, overrides: Partial<IWorkItem> = {}) => ({
		...inFlightItem(id, `ZEN-${id}`),
		...overrides,
	});

	const idsOfEvery = (built: ReturnType<typeof viewDataWith>) =>
		Object.fromEntries(
			Object.entries(built).map(([key, payload]) => [
				key,
				payload?.items.map((item) => item.id),
			]),
		);

	const aFeature = (
		id: number,
		stateCategory: StateCategory,
		size: number,
	): IFeature => ({ id, stateCategory, size }) as unknown as IFeature;

	it("each list is titled with the team's own words", () => {
		const built = viewDataWith([], "team", {
			title: "Zen Items",
			featuresInProgress: [aWorkItem(3)],
			terms: {
				workItem: "Task",
				workItems: "Tasks",
				features: "Goals",
				cycleTime: "Lead Time",
				workItemAge: "Task Age",
				blocked: "Halted",
				sle: "Promise",
			},
		});

		const titles = Object.fromEntries(
			Object.entries(built).map(([key, payload]) => [key, payload?.title]),
		);

		expect(titles).toEqual({
			wipOverview: "Zen Items in Progress",
			blockedOverview: "Halted Tasks",
			staleOverview: "Stale Tasks",
			featuresWorkedOnOverview: "Goals being Worked On",
			percentiles: "Closed Tasks",
			totalWorkItemAge: "Zen Items in Progress",
			workItemAgePercentiles: "Zen Items in Progress",
			sleRisk: "Zen Items in Progress",
			throughput: "Zen Items Completed",
			cycleScatter: "Closed Tasks",
			workDistribution: "Tasks Distribution",
			aging: "Tasks in Progress",
			wipOverTime: "Zen Items In Progress",
			totalWorkItemAgeOverTime: "Zen Items Contributing to Total Age",
			stacked: "Started and Closed Tasks",
			estimationVsCycleTime: "Tasks with Estimates",
			featureSize: "Goals",
			featureSizePercentiles: "Closed Goals",
			throughputPbc: "Zen Items Completed",
			wipPbc: "Zen Items In Progress",
			totalWorkItemAgePbc: "Zen Items Contributing to Total Age",
			cycleTimePbc: "Closed Tasks",
			featureSizePbc: "Goals",
			arrivals: "Zen Items Started",
			arrivalsPbc: "Zen Items Started",
			totalThroughput: "Zen Items Completed",
			totalArrivals: "Zen Items Started",
		});
	});

	it("each list is fed from its own source", () => {
		const openArrival = aWorkItem(30, {
			closedDate: null as unknown as Date,
		});
		const closedArrival = aWorkItem(31);

		const built = viewDataWith([], "team", {
			inProgressItems: [aWorkItem(1), aWorkItem(2)],
			blockedItems: [aWorkItem(4)],
			staleItems: [aWorkItem(5)],
			featuresInProgress: [aWorkItem(6)],
			cycleTimeData: [aWorkItem(7)],
			throughputData: new RunChartData(
				{ 0: [aWorkItem(10)], 1: [aWorkItem(11)] },
				2,
				2,
			),
			wipOverTimeData: new RunChartData({ 0: [aWorkItem(20)] }, 1, 1),
			arrivalsData: new RunChartData(
				{ 0: [openArrival], 1: [closedArrival] },
				2,
				2,
			),
		});

		expect(idsOfEvery(built)).toEqual({
			wipOverview: [1, 2],
			blockedOverview: [4],
			staleOverview: [5],
			featuresWorkedOnOverview: [6],
			percentiles: [7],
			totalWorkItemAge: [1, 2],
			workItemAgePercentiles: [1, 2],
			sleRisk: [1, 2],
			throughput: [10, 11],
			cycleScatter: [7],
			workDistribution: [7, 1, 2],
			aging: [1, 2],
			wipOverTime: [20],
			totalWorkItemAgeOverTime: [20],
			stacked: [30, 10, 11],
			estimationVsCycleTime: [],
			featureSize: [],
			featureSizePercentiles: [],
			throughputPbc: [10, 11],
			wipPbc: [20],
			totalWorkItemAgePbc: [20],
			cycleTimePbc: [7],
			featureSizePbc: [],
			arrivals: [30, 31],
			arrivalsPbc: [30, 31],
			totalThroughput: [10, 11],
			totalArrivals: [30, 31],
		});
	});

	it("Started and Closed Work Items counts only what was started and is still open, and what finished", () => {
		const arrivalsOnly = viewDataWith([], "team", {
			arrivalsData: new RunChartData(
				{
					0: [
						aWorkItem(30, { closedDate: null as unknown as Date }),
						aWorkItem(31),
					],
				},
				1,
				2,
			),
		});
		const throughputOnly = viewDataWith([], "team", {
			throughputData: new RunChartData({ 0: [aWorkItem(10)] }, 1, 1),
		});

		expect(arrivalsOnly.stacked?.items.map((item) => item.id)).toEqual([30]);
		expect(throughputOnly.stacked?.items.map((item) => item.id)).toEqual([10]);
	});

	it("the Estimation list holds each Work Item an estimate point names, and skips ones the lookup does not know", () => {
		const known = aWorkItem(1);
		const alsoKnown = aWorkItem(2);

		const withData = viewDataWith([], "team", {
			workItemLookup: new Map([
				[1, known],
				[2, alsoKnown],
			]),
			estimationVsCycleTimeData: {
				status: "Ready",
				diagnostics: {
					totalCount: 3,
					mappedCount: 3,
					unmappedCount: 0,
					invalidCount: 0,
				},
				estimationUnit: "Points",
				useNonNumericEstimation: false,
				categoryValues: [],
				dataPoints: [
					{
						workItemIds: [1, 99],
						estimationNumericValue: 3,
						estimationDisplayValue: "3",
						cycleTime: 4,
					},
					{
						workItemIds: [2],
						estimationNumericValue: 5,
						estimationDisplayValue: "5",
						cycleTime: 8,
					},
				],
			},
		});
		const withoutData = viewDataWith([]);

		expect(withData.estimationVsCycleTime?.items).toEqual([known, alsoKnown]);
		expect(withoutData.estimationVsCycleTime?.items).toEqual([]);
	});

	it("the Features lists hold the size chart's Features, and the percentiles only its sized, finished ones", () => {
		const finished = aFeature(1, "Done", 5);
		const finishedWithDefaultSize = aFeature(2, "Done", 0);
		const stillOpen = aFeature(3, "Doing", 8);

		const built = viewDataWith([], "team", {
			allFeaturesForSizeChart: [finished, finishedWithDefaultSize, stillOpen],
		});

		const featureIds = (key: string) =>
			built[key]?.items.map((item) => item.id);

		expect(featureIds("featureSize")).toEqual([1, 2, 3]);
		expect(featureIds("featureSizePbc")).toEqual([1, 2, 3]);
		expect(featureIds("featureSizePercentiles")).toEqual([1]);
	});

	it("only a Feature list that was asked for is built", () => {
		expect(viewDataWith([]).featuresWorkedOnOverview).toBeUndefined();
	});

	it("the closed lists carry the published target, and none when there is no target", () => {
		const withTarget = viewDataWith([]);
		const withoutTarget = viewDataWith([], "team", {
			serviceLevelExpectation: null,
		});

		for (const key of ["percentiles", "cycleScatter", "cycleTimePbc"]) {
			expect(withTarget[key]?.sle, key).toBe(10);
			expect(withoutTarget[key]?.sle, key).toBeUndefined();
		}
	});

	describe("closed Work Items scoped to a named cycle time", () => {
		const closedIn = (
			id: number,
			namedCycleTimes?: IWorkItem["namedCycleTimes"],
		) => aWorkItem(id, { namedCycleTimes });

		const closed = [
			closedIn(1, [{ definitionId: 5, days: 3 }]),
			closedIn(2),
			closedIn(3, [{ definitionId: 6, days: 4 }]),
			closedIn(4, [
				{ definitionId: 6, days: 4 },
				{ definitionId: 5, days: 0 },
			]),
		];
		const definitions = [
			{ id: 5, name: "Development" },
			{ id: 7, name: "Review" },
		];

		it("lists only the items that have a value for it, and names the scope", () => {
			const percentiles = viewDataWith([], "team", {
				cycleTimeData: closed,
				percentilesScopeDefinitionId: 5,
				namedCycleTimeDefinitions: definitions,
			}).percentiles;

			expect(percentiles?.items.map((item) => item.id)).toEqual([1, 4]);
			expect(percentiles?.cycleTimeScope).toBe(5);
			expect(percentiles?.namedCycleTimeDefinitions).toEqual(definitions);
			expect(percentiles?.sle).toBeUndefined();
			expect(percentiles?.context).toBe("closedItems");
		});

		it("finds the scope among several definitions", () => {
			const percentiles = viewDataWith([], "team", {
				cycleTimeData: [closedIn(8, [{ definitionId: 7, days: 2 }])],
				percentilesScopeDefinitionId: 7,
				namedCycleTimeDefinitions: definitions,
			}).percentiles;

			expect(percentiles?.cycleTimeScope).toBe(7);
			expect(percentiles?.items.map((item) => item.id)).toEqual([8]);
		});

		it("falls back to every closed item and the target when the scope names no known definition", () => {
			const percentiles = viewDataWith([], "team", {
				cycleTimeData: closed,
				percentilesScopeDefinitionId: 9,
				namedCycleTimeDefinitions: definitions,
			}).percentiles;

			expect(percentiles?.items.map((item) => item.id)).toEqual([1, 2, 3, 4]);
			expect(percentiles?.cycleTimeScope).toBeUndefined();
			expect(percentiles?.sle).toBe(10);
		});

		it("is not scoped when no scope is chosen", () => {
			const percentiles = viewDataWith([], "team", {
				cycleTimeData: closed,
				percentilesScopeDefinitionId: null,
				namedCycleTimeDefinitions: definitions,
			}).percentiles;

			expect(percentiles?.items).toHaveLength(4);
			expect(percentiles?.cycleTimeScope).toBeUndefined();
			expect(percentiles?.sle).toBe(10);
		});
	});
});
