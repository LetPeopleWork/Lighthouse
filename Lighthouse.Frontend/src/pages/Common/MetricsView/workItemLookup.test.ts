import { describe, expect, it } from "vitest";
import type { WorkItemsDialogOwnerKind } from "../../../components/Common/WorkItemsDialog/workItemsDialogContexts";
import type { IFeature } from "../../../models/Feature";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem, StateCategory } from "../../../models/WorkItem";

// The builder is loaded at run time so a missing module fails the rows below with a message,
// instead of stopping the whole file before any row runs. The local type states the builder's
// contract, so the rows stay typed whether or not the module exists yet.
type LookupBuilders = {
	buildWorkItemLookup(
		throughputData: RunChartData | null,
		wipOverTimeData: RunChartData | null,
		cycleTimeData: IWorkItem[],
		inProgressItems: IWorkItem[],
		allFeaturesForSizeChart: IFeature[],
		ownerKind: WorkItemsDialogOwnerKind,
	): Map<number, IWorkItem>;
};

const MODULE_PATH = "./workItemLookup";

async function loadBuilders(): Promise<LookupBuilders> {
	const loaded = await import(/* @vite-ignore */ MODULE_PATH).catch(
		() => undefined,
	);
	expect(
		loaded,
		"workItemLookup.ts does not exist or cannot be loaded. The Work Item lookup builder must live in its own module, because the metrics view only imports it. Create workItemLookup.ts exporting buildWorkItemLookup, moved unchanged.",
	).toBeDefined();
	expect(
		typeof loaded?.buildWorkItemLookup,
		"workItemLookup.ts must export buildWorkItemLookup, the function the metrics view uses to resolve a Work Item by id. Export it under that name.",
	).toBe("function");
	return loaded as LookupBuilders;
}

describe("buildWorkItemLookup", () => {
	const aWorkItem = (overrides: Partial<IWorkItem>): IWorkItem => ({
		id: 1,
		name: "Item",
		referenceId: "REF-1",
		url: null,
		state: "Done",
		stateCategory: "Done" as StateCategory,
		type: "User Story",
		startedDate: new Date("2026-08-01"),
		closedDate: new Date("2026-08-05"),
		cycleTime: 0,
		workItemAge: 0,
		parentWorkItemReference: "",
		isBlocked: false,
		...overrides,
	});

	const runChart = (items: IWorkItem[]): RunChartData =>
		new RunChartData({ 0: items }, 1, items.length);

	it("keeps the cycle time when the same item also appears in the throughput run chart", async () => {
		const id = 42;
		const runChartRecord = aWorkItem({ id, cycleTime: 0 });
		const cycleTimeRecord = aWorkItem({ id, cycleTime: 12 });

		const { buildWorkItemLookup } = await loadBuilders();
		const lookup = buildWorkItemLookup(
			runChart([runChartRecord]),
			null,
			[cycleTimeRecord],
			[],
			[],
			"team",
		);

		expect(lookup.get(id)?.cycleTime).toBe(12);
	});

	it("keeps the work item age when the same item also appears in the wip run chart", async () => {
		const id = 43;
		const runChartRecord = aWorkItem({ id, workItemAge: 0, isBlocked: false });
		const inProgressRecord = aWorkItem({ id, workItemAge: 7, isBlocked: true });

		const { buildWorkItemLookup } = await loadBuilders();
		const lookup = buildWorkItemLookup(
			null,
			runChart([runChartRecord]),
			[],
			[inProgressRecord],
			[],
			"team",
		);

		expect(lookup.get(id)?.workItemAge).toBe(7);
		expect(lookup.get(id)?.isBlocked).toBe(true);
	});

	it("lets the size chart features win over every other source", async () => {
		const id = 46;
		const cycleTimeRecord = aWorkItem({ id, cycleTime: 12 });
		const sizeChartFeature = aWorkItem({
			id,
			cycleTime: 99,
		}) as unknown as IFeature;

		const { buildWorkItemLookup } = await loadBuilders();
		const lookup = buildWorkItemLookup(
			null,
			null,
			[cycleTimeRecord],
			[],
			[sizeChartFeature],
			"portfolio",
		);

		expect(lookup.get(id)?.cycleTime).toBe(99);
	});

	it("keeps a Team's own Work Item when one of its Features shares the id", async () => {
		const id = 47;
		const teamWorkItem = aWorkItem({ id, name: "The Team's Work Item" });
		const featureWithTheSameId = aWorkItem({
			id,
			name: "A Feature",
		}) as unknown as IFeature;

		const { buildWorkItemLookup } = await loadBuilders();
		const lookup = buildWorkItemLookup(
			null,
			null,
			[teamWorkItem],
			[],
			[featureWithTheSameId],
			"team",
		);

		expect(lookup.get(id)?.name).toBe("The Team's Work Item");
	});

	it("still includes items that only the run charts know about", async () => {
		const throughputOnly = aWorkItem({ id: 44 });
		const wipOnly = aWorkItem({ id: 45 });

		const { buildWorkItemLookup } = await loadBuilders();
		const lookup = buildWorkItemLookup(
			runChart([throughputOnly]),
			runChart([wipOnly]),
			[],
			[],
			[],
			"team",
		);

		expect(lookup.get(44)).toBe(throughputOnly);
		expect(lookup.get(45)).toBe(wipOnly);
	});
});

// The risk column is attached per payload, and a payload added without one is invisible: nothing
// fails, the list simply renders without the column. Two of the four lists of in-flight work went
// a release that way. So every payload the builder returns has to be classified here, and a new
