import { describe, expect, it } from "vitest";
import type { IFeatureDependency } from "../../models/FeatureDependency";
import { featureWarningSentences } from "./featureWarningSentences";

const TERMS = {
	workItemsTerm: "Work Items",
	featureTerm: "Feature",
	portfolioTerm: "Portfolio",
	teamTerm: "Team",
};

const NOTHING_WRONG = {
	isDoneWithRemainingWork: false,
	isUsingDefaultFeatureSize: false,
};

const aDependency = (
	overrides: Partial<IFeatureDependency> = {},
): IFeatureDependency => ({
	referenceId: "FTR-9",
	name: "Warehouse sync",
	url: null,
	source: "TrackerLink",
	notHonouredReason: null,
	blockerPositionedBelow: false,
	isWithheld: false,
	...overrides,
});

describe("featureWarningSentences", () => {
	it("says nothing about a Feature with nothing wrong with it", () => {
		expect(featureWarningSentences(NOTHING_WRONG, TERMS)).toEqual([]);
	});

	it("names work left on a Feature somebody has already called done", () => {
		const warnings = featureWarningSentences(
			{ ...NOTHING_WRONG, isDoneWithRemainingWork: true },
			TERMS,
		);

		expect(warnings).toEqual([
			"This feature is marked as done but still has remaining work items. Please verify if all work has been completed.",
		]);
	});

	it("explains a size nobody supplied in the words this instance uses", () => {
		const warnings = featureWarningSentences(
			{ ...NOTHING_WRONG, isUsingDefaultFeatureSize: true },
			{
				workItemsTerm: "Tickets",
				featureTerm: "Epic",
				portfolioTerm: "Stream",
				teamTerm: "Squad",
			},
		);

		expect(warnings).toEqual([
			"No child Tickets were found for this Epic. The remaining Tickets displayed are based on the default Epic size specified in the advanced project settings.",
		]);
	});

	it("keeps quiet about a dependency there is nothing wrong with", () => {
		const warnings = featureWarningSentences(
			{ ...NOTHING_WRONG, dependencies: [aDependency()] },
			TERMS,
		);

		expect(warnings).toEqual([]);
	});

	it("explains a dependency Lighthouse will not act on", () => {
		const warnings = featureWarningSentences(
			{
				...NOTHING_WRONG,
				dependencies: [aDependency({ notHonouredReason: "InALoop" })],
			},
			TERMS,
		);

		expect(warnings).toEqual([
			"This Feature and Warehouse sync are waiting on each other. That dependency is not included in the forecast.",
		]);
	});

	it("explains a dependency sitting below the Feature waiting on it", () => {
		const warnings = featureWarningSentences(
			{
				...NOTHING_WRONG,
				dependencies: [aDependency({ blockerPositionedBelow: true })],
			},
			TERMS,
		);

		expect(warnings).toEqual([
			"This Feature depends on Warehouse sync, which sits below it in the order.",
		]);
	});

	it("withholds the name of a dependency the reader may not see", () => {
		const warnings = featureWarningSentences(
			{
				...NOTHING_WRONG,
				dependencies: [
					aDependency({ isWithheld: true, blockerPositionedBelow: true }),
				],
			},
			TERMS,
		);

		expect(warnings).toEqual([
			"This Feature depends on a Feature you do not have access to, which sits below it in the order.",
		]);
	});

	it("stays silent about a Portfolio that has set its dependencies aside", () => {
		const warnings = featureWarningSentences(
			{
				...NOTHING_WRONG,
				dependencies: [
					aDependency({ notHonouredReason: "IgnoredByPortfolio" }),
				],
			},
			TERMS,
		);

		expect(warnings).toEqual([]);
	});

	it("collects every reason a Feature needs attention, in the order they are read", () => {
		const warnings = featureWarningSentences(
			{
				isDoneWithRemainingWork: true,
				isUsingDefaultFeatureSize: true,
				dependencies: [aDependency({ notHonouredReason: "InALoop" })],
			},
			TERMS,
		);

		expect(warnings).toHaveLength(3);
		expect(warnings[0]).toContain("marked as done");
		expect(warnings[1]).toContain("No child Work Items");
		expect(warnings[2]).toContain("waiting on each other");
	});
});
