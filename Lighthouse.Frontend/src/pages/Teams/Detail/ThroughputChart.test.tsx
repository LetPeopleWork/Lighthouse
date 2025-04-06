import { render, screen } from "@testing-library/react";
import { Throughput } from "../../../models/Forecasts/Throughput";
import ThroughputBarChart from "./ThroughputChart";

describe("ThroughputBarChart component", () => {
	it("should render BarChart when throughputData.history > 0", () => {
		const rawData = [10, 20, 30];
		const mockThroughputData = new Throughput(rawData, rawData.length, 60);

		render(
			<ThroughputBarChart
				throughput={mockThroughputData}
				startDate={new Date()}
			/>,
		);

		const svgElement = document.querySelector(
			".css-1evyvmv-MuiChartsSurface-root",
		);
		const barElements = svgElement?.querySelectorAll("rect");
		expect(barElements?.length).toBeGreaterThan(0);
	});

	it("should display the correct total throughput value", () => {
		const rawData = [10, 20, 30];
		const mockThroughputData = new Throughput(rawData, rawData.length, 60);

		render(
			<ThroughputBarChart
				throughput={mockThroughputData}
				startDate={new Date()}
			/>,
		);

		const totalThroughputText = screen.getByText("Total Throughput: 60 Items");
		expect(totalThroughputText).toBeInTheDocument();
	});

	it("should render CircularProgress when throughputData.history <= 0", () => {
		const mockThroughputData: Throughput = new Throughput([], 0, 0);

		render(
			<ThroughputBarChart
				throughput={mockThroughputData}
				startDate={new Date()}
			/>,
		);

		const circularProgressElement = screen.getByRole("progressbar");
		expect(circularProgressElement).toBeInTheDocument();
	});
});
