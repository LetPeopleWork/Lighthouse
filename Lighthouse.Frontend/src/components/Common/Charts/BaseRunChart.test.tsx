import { render, screen } from "@testing-library/react";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import { generateWorkItemMapForRunChart } from "../../../tests/TestDataProvider";
import BaseRunChart from "./BaseRunChart";

describe("BaseRunChart component", () => {
	it("should render children when chartData.history > 0", () => {
		const rawData = [10, 20, 30];
		const mockChartData = new RunChartData(
			generateWorkItemMapForRunChart(rawData),
			rawData.length,
			60,
		);
		const childContent = "Test Chart Content";

		render(
			<BaseRunChart chartData={mockChartData} startDate={new Date()}>
				{() => <div data-testid="chart-content">{childContent}</div>}
			</BaseRunChart>,
		);

		const contentElement = screen.getByTestId("chart-content");
		expect(contentElement).toBeInTheDocument();
		expect(contentElement).toHaveTextContent(childContent);
	});

	it("should pass correctly formatted data to children", () => {
		const rawData = [10, 20, 30];
		const mockChartData = new RunChartData(
			generateWorkItemMapForRunChart(rawData),
			rawData.length,
			60,
		);
		const startDate = new Date("2023-01-01");
		let passedData: { value: number; day: string }[] = [];

		render(
			<BaseRunChart chartData={mockChartData} startDate={startDate}>
				{(data) => {
					passedData = data;
					return <div data-testid="chart-content" />;
				}}
			</BaseRunChart>,
		);

		expect(passedData.length).toBe(3);
		expect(passedData[0].value).toBe(10);
		expect(passedData[1].value).toBe(20);
		expect(passedData[2].value).toBe(30);

		// Check the dates
		const expectedDate1 = new Date("2023-01-01").toLocaleDateString();
		const expectedDate2 = new Date("2023-01-02").toLocaleDateString();
		const expectedDate3 = new Date("2023-01-03").toLocaleDateString();

		expect(passedData[0].day).toBe(expectedDate1);
		expect(passedData[1].day).toBe(expectedDate2);
		expect(passedData[2].day).toBe(expectedDate3);
	});

	it("should display the title correctly", () => {
		const rawData = [10, 20, 30];
		const mockChartData = new RunChartData(
			generateWorkItemMapForRunChart(rawData),
			rawData.length,
			60,
		);
		const customTitle = "Custom Base Chart";

		render(
			<BaseRunChart
				chartData={mockChartData}
				startDate={new Date()}
				title={customTitle}
			>
				{() => <div />}
			</BaseRunChart>,
		);

		const titleElement = screen.getByText(customTitle);
		expect(titleElement).toBeInTheDocument();
	});

	it("should display total when displayTotal is true", () => {
		const rawData = [10, 20, 30];
		const mockChartData = new RunChartData(
			generateWorkItemMapForRunChart(rawData),
			rawData.length,
			60,
		);

		render(
			<BaseRunChart
				chartData={mockChartData}
				startDate={new Date()}
				displayTotal={true}
			>
				{() => <div />}
			</BaseRunChart>,
		);

		const totalElement = screen.getByText("Total: 60 Items");
		expect(totalElement).toBeInTheDocument();
	});

	it("should display 'No data available' when no percentiles are provided", () => {
		const mockChartData = new RunChartData([], 0, 0);

		render(
			<BaseRunChart chartData={mockChartData} startDate={new Date()}>
				{() => <div>This should not render</div>}
			</BaseRunChart>,
		);

		expect(screen.getByText("No data available")).toBeInTheDocument();
	});
});
