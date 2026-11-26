import { describe, expect, it } from "vitest";
import type { IWorkItem } from "../../../models/WorkItem";
import { totalAgeOfStartedItems } from "./totalAgeOfStartedItems";

const day = new Date("2025-01-10T00:00:00Z");

const workItem = (
	id: number,
	startedDate: Date | null | undefined,
): IWorkItem =>
	({
		id,
		name: `Work Item ${id}`,
		state: "In Progress",
		stateCategory: "Doing",
		type: "Task",
		referenceId: `REF-${id}`,
		url: null,
		startedDate,
		closedDate: new Date("2025-02-01T00:00:00Z"),
		cycleTime: 0,
		workItemAge: 0,
		parentWorkItemReference: "",
		isBlocked: false,
	}) as IWorkItem;

describe("totalAgeOfStartedItems", () => {
	it("skips an item whose start date is null", () => {
		const items = [
			workItem(1, new Date("2025-01-08T00:00:00Z")),
			workItem(2, null),
		];

		expect(totalAgeOfStartedItems(items, day)).toEqual({
			totalAge: 3,
			itemCount: 1,
		});
	});

	it("skips an item whose start date is undefined", () => {
		const items = [
			workItem(1, new Date("2025-01-08T00:00:00Z")),
			workItem(2, undefined),
		];

		expect(totalAgeOfStartedItems(items, day)).toEqual({
			totalAge: 3,
			itemCount: 1,
		});
	});

	it("sums the age of every item that has a start date and counts them", () => {
		const items = [
			workItem(1, new Date("2025-01-08T00:00:00Z")),
			workItem(2, new Date("2025-01-10T00:00:00Z")),
		];

		expect(totalAgeOfStartedItems(items, day)).toEqual({
			totalAge: 4,
			itemCount: 2,
		});
	});

	it("totals zero over zero items when no item has a start date", () => {
		const items = [workItem(1, null), workItem(2, undefined)];

		expect(totalAgeOfStartedItems(items, day)).toEqual({
			totalAge: 0,
			itemCount: 0,
		});
	});
});
