import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IFeature, IFeatureStart } from "../../../models/Feature";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import { createForecastedStartColumn, createForecastsColumn } from "./columns";
import { OBSERVED_START_LABEL } from "./ForecastedStartCell";

const aPercentile = (probability: number, day: number) =>
	WhenForecast.new(probability, new Date(2026, 9, day));

const forecastStartingOn = (day: number): IFeatureStart => ({
	source: "Forecast",
	percentiles: [
		aPercentile(50, day),
		aPercentile(70, day + 1),
		aPercentile(85, day + 2),
		aPercentile(95, day + 4),
	],
});

const feature = (overrides: Partial<IFeature> = {}): IFeature =>
	({
		id: 1,
		name: "Deep Sea Mapping Initiative",
		forecasts: [],
		teamsWithoutForecast: [],
		startForecast: forecastStartingOn(12),
		...overrides,
	}) as IFeature;

const renderStartCell = (row: IFeature) => {
	const column = createForecastedStartColumn();
	render(column.renderCell?.({ row, value: undefined }));
};

// The cell renders dates, not probabilities - the confidence level lives in a tooltip that only opens
// on hover. Asserting the dates is therefore the only way to tell four percentiles from three, and it
// keeps the negative assertions honest: "no percentile shown" has to name what would have been shown.
const inOctober = (day: number) => `10/${day}/2026`;

const expectShown = (text: string) =>
	expect(screen.getByText(text)).toBeInTheDocument();

const expectNotShown = (text: string) =>
	expect(screen.queryByText(text)).not.toBeInTheDocument();

describe("createForecastedStartColumn", () => {
	it("shows a start percentile for every confidence level the completion column shows", () => {
		renderStartCell(feature());

		expect(
			screen.getByTestId("feature-forecasted-start-cell"),
		).toBeInTheDocument();

		for (const day of [12, 13, 14, 16]) {
			expectShown(inOctober(day));
		}
	});

	it("shows a started Feature the day it actually started, with no percentile beside it", () => {
		renderStartCell(
			feature({
				startForecast: {
					source: "Observed",
					observedDate: new Date(2026, 8, 14),
					percentiles: [],
				},
			}),
		);

		expect(screen.getByTestId("observed-start")).toHaveTextContent("9/14/2026");

		for (const day of [12, 13, 14, 16]) {
			expectNotShown(inOctober(day));
		}
	});

	it("says a started Feature has started in the cell rather than in a tooltip", () => {
		renderStartCell(
			feature({
				startForecast: {
					source: "Observed",
					observedDate: new Date(2026, 8, 14),
					percentiles: [],
				},
			}),
		);

		expect(screen.getByTestId("observed-start")).toHaveTextContent(
			OBSERVED_START_LABEL,
		);
	});

	it("reuses the empty state the completion column already uses when a team cannot be forecast", () => {
		renderStartCell(feature({ teamsWithoutForecast: ["Team Meridian"] }));

		expect(screen.getByText("Cannot forecast")).toBeInTheDocument();
	});

	it("names the team that could not be forecast, exactly as the completion column does", () => {
		renderStartCell(feature({ teamsWithoutForecast: ["Team Meridian"] }));

		expect(
			screen.getByLabelText(
				"No throughput history for Team Meridian. Forecast unavailable until that team has data.",
			),
		).toBeInTheDocument();
	});

	it("renders nothing rather than a date when the start is unknown but the teams are forecastable", () => {
		renderStartCell(
			feature({ startForecast: { source: "Unknown", percentiles: [] } }),
		);

		expect(screen.queryByTestId("observed-start")).not.toBeInTheDocument();

		for (const day of [12, 13, 14, 16]) {
			expectNotShown(inOctober(day));
		}
	});

	it("tolerates a backend payload that omits the field entirely", () => {
		renderStartCell(feature({ startForecast: undefined }));

		expect(
			screen.getByTestId("feature-forecasted-start-cell"),
		).toBeInTheDocument();
		expect(screen.queryByTestId("observed-start")).not.toBeInTheDocument();
	});

	it("leaves the completion column showing exactly what it showed before", () => {
		const row = feature({
			forecasts: [aPercentile(50, 20), aPercentile(85, 24)],
			startForecast: forecastStartingOn(12),
		});

		const completion = createForecastsColumn();
		render(completion.renderCell?.({ row, value: undefined }));

		expect(screen.getByTestId("feature-forecast-cell")).toBeInTheDocument();

		expectShown(inOctober(20));
		expectShown(inOctober(24));

		// The start dates are on the row and must not leak into the completion cell.
		for (const day of [12, 13, 14, 16]) {
			expectNotShown(inOctober(day));
		}
	});
});
