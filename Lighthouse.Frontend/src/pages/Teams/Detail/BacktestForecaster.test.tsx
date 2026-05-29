import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import dayjs from "dayjs";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import { HowManyForecast } from "../../../models/Forecasts/HowManyForecast";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import { Team } from "../../../models/Team/Team";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockTeamMetricsService,
	createMockTeamService,
} from "../../../tests/MockApiServiceProvider";
import BacktestForecaster, { type HistoricalMode } from "./BacktestForecaster";

vi.mock("./BacktestResultDisplay", () => ({
	default: ({ backtestResult }: { backtestResult: BacktestResult }) => (
		<div data-testid="backtest-result-display">
			<span>Actual: {backtestResult.actualThroughput}</span>
			<span>
				Percentiles: {backtestResult.percentiles.map((p) => p.value).join(", ")}
			</span>
		</div>
	),
}));

vi.mock("../../../components/Common/Charts/BarRunChart", () => ({
	default: ({ title }: { title: string }) => (
		<div data-testid="bar-run-chart">
			<span>{title}</span>
		</div>
	),
}));

vi.mock("../../../components/Common/LoadingAnimation/LoadingAnimation", () => ({
	default: () => <div data-testid="loading-animation">Loading...</div>,
}));

const mockCanUsePremiumFeatures = vi.fn(() => true);

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

const mockTeamMetricsService = createMockTeamMetricsService();
const mockTeamService = createMockTeamService();
const mockApiServiceContext = {
	...createMockApiServiceContext({}),
	teamMetricsService: mockTeamMetricsService,
	teamService: mockTeamService,
};

const buildTeam = (): Team => {
	const team = new Team();
	team.id = 1;
	team.useFixedDatesForThroughput = false;
	team.throughputStartDate = dayjs().subtract(29, "day").toDate();
	team.throughputEndDate = new Date();
	return team;
};

interface HarnessProps {
	onInputChange?: (complete: boolean) => void;
	onClearBacktestResult?: () => void;
	onApplyFilterOverrideChange?: (apply: boolean) => void;
	backtestResult?: BacktestResult | null;
	hasForecastFilter?: boolean;
	applyFilterOverride?: boolean;
	team?: Team;
	initialMode?: HistoricalMode;
	initialWindowDays?: number | "";
}

const ControlledHarness = ({
	onInputChange = () => {},
	onClearBacktestResult = () => {},
	onApplyFilterOverrideChange,
	backtestResult = null,
	hasForecastFilter,
	applyFilterOverride,
	team = buildTeam(),
	initialMode = "rolling",
	initialWindowDays = 30,
}: HarnessProps) => {
	const [startDate, setStartDate] = useState<dayjs.Dayjs | null>(
		dayjs().subtract(60, "day"),
	);
	const [endDate, setEndDate] = useState<dayjs.Dayjs | null>(
		dayjs().subtract(30, "day"),
	);
	const [historicalMode, setHistoricalMode] =
		useState<HistoricalMode>(initialMode);
	const [historicalWindowDays, setHistoricalWindowDays] = useState<number | "">(
		initialWindowDays,
	);
	const [historicalFixedStartDate, setHistoricalFixedStartDate] =
		useState<dayjs.Dayjs | null>(dayjs().subtract(90, "day"));
	const [historicalFixedEndDate, setHistoricalFixedEndDate] =
		useState<dayjs.Dayjs | null>(dayjs().subtract(60, "day"));

	return (
		<LocalizationProvider dateAdapter={AdapterDayjs}>
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BacktestForecaster
					team={team}
					backtestResult={backtestResult}
					onClearBacktestResult={onClearBacktestResult}
					hasForecastFilter={hasForecastFilter}
					applyFilterOverride={applyFilterOverride}
					onApplyFilterOverrideChange={onApplyFilterOverrideChange}
					startDate={startDate}
					endDate={endDate}
					historicalMode={historicalMode}
					historicalWindowDays={historicalWindowDays}
					historicalFixedStartDate={historicalFixedStartDate}
					historicalFixedEndDate={historicalFixedEndDate}
					onStartDateChange={setStartDate}
					onEndDateChange={setEndDate}
					onHistoricalModeChange={setHistoricalMode}
					onHistoricalWindowDaysChange={setHistoricalWindowDays}
					onHistoricalFixedStartDateChange={setHistoricalFixedStartDate}
					onHistoricalFixedEndDateChange={setHistoricalFixedEndDate}
					onInputChange={onInputChange}
				/>
			</ApiServiceContext.Provider>
		</LocalizationProvider>
	);
};

const buildResult = (): BacktestResult => ({
	startDate: dayjs().subtract(60, "day").toDate(),
	endDate: dayjs().subtract(30, "day").toDate(),
	historicalStartDate: dayjs().subtract(90, "day").toDate(),
	historicalEndDate: dayjs().subtract(60, "day").toDate(),
	percentiles: [
		new HowManyForecast(50, 10),
		new HowManyForecast(70, 12),
		new HowManyForecast(85, 15),
		new HowManyForecast(95, 18),
	],
	actualThroughput: 12,
});

describe("BacktestForecaster component", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		(
			mockTeamService.getTeamSettings as ReturnType<typeof vi.fn>
		).mockResolvedValue({
			throughputHistory: 30,
			useFixedDatesForThroughput: false,
			throughputHistoryStartDate: new Date(),
			throughputHistoryEndDate: new Date(),
		});
		(
			mockTeamMetricsService.getThroughput as ReturnType<typeof vi.fn>
		).mockResolvedValue(new RunChartData({}, 30, 15));
	});

	it("renders the input fields without a Run Backtest button", () => {
		render(<ControlledHarness />);

		expect(screen.getAllByText(/Backtest Start Date/i).length).toBeGreaterThan(
			0,
		);
		expect(screen.getAllByText(/Backtest End Date/i).length).toBeGreaterThan(0);
		expect(
			screen.getByLabelText(/Historical Window \(Days\)/i),
		).toBeInTheDocument();
		expect(
			screen.queryByRole("button", { name: /Run Backtest/i }),
		).not.toBeInTheDocument();
	});

	it("reports completeness when an input changes", () => {
		const onInputChange = vi.fn();
		render(<ControlledHarness onInputChange={onInputChange} />);

		fireEvent.change(screen.getByLabelText(/Historical Window \(Days\)/i), {
			target: { value: "60" },
		});

		expect(onInputChange).toHaveBeenLastCalledWith(true);
	});

	it("reports incompleteness when the historical window is cleared", () => {
		const onInputChange = vi.fn();
		render(<ControlledHarness onInputChange={onInputChange} />);

		fireEvent.change(screen.getByLabelText(/Historical Window \(Days\)/i), {
			target: { value: "" },
		});

		expect(onInputChange).toHaveBeenLastCalledWith(false);
	});

	it("allows the user to clear the field and type a new value without prepending the minimum", async () => {
		const user = userEvent.setup();
		render(<ControlledHarness />);

		const historicalWindowInput = screen.getByLabelText(
			/Historical Window \(Days\)/i,
		);
		await user.clear(historicalWindowInput);
		await user.type(historicalWindowInput, "5");

		expect(historicalWindowInput).toHaveValue(5);
	});

	it("displays tabs and the result display when a backtestResult is provided", async () => {
		render(<ControlledHarness backtestResult={buildResult()} />);

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: /Results/i })).toBeInTheDocument();
			expect(
				screen.getByRole("tab", { name: /Historical Throughput/i }),
			).toBeInTheDocument();
		});
		expect(screen.getByTestId("backtest-result-display")).toBeInTheDocument();
		expect(screen.getByText("Actual: 12")).toBeInTheDocument();
	});

	it("does not display tabs when backtestResult is null", () => {
		render(<ControlledHarness backtestResult={null} />);

		expect(
			screen.queryByRole("tab", { name: /Results/i }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("backtest-result-display"),
		).not.toBeInTheDocument();
	});

	it("fetches historical and actual throughput when a backtestResult is provided", async () => {
		render(<ControlledHarness backtestResult={buildResult()} />);

		await waitFor(() => {
			expect(mockTeamMetricsService.getThroughput).toHaveBeenCalledTimes(2);
		});
	});

	describe("Historical Window Mode Toggle", () => {
		it("renders both mode toggle buttons", () => {
			render(<ControlledHarness />);

			expect(screen.getByTestId("toggle-rolling")).toBeInTheDocument();
			expect(screen.getByTestId("toggle-date-range")).toBeInTheDocument();
		});

		it("shows Historical Window field in rolling mode and date pickers in date-range mode", async () => {
			render(<ControlledHarness />);

			expect(
				screen.getByLabelText(/Historical Window \(Days\)/i),
			).toBeInTheDocument();

			fireEvent.click(screen.getByTestId("toggle-date-range"));

			await waitFor(() => {
				expect(
					screen.getAllByText(/Historical Start Date/i).length,
				).toBeGreaterThan(0);
				expect(
					screen.queryByLabelText(/Historical Window \(Days\)/i),
				).not.toBeInTheDocument();
			});
		});
	});

	describe("Use filtered Throughput toggle", () => {
		const TOGGLE_LABEL = "Use filtered Throughput";

		afterEach(() => {
			mockCanUsePremiumFeatures.mockReturnValue(true);
		});

		it("renders the toggle when premium and team has a forecast filter", () => {
			mockCanUsePremiumFeatures.mockReturnValue(true);
			render(
				<ControlledHarness
					hasForecastFilter={true}
					applyFilterOverride={true}
				/>,
			);
			expect(screen.getByLabelText(TOGGLE_LABEL)).toBeInTheDocument();
		});

		it("hides the toggle on non-premium tenants", () => {
			mockCanUsePremiumFeatures.mockReturnValue(false);
			render(<ControlledHarness hasForecastFilter={true} />);
			expect(screen.queryByLabelText(TOGGLE_LABEL)).not.toBeInTheDocument();
		});

		it("hides the toggle when the team has no forecast filter configured", () => {
			mockCanUsePremiumFeatures.mockReturnValue(true);
			render(<ControlledHarness hasForecastFilter={false} />);
			expect(screen.queryByLabelText(TOGGLE_LABEL)).not.toBeInTheDocument();
		});

		it("notifies the parent and clears the result when the user toggles the switch", () => {
			mockCanUsePremiumFeatures.mockReturnValue(true);
			const onApplyFilterOverrideChange = vi.fn();
			const onClearBacktestResult = vi.fn();
			render(
				<ControlledHarness
					hasForecastFilter={true}
					applyFilterOverride={true}
					onApplyFilterOverrideChange={onApplyFilterOverrideChange}
					onClearBacktestResult={onClearBacktestResult}
				/>,
			);
			fireEvent.click(screen.getByLabelText(TOGGLE_LABEL));
			expect(onApplyFilterOverrideChange).toHaveBeenCalledWith(false);
			expect(onClearBacktestResult).toHaveBeenCalled();
		});

		it("requests view=filtered for both throughput fetches when filter is on, premium, and configured", async () => {
			mockCanUsePremiumFeatures.mockReturnValue(true);
			const result = buildResult();
			render(
				<ControlledHarness
					backtestResult={result}
					hasForecastFilter={true}
					applyFilterOverride={true}
				/>,
			);

			await waitFor(() => {
				expect(mockTeamMetricsService.getThroughput).toHaveBeenCalledTimes(2);
			});
			expect(mockTeamMetricsService.getThroughput).toHaveBeenNthCalledWith(
				1,
				1,
				result.historicalStartDate,
				result.historicalEndDate,
				"filtered",
			);
			expect(mockTeamMetricsService.getThroughput).toHaveBeenNthCalledWith(
				2,
				1,
				result.startDate,
				result.endDate,
				"filtered",
			);
		});

		it("omits the view argument from throughput fetches when the filter toggle is off", async () => {
			mockCanUsePremiumFeatures.mockReturnValue(true);
			const result = buildResult();
			render(
				<ControlledHarness
					backtestResult={result}
					hasForecastFilter={true}
					applyFilterOverride={false}
				/>,
			);

			await waitFor(() => {
				expect(mockTeamMetricsService.getThroughput).toHaveBeenCalledTimes(2);
			});
			expect(mockTeamMetricsService.getThroughput).toHaveBeenNthCalledWith(
				1,
				1,
				result.historicalStartDate,
				result.historicalEndDate,
			);
			expect(mockTeamMetricsService.getThroughput).toHaveBeenNthCalledWith(
				2,
				1,
				result.startDate,
				result.endDate,
			);
		});
	});
});
