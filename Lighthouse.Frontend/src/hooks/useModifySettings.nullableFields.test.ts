import { describe, expect, it } from "vitest";
import type { IBaseSettings } from "../models/Common/BaseSettings";
import type { IPortfolioSettings } from "../models/Portfolio/PortfolioSettings";
import type { ITeamSettings } from "../models/Team/TeamSettings";
import { NULLABLE_FIELDS } from "./useModifySettings";

type KeysAdmittingNull<T> = {
	[K in keyof T]-?: null extends T[K] ? K : never;
}[keyof T];

/**
 * "cleared": the user can empty the field, so the edit arrives as null and must pass the gate.
 * "never-written": the page never writes null into it (it is read back from the server, or always
 * holds an object), so the gate does not need to know it.
 */
type Writes = "cleared" | "never-written";

// tsc keeps every list equal to the fields whose type admits null: a field that starts admitting
// null without being classified here fails the build, so nobody can forget to decide.
const baseSettings = {
	parentOverrideAdditionalFieldDefinitionId: "cleared",
	parentOverrideDefaultName: "never-written",
	blockedRuleSetJson: "cleared",
	processBehaviourChartBaselineStartDate: "cleared",
	processBehaviourChartBaselineEndDate: "cleared",
	estimationAdditionalFieldDefinitionId: "cleared",
	estimationUnit: "cleared",
	estimationDefaultName: "never-written",
	dataRetrievalSchema: "never-written",
} satisfies Record<KeysAdmittingNull<IBaseSettings>, Writes>;

const teamSettings = {
	...baseSettings,
	forecastFilterRuleSetJson: "cleared",
	refinement: "never-written",
} satisfies Record<KeysAdmittingNull<ITeamSettings>, Writes>;

const portfolioSettings = {
	...baseSettings,
	sizeEstimateAdditionalFieldDefinitionId: "cleared",
	sizeEstimateDefaultName: "never-written",
	featureOwnerAdditionalFieldDefinitionId: "cleared",
	featureOwnerDefaultName: "never-written",
	dependencyOverrideAdditionalFieldDefinitionId: "cleared",
	dependencyFieldDefaultName: "never-written",
} satisfies Record<KeysAdmittingNull<IPortfolioSettings>, Writes>;

const clearedFieldsOf = (settings: Record<string, Writes>) =>
	Object.entries(settings)
		.filter(([, writes]) => writes === "cleared")
		.map(([field]) => field);

describe("NULLABLE_FIELDS", () => {
	it.each([
		["team", teamSettings],
		["portfolio", portfolioSettings],
	])(
		"lets every %s settings field the user can clear be written as null",
		(_owner, settings) => {
			for (const field of clearedFieldsOf(settings)) {
				expect(NULLABLE_FIELDS.has(field), field).toBe(true);
			}
		},
	);
});
