import { describe, expect, it } from "vitest";
import type { IBaseSettings } from "../../../models/Common/BaseSettings";
import type { IConnectionDefaults } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	type ConnectionDefaultName,
	WorkTrackingSystemConnection,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	nameOfTheDefault,
	trackerHoldingTheDefault,
} from "./connectionDefaultEntry";

const aConnection = (
	workTrackingSystem: WorkTrackingSystemType,
	supportedDefaults?: ConnectionDefaultName[],
) =>
	new WorkTrackingSystemConnection({
		id: 1,
		name: "Payments",
		workTrackingSystem,
		options: [],
		supportedDefaults,
	});

describe("trackerHoldingTheDefault", () => {
	it.each([
		["Jira", [], "parentOverrideForTeams", undefined],
		["Csv", [], "featureOwnerFieldForPortfolios", undefined],
		[
			"Csv",
			["sizeEstimateFieldForPortfolios"],
			"sizeEstimateFieldForPortfolios",
			"CSV",
		],
	] as const)(
		"on a %s connection offering %j, %s is held by %s",
		(tracker, offered, theDefault, expected) => {
			expect(
				trackerHoldingTheDefault(
					null,
					aConnection(tracker, [...offered]),
					theDefault,
				),
			).toBe(expected);
		},
	);

	it.each([
		["Jira", "parentOverrideForTeams", "Jira"],
		["Csv", "featureOwnerFieldForPortfolios", "CSV"],
		["Csv", "estimationForTeams", "CSV"],
		["Csv", "estimationForPortfolios", "CSV"],
		["Csv", "sizeEstimateFieldForPortfolios", undefined],
	] as const)(
		"on a %s connection that says nothing of what it offers, %s is held by %s",
		(tracker, theDefault, expected) => {
			expect(
				trackerHoldingTheDefault(null, aConnection(tracker), theDefault),
			).toBe(expected);
		},
	);
});

const StoryPoints = {
	id: 13,
	displayName: "Story Points",
	reference: "customfield_10016",
};

const theLoadedConnection = (defaults: IConnectionDefaults) =>
	new WorkTrackingSystemConnection({
		id: 1,
		name: "Payments",
		workTrackingSystem: "Jira",
		options: [],
		additionalFieldDefinitions: [StoryPoints],
		defaults,
	});

const settingsOnTheConnection = {
	workTrackingSystemConnectionId: 1,
} as IBaseSettings;

describe("nameOfTheDefault", () => {
	// @story-6313 @contract-shape:pure-function
	it("names an Estimation default off the loaded connection by its field and unit, whatever name the settings carry", () => {
		expect(
			nameOfTheDefault(
				settingsOnTheConnection,
				"Effort, Hours",
				theLoadedConnection({
					estimationForTeams: {
						additionalFieldDefinitionId: 13,
						unit: "Points",
						useNonNumericEstimation: false,
						estimationCategoryValues: [],
					},
				}),
				"estimationForTeams",
			),
		).toBe("Story Points, Points");
	});

	// @story-6313 @contract-shape:pure-function
	it("names no Estimation default whose field the connection no longer lists, even with a unit", () => {
		expect(
			nameOfTheDefault(
				settingsOnTheConnection,
				"Effort, Hours",
				theLoadedConnection({
					estimationForTeams: {
						additionalFieldDefinitionId: 99,
						unit: "Points",
						useNonNumericEstimation: false,
						estimationCategoryValues: [],
					},
				}),
				"estimationForTeams",
			),
		).toBeUndefined();
	});

	// @story-6313 @contract-shape:pure-function
	it("names an Estimation default as the settings carry it while the connection cannot be read", () => {
		expect(
			nameOfTheDefault(
				settingsOnTheConnection,
				"Effort, Hours",
				undefined,
				"estimationForTeams",
			),
		).toBe("Effort, Hours");
	});

	// @story-6313 @contract-shape:pure-function
	it("before the settings arrive, names a field default off the loaded connection", () => {
		expect(
			nameOfTheDefault(
				null,
				"Effort",
				theLoadedConnection({ parentOverrideForTeams: 13 }),
				"parentOverrideForTeams",
			),
		).toBe("Story Points");
	});
});
