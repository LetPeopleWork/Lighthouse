import { fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
	ApiServiceContext,
	type IApiServiceContext,
} from "../../../services/Api/ApiServiceContext";
import type { IForecastService } from "../../../services/Api/ForecastService";
import { createMockApiServiceContext } from "../../../tests/MockApiServiceProvider";
import TeamForecastForm from "./TeamForecastForm";

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

const getMockManualForecastResponse = (
	overrides: { filterApplied?: boolean; excludedSummary?: string } = {},
) => ({
	remainingItems: 5,
	targetDate: new Date(),
	whenForecasts: [],
	howManyForecasts: [],
	likelihood: 0,
	filterApplied: overrides.filterApplied ?? false,
	excludedSummary: overrides.excludedSummary,
});

const getMockForecastService = (
	response: ReturnType<
		typeof getMockManualForecastResponse
	> = getMockManualForecastResponse(),
): IForecastService => ({
	runManualForecast: vi.fn().mockResolvedValue(response),
	runItemPrediction: vi.fn(),
	runBacktest: vi.fn(),
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
			<TeamForecastForm teamId={42} hasFilter={props.hasFilter} />
		</ApiServiceContext.Provider>,
	);
	return forecastService;
};

const TOGGLE_LABEL = "Apply forecast-throughput filter";

describe("TeamForecastForm — Apply forecast-throughput filter toggle", () => {
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

		fireEvent.change(screen.getByLabelText("Remaining items"), {
			target: { value: "10" },
		});
		fireEvent.click(screen.getByLabelText(TOGGLE_LABEL));
		fireEvent.click(screen.getByRole("button", { name: /forecast/i }));

		await vi.waitFor(() => {
			expect(forecastService.runManualForecast).toHaveBeenCalledWith(
				42,
				10,
				null,
				false,
			);
		});
	});

	it("submitting the form with toggle On sends applyFilterOverride=true", async () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		const forecastService = getMockForecastService();
		renderForm({ hasFilter: true, forecastService });

		fireEvent.change(screen.getByLabelText("Remaining items"), {
			target: { value: "10" },
		});
		fireEvent.click(screen.getByRole("button", { name: /forecast/i }));

		await vi.waitFor(() => {
			expect(forecastService.runManualForecast).toHaveBeenCalledWith(
				42,
				10,
				null,
				true,
			);
		});
	});
});

describe("TeamForecastForm — FilteredThroughputChip on result panel", () => {
	afterEach(() => {
		vi.clearAllMocks();
		mockCanUsePremiumFeatures.mockReturnValue(true);
	});

	it("renders the chip on the result panel when the forecast response has filterApplied=true", async () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		const forecastService = getMockForecastService(
			getMockManualForecastResponse({
				filterApplied: true,
				excludedSummary: 'Type = Bug; Tags contains "maintenance"',
			}),
		);
		renderForm({ hasFilter: true, forecastService });

		fireEvent.click(screen.getByRole("button", { name: /forecast/i }));

		expect(await screen.findByText("Filtered throughput")).toBeInTheDocument();
	});

	it("does not render the chip when the forecast response has filterApplied=false", async () => {
		mockCanUsePremiumFeatures.mockReturnValue(true);
		const forecastService = getMockForecastService(
			getMockManualForecastResponse({ filterApplied: false }),
		);
		renderForm({ hasFilter: true, forecastService });

		fireEvent.click(screen.getByRole("button", { name: /forecast/i }));

		await vi.waitFor(() => {
			expect(forecastService.runManualForecast).toHaveBeenCalled();
		});
		expect(screen.queryByText("Filtered throughput")).not.toBeInTheDocument();
	});
});
