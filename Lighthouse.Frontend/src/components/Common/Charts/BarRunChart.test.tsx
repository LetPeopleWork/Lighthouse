import { render, screen } from "@testing-library/react";
import { RunChartData } from "../../../models/Forecasts/RunChartData";
import BarRunChart from "./BarRunChart";

describe("ThroughputBarChart component", () => {
	it("should render BarChart when throughputData.history > 0", () => {
		const rawData = [10, 20, 30];
		const mockThroughputData = new RunChartData(rawData, rawData.length, 60);

		render(
			<BarRunChart chartData={mockThroughputData} startDate={new Date()} />,
		);

		const svgElement = document.querySelector(
			".css-1evyvmv-MuiChartsSurface-root",
		);
		const barElements = svgElement?.querySelectorAll("rect");
		expect(barElements?.length).toBeGreaterThan(0);
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

	it("should render CircularProgress when throughputData.history <= 0", () => {
		const mockThroughputData: RunChartData = new RunChartData([], 0, 0);

		render(
			<BarRunChart chartData={mockThroughputData} startDate={new Date()} />,
		);

		const circularProgressElement = screen.getByRole("progressbar");
		expect(circularProgressElement).toBeInTheDocument();
	});
});
