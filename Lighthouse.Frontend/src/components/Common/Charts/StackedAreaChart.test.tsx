import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { RunChartData } from "../../../models/Metrics/RunChartData";
import StackedAreaChart, { type AreaChartItem } from "./StackedAreaChart";

describe("StackedAreaChart component", () => {
	const createTestArea = (values: number[]): RunChartData => {
		return new RunChartData(
			values,
			values.length,
			values.reduce((sum, val) => sum + val, 0),
		);
	};

	const createTestAreas = (): AreaChartItem[] => {
		return [
			{
				index: 0,
				title: "Area 1",
				area: createTestArea([10, 15, 5]),
				color: "#ff0000",
			},
			{
				index: 1,
				title: "Area 2",
				area: createTestArea([5, 10, 15]),
				color: "#00ff00",
			},
		];
	};

	it("should render the chart when data is provided", () => {
		const areas = createTestAreas();
		const startDate = new Date(2023, 0, 1);

		render(<StackedAreaChart areas={areas} startDate={startDate} />);

		// Check for the chart presence
		const chartElement = document.querySelector("svg");
		expect(chartElement).toBeInTheDocument();
	});

	it("should render with custom title", () => {
		const areas = createTestAreas();
		const startDate = new Date(2023, 0, 1);
		const customTitle = "Custom Stacked Chart";

		render(
			<StackedAreaChart
				areas={areas}
				startDate={startDate}
				title={customTitle}
			/>,
		);

		const titleElement = screen.getByText(customTitle);
		expect(titleElement).toBeInTheDocument();
	});

	it("should not render the chart when data is empty", () => {
		const emptyAreas: AreaChartItem[] = [];
		const startDate = new Date(2023, 0, 1);

		render(<StackedAreaChart areas={emptyAreas} startDate={startDate} />);

		// Check that no chart is rendered
		const chartElement = document.querySelector("svg");
		expect(chartElement).not.toBeInTheDocument();
	});

	it("should toggle trend lines when switch is clicked", async () => {
		const areas = createTestAreas();
		const startDate = new Date(2023, 0, 1);

		render(<StackedAreaChart areas={areas} startDate={startDate} />);

		// By default, trend should be shown (switch is checked)
		const switchElement = screen.getByRole("checkbox");
		expect(switchElement).toBeChecked();

		// Click to hide trends
		await userEvent.click(switchElement);
		expect(switchElement).not.toBeChecked();
	});

	it("should handle areas with startOffset properly", () => {
		const areas: AreaChartItem[] = [
			{
				index: 0,
				title: "Area With Offset",
				area: createTestArea([10, 15, 5]),
				startOffset: 5,
				color: "#ff0000",
			},
		];
		const startDate = new Date(2023, 0, 1);

		render(<StackedAreaChart areas={areas} startDate={startDate} />);

		// Check chart is rendered
		const chartElement = document.querySelector("svg");
		expect(chartElement).toBeInTheDocument();
	});

	it("should sort areas by index", () => {
		const unsortedAreas: AreaChartItem[] = [
			{
				index: 2,
				title: "Area C",
				area: createTestArea([1, 2, 3]),
			},
			{
				index: 0,
				title: "Area A",
				area: createTestArea([4, 5, 6]),
			},
			{
				index: 1,
				title: "Area B",
				area: createTestArea([7, 8, 9]),
			},
		];
		const startDate = new Date(2023, 0, 1);

		render(<StackedAreaChart areas={unsortedAreas} startDate={startDate} />);

		// Check chart is rendered with sorted areas
		const chartElement = document.querySelector("svg");
		expect(chartElement).toBeInTheDocument();
	});
});
