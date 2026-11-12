import { describe, expect, it } from "vitest";
import type { IFeature } from "../../../models/Feature";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem, StateCategory } from "../../../models/WorkItem";
import { buildWorkItemLookup, extractWorkItems } from "./workItemLookup";

describe("extractWorkItems", () => {
	it("holds no Work Items when there is nothing to extract from", () => {
		expect(extractWorkItems(undefined)).toEqual([]);
	});
});

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

	it("keeps the cycle time when the same item also appears in the throughput run chart", () => {
		const id = 42;
		const runChartRecord = aWorkItem({ id, cycleTime: 0 });
		const cycleTimeRecord = aWorkItem({ id, cycleTime: 12 });

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

	it("keeps the work item age when the same item also appears in the wip run chart", () => {
		const id = 43;
		const runChartRecord = aWorkItem({ id, workItemAge: 0, isBlocked: false });
		const inProgressRecord = aWorkItem({ id, workItemAge: 7, isBlocked: true });

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

	it("lets the size chart features win over every other source", () => {
		const id = 46;
		const cycleTimeRecord = aWorkItem({ id, cycleTime: 12 });
		const sizeChartFeature = aWorkItem({
			id,
			cycleTime: 99,
		}) as unknown as IFeature;

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

	it("keeps a Team's own Work Item when one of its Features shares the id", () => {
		const id = 47;
		const teamWorkItem = aWorkItem({ id, name: "The Team's Work Item" });
		const featureWithTheSameId = aWorkItem({
			id,
			name: "A Feature",
		}) as unknown as IFeature;

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

	it("still includes items that only the run charts know about", () => {
		const throughputOnly = aWorkItem({ id: 44 });
		const wipOnly = aWorkItem({ id: 45 });

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
