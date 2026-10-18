import { render, screen } from "@testing-library/react";
import type dayjs from "dayjs";
import type React from "react";
import { describe, expect, it, vi } from "vitest";
import { ManualForecast } from "../../../models/Forecasts/ManualForecast";
import { WhenForecast } from "../../../models/Forecasts/WhenForecast";
import ManualForecaster from "./ManualForecaster";

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: { canUsePremiumFeatures: true },
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

vi.mock("@mui/x-date-pickers/DatePicker", () => ({
	DatePicker: () => <div data-testid="date-picker-container" />,
}));

vi.mock("@mui/x-date-pickers/LocalizationProvider", () => ({
	LocalizationProvider: ({ children }: { children: React.ReactNode }) => (
		<div>{children}</div>
	),
	AdapterDayjs: () => null,
}));

const pendingEpic6311 =
	"Pending: a forecast asked for by hand words a percentile beyond the horizon (epic 6311, slice 03)";

const percentile = (probability: number, beyondHorizon: boolean) => {
	const forecast = WhenForecast.new(
		probability,
		beyondHorizon
			? new Date("2031-10-18T00:00:00Z")
			: new Date("2029-03-14T00:00:00Z"),
	);
	forecast.beyondHorizon = beyondHorizon;
	forecast.horizonYears = 5;
	return forecast;
};

/** 480 Work Items for a Team that delivers about one every ten days: only the 50% has a day. */
const theAnswer = () =>
	new ManualForecast(
		480,
		new Date("2027-01-01T00:00:00Z"),
		[
			percentile(50, false),
			percentile(70, true),
			percentile(85, true),
			percentile(95, true),
		],
		[],
		0,
	);

const renderTheForecaster = () =>
	render(
		<ManualForecaster
			remainingItems={480}
			targetDate={null as dayjs.Dayjs | null}
			manualForecastResult={theAnswer()}
			forecastInputCandidates={null}
			onRemainingItemsChange={vi.fn()}
			onTargetDateChange={vi.fn()}
		/>,
	);

describe("the manual forecast's answer when percentiles are beyond the horizon", () => {
	it.skip(`reads "More than 5 years" for each beyond percentile (${pendingEpic6311})`, () => {
		renderTheForecaster();

		expect(screen.getAllByText(/^More than 5 years$/)).toHaveLength(3);
		expect(screen.queryByText(/2031/)).not.toBeInTheDocument();
	});

	/** Already true; the answer is shown, and the percentile that has a day keeps it. */
	it("shows the answer with the date of the percentile that has one", () => {
		renderTheForecaster();

		expect(
			screen.getByText(/^When will 480 .+ be done\?$/),
		).toBeInTheDocument();
		expect(screen.getAllByText(/2029/).length).toBeGreaterThan(0);
	});
});
