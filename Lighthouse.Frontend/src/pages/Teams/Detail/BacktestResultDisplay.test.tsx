import { render, screen } from "@testing-library/react";
import dayjs from "dayjs";
import { describe, expect, it } from "vitest";
import type { BacktestResult } from "../../../models/Forecasts/BacktestResult";
import { HowManyForecast } from "../../../models/Forecasts/HowManyForecast";
import BacktestResultDisplay from "./BacktestResultDisplay";

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

	it("should display all percentile values", () => {
		const mockResult = createMockResult(12);
		render(<BacktestResultDisplay backtestResult={mockResult} />);

		expect(screen.getByText("50%: 10 items")).toBeInTheDocument();
		expect(screen.getByText("70%: 12 items")).toBeInTheDocument();
		expect(screen.getByText("85%: 15 items")).toBeInTheDocument();
		expect(screen.getByText("95%: 18 items")).toBeInTheDocument();
	});

	it("should display the actual throughput", () => {
		const mockResult = createMockResult(
			13, // Use a unique value that doesn't appear in percentiles
		);
		render(<BacktestResultDisplay backtestResult={mockResult} />);

		expect(screen.getByText(/Actual Throughput:/)).toBeInTheDocument();
		expect(screen.getByText(/13 items/)).toBeInTheDocument();
	});

	it("should render chart component", () => {
		const mockResult = createMockResult(12);
		render(<BacktestResultDisplay backtestResult={mockResult} />);

		// The BarChart renders as an SVG
		const svg = document.querySelector("svg");
		expect(svg).toBeInTheDocument();
	});
});
