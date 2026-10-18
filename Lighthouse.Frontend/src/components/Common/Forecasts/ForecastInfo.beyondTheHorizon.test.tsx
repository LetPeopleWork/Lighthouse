import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import type { IWhenForecast } from "../../../models/Forecasts/WhenForecast";
import ForecastInfo from "./ForecastInfo";

const pendingEpic6311 =
	"Pending: a percentile beyond the horizon reads 'More than 5 years' (epic 6311, slice 03)";

/**
 * Some of a Feature's simulated runs never finish within the horizon, so the percentiles above the share
 * that did finish have no date anybody forecast. The forecast cell says so in words, keeps the level icon,
 * and its tooltip says why. It never shows the stand-in date the backend sends for older clients, and it
 * is not "Cannot forecast", which means a Team with no throughput at all.
 */
describe("ForecastInfo, a percentile beyond the horizon", () => {
	const theStandInDate = new Date("2031-10-18T00:00:00Z");

	const beyond = (probability: number, horizonYears = 5): IWhenForecast => ({
		probability,
		expectedDate: theStandInDate,
		beyondHorizon: true,
		horizonYears,
	});

	it.skip(`reads "More than 5 years" instead of a date (${pendingEpic6311})`, () => {
		render(<ForecastInfo forecast={beyond(85)} />);

		expect(screen.getByText(/^More than 5 years$/)).toBeInTheDocument();
		expect(screen.queryByText(/2031/)).not.toBeInTheDocument();
	});

	it.skip(`takes the years from the forecast, not from a number of its own (${pendingEpic6311})`, () => {
		render(<ForecastInfo forecast={beyond(85, 7)} />);

		expect(screen.getByText(/^More than 7 years$/)).toBeInTheDocument();
	});

	it.skip(`keeps the level icon and says why in the tooltip (${pendingEpic6311})`, async () => {
		render(<ForecastInfo forecast={beyond(85)} />);

		await userEvent.hover(screen.getByText(/^More than 5 years$/));

		expect(
			await screen.findByText(/^Confident \(85% Chance\)$/),
		).toBeInTheDocument();
		expect(
			screen.getByText(
				/^Fewer than 85% of simulated runs finished within 5 years\.$/,
			),
		).toBeInTheDocument();
		expect(
			screen.getByTestId("CheckCircleOutlineOutlinedIcon"),
		).toBeInTheDocument();
	});

	/** Already true; it has to stay true once the words are added. */
	it(`is never worded as "Cannot forecast"`, () => {
		const { container } = render(<ForecastInfo forecast={beyond(95)} />);

		expect(container.querySelector("svg")).not.toBeNull();
		expect(container.textContent?.trim()).not.toBe("");
		expect(screen.queryByText(/cannot forecast/i)).not.toBeInTheDocument();
	});

	/** Already true; the guard that the words appear only when the backend says the percentile is beyond. */
	it("still shows the date of a percentile that is within the horizon", () => {
		const withinTheHorizon: IWhenForecast = {
			probability: 50,
			expectedDate: new Date("2029-03-14T00:00:00Z"),
			beyondHorizon: false,
			horizonYears: 5,
		};

		render(<ForecastInfo forecast={withinTheHorizon} />);

		expect(screen.getByText(/2029/)).toBeInTheDocument();
		expect(screen.queryByText(/More than/)).not.toBeInTheDocument();
	});
});
