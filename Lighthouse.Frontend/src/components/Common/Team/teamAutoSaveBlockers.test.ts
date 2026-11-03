import { describe, expect, it } from "vitest";
import type { ITeamSettings } from "../../../models/Team/TeamSettings";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { createMockTeamSettings } from "../../../tests/TestDataProvider";
import { teamAutoSaveBlockers } from "./teamAutoSaveBlockers";

const words: Record<string, string> = {
	throughput: "Output",
	feature: "Deliverable",
	wip: "Limit",
	workItem: "Task",
	refinement: "Grooming",
	workTrackingSystem: "Tracker",
};
const getTerm = (key: string) => words[key] ?? key;

const jira = {
	workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
} as unknown as IWorkTrackingSystemConnection;

const blockersOf = (
	change: Partial<ITeamSettings>,
	options: {
		system?: IWorkTrackingSystemConnection | null;
		isDefault?: boolean;
	} = {},
) =>
	teamAutoSaveBlockers(
		{ ...createMockTeamSettings(), ...change },
		options.system === undefined ? jira : options.system,
		options.isDefault ?? false,
		getTerm,
	);

describe("what keeps a Team's settings from being auto-saved", () => {
	it("names nothing for complete settings", () => {
		expect(blockersOf({})).toEqual([]);
	});

	it.each<[string, Partial<ITeamSettings>, string]>([
		["a missing name", { name: "" }, "Enter a Name"],
		[
			"no throughput history",
			{ throughputHistory: 0 },
			"Set a Output History of at least one day",
		],
		[
			"a throughput history that was never set",
			{ throughputHistory: undefined },
			"Set a Output History of at least one day",
		],
		["no feature WIP", { featureWIP: undefined }, "Set a Deliverable Limit"],
		["no To Do state", { toDoStates: [] }, "Add at least one To Do State"],
		["no Doing state", { doingStates: [] }, "Add at least one Doing State"],
		["no Done state", { doneStates: [] }, "Add at least one Done State"],
		["no work item type", { workItemTypes: [] }, "Add at least one Task Type"],
		["no query", { dataRetrievalValue: "" }, "Enter a JQL Query"],
	])("names %s", (_, change, reason) => {
		expect(blockersOf(change)).toEqual([reason]);
	});

	it("does not ask for work item types when the system does not need them", () => {
		const schema = {
			isWorkItemTypesRequired: false,
		} as ITeamSettings["dataRetrievalSchema"];

		expect(
			blockersOf({ workItemTypes: [], dataRetrievalSchema: schema }),
		).toEqual([]);
	});

	it("does not ask for a query when the system does not need one", () => {
		const schema = {
			isRequired: false,
		} as ITeamSettings["dataRetrievalSchema"];

		expect(
			blockersOf({ dataRetrievalValue: "", dataRetrievalSchema: schema }),
		).toEqual([]);
	});

	it("names the schema's own label for the query when it has one", () => {
		const schema = {
			displayLabel: "Board",
		} as ITeamSettings["dataRetrievalSchema"];

		expect(
			blockersOf({ dataRetrievalValue: "", dataRetrievalSchema: schema }),
		).toEqual(["Enter a Board"]);
	});

	it("asks for a work tracking system when none is chosen", () => {
		expect(blockersOf({}, { system: null })).toEqual(["Select a Tracker"]);
	});

	it("asks for neither a system nor a query on the default settings", () => {
		expect(
			blockersOf({ dataRetrievalValue: "" }, { system: null, isDefault: true }),
		).toEqual([]);
	});

	it("still names the other missing fields on the default settings", () => {
		expect(blockersOf({ name: "" }, { system: null, isDefault: true })).toEqual(
			["Enter a Name"],
		);
	});

	it("names every missing field, in the order the form lists them", () => {
		expect(
			blockersOf({ name: "", toDoStates: [], dataRetrievalValue: "" }),
		).toEqual([
			"Enter a Name",
			"Add at least one To Do State",
			"Enter a JQL Query",
		]);
	});

	it("names the Refinement settings that block, in the Team's word for refinement", () => {
		const refinement = {
			states: [],
			cadence: { weekdays: [], intervalWeeks: 0 },
		} as unknown as ITeamSettings["refinement"];

		expect(blockersOf({ refinement })).toEqual([
			"Complete the Grooming cadence",
		]);
	});
});
