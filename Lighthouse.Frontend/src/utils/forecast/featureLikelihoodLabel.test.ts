import { describe, expect, it } from "vitest";
import type { IFeatureLikelihood } from "../../models/Delivery";
import { featureLikelihoodLabel } from "./featureLikelihoodLabel";

const aLikelihood = (
	overrides: Partial<IFeatureLikelihood> = {},
): IFeatureLikelihood => ({
	featureId: 1,
	likelihoodPercentage: 62,
	hasSufficientData: true,
	...overrides,
});

describe("featureLikelihoodLabel", () => {
	it("gives the rounded percentage when there is a forecast to report", () => {
		expect(
			featureLikelihoodLabel(aLikelihood({ likelihoodPercentage: 61.7 }), true),
		).toBe("62%");
	});

	it("says it cannot forecast when a contributing team has no throughput history", () => {
		expect(
			featureLikelihoodLabel(
				aLikelihood({ teamsWithoutForecast: ["Team Pulsar"] }),
				true,
			),
		).toBe("Cannot forecast");
	});

	it("says it cannot forecast when there is no number at all", () => {
		expect(
			featureLikelihoodLabel(aLikelihood({ likelihoodPercentage: null }), true),
		).toBe("Cannot forecast");
	});

	it("says the history is too thin rather than reporting a number resting on it", () => {
		expect(
			featureLikelihoodLabel(aLikelihood({ hasSufficientData: false }), true),
		).toBe("Not enough data");
	});

	it("reports a number for a Feature with nothing left to do, however thin the history", () => {
		expect(
			featureLikelihoodLabel(
				aLikelihood({ likelihoodPercentage: 100, hasSufficientData: false }),
				false,
			),
		).toBe("100%");
	});
});
