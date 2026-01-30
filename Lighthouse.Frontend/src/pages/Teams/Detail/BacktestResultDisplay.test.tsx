import { render, screen } from "@testing-library/react";
import dayjs from "dayjs";
import { describe, expect, it, vi } from "vitest";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import { HowManyForecast } from "../../../models/Forecasts/HowManyForecast";
import BacktestResultDisplay from "./BacktestResultDisplay";

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
