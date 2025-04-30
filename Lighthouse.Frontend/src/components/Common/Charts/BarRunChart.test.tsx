import { render, screen } from "@testing-library/react";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import BarRunChart from "./BarRunChart";

describe("ThroughputBarChart component", () => {
	it("should render BarChart when throughputData.history > 0", () => {
		const rawData = [10, 20, 30];
		const mockThroughputData = new RunChartData(rawData, rawData.length, 60);

		render(
			<BarRunChart chartData={mockThroughputData} startDate={new Date()} />,
		);

		// Look for any chart SVG element instead of specific CSS class
		const chartElement = document.querySelector("svg");
		expect(chartElement).toBeInTheDocument();
	});

	it("should display the correct total throughput value", () => {
		const rawData = [10, 20, 30];
		const mockThroughputData = new RunChartData(rawData, rawData.length, 60);

		render(
			<BarRunChart
				chartData={mockThroughputData}
				startDate={new Date()}
				displayTotal={true}
			/>,
		);

		const totalThroughputText = screen.getByText("Total: 60 Items");
		expect(totalThroughputText).toBeInTheDocument();
	});

	it("should display 'No data available' when no percentiles are provided", () => {
		const mockThroughputData: RunChartData = new RunChartData([], 0, 0);

		render(
			<BarRunChart chartData={mockThroughputData} startDate={new Date()} />,
		);

		expect(screen.getByText("No data available")).toBeInTheDocument();
	});
});
