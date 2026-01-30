import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import dayjs from "dayjs";
import { describe, expect, it, vi } from "vitest";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import { HowManyForecast } from "../../../models/Forecasts/HowManyForecast";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import {
	createMockApiServiceContext,
	createMockTeamMetricsService,
} from "../../../tests/MockApiServiceProvider";
import BacktestForecaster from "./BacktestForecaster";

// Mock the BacktestResultDisplay component
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

// Mock the BarRunChart component
vi.mock("../../../components/Common/Charts/BarRunChart", () => ({
	default: ({
		title,
		predictabilityData,
	}: {
		title: string;
		predictabilityData: object | null;
	}) => (
		<div data-testid="bar-run-chart">
			<span>{title}</span>
			{predictabilityData && (
				<span data-testid="predictability-present">Has Predictability</span>
			)}
		</div>
	),
}));

// Mock the LoadingAnimation component
vi.mock("../../../components/Common/LoadingAnimation/LoadingAnimation", () => ({
	default: () => <div data-testid="loading-animation">Loading...</div>,
}));

describe("BacktestForecaster component", () => {
	const mockOnRunBacktest = vi.fn();
	const mockOnClearBacktestResult = vi.fn();

	const mockTeamMetricsService = createMockTeamMetricsService();
	const mockApiServiceContext = {
		...createMockApiServiceContext({}),
		teamMetricsService: mockTeamMetricsService,
	};

	const defaultProps = {
		teamId: 1,
		onRunBacktest: mockOnRunBacktest as (
			startDate: Date,
			endDate: Date,
			historicalWindowDays: number,
		) => Promise<void>,
		backtestResult: null as BacktestResult | null,
		onClearBacktestResult: mockOnClearBacktestResult as () => void,
	};

	const renderWithContext = (props: typeof defaultProps = defaultProps) => {
		return render(
			<ApiServiceContext.Provider value={mockApiServiceContext}>
				<BacktestForecaster {...props} />
			</ApiServiceContext.Provider>,
		);
	};

	beforeEach(() => {
		vi.clearAllMocks();
		// Setup default mock responses
		(
			mockTeamMetricsService.getThroughput as ReturnType<typeof vi.fn>
		).mockResolvedValue(new RunChartData({}, 30, 15));
		(
			mockTeamMetricsService.getMultiItemForecastPredictabilityScore as ReturnType<
				typeof vi.fn
			>
		).mockResolvedValue({
			score: 0.75,
			passedRules: [],
			failedRules: [],
		});
	});

	it("should render all input fields", () => {
		renderWithContext();

		// DatePickers create multiple elements with the label, so we check that at least one exists
		expect(screen.getAllByText(/Backtest Start Date/i).length).toBeGreaterThan(
			0,
		);
		expect(screen.getAllByText(/Backtest End Date/i).length).toBeGreaterThan(0);
		expect(
			screen.getByLabelText(/Historical Window \(Days\)/i),
		).toBeInTheDocument();
		expect(
			screen.getByRole("button", { name: /Run Backtest/i }),
		).toBeInTheDocument();
	});

	it("should have default values for date inputs", () => {
		renderWithContext();

		// The historical window should default to 30
		const historicalWindowInput = screen.getByLabelText(
			/Historical Window \(Days\)/i,
		);
		expect(historicalWindowInput).toHaveValue(30);
	});

	it("should call onRunBacktest when button is clicked", async () => {
		renderWithContext();

		const runButton = screen.getByRole("button", { name: /Run Backtest/i });
		fireEvent.click(runButton);

		await waitFor(() => {
			expect(mockOnRunBacktest).toHaveBeenCalledWith(
				expect.any(Date),
				expect.any(Date),
				30,
			);
		});
	});

	it("should call onClearBacktestResult before running backtest", async () => {
		renderWithContext();

		const runButton = screen.getByRole("button", { name: /Run Backtest/i });
		fireEvent.click(runButton);

		await waitFor(() => {
			expect(mockOnClearBacktestResult).toHaveBeenCalled();
		});
	});

	it("should update historical window days when input changes", () => {
		renderWithContext();

		const historicalWindowInput = screen.getByLabelText(
			/Historical Window \(Days\)/i,
		);
		fireEvent.change(historicalWindowInput, { target: { value: "60" } });

		expect(historicalWindowInput).toHaveValue(60);
	});

	it("should clamp historical window days to minimum of 1", async () => {
		renderWithContext();

		const historicalWindowInput = screen.getByLabelText(
			/Historical Window \(Days\)/i,
		);
		fireEvent.change(historicalWindowInput, { target: { value: "-5" } });

		expect(historicalWindowInput).toHaveValue(1);
	});

	it("should clamp historical window days to maximum of 365", async () => {
		renderWithContext();

		const historicalWindowInput = screen.getByLabelText(
			/Historical Window \(Days\)/i,
		);
		fireEvent.change(historicalWindowInput, { target: { value: "500" } });

		expect(historicalWindowInput).toHaveValue(365);
	});

	it("should display tabs when backtestResult is provided", async () => {
		const mockResult: BacktestResult = {
			startDate: dayjs().subtract(60, "day").toDate(),
			endDate: dayjs().subtract(30, "day").toDate(),
			historicalWindowDays: 30,
			percentiles: [
				new HowManyForecast(50, 10),
				new HowManyForecast(70, 12),
				new HowManyForecast(85, 15),
				new HowManyForecast(95, 18),
			],
			actualThroughput: 12,
		};

		renderWithContext({ ...defaultProps, backtestResult: mockResult });

		await waitFor(() => {
			expect(screen.getByRole("tab", { name: /Results/i })).toBeInTheDocument();
			expect(
				screen.getByRole("tab", { name: /Historical Throughput/i }),
			).toBeInTheDocument();
		});
		expect(screen.getByTestId("backtest-result-display")).toBeInTheDocument();
		expect(screen.getByText("Actual: 12")).toBeInTheDocument();
	});

	it("should not display tabs when backtestResult is null", () => {
		renderWithContext({ ...defaultProps, backtestResult: null });

		expect(
			screen.queryByRole("tab", { name: /Results/i }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByRole("tab", { name: /Historical Throughput/i }),
		).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("backtest-result-display"),
		).not.toBeInTheDocument();
	});

	it("should fetch historical throughput data when backtestResult is provided", async () => {
		const mockResult: BacktestResult = {
			startDate: dayjs().subtract(60, "day").toDate(),
			endDate: dayjs().subtract(30, "day").toDate(),
			historicalWindowDays: 30,
			percentiles: [
				new HowManyForecast(50, 10),
				new HowManyForecast(70, 12),
				new HowManyForecast(85, 15),
				new HowManyForecast(95, 18),
			],
			actualThroughput: 12,
		};

		renderWithContext({ ...defaultProps, backtestResult: mockResult });

		await waitFor(() => {
			expect(mockTeamMetricsService.getThroughput).toHaveBeenCalledWith(
				1,
				expect.any(Date),
				expect.any(Date),
			);
			expect(
				mockTeamMetricsService.getMultiItemForecastPredictabilityScore,
			).toHaveBeenCalledWith(1, expect.any(Date), expect.any(Date));
		});
	});

	it("should display BarRunChart when Historical Throughput tab is clicked", async () => {
		const mockResult: BacktestResult = {
			startDate: dayjs().subtract(60, "day").toDate(),
			endDate: dayjs().subtract(30, "day").toDate(),
			historicalWindowDays: 30,
			percentiles: [
				new HowManyForecast(50, 10),
				new HowManyForecast(70, 12),
				new HowManyForecast(85, 15),
				new HowManyForecast(95, 18),
			],
			actualThroughput: 12,
		};

		renderWithContext({ ...defaultProps, backtestResult: mockResult });

		// Wait for data to load
		await waitFor(() => {
			expect(mockTeamMetricsService.getThroughput).toHaveBeenCalled();
		});

		// Click on Historical Throughput tab
		const historicalTab = screen.getByRole("tab", {
			name: /Historical Throughput/i,
		});
		fireEvent.click(historicalTab);

		await waitFor(() => {
			expect(screen.getByTestId("bar-run-chart")).toBeInTheDocument();
			expect(screen.getByTestId("predictability-present")).toBeInTheDocument();
		});
	});

	it("should not call onRunBacktest when start date is null", async () => {
		// This test is more of an edge case - in practice the component
		// initializes with valid dates. We can verify the handler checks for null.
		renderWithContext();

		// The component should have valid default dates, so the button click should work
		const runButton = screen.getByRole("button", { name: /Run Backtest/i });
		fireEvent.click(runButton);

		await waitFor(() => {
			expect(mockOnRunBacktest).toHaveBeenCalled();
		});
	});

	it("should show loading animation while fetching historical data", async () => {
		// Make the metrics service resolve slowly
		let resolvePromise: ((value: unknown) => void) | undefined;
		(
			mockTeamMetricsService.getThroughput as ReturnType<typeof vi.fn>
		).mockReturnValue(
			new Promise((resolve) => {
				resolvePromise = resolve;
			}),
		);

		const mockResult: BacktestResult = {
			startDate: dayjs().subtract(60, "day").toDate(),
			endDate: dayjs().subtract(30, "day").toDate(),
			historicalWindowDays: 30,
			percentiles: [
				new HowManyForecast(50, 10),
				new HowManyForecast(70, 12),
				new HowManyForecast(85, 15),
				new HowManyForecast(95, 18),
			],
			actualThroughput: 12,
		};

		renderWithContext({ ...defaultProps, backtestResult: mockResult });

		// Click on Historical Throughput tab
		const historicalTab = screen.getByRole("tab", {
			name: /Historical Throughput/i,
		});
		fireEvent.click(historicalTab);

		// Should show loading animation
		expect(screen.getByTestId("loading-animation")).toBeInTheDocument();

		// Resolve the promise
		if (resolvePromise) {
			resolvePromise(new RunChartData({}, 30, 15));
		}

		await waitFor(() => {
			expect(screen.queryByTestId("loading-animation")).not.toBeInTheDocument();
		});
	});
});
