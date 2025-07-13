import { createTheme, ThemeProvider } from "@mui/material/styles";
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import PredictabilityScore from "./PredictabilityScore";

// Create a default theme for testing
const theme = createTheme();

const TestWrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
	<ThemeProvider theme={theme}>{children}</ThemeProvider>
);

describe("PredictabilityScore", () => {
	const mockDataWithResults: IForecastPredictabilityScore = {
		predictabilityScore: 0.75,
		percentiles: [
			{ percentile: 50, value: 5 },
			{ percentile: 70, value: 7 },
			{ percentile: 85, value: 10 },
			{ percentile: 95, value: 15 },
		],
		forecastResults: new Map([
			[3, 1],
			[5, 5],
			[7, 12],
			[10, 6],
			[15, 1],
		]),
	};

	const mockDataWithoutResults: IForecastPredictabilityScore = {
		predictabilityScore: 0.45,
		percentiles: [
			{ percentile: 50, value: 5 },
			{ percentile: 70, value: 7 },
			{ percentile: 85, value: 10 },
			{ percentile: 95, value: 15 },
		],
		forecastResults: new Map(),
	};

	it("renders the component with default title", () => {
		render(
			<TestWrapper>
				<PredictabilityScore data={mockDataWithResults} />
			</TestWrapper>,
		);

		expect(
			screen.getByRole("heading", { name: "Predictability Score" }),
		).toBeInTheDocument();
	});

	it("renders with custom title", () => {
		render(
			<TestWrapper>
				<PredictabilityScore
					data={mockDataWithResults}
					title="Custom Predictability"
				/>
			</TestWrapper>,
		);

		expect(
			screen.getByRole("heading", { name: "Custom Predictability" }),
		).toBeInTheDocument();
	});

	it("displays predictability score as percentage", () => {
		render(
			<TestWrapper>
				<PredictabilityScore data={mockDataWithResults} />
			</TestWrapper>,
		);

		expect(screen.getByText("75.0%")).toBeInTheDocument();
	});

	it("displays score with one decimal place", () => {
		const dataWithDecimal: IForecastPredictabilityScore = {
			...mockDataWithResults,
			predictabilityScore: 0.876,
		};

		render(
			<TestWrapper>
				<PredictabilityScore data={dataWithDecimal} />
			</TestWrapper>,
		);

		expect(screen.getByText("87.6%")).toBeInTheDocument();
	});

	it("renders bar chart when forecast results exist", () => {
		const { container } = render(
			<TestWrapper>
				<PredictabilityScore data={mockDataWithResults} />
			</TestWrapper>,
		);

		// First, let's check that we have results
		expect(mockDataWithResults.forecastResults.size).toBeGreaterThan(0);

		// Check for the presence of SVG chart elements
		const chartSvg = container.querySelector("svg");
		expect(chartSvg).toBeInTheDocument();
	});

	it("does not render bar chart when no forecast results", () => {
		const { container } = render(
			<TestWrapper>
				<PredictabilityScore data={mockDataWithoutResults} />
			</TestWrapper>,
		);

		// Check that no chart SVG exists
		const chartSvg = container.querySelector("svg");
		expect(chartSvg).toBeNull();
	});

	it("renders percentile lines when forecast results exist", () => {
		render(
			<TestWrapper>
				<PredictabilityScore data={mockDataWithResults} />
			</TestWrapper>,
		);

		// Check for percentile reference lines by looking for the specific percentile labels
		expect(screen.getByText("50%")).toBeInTheDocument();
		expect(screen.getByText("70%")).toBeInTheDocument();
		expect(screen.getByText("85%")).toBeInTheDocument();
		expect(screen.getByText("95%")).toBeInTheDocument();
	});

	it("does not render percentile lines when no forecast results", () => {
		render(
			<TestWrapper>
				<PredictabilityScore data={mockDataWithoutResults} />
			</TestWrapper>,
		);

		// Check that no percentile lines exist
		expect(screen.queryByTestId("percentile-line-50")).not.toBeInTheDocument();
		expect(screen.queryByTestId("percentile-line-70")).not.toBeInTheDocument();
		expect(screen.queryByTestId("percentile-line-85")).not.toBeInTheDocument();
		expect(screen.queryByTestId("percentile-line-95")).not.toBeInTheDocument();

		// Check that no percentile labels exist
		expect(screen.queryByText("50%")).not.toBeInTheDocument();
		expect(screen.queryByText("70%")).not.toBeInTheDocument();
		expect(screen.queryByText("85%")).not.toBeInTheDocument();
		expect(screen.queryByText("95%")).not.toBeInTheDocument();
	});

	describe("score color coding", () => {
		it("applies certain color for score >= 0.8", () => {
			const certainData: IForecastPredictabilityScore = {
				...mockDataWithResults,
				predictabilityScore: 0.85,
			};

			render(
				<TestWrapper>
					<PredictabilityScore data={certainData} />
				</TestWrapper>,
			);

			const scoreElement = screen.getByText("85.0%");
			expect(scoreElement).toHaveStyle({ color: "rgb(56, 142, 60)" }); // certain color
		});

		it("applies confident color for score >= 0.6 and < 0.75", () => {
			const confidentData: IForecastPredictabilityScore = {
				...mockDataWithResults,
				predictabilityScore: 0.7,
			};

			render(
				<TestWrapper>
					<PredictabilityScore data={confidentData} />
				</TestWrapper>,
			);

			const scoreElement = screen.getByText("70.0%");
			expect(scoreElement).toHaveStyle({ color: "rgb(76, 175, 80)" }); // confident color
		});

		it("applies realistic color for score >= 0.5 and < 0.6", () => {
			const realisticData: IForecastPredictabilityScore = {
				...mockDataWithResults,
				predictabilityScore: 0.58,
			};

			render(
				<TestWrapper>
					<PredictabilityScore data={realisticData} />
				</TestWrapper>,
			);

			const scoreElement = screen.getByText("58.0%");
			expect(scoreElement).toHaveStyle({ color: "rgb(255, 152, 0)" }); // realistic color
		});

		it("applies risky color for score < 0.6", () => {
			const riskyData: IForecastPredictabilityScore = {
				...mockDataWithResults,
				predictabilityScore: 0.45,
			};

			render(
				<TestWrapper>
					<PredictabilityScore data={riskyData} />
				</TestWrapper>,
			);

			const scoreElement = screen.getByText("45.0%");
			expect(scoreElement).toHaveStyle({ color: "rgb(244, 67, 54)" }); // risky color
		});
	});

	it("handles empty percentiles array", () => {
		const dataWithoutPercentiles: IForecastPredictabilityScore = {
			predictabilityScore: 0.75,
			percentiles: [],
			forecastResults: new Map([
				[3, 1],
				[5, 5],
			]),
		};

		const { container } = render(
			<TestWrapper>
				<PredictabilityScore data={dataWithoutPercentiles} />
			</TestWrapper>,
		);

		// Should still render the chart but without percentile lines
		const chartSvg = container.querySelector("svg");
		expect(chartSvg).toBeInTheDocument();
	});

	it("handles zero predictability score", () => {
		const zeroScoreData: IForecastPredictabilityScore = {
			...mockDataWithResults,
			predictabilityScore: 0,
		};

		render(
			<TestWrapper>
				<PredictabilityScore data={zeroScoreData} />
			</TestWrapper>,
		);

		expect(screen.getByText("0.0%")).toBeInTheDocument();
	});

	it("handles perfect predictability score", () => {
		const perfectScoreData: IForecastPredictabilityScore = {
			...mockDataWithResults,
			predictabilityScore: 1.0,
		};

		render(
			<TestWrapper>
				<PredictabilityScore data={perfectScoreData} />
			</TestWrapper>,
		);

		expect(screen.getByText("100.0%")).toBeInTheDocument();
	});
});
