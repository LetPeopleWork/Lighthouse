import { describe, expect, it } from "vitest";
import type { IPortfolioSettings } from "../../../models/Portfolio/PortfolioSettings";
import type { IWorkTrackingSystemConnection } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { createMockProjectSettings } from "../../../tests/TestDataProvider";
import { storedStateSettingsOf } from "../../../utils/stateMappingValidation";
import { portfolioAutoSaveBlockers } from "./portfolioAutoSaveBlockers";

const words: Record<string, string> = {
	feature: "Deliverable",
	workItem: "Task",
	workItems: "Tasks",
	workTrackingSystem: "Tracker",
};
const getTerm = (key: string) => words[key] ?? key;

const jira = {
	workTrackingSystemGetDataRetrievalDisplayName: () => "JQL Query",
} as unknown as IWorkTrackingSystemConnection;

const blockersOf = (
	change: Partial<IPortfolioSettings>,
	options: {
		system?: IWorkTrackingSystemConnection | null;
		isDefault?: boolean;
		stored?: ReturnType<typeof storedStateSettingsOf>;
	} = {},
) =>
	portfolioAutoSaveBlockers(
		{ ...createMockProjectSettings(), ...change },
		options.system === undefined ? jira : options.system,
		options.isDefault ?? false,
		getTerm,
		options.stored,
	);

const percentileReason =
	"Set a Deliverable Size Percentile above 0 and a History in Days of at least 30";

describe("what keeps a Portfolio's settings from being auto-saved", () => {
	it("names nothing for complete settings", () => {
		expect(blockersOf({})).toEqual([]);
	});

	it.each<[string, Partial<IPortfolioSettings>, string]>([
		["a missing name", { name: "" }, "Enter a Name"],
		[
			"no default number of work items",
			{ defaultAmountOfWorkItemsPerFeature: undefined },
			"Set a Default Number of Tasks per Deliverable",
		],
		["no work item type", { workItemTypes: [] }, "Add at least one Task Type"],
		["no To Do state", { toDoStates: [] }, "Add at least one To Do State"],
		["no Doing state", { doingStates: [] }, "Add at least one Doing State"],
		["no Done state", { doneStates: [] }, "Add at least one Done State"],
		["no query", { dataRetrievalValue: "" }, "Enter a JQL Query"],
		["an unset query", { dataRetrievalValue: undefined }, "Enter a JQL Query"],
	])("names %s", (_, change, reason) => {
		expect(blockersOf(change)).toEqual([reason]);
	});

	describe("a size percentile in use", () => {
		const withPercentile = (
			percentile: number,
			history: number | null,
		): Partial<IPortfolioSettings> => ({
			usePercentileToCalculateDefaultAmountOfWorkItems: true,
			defaultWorkItemPercentile: percentile,
			percentileHistoryInDays: history as number,
		});

		it.each<[string, number, number | null]>([
			["a percentile of 0", 0, 90],
			["a history that was never set", 80, null],
			["a history of 29 days", 80, 29],
		])("is named for %s", (_, percentile, history) => {
			expect(blockersOf(withPercentile(percentile, history))).toEqual([
				percentileReason,
			]);
		});

		it.each<[string, number, number]>([
			["a percentile just above 0", 1, 90],
			["a history of exactly 30 days", 80, 30],
		])("is accepted with %s", (_, percentile, history) => {
			expect(blockersOf(withPercentile(percentile, history))).toEqual([]);
		});

		it("is not asked for while the default number is fixed", () => {
			expect(
				blockersOf({
					usePercentileToCalculateDefaultAmountOfWorkItems: false,
					defaultWorkItemPercentile: 0,
					percentileHistoryInDays: 0,
				}),
			).toEqual([]);
		});
	});

	it("does not ask for work item types when the system does not need them", () => {
		const schema = {
			isWorkItemTypesRequired: false,
		} as IPortfolioSettings["dataRetrievalSchema"];

		expect(
			blockersOf({ workItemTypes: [], dataRetrievalSchema: schema }),
		).toEqual([]);
	});

	it("does not ask for a query when the system does not need one", () => {
		const schema = {
			isRequired: false,
		} as IPortfolioSettings["dataRetrievalSchema"];

		expect(
			blockersOf({ dataRetrievalValue: "", dataRetrievalSchema: schema }),
		).toEqual([]);
	});

	it("names the schema's own label for the query when it has one", () => {
		const schema = {
			displayLabel: "Linear Projects",
		} as IPortfolioSettings["dataRetrievalSchema"];

		expect(
			blockersOf({ dataRetrievalValue: "", dataRetrievalSchema: schema }),
		).toEqual(["Enter a Linear Projects"]);
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

	it("names a state mapping that is named after a state it does not group", () => {
		expect(
			blockersOf({
				doingStates: ["Group", "Active"],
				stateMappings: [{ name: "Group", states: ["Active"] }],
			}),
		).toEqual([
			'Mapping "Group" has the same name as the Doing state "Group". Add "Group" to this mapping to group it, or choose another name.',
		]);
	});

	it("leaves a stored mapping alone that was already named that way", () => {
		const settings = {
			doingStates: ["In Progress", "Group", "In Arbeit"],
			stateMappings: [
				{ name: "Group", states: ["In Arbeit"] },
				{ name: "In Arbeit", states: ["Review"] },
			],
		};

		expect(
			blockersOf(settings, {
				stored: storedStateSettingsOf({
					...createMockProjectSettings(),
					...settings,
				}),
			}),
		).toEqual([]);
	});

	it("names every missing field, in the order the form lists them", () => {
		expect(
			blockersOf(
				{
					name: "",
					defaultAmountOfWorkItemsPerFeature: undefined,
					workItemTypes: [],
					toDoStates: [],
					doingStates: [],
					doneStates: [],
					dataRetrievalValue: "",
				},
				{ system: null },
			),
		).toEqual([
			"Enter a Name",
			"Set a Default Number of Tasks per Deliverable",
			"Add at least one Task Type",
			"Add at least one To Do State",
			"Add at least one Doing State",
			"Add at least one Done State",
			"Select a Tracker",
		]);
	});
});
