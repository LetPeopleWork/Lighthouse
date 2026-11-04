import { describe, expect, it } from "vitest";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import type { IEstimationDefault } from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import { defaultsKeepingOnlyFields } from "./defaultsKeepingOnlyFields";

const field = (id: number): IAdditionalFieldDefinition => ({
	id,
	displayName: `Field ${id}`,
	reference: `customfield_${id}`,
});

const inPoints: IEstimationDefault = {
	additionalFieldDefinitionId: 13,
	unit: "Points",
	useNonNumericEstimation: false,
	estimationCategoryValues: [],
};

describe("defaultsKeepingOnlyFields", () => {
	it("clears a default whose field is no longer in the list and leaves the others as they were", () => {
		expect(
			defaultsKeepingOnlyFields(
				{ parentOverrideForTeams: 11, estimationForTeams: inPoints },
				[field(12), field(13)],
			),
		).toEqual({ parentOverrideForTeams: null, estimationForTeams: inPoints });
	});

	it("clears an Estimation default when the field inside its bundle is gone", () => {
		expect(
			defaultsKeepingOnlyFields(
				{ parentOverrideForTeams: 11, estimationForTeams: inPoints },
				[field(11), field(12)],
			),
		).toEqual({ parentOverrideForTeams: 11, estimationForTeams: null });
	});

	it.each([
		"parentOverrideForTeams",
		"parentOverrideForPortfolios",
		"dependencyFieldForPortfolios",
		"sizeEstimateFieldForPortfolios",
		"featureOwnerFieldForPortfolios",
	] as const)("clears %s when its field is gone", (name) => {
		expect(defaultsKeepingOnlyFields({ [name]: 11 }, [field(12)])).toEqual({
			[name]: null,
		});
	});

	it("clears estimationForPortfolios when the field inside its bundle is gone", () => {
		expect(
			defaultsKeepingOnlyFields({ estimationForPortfolios: inPoints }, []),
		).toEqual({ estimationForPortfolios: null });
	});

	it("keeps every default when every field is still there", () => {
		const defaults = {
			parentOverrideForPortfolios: 12,
			estimationForPortfolios: inPoints,
		};
		expect(defaultsKeepingOnlyFields(defaults, [field(12), field(13)])).toEqual(
			defaults,
		);
	});

	it("leaves a default that names no field as it is", () => {
		expect(
			defaultsKeepingOnlyFields(
				{ parentOverrideForTeams: null, estimationForTeams: null },
				[],
			),
		).toEqual({ parentOverrideForTeams: null, estimationForTeams: null });
	});

	it("returns no defaults for none", () => {
		expect(defaultsKeepingOnlyFields({}, [field(11)])).toEqual({});
	});
});
