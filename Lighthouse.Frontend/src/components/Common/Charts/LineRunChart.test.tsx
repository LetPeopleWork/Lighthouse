import { render, screen } from "@testing-library/react";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import LineRunChart from "./LineRunChart";

describe("LineRunChart component", () => {
	it("should render LineChart when chartData.history > 0", () => {
		const rawData = [10, 20, 30];
		const mockChartData = new RunChartData(rawData, rawData.length, 60);

		render(<LineRunChart chartData={mockChartData} startDate={new Date()} />);

		// Look for any chart SVG element instead of specific CSS class
		const chartElement = document.querySelector("svg");
		expect(chartElement).toBeInTheDocument();
	});

	it("should display the correct total value", () => {
		const rawData = [10, 20, 30];
		const mockChartData = new RunChartData(rawData, rawData.length, 60);

		render(
			<LineRunChart
				chartData={mockChartData}
				startDate={new Date()}
				displayTotal={true}
			/>,
		);

		const totalText = screen.getByText("Total: 60 Items");
		expect(totalText).toBeInTheDocument();
	});

	it("should display 'No data available' when no percentiles are provided", () => {
		const mockChartData: RunChartData = new RunChartData([], 0, 0);

		render(<LineRunChart chartData={mockChartData} startDate={new Date()} />);

		expect(screen.getByText("No data available")).toBeInTheDocument();
	});

	it("should render with custom title", () => {
		const rawData = [10, 20, 30];
		const mockChartData = new RunChartData(rawData, rawData.length, 60);
		const customTitle = "Custom Line Chart";

		render(
			<LineRunChart
				chartData={mockChartData}
				startDate={new Date()}
				title={customTitle}
			/>,
		);

		const titleElement = screen.getByText(customTitle);
		expect(titleElement).toBeInTheDocument();
	});
});
