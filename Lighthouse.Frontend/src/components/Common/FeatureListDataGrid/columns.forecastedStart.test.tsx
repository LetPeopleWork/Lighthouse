import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IFeature, IFeatureStart } from "../../../models/Feature";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import { createForecastedStartColumn, createForecastsColumn } from "./columns";

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

		// Drawn the way a Feature that is already done draws its completion: the same four confidence
		// levels, all carrying the same day. It reads as settled because the four agree.
		expect(screen.getAllByText("9/14/2026")).toHaveLength(4);

		for (const day of [12, 13, 14, 16]) {
			expectNotShown(inOctober(day));
		}

		// The four dates and nothing else. The column header already says what this column is, so a
		// caption inside the cell would repeat it on every row.
		expect(
			screen.getByTestId("feature-forecasted-start-cell"),
		).toHaveTextContent(/^(9\/14\/2026){4}$/);
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

	// Both states at once, which is the combination the server actually produces: it reports an observed
	// start before it asks whether the Feature can be forecast, so a started Feature keeps its start date
	// even while one of its teams has no history. A cell that asked the questions the other way round
	// would hide a date it had been handed.
	it("still shows the day a started Feature began when another team cannot be forecast", () => {
		renderStartCell(
			feature({
				teamsWithoutForecast: ["Team Meridian"],
				startForecast: {
					source: "Observed",
					observedDate: new Date(2026, 8, 14),
					percentiles: [],
				},
			}),
		);

		expect(screen.getAllByText("9/14/2026")).toHaveLength(4);
		expectNotShown("Cannot forecast");
	});

	it("renders nothing rather than a date when the start is unknown but the teams are forecastable", () => {
		renderStartCell(
			feature({ startForecast: { source: "Unknown", percentiles: [] } }),
		);

		expect(screen.queryByTestId("observed-start")).not.toBeInTheDocument();

		// Naming days the fixture never carried would assert nothing, so this asks the only question that
		// discriminates: whether any date at all reached the cell.
		expect(
			screen.queryByText(/\d{1,2}\/\d{1,2}\/\d{4}/),
		).not.toBeInTheDocument();
	});

	it("tolerates a backend payload that omits the field entirely", () => {
		renderStartCell(feature({ startForecast: undefined }));

		const cell = screen.getByTestId("feature-forecasted-start-cell");

		expect(cell).toBeInTheDocument();
		expect(screen.queryByTestId("observed-start")).not.toBeInTheDocument();

		// An empty cell, down to the last character. Anything standing in for the missing forecasts -
		// a placeholder entry, a caption - would be this column inventing something the server did not
		// say, and the test would not otherwise notice.
		expect(cell.textContent).toBe("");
	});

	// The grid identifies a column by its field: that is what the column-visibility menu toggles and
	// what an export writes out. A field naming no real property breaks both silently, because the cell
	// still draws correctly - it reads the row directly and never looks the field up.
	it("identifies itself by a property the row actually carries", () => {
		expect(Object.keys(feature())).toContain(
			createForecastedStartColumn().field,
		);
	});

	// Sorting here would order rows by the start object itself rather than by the date inside it, which
	// is not an order anyone would recognise.
	it("does not offer to sort rows by a forecast object", () => {
		expect(createForecastedStartColumn().sortable).toBe(false);
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
