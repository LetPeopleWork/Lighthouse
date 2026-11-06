import { describe, expect, it } from "vitest";
import type { ParentSourceEntry } from "../../../models/Common/ParentSourceEntry";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { WorkTrackingSystemType } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	entriesStillToAdd,
	nameOfParentSource,
	type ParentSourcesAction,
	parentSourcesAreOffered,
	reduceParentSources,
} from "./parentSources";

const ownParent: ParentSourceEntry = { kind: "TrackerParent" };
const field = (id: number): ParentSourceEntry => ({
	kind: "AdditionalField",
	additionalFieldDefinitionId: id,
});

const TheFields: IAdditionalFieldDefinition[] = [
	{ id: 11, displayName: "Is caused by", reference: "customfield_10500" },
	{ id: 12, displayName: "Program Ref", reference: "customfield_10014" },
	{ id: 13, displayName: "Sponsor", reference: "customfield_10015" },
];

describe("reduceParentSources", () => {
	// Every edit the rules allow, and what it leaves.
	it.each<
		[string, ParentSourceEntry[], ParentSourcesAction, ParentSourceEntry[]]
	>([
		[
			"adds a source at the end",
			[field(11)],
			{ type: "add", entry: ownParent },
			[field(11), ownParent],
		],
		[
			"moves a source up",
			[field(11), field(12)],
			{ type: "moveUp", index: 1 },
			[field(12), field(11)],
		],
		[
			"moves a source down",
			[field(11), field(12), ownParent],
			{ type: "moveDown", index: 0 },
			[field(12), field(11), ownParent],
		],
		[
			"removes a source and keeps the order of the rest",
			[field(11), field(12), ownParent],
			{ type: "remove", index: 1 },
			[field(11), ownParent],
		],
	])("%s", (_name, list, action, expected) => {
		expect(reduceParentSources(list, action)).toEqual(expected);
	});

	// Every edit the rules refuse: the list comes back exactly as it was given.
	it.each<[string, ParentSourceEntry[], ParentSourcesAction]>([
		[
			"will not add a sixth source",
			[field(11), field(12), field(13), field(14), field(15)],
			{ type: "add", entry: ownParent },
		],
		[
			"will not add a source that is already listed",
			[field(11), ownParent],
			{ type: "add", entry: field(11) },
		],
		[
			"will not add the tracker's own parent twice",
			[field(11), ownParent],
			{ type: "add", entry: ownParent },
		],
		[
			"will not remove the only source",
			[field(11)],
			{ type: "remove", index: 0 },
		],
		[
			"will not move the first source up",
			[field(11), field(12)],
			{ type: "moveUp", index: 0 },
		],
		[
			"will not move the last source down",
			[field(11), field(12)],
			{ type: "moveDown", index: 1 },
		],
		[
			"ignores a position that is not in the list",
			[field(11), field(12)],
			{ type: "remove", index: 7 },
		],
	])("%s", (_name, list, action) => {
		const result = reduceParentSources(list, action);

		expect(result).toEqual(list);
	});

	it("never changes the list it was handed", () => {
		const list = Object.freeze([field(11), field(12)]);

		const result = reduceParentSources(list, { type: "moveUp", index: 1 });

		expect(result).not.toBe(list);
		expect(list).toEqual([field(11), field(12)]);
	});
});

describe("entriesStillToAdd", () => {
	it("offers the tracker's own parent and every field not yet listed, and nothing already listed", () => {
		expect(entriesStillToAdd([field(11)], TheFields)).toEqual([
			ownParent,
			field(12),
			field(13),
		]);
		expect(entriesStillToAdd([ownParent, field(11)], TheFields)).toEqual([
			field(12),
			field(13),
		]);
	});
});

describe("nameOfParentSource", () => {
	it.each<[WorkTrackingSystemType, ParentSourceEntry, string]>([
		["Jira", ownParent, "Parent (Jira's own)"],
		["Jira", field(12), "Program Ref"],
		["Jira", field(99), "Unknown field (id 99)"],
	])("on %s a row reads %j as %s", (tracker, entry, expected) => {
		expect(nameOfParentSource(entry, tracker, TheFields)).toBe(expected);
	});

	// Azure DevOps does not read the list yet, so no row can show there.
	it.skip.each<[WorkTrackingSystemType, ParentSourceEntry, string]>([
		["AzureDevOps", ownParent, "Parent (Azure DevOps' own)"],
	])("on %s a row reads %j as %s", (tracker, entry, expected) => {
		expect(nameOfParentSource(entry, tracker, TheFields)).toBe(expected);
	});
});

describe("parentSourcesAreOffered", () => {
	it.each<[WorkTrackingSystemType, boolean]>([
		["Jira", true],
		["Linear", false],
		["Csv", false],
		["ServiceNow", false],
	])("on %s the Parent sources are offered: %s", (tracker, offered) => {
		expect(parentSourcesAreOffered(tracker)).toBe(offered);
	});

	// Offering the list on Azure DevOps before it reads the list would promise an order nothing honours.
	it.skip.each<[WorkTrackingSystemType, boolean]>([["AzureDevOps", true]])(
		"on %s the Parent sources are offered: %s",
		(tracker, offered) => {
			expect(parentSourcesAreOffered(tracker)).toBe(offered);
		},
	);
});
