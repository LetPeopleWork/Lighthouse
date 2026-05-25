import { describe, expect, test } from "vitest";
import type { IWorkItem } from "../../models/WorkItem";
import { deriveStaleness } from "./deriveStaleness";

describe("deriveStaleness", () => {
	const now = new Date("2026-05-25T12:00:00Z");

	const itemEnteredAt = (
		enteredAt: string | null,
		overrides: Partial<IWorkItem> = {},
	): IWorkItem => ({
		id: 1,
		name: "Item",
		referenceId: "ITEM-1",
		url: null,
		state: "In Progress",
		stateCategory: "Doing",
		type: "Story",
		startedDate: new Date("2026-05-01T00:00:00Z"),
		closedDate: new Date("2026-05-01T00:00:00Z"),
		cycleTime: 0,
		workItemAge: 0,
		parentWorkItemReference: "",
		isBlocked: false,
		currentStateEnteredAt: enteredAt === null ? null : new Date(enteredAt),
		...overrides,
	});

	test.each([
		["2026-05-23T23:00:00Z", 0, false],
		["2026-05-23T23:00:00Z", 3, false],
		["2026-05-23T23:00:00Z", 2, true],
	])("daysInState=3 for %s with threshold %d → stale=%s (off / at-threshold / one-over)", (enteredAt, thresholdDays, expected) => {
		expect(deriveStaleness(itemEnteredAt(enteredAt), thresholdDays, now)).toBe(
			expected,
		);
	});

	test("a blocked item over the threshold is never stale (blocked precedence)", () => {
		const blockedOverThreshold = itemEnteredAt("2026-05-23T23:00:00Z", {
			isBlocked: true,
		});

		expect(deriveStaleness(blockedOverThreshold, 1, now)).toBe(false);
		expect(
			deriveStaleness({ ...blockedOverThreshold, isBlocked: false }, 1, now),
		).toBe(true);
	});

	test("an item with no entered date is never stale", () => {
		expect(deriveStaleness(itemEnteredAt(null), 1, now)).toBe(false);
	});
});
