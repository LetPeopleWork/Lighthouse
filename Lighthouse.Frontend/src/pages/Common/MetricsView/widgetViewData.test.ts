import { describe, expect, it } from "vitest";
import type { WorkItemsDialogContextId } from "../../../components/Common/WorkItemsDialog/workItemsDialogContexts";
import type { IWorkItem, StateCategory } from "../../../models/WorkItem";

// The builder is loaded at run time so a missing module fails the rows below with a message,
// instead of stopping the whole file before any row runs. The local types state the builder's
// contract, so the rows stay typed whether or not the module exists yet.
type ViewDataInputs = Record<string, unknown>;

type ViewDataPayload = {
	sleRiskColumn?: unknown;
	timeInStateColumn?: { now?: Date };
	context?: string;
	ownerKind?: string;
	ageOn?: Date;
};

type ViewDataBuilders = {
	buildViewData(
		inputs: ViewDataInputs,
	): Record<string, ViewDataPayload | undefined>;
};

const MODULE_PATH = "./widgetViewData";

async function loadBuilders(): Promise<ViewDataBuilders> {
	const loaded = await import(/* @vite-ignore */ MODULE_PATH).catch(
		() => undefined,
	);
	expect(
		loaded,
		"widgetViewData.ts does not exist or cannot be loaded. The View Data builder must live in its own module, because the metrics view only imports it. Create widgetViewData.ts exporting buildViewData, moved unchanged.",
	).toBeDefined();
	expect(
		typeof loaded?.buildViewData,
		"widgetViewData.ts must export buildViewData, the function that builds every widget's list payload. Export it under that name.",
	).toBe("function");
	return loaded as ViewDataBuilders;
}

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

	const viewDataWith = async (
		sleRiskValues: {
			referenceId: string;
			risk: number;
			finishedItemsStillOpenAtThisAge: number;
			finishedItemsThatWentOnToMiss: number | null;
		}[],
		ownerKind: "team" | "portfolio" = "team",
		overrides: Partial<ViewDataInputs> = {},
	) => {
		const { buildViewData } = await loadBuilders();
		return buildViewData({
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
	};

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
		async (key) => {
			expect((await builtWithRisk())[key]?.sleRiskColumn).toBeDefined();
		},
	);

	it.each(DOES_NOT_LIST_WHAT_IS_IN_FLIGHT_TODAY)(
		"no other list carries it: %s",
		async (key) => {
			expect((await builtWithRisk())[key]?.sleRiskColumn).toBeUndefined();
		},
	);

	it("every list is in one of the two lists above", async () => {
		const unaccountedFor = Object.keys(await builtWithRisk()).filter(
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
		async (ownerKind) => {
			const estimation = (await viewDataWith([], ownerKind))
				.estimationVsCycleTime;

			expect(estimation?.context).toBe("estimation");
			expect(estimation?.ownerKind).toBe(ownerKind);
		},
	);

	// Lighthouse judged staleness on the range's last day, so the Time in State label beside its
	// reason must count to that same day, not to today.
	it.each(["wipOverview", "staleOverview"])(
		"the %s list counts Time in State to the range's last day",
		async (key) => {
			const rangeEnd = new Date(2026, 4, 25);

			const list = (await viewDataWith([], "team", { endDate: rangeEnd }))[key];

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
		async (ownerKind) => {
			const built = await viewDataWith([], ownerKind, {
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

	it("the lists of what was in progress over the range measure age on the range's last day, and no other list does", async () => {
		const lastDay = new Date("2026-09-30T12:00:00");
		const built = await viewDataWith([], "team", { endDate: lastDay });

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

	it("no list is claimed that no longer exists", async () => {
		const built = await builtWithRisk();
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
	it("a team with no published target gets the column nowhere", async () => {
		const built = await viewDataWith([]);

		for (const key of Object.keys(built)) {
			expect(built[key]?.sleRiskColumn).toBeUndefined();
		}
	});
});
