import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IFeatureLikelihood } from "../../../models/Delivery";
import { FeatureLikelihoodChip } from "./FeatureLikelihoodChip";

const featureLikelihood = (
	overrides: Partial<IFeatureLikelihood> = {},
): IFeatureLikelihood => ({
	featureId: 1,
	likelihoodPercentage: 62,
	hasSufficientData: true,
	...overrides,
});

describe("FeatureLikelihoodChip", () => {
	it("shows the rounded likelihood when the feature can be forecast", () => {
		render(
			<FeatureLikelihoodChip
				featureLikelihood={featureLikelihood()}
				hasRemainingWork={true}
			/>,
		);

		expect(screen.getByText("62%")).toBeInTheDocument();
	});

	it("says it cannot forecast instead of showing a number when a team has no throughput", () => {
		render(
			<FeatureLikelihoodChip
				featureLikelihood={featureLikelihood({
					likelihoodPercentage: null,
					teamsWithoutForecast: ["Team Pulsar"],
				})}
				hasRemainingWork={true}
			/>,
		);

		expect(screen.getByText("Cannot forecast")).toBeInTheDocument();
		expect(screen.queryByText(/%/)).not.toBeInTheDocument();
	});

	it("names the team that could not be forecast, so the reader knows which gap to close", () => {
		render(
			<FeatureLikelihoodChip
				featureLikelihood={featureLikelihood({
					likelihoodPercentage: null,
					teamsWithoutForecast: ["Team Pulsar"],
				})}
				hasRemainingWork={true}
			/>,
		);

		expect(
			screen.getByLabelText(
				"No throughput history for Team Pulsar. Forecast unavailable until that team has data.",
			),
		).toBeInTheDocument();
	});

	it("never renders 100% for an un-forecastable feature", () => {
		// The whole point of ADR-112: the empty-histogram path used to report total confidence.
		render(
			<FeatureLikelihoodChip
				featureLikelihood={featureLikelihood({
					likelihoodPercentage: null,
					teamsWithoutForecast: ["Team Pulsar"],
				})}
				hasRemainingWork={true}
			/>,
		);

		expect(screen.queryByText("100%")).not.toBeInTheDocument();
		expect(screen.queryByText(">95%")).not.toBeInTheDocument();
	});

	it("still reports insufficient data when the forecast merely rests on thin history", () => {
		render(
			<FeatureLikelihoodChip
				featureLikelihood={featureLikelihood({ hasSufficientData: false })}
				hasRemainingWork={true}
			/>,
		);

		expect(screen.queryByText("Cannot forecast")).not.toBeInTheDocument();
		expect(screen.queryByText("62%")).not.toBeInTheDocument();
	});

	it("shows the percentage once there is no remaining work to be insufficient about", () => {
		render(
			<FeatureLikelihoodChip
				featureLikelihood={featureLikelihood({ hasSufficientData: false })}
				hasRemainingWork={false}
			/>,
		);

		expect(screen.getByText("62%")).toBeInTheDocument();
	});

	it("prefers cannot-forecast over insufficient data when both apply", () => {
		// Unknown outranks thin history: one says no forecast exists, the other that a
		// forecast exists but rests on little data.
		render(
			<FeatureLikelihoodChip
				featureLikelihood={featureLikelihood({
					likelihoodPercentage: null,
					teamsWithoutForecast: ["Team Meridian"],
					hasSufficientData: false,
				})}
				hasRemainingWork={true}
			/>,
		);

		expect(screen.getByText("Cannot forecast")).toBeInTheDocument();
	});
});
