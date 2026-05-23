import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import type { IForecastService } from "../../../services/Api/ForecastService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import BacktestForm from "./BacktestForm";

const mockCanUsePremiumFeatures = vi.fn();

vi.mock("../../../hooks/useLicenseRestrictions", () => ({
	useLicenseRestrictions: () => ({
		canCreateTeam: true,
		canUpdateTeamData: true,
		canCreatePortfolio: true,
		canUpdatePortfolioData: true,
		licenseStatus: { canUsePremiumFeatures: mockCanUsePremiumFeatures() },
		maxTeamsWithoutPremium: 3,
		maxPortfoliosWithoutPremium: 1,
	}),
}));

const getMockBacktestResponse = (
	overrides: { filterApplied?: boolean; excludedSummary?: string } = {},
) => ({
	startDate: new Date("2024-01-01"),
	endDate: new Date("2024-01-31"),
	historicalStartDate: new Date("2023-12-01"),
	historicalEndDate: new Date("2023-12-31"),
	percentiles: [],
	actualThroughput: 10,
	filterApplied: overrides.filterApplied ?? false,
	excludedSummary: overrides.excludedSummary,
});

const getMockForecastService = (
	response: ReturnType<
		typeof getMockBacktestResponse
	> = getMockBacktestResponse(),
): IForecastService => ({
	runManualForecast: vi.fn(),
	runItemPrediction: vi.fn(),
	runBacktest: vi.fn().mockResolvedValue(response),
});

const renderForm = (props: {
	hasFilter: boolean;
	forecastService?: IForecastService;
}) => {
	const forecastService = props.forecastService ?? getMockForecastService();
	const ctx: IApiServiceContext = createMockApiServiceContext({
		forecastService,
	});
	render(
		<ApiServiceContext.Provider value={ctx}>
			<BacktestForm teamId={42} hasFilter={props.hasFilter} />
		</ApiServiceContext.Provider>,
	);
	return forecastService;
};

const TOGGLE_LABEL = "Apply forecast-throughput filter";

describe("BacktestForm — Apply forecast-throughput filter toggle", () => {
	afterEach(() => {
		vi.clearAllMocks();
		mockCanUsePremiumFeatures.mockReturnValue(true);
	});

	it("renders the toggle only on premium tenants where the team has a non-empty filter", () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		renderForm({ hasFilter: true });
		expect(screen.getByLabelText(TOGGLE_LABEL)).toBeInTheDocument();
	});

	it("hides the toggle when the tenant is non-premium", () => {
		mockCanUsePremiumFeatures.mockReturnValue(false);
		renderForm({ hasFilter: true });
		expect(screen.queryByLabelText(TOGGLE_LABEL)).not.toBeInTheDocument();
	});

	it("hides the toggle when the team has no filter configured", () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		renderForm({ hasFilter: false });
		expect(screen.queryByLabelText(TOGGLE_LABEL)).not.toBeInTheDocument();
	});

	it("defaults the toggle to On when visible", () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		renderForm({ hasFilter: true });
		expect(screen.getByLabelText(TOGGLE_LABEL)).toBeChecked();
	});

	it("submitting the form with toggle Off sends applyFilterOverride=false", async () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		const forecastService = getMockForecastService();
		renderForm({ hasFilter: true, forecastService });

		fireEvent.click(screen.getByLabelText(TOGGLE_LABEL));
		fireEvent.click(screen.getByRole("button", { name: /run backtest/i }));

		await vi.waitFor(() => {
			expect(forecastService.runBacktest).toHaveBeenCalledWith(
				42,
				expect.any(Date),
				expect.any(Date),
				expect.any(Date),
				expect.any(Date),
				false,
			);
		});
	});
});

describe("BacktestForm — FilteredThroughputChip on backtest result view", () => {
	afterEach(() => {
		vi.clearAllMocks();
		mockCanUsePremiumFeatures.mockReturnValue(true);
	});

	it("shows the FilteredThroughputChip on the backtest result view when filterApplied=true", async () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		const forecastService = getMockForecastService(
			getMockBacktestResponse({
				filterApplied: true,
				excludedSummary: 'Type = Bug; Tags contains "maintenance"',
			}),
		);
		renderForm({ hasFilter: true, forecastService });

		fireEvent.click(screen.getByRole("button", { name: /run backtest/i }));

		expect(await screen.findByText("Filtered throughput")).toBeInTheDocument();
	});
});
