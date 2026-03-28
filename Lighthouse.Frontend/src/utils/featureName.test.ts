import { describe, expect, it } from "vitest";
import type { IWorkItem } from "../models/WorkItem";
import { getWorkItemName } from "./featureName";

const makeWorkItem = (referenceId: string, name: string): IWorkItem =>
	({ referenceId, name }) as IWorkItem;

describe("getWorkItemName", () => {
	it("returns only the name when it contains 'unparented'", () => {
		const workItem = makeWorkItem("123", "Unparented items");
		expect(getWorkItemName(workItem.name, workItem.referenceId)).toBe(
			"Unparented items",
		);
	});

	it("returns only the name when 'unparented' is lowercase", () => {
		const workItem = makeWorkItem("123", "unparented work items");
		expect(getWorkItemName(workItem.name, workItem.referenceId)).toBe(
			"unparented work items",
		);
	});

	it("returns referenceId and name for a normal work item", () => {
		const workItem = makeWorkItem("ABC-42", "Fix login bug");
		expect(getWorkItemName(workItem.name, workItem.referenceId)).toBe(
			"ABC-42: Fix login bug",
		);
	});

	it("returns only the name when referenceId is a GUID", () => {
		const workItem = makeWorkItem(
			"6558a430-b455-4204-bdd7-d1f27d457e81",
			"My Task",
		);
		expect(getWorkItemName(workItem.name, workItem.referenceId)).toBe(
			"My Task",
		);
	});

	it("returns only the reference ID when name is empty", () => {
		const workItem = makeWorkItem("XYZ-99", "");
		expect(getWorkItemName(workItem.name, workItem.referenceId)).toBe("XYZ-99");
	});
});
