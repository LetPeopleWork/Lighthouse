import { describe, expect, it } from "vitest";
import {
	type ConnectionDefaultName,
	WorkTrackingSystemConnection,
	type WorkTrackingSystemType,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { trackerHoldingTheDefault } from "./connectionDefaultEntry";

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
