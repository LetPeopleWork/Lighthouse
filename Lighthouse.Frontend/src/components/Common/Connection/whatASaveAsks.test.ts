import { describe, expect, it } from "vitest";
import type { IAdditionalFieldDefinition } from "../../../models/WorkTracking/AdditionalFieldDefinition";
import {
	type ConnectionDefaultName,
	ConnectionDefaultNames,
	type IConnectionUsage,
	type IEstimationDefault,
} from "../../../models/WorkTracking/WorkTrackingSystemConnection";
import {
	changedDefaultsToUpdateFollowersOf,
	changedEstimationDefaultsWithFollowers,
	fieldOfDefault,
	fieldsChangedSince,
	whyUpdate,
} from "./whatASaveAsks";

const storyPoints: IAdditionalFieldDefinition = {
	id: 11,
	displayName: "Story Points",
	reference: "customfield_10016",
};
const fixVersions: IAdditionalFieldDefinition = {
	id: 12,
	displayName: "Fix Versions",
	reference: "fixVersions",
};

const inPoints: IEstimationDefault = {
	additionalFieldDefinitionId: 11,
	unit: "Points",
	useNonNumericEstimation: false,
	estimationCategoryValues: [],
};

const withFollowers = (
	followers: IConnectionUsage["followers"],
	extra: Partial<IConnectionUsage> = {},
): IConnectionUsage => ({ teams: 4, portfolios: 2, followers, ...extra });

describe("fieldsChangedSince", () => {
	it.each([
		["nothing changed", [storyPoints, fixVersions], [storyPoints, fixVersions]],
		["no fields on either side", [], []],
		[
			"the same fields in another order",
			[storyPoints, fixVersions],
			[fixVersions, storyPoints],
		],
	])("%s is no change", (_, loaded, current) => {
		expect(fieldsChangedSince(loaded, current)).toBe(false);
	});

	it.each([
		["a field added", [storyPoints], [storyPoints, fixVersions]],
		["a field removed", [storyPoints, fixVersions], [storyPoints]],
		[
			"a field renamed",
			[storyPoints],
			[{ ...storyPoints, displayName: "Points" }],
		],
		[
			"a field pointed at another reference",
			[storyPoints],
			[{ ...storyPoints, reference: "customfield_99999" }],
		],
		[
			"a field back under a new id with the same name and reference",
			[storyPoints],
			[{ ...storyPoints, id: 99 }],
		],
		[
			"the names and references of two fields swapped",
			[storyPoints, fixVersions],
			[
				{
					...storyPoints,
					displayName: "Fix Versions",
					reference: "fixVersions",
				},
				{
					...fixVersions,
					displayName: "Story Points",
					reference: "customfield_10016",
				},
			],
		],
	])("%s is a change", (_, loaded, current) => {
		expect(fieldsChangedSince(loaded, current)).toBe(true);
	});
});

describe("fieldOfDefault", () => {
	it("is the field id for a default that names a field", () => {
		expect(
			fieldOfDefault("parentOverrideForTeams", { parentOverrideForTeams: 11 }),
		).toBe(11);
	});

	it.each(["estimationForTeams", "estimationForPortfolios"] as const)(
		"is the field inside the bundle for %s",
		(name) => {
			expect(fieldOfDefault(name, { [name]: inPoints })).toBe(11);
		},
	);

	it("is undefined for a default that is not set", () => {
		expect(fieldOfDefault("estimationForTeams", {})).toBeUndefined();
	});
});

describe("changedDefaultsToUpdateFollowersOf", () => {
	const followedByTwo = (name: ConnectionDefaultName) =>
		withFollowers({ [name]: 2 });

	it.each([
		"parentOverrideForTeams",
		"parentOverrideForPortfolios",
		"dependencyFieldForPortfolios",
		"sizeEstimateFieldForPortfolios",
		"featureOwnerFieldForPortfolios",
	] as const)("lists a changed %s that has followers", (name) => {
		expect(
			changedDefaultsToUpdateFollowersOf(
				{ [name]: 11 },
				{ [name]: 12 },
				followedByTwo(name),
			),
		).toEqual([name]);
	});

	it.each(["estimationForTeams", "estimationForPortfolios"] as const)(
		"leaves out %s, which is read when a page loads",
		(name) => {
			expect(
				changedDefaultsToUpdateFollowersOf(
					{ [name]: inPoints },
					{ [name]: { ...inPoints, unit: "Hours" } },
					followedByTwo(name),
				),
			).toEqual([]);
		},
	);

	it("leaves out a changed default nobody follows, though others have followers", () => {
		expect(
			changedDefaultsToUpdateFollowersOf(
				{ parentOverrideForTeams: 11 },
				{ parentOverrideForTeams: 12 },
				followedByTwo("parentOverrideForPortfolios"),
			),
		).toEqual([]);
	});

	it("leaves out a default with a follower count of zero", () => {
		expect(
			changedDefaultsToUpdateFollowersOf(
				{ parentOverrideForTeams: 11 },
				{ parentOverrideForTeams: 12 },
				withFollowers({ parentOverrideForTeams: 0 }),
			),
		).toEqual([]);
	});

	it("leaves out a followed default that did not change", () => {
		expect(
			changedDefaultsToUpdateFollowersOf(
				{ parentOverrideForTeams: 11 },
				{ parentOverrideForTeams: 11 },
				followedByTwo("parentOverrideForTeams"),
			),
		).toEqual([]);
	});

	it.each([
		["unset becoming null", {}, { parentOverrideForTeams: null }],
		["null becoming unset", { parentOverrideForTeams: null }, {}],
	])("counts %s as no change", (_, loaded, current) => {
		expect(
			changedDefaultsToUpdateFollowersOf(
				loaded,
				current,
				followedByTwo("parentOverrideForTeams"),
			),
		).toEqual([]);
	});

	it("counts a default cleared as a change", () => {
		expect(
			changedDefaultsToUpdateFollowersOf(
				{ parentOverrideForTeams: 11 },
				{ parentOverrideForTeams: null },
				followedByTwo("parentOverrideForTeams"),
			),
		).toEqual(["parentOverrideForTeams"]);
	});

	it("lists every changed default that has followers, in the order of the table", () => {
		expect(
			changedDefaultsToUpdateFollowersOf(
				{},
				{ dependencyFieldForPortfolios: 12, parentOverrideForTeams: 12 },
				withFollowers({
					dependencyFieldForPortfolios: 1,
					parentOverrideForTeams: 1,
				}),
			),
		).toEqual(["parentOverrideForTeams", "dependencyFieldForPortfolios"]);
	});

	it("reads the followers from the groups when the server sends them", () => {
		expect(
			changedDefaultsToUpdateFollowersOf(
				{ parentOverrideForPortfolios: 11 },
				{ parentOverrideForPortfolios: 12 },
				withFollowers(undefined, {
					followerGroups: [
						{ follows: ["parentOverrideForPortfolios"], count: 3 },
					],
				}),
			),
		).toEqual(["parentOverrideForPortfolios"]);
	});

	it("has no followers when the server sends neither groups nor counts", () => {
		expect(
			changedDefaultsToUpdateFollowersOf(
				{},
				{ parentOverrideForTeams: 12 },
				{ teams: 4, portfolios: 2 },
			),
		).toEqual([]);
	});
});

describe("changedEstimationDefaultsWithFollowers", () => {
	it.each(["estimationForTeams", "estimationForPortfolios"] as const)(
		"lists a changed %s that has followers",
		(name) => {
			expect(
				changedEstimationDefaultsWithFollowers(
					{},
					{ [name]: inPoints },
					withFollowers({ [name]: 2 }),
				),
			).toEqual([name]);
		},
	);

	it("leaves out a cleared default that is no Estimation default, though it has followers", () => {
		expect(
			changedEstimationDefaultsWithFollowers(
				{ parentOverrideForTeams: 11 },
				{ parentOverrideForTeams: null },
				withFollowers({ parentOverrideForTeams: 3 }),
			),
		).toEqual([]);
	});

	it("leaves out a changed Estimation default nobody follows", () => {
		expect(
			changedEstimationDefaultsWithFollowers(
				{},
				{ estimationForTeams: inPoints },
				withFollowers({}),
			),
		).toEqual([]);
	});

	it("leaves out a followed Estimation default that stayed as it was", () => {
		expect(
			changedEstimationDefaultsWithFollowers(
				{ estimationForTeams: inPoints },
				{ estimationForTeams: { ...inPoints } },
				withFollowers({ estimationForTeams: 4 }),
			),
		).toEqual([]);
	});

	it.each([
		["another field", { additionalFieldDefinitionId: 12 }],
		["another unit", { unit: "Hours" }],
		["non-numeric estimation switched on", { useNonNumericEstimation: true }],
		["other category values", { estimationCategoryValues: ["S", "M"] }],
	] as [string, Partial<IEstimationDefault>][])(
		"sees %s as a change",
		(_, difference) => {
			expect(
				changedEstimationDefaultsWithFollowers(
					{ estimationForTeams: inPoints },
					{ estimationForTeams: { ...inPoints, ...difference } },
					withFollowers({ estimationForTeams: 4 }),
				),
			).toEqual(["estimationForTeams"]);
		},
	);

	it.each([
		["set to nothing", { estimationForTeams: null }],
		["removed", {}],
	])("sees a default %s as a change", (_, current) => {
		expect(
			changedEstimationDefaultsWithFollowers(
				{ estimationForTeams: inPoints },
				current,
				withFollowers({ estimationForTeams: 4 }),
			),
		).toEqual(["estimationForTeams"]);
	});

	it.each([
		["unset becoming null", {}, { estimationForTeams: null }],
		["null becoming unset", { estimationForTeams: null }, {}],
	])("counts %s as no change", (_, loaded, current) => {
		expect(
			changedEstimationDefaultsWithFollowers(
				loaded,
				current,
				withFollowers({ estimationForTeams: 4 }),
			),
		).toEqual([]);
	});
});

describe("whyUpdate", () => {
	const nothingChanged = (usage: IConnectionUsage) =>
		whyUpdate(false, [], [], usage, "Feature");

	describe("when the Additional Fields changed", () => {
		it.each([
			[{ teams: 1, portfolios: 1 }],
			[{ teams: 4, portfolios: 2 }],
			[{ teams: 3, portfolios: 0 }],
			[{ teams: 0, portfolios: 2 }],
		])("asks about everyone on a connection used by %j", (usage) => {
			expect(whyUpdate(true, [], [], usage, "Feature")).toEqual({
				because: "fields",
				...usage,
			});
		});

		it("asks nothing on a connection no Team or Portfolio uses", () => {
			expect(
				whyUpdate(true, [], [], { teams: 0, portfolios: 0 }, "Feature"),
			).toBeNull();
		});

		it("asks only the Additional Fields question when a followed default changed too", () => {
			expect(
				whyUpdate(
					true,
					["parentOverrideForTeams"],
					["estimationForTeams"],
					withFollowers({ parentOverrideForTeams: 3, estimationForTeams: 4 }),
					"Feature",
				),
			).toEqual({ because: "fields", teams: 4, portfolios: 2 });
		});

		it("still asks about the defaults on a connection nobody uses", () => {
			expect(
				whyUpdate(
					true,
					["parentOverrideForTeams"],
					[],
					{
						teams: 0,
						portfolios: 0,
						followers: { parentOverrideForTeams: 1 },
					},
					"Feature",
				),
			).toMatchObject({ because: "defaults", teams: 1 });
		});
	});

	it("asks nothing when nothing changed", () => {
		expect(
			nothingChanged(withFollowers({ parentOverrideForTeams: 3 })),
		).toBeNull();
	});

	describe("when a default read while updating changed", () => {
		it.each([
			["parentOverrideForTeams", "Parent sources", "parents", 3, 0],
			["parentOverrideForPortfolios", "Parent sources", "parents", 0, 2],
			[
				"dependencyFieldForPortfolios",
				"Dependency Field",
				"dependencies",
				0,
				2,
			],
			["sizeEstimateFieldForPortfolios", "Size Estimate Field", "sizes", 0, 2],
			["featureOwnerFieldForPortfolios", "Feature Owner Field", "owners", 0, 2],
		] as const)(
			"%s names its label and what changes",
			(name, label, changes, teams, portfolios) => {
				expect(
					whyUpdate(
						false,
						[name],
						[],
						withFollowers({ [name]: teams + portfolios }),
						"Feature",
					),
				).toEqual({
					because: "defaults",
					teams,
					portfolios,
					defaults: [label],
					changes: [changes],
				});
			},
		);

		it("names the Feature Owner Field after the word this instance uses for Feature", () => {
			expect(
				whyUpdate(
					false,
					["featureOwnerFieldForPortfolios"],
					[],
					withFollowers({ featureOwnerFieldForPortfolios: 2 }),
					"Epic",
				),
			).toEqual({
				because: "defaults",
				teams: 0,
				portfolios: 2,
				defaults: ["Epic Owner Field"],
				changes: ["owners"],
			});
		});

		it("names the Estimation Field once for both Estimation defaults, with nothing they change", () => {
			expect(
				whyUpdate(
					false,
					["estimationForTeams", "estimationForPortfolios"],
					[],
					withFollowers({ estimationForTeams: 1, estimationForPortfolios: 1 }),
					"Feature",
				),
			).toEqual({
				because: "defaults",
				teams: 1,
				portfolios: 1,
				defaults: ["Estimation Field"],
				changes: [],
			});
		});

		it("names each label once when the Teams' and the Portfolios' defaults of one kind both changed", () => {
			expect(
				whyUpdate(
					false,
					["parentOverrideForTeams", "parentOverrideForPortfolios"],
					[],
					withFollowers({
						parentOverrideForTeams: 3,
						parentOverrideForPortfolios: 2,
					}),
					"Feature",
				),
			).toEqual({
				because: "defaults",
				teams: 3,
				portfolios: 2,
				defaults: ["Parent sources"],
				changes: ["parents"],
			});
		});

		it("lists what changes for each changed default", () => {
			expect(
				whyUpdate(
					false,
					["dependencyFieldForPortfolios", "sizeEstimateFieldForPortfolios"],
					[],
					withFollowers({
						dependencyFieldForPortfolios: 3,
						sizeEstimateFieldForPortfolios: 3,
					}),
					"Feature",
				),
			).toEqual({
				because: "defaults",
				teams: 0,
				portfolios: 6,
				defaults: ["Dependency Field", "Size Estimate Field"],
				changes: ["dependencies", "sizes"],
			});
		});

		it.each([
			[
				"parentOverrideForPortfolios",
				"dependencyFieldForPortfolios",
				["Parent sources", "Dependency Field"],
				["parents", "dependencies"],
			],
			[
				"dependencyFieldForPortfolios",
				"sizeEstimateFieldForPortfolios",
				["Dependency Field", "Size Estimate Field"],
				["dependencies", "sizes"],
			],
			[
				"sizeEstimateFieldForPortfolios",
				"featureOwnerFieldForPortfolios",
				["Size Estimate Field", "Feature Owner Field"],
				["sizes", "owners"],
			],
		] as const)(
			"counts 3 Portfolios following both %s and %s once",
			(first, second, labels, changes) => {
				expect(
					whyUpdate(
						false,
						[first, second],
						[],
						withFollowers(
							{ [first]: 3, [second]: 3 },
							{ followerGroups: [{ follows: [first, second], count: 3 }] },
						),
						"Feature",
					),
				).toEqual({
					because: "defaults",
					teams: 0,
					portfolios: 3,
					defaults: labels,
					changes,
				});
			},
		);

		it("counts a whole group when it follows a changed default and another one left as it was", () => {
			expect(
				whyUpdate(
					false,
					["parentOverrideForPortfolios"],
					[],
					withFollowers(
						{ parentOverrideForPortfolios: 3, dependencyFieldForPortfolios: 3 },
						{
							followerGroups: [
								{
									follows: [
										"parentOverrideForPortfolios",
										"dependencyFieldForPortfolios",
									],
									count: 3,
								},
							],
						},
					),
					"Feature",
				),
			).toEqual({
				because: "defaults",
				teams: 0,
				portfolios: 3,
				defaults: ["Parent sources"],
				changes: ["parents"],
			});
		});

		it("leaves out a group that follows another default only", () => {
			expect(
				whyUpdate(
					false,
					["dependencyFieldForPortfolios"],
					[],
					withFollowers(
						{ dependencyFieldForPortfolios: 2 },
						{
							followerGroups: [
								{ follows: ["dependencyFieldForPortfolios"], count: 2 },
								{ follows: ["parentOverrideForPortfolios"], count: 1 },
							],
						},
					),
					"Feature",
				),
			).toEqual({
				because: "defaults",
				teams: 0,
				portfolios: 2,
				defaults: ["Dependency Field"],
				changes: ["dependencies"],
			});
		});

		it("treats every default as its own group of that size when the server sends no groups", () => {
			expect(
				whyUpdate(
					false,
					["parentOverrideForTeams", "dependencyFieldForPortfolios"],
					[],
					withFollowers({
						parentOverrideForTeams: 3,
						dependencyFieldForPortfolios: 2,
					}),
					"Feature",
				),
			).toEqual({
				because: "defaults",
				teams: 3,
				portfolios: 2,
				defaults: ["Parent sources", "Dependency Field"],
				changes: ["parents", "dependencies"],
			});
		});

		it("names each label of the defaults read while updating once", () => {
			const updating = ConnectionDefaultNames.filter(
				(name) => !name.startsWith("estimation"),
			);
			expect(
				whyUpdate(
					false,
					[...updating],
					[],
					withFollowers(Object.fromEntries(updating.map((name) => [name, 1]))),
					"Feature",
				),
			).toEqual({
				because: "defaults",
				teams: 1,
				portfolios: 4,
				defaults: [
					"Parent sources",
					"Dependency Field",
					"Size Estimate Field",
					"Feature Owner Field",
				],
				changes: ["parents", "dependencies", "sizes", "owners"],
			});
		});
	});

	describe("when only Estimation defaults changed", () => {
		it("asks just whether to save, counting the Teams that follow", () => {
			expect(
				whyUpdate(
					false,
					[],
					["estimationForTeams"],
					withFollowers({ estimationForTeams: 4 }),
					"Feature",
				),
			).toEqual({
				because: "estimation",
				teams: 4,
				portfolios: 0,
				defaults: 1,
			});
		});

		it("asks about the Portfolios that follow a changed Portfolios' Estimation default", () => {
			expect(
				whyUpdate(
					false,
					[],
					["estimationForPortfolios"],
					withFollowers({ estimationForPortfolios: 2 }),
					"Feature",
				),
			).toEqual({
				because: "estimation",
				teams: 0,
				portfolios: 2,
				defaults: 1,
			});
		});

		it("counts an owner following both Estimation defaults once", () => {
			expect(
				whyUpdate(
					false,
					[],
					["estimationForTeams", "estimationForPortfolios"],
					withFollowers(
						{ estimationForTeams: 1, estimationForPortfolios: 1 },
						{
							followerGroups: [
								{
									follows: ["estimationForTeams", "estimationForPortfolios"],
									count: 1,
								},
							],
						},
					),
					"Feature",
				),
			).toEqual({
				because: "estimation",
				teams: 1,
				portfolios: 1,
				defaults: 2,
			});
		});
	});

	it("asks only the update question when a default read while updating changed alongside an Estimation default", () => {
		expect(
			whyUpdate(
				false,
				["parentOverrideForTeams"],
				["estimationForTeams"],
				withFollowers({ parentOverrideForTeams: 3, estimationForTeams: 4 }),
				"Feature",
			),
		).toEqual({
			because: "defaults",
			teams: 3,
			portfolios: 0,
			defaults: ["Parent sources"],
			changes: ["parents"],
		});
	});
});
