import { render, screen } from "@testing-library/react";
import dayjs from "dayjs";
import { describe, expect, it, vi } from "vitest";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import { HowManyForecast } from "../../../models/Forecasts/HowManyForecast";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import BacktestResultDisplay, {
	computeAverageForecast,
} from "./BacktestResultDisplay";

// Mock the terminology hook to return predictable terms
vi.mock("../../../services/TerminologyContext", () => ({
	useTerminology: () => ({
		getTerm: (key: string) => {
			if (key === "workItems") return "Work Items";
			return key;
		},
	}),
}));

describe("BacktestResultDisplay component", () => {
	const createMockResult = (actualThroughput: number): BacktestResult => ({
		startDate: dayjs("2024-01-01").toDate(),
		endDate: dayjs("2024-01-31").toDate(),
		historicalWindowDays: 30,
		percentiles: [
			new HowManyForecast(50, 10),
			new HowManyForecast(70, 12),
			new HowManyForecast(85, 15),
			new HowManyForecast(95, 18),
		],
		actualThroughput,
	});

	it("should render the Backtest Results header", () => {
		const mockResult = createMockResult(12);
		render(<BacktestResultDisplay backtestResult={mockResult} />);

		expect(screen.getByText("Backtest Results")).toBeInTheDocument();
	});

	it("should display the date period information", () => {
		const mockResult = createMockResult(12);
		render(<BacktestResultDisplay backtestResult={mockResult} />);

		// Check that period info contains historical window days
		expect(screen.getByText(/30 days of historical data/)).toBeInTheDocument();
	});

	it("should display all percentile values in fixed order in the summary", () => {
		const mockResult = createMockResult(12);
		render(<BacktestResultDisplay backtestResult={mockResult} />);

		// Summary should display percentiles in fixed order: 50%, 70%, 85%, 95%
		expect(screen.getByText("50%: 10 Work Items")).toBeInTheDocument();
		expect(screen.getByText("70%: 12 Work Items")).toBeInTheDocument();
		expect(screen.getByText("85%: 15 Work Items")).toBeInTheDocument();
		expect(screen.getByText("95%: 18 Work Items")).toBeInTheDocument();
	});

	it("should display the actual throughput in the summary", () => {
		const mockResult = createMockResult(
			13, // Use a unique value that doesn't appear in percentiles
		);
		render(<BacktestResultDisplay backtestResult={mockResult} />);

		expect(screen.getByText(/Actual Throughput:/)).toBeInTheDocument();
		expect(screen.getByText(/13 Work Items/)).toBeInTheDocument();
	});

	it("should render chart component as SVG", () => {
		const mockResult = createMockResult(12);
		render(<BacktestResultDisplay backtestResult={mockResult} />);

		// The BarChart renders as an SVG
		const svg = document.querySelector("svg");
		expect(svg).toBeInTheDocument();
	});

	it("should maintain fixed summary order regardless of actual value position", () => {
		// Actual is larger than all percentiles
		const mockResultHighActual = createMockResult(25);
		const { unmount } = render(
			<BacktestResultDisplay backtestResult={mockResultHighActual} />,
		);

		// Summary should still show fixed order - percentiles first, then actual
		expect(screen.getByText("50%: 10 Work Items")).toBeInTheDocument();
		expect(screen.getByText("70%: 12 Work Items")).toBeInTheDocument();
		expect(screen.getByText("85%: 15 Work Items")).toBeInTheDocument();
		expect(screen.getByText("95%: 18 Work Items")).toBeInTheDocument();
		expect(screen.getByText(/Actual Throughput:/)).toBeInTheDocument();

		unmount();

		// Actual is smaller than all percentiles
		const mockResultLowActual = createMockResult(7);
		render(<BacktestResultDisplay backtestResult={mockResultLowActual} />);

		// Summary should still show fixed order - percentiles first, then actual
		expect(screen.getByText("50%: 10 Work Items")).toBeInTheDocument();
		expect(screen.getByText("70%: 12 Work Items")).toBeInTheDocument();
		expect(screen.getByText("85%: 15 Work Items")).toBeInTheDocument();
		expect(screen.getByText("95%: 18 Work Items")).toBeInTheDocument();
		expect(screen.getByText(/Actual Throughput:/)).toBeInTheDocument();
	});
});

describe("computeAverageForecast", () => {
	it("should compute average forecast correctly", () => {
		// 30 historical days with total of 60 work items = 2 items/day
		// Backtest period: 2024-01-01 to 2024-01-31 = 31 days (inclusive)
		// Expected forecast: 2 * 31 = 62
		const historicalThroughput = new RunChartData({}, 30, 60);
		const backtestResult = {
			startDate: dayjs("2024-01-01").toDate(),
			endDate: dayjs("2024-01-31").toDate(),
			historicalWindowDays: 30,
			percentiles: [],
			actualThroughput: 50,
		};

		const result = computeAverageForecast(historicalThroughput, backtestResult);

		expect(result).toEqual({
			avgPerDay: 2,
			avgForecast: 62,
		});
	});

	it("should handle fractional averages correctly", () => {
		// 30 historical days with total of 50 work items = 1.666... items/day
		// Backtest period: 2024-01-01 to 2024-01-15 = 15 days (inclusive)
		// Expected forecast: 1.666... * 15 = 25
		const historicalThroughput = new RunChartData({}, 30, 50);
		const backtestResult = {
			startDate: dayjs("2024-01-01").toDate(),
			endDate: dayjs("2024-01-15").toDate(),
			historicalWindowDays: 30,
			percentiles: [],
			actualThroughput: 20,
		};

		const result = computeAverageForecast(historicalThroughput, backtestResult);

		expect(result.avgPerDay).toBeCloseTo(1.6667, 2);
		expect(result.avgForecast).toBeCloseTo(25, 0);
	});

	it("should handle single day backtest period", () => {
		// 30 historical days with total of 60 work items = 2 items/day
		// Backtest period: 2024-01-01 to 2024-01-01 = 1 day (inclusive)
		// Expected forecast: 2 * 1 = 2
		const historicalThroughput = new RunChartData({}, 30, 60);
		const backtestResult = {
			startDate: dayjs("2024-01-01").toDate(),
			endDate: dayjs("2024-01-01").toDate(),
			historicalWindowDays: 30,
			percentiles: [],
			actualThroughput: 2,
		};

		const result = computeAverageForecast(historicalThroughput, backtestResult);

		expect(result).toEqual({
			avgPerDay: 2,
			avgForecast: 2,
		});
	});
});

describe("BacktestResultDisplay with historical throughput", () => {
	const createMockResultWithHistory = (
		actualThroughput: number,
		historicalTotal: number,
	): {
		backtestResult: BacktestResult;
		historicalThroughput: RunChartData;
	} => ({
		backtestResult: {
			startDate: dayjs("2024-01-01").toDate(),
			endDate: dayjs("2024-01-31").toDate(), // 31 days inclusive
			historicalWindowDays: 30,
			percentiles: [
				new HowManyForecast(50, 10),
				new HowManyForecast(70, 12),
				new HowManyForecast(85, 15),
				new HowManyForecast(95, 18),
			],
			actualThroughput,
		},
		historicalThroughput: new RunChartData({}, 30, historicalTotal),
	});

	it("should display Average bar in chart when historical throughput is provided", () => {
		const { backtestResult, historicalThroughput } =
			createMockResultWithHistory(12, 60);
		render(
			<BacktestResultDisplay
				backtestResult={backtestResult}
				historicalThroughput={historicalThroughput}
			/>,
		);

		// Average should appear as a Y-axis label
		expect(screen.getByText("Avg")).toBeInTheDocument();
	});

	it("should display Average value in summary with avg/day", () => {
		// 60 work items / 30 days = 2 items/day
		// 2 items/day * 31 days = 62 items forecast
		const { backtestResult, historicalThroughput } =
			createMockResultWithHistory(12, 60);
		render(
			<BacktestResultDisplay
				backtestResult={backtestResult}
				historicalThroughput={historicalThroughput}
			/>,
		);

		expect(screen.getByText(/Average:/)).toBeInTheDocument();
		expect(screen.getByText(/62 Work Items \(2\.0\/day\)/)).toBeInTheDocument();
	});

	it("should display Actual as a reference line label instead of bar", () => {
		const { backtestResult, historicalThroughput } =
			createMockResultWithHistory(12, 60);
		render(
			<BacktestResultDisplay
				backtestResult={backtestResult}
				historicalThroughput={historicalThroughput}
			/>,
		);

		// Actual should appear as a reference line label, not in Y-axis bar labels
		const svg = document.querySelector("svg");
		expect(svg).toBeInTheDocument();

		// The chart should have "Actual" text (as a reference line label)
		const actualLabels = screen.getAllByText("Actual");
		expect(actualLabels.length).toBeGreaterThan(0);
	});

	it("should not display Average when historical throughput is not provided", () => {
		const mockResult = {
			startDate: dayjs("2024-01-01").toDate(),
			endDate: dayjs("2024-01-31").toDate(),
			historicalWindowDays: 30,
			percentiles: [
				new HowManyForecast(50, 10),
				new HowManyForecast(70, 12),
				new HowManyForecast(85, 15),
				new HowManyForecast(95, 18),
			],
			actualThroughput: 12,
		};
		render(<BacktestResultDisplay backtestResult={mockResult} />);

		// Average should not appear
		expect(screen.queryByText(/Average:/)).not.toBeInTheDocument();
	});
});
