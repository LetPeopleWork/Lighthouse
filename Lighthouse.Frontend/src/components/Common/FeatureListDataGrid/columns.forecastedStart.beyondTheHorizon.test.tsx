import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IFeature } from "../../../models/Feature";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import { createForecastedStartColumn } from "./columns";

const percentile = (probability: number, beyondHorizon: boolean) => {
	const forecast = WhenForecast.new(
		probability,
		beyondHorizon ? new Date(2031, 9, 18) : new Date(2026, 9, 12),
	);
	forecast.beyondHorizon = beyondHorizon;
	forecast.horizonYears = 5;
	return forecast;
};

/**
 * Invoice PDF waits behind Billing Migration, which never finishes in four runs of every ten, so work on
 * Invoice PDF begins in only six of every ten: its 50% start has a day and the three above it do not.
 */
const invoicePdf = (): IFeature =>
	({
		id: 2,
		name: "Invoice PDF",
		forecasts: [],
		teamsWithoutForecast: [],
		startForecast: {
			source: "Forecast",
			percentiles: [
				percentile(50, false),
				percentile(70, true),
				percentile(85, true),
				percentile(95, true),
			],
		},
	}) as unknown as IFeature;

const renderStartCell = (row: IFeature) => {
	const column = createForecastedStartColumn();
	render(column.renderCell?.({ row, value: undefined }));
};

describe("the forecasted start of a Feature whose start is beyond the horizon", () => {
	it(`reads "More than 5 years" for each beyond percentile and keeps the 50% date`, () => {
		renderStartCell(invoicePdf());

		expect(screen.getAllByText(/^More than 5 years$/)).toHaveLength(3);
		expect(screen.getByText("10/12/2026")).toBeInTheDocument();
		expect(screen.queryByText("10/18/2031")).not.toBeInTheDocument();
	});

	/** Already true; the cell is drawn, and it is not the "Cannot forecast" a Team without history gets. */
	it("is drawn as a forecast, not as one that cannot be made", () => {
		renderStartCell(invoicePdf());

		expect(screen.getByText("10/12/2026")).toBeInTheDocument();
		expect(screen.queryByText(/cannot forecast/i)).not.toBeInTheDocument();
	});
});
