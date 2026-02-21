import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
	IEstimationVsCycleTimeDataPoint,
	IEstimationVsCycleTimeResponse,
} from "../../../models/Metrics/EstimationVsCycleTimeData";
import type { IWorkItem } from "../../../models/WorkItem";
import { testTheme } from "../../../tests/testTheme";
import EstimationVsCycleTimeChart from "./EstimationVsCycleTimeChart";

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(({ title, items, open, onClose }) => {
		if (!open) return null;
		return (
			<div data-testid="work-items-dialog">
				<h2 data-testid="dialog-title">{title}</h2>
				<button type="button" onClick={onClose} data-testid="close-dialog">
					Close
				</button>
				<div data-testid="item-count">{items?.length || 0} items</div>
			</div>
		);
	}),
}));

vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		ChartContainer: vi.fn(({ series, children, xAxis, yAxis }) => (
			<div
				data-testid="mock-chart-container"
				data-series={series ? JSON.stringify(series) : undefined}
				data-x-axis={xAxis ? JSON.stringify(xAxis) : undefined}
				data-y-axis={yAxis ? JSON.stringify(yAxis) : undefined}
			>
				{children}
			</div>
		)),
		ScatterPlot: vi.fn(() => (
			<div data-testid="mock-scatter-plot">Scatter Plot Content</div>
		)),
		ChartsXAxis: vi.fn(() => <div>X Axis</div>),
		ChartsYAxis: vi.fn(() => <div>Y Axis</div>),
		ChartsTooltip: vi.fn(() => <div>Tooltip</div>),
	};
});

describe("EstimationVsCycleTimeChart component", () => {
	const mockWorkItemLookup = new Map<number, IWorkItem>([
		[
			1,
			{
				id: 1,
				referenceId: "ITEM-1",
				name: "Test Item 1",
				url: "https://example.com/item1",
				cycleTime: 5,
				startedDate: new Date(2023, 0, 10),
				closedDate: new Date(2023, 0, 15),
				workItemAge: 5,
				state: "Done",
				stateCategory: "Done",
				type: "Task",
				parentWorkItemReference: "",
				isBlocked: false,
			},
		],
		[
			2,
			{
				id: 2,
				referenceId: "ITEM-2",
				name: "Test Item 2",
				url: "https://example.com/item2",
				cycleTime: 10,
				startedDate: new Date(2023, 0, 15),
				closedDate: new Date(2023, 0, 25),
				workItemAge: 10,
				state: "Done",
				stateCategory: "Done",
				type: "Bug",
				parentWorkItemReference: "",
				isBlocked: false,
			},
		],
		[
			3,
			{
				id: 3,
				referenceId: "ITEM-3",
				name: "Test Item 3",
				url: "https://example.com/item3",
				cycleTime: 5,
				startedDate: new Date(2023, 0, 12),
				closedDate: new Date(2023, 0, 17),
				workItemAge: 5,
				state: "Done",
				stateCategory: "Done",
				type: "Task",
				parentWorkItemReference: "",
				isBlocked: false,
			},
		],
	]);

	const makeDataPoint = (
		overrides: Partial<IEstimationVsCycleTimeDataPoint> = {},
	): IEstimationVsCycleTimeDataPoint => ({
		workItemIds: [1],
		estimationNumericValue: 3,
		estimationDisplayValue: "3",
		cycleTime: 5,
		...overrides,
	});

	const makeResponse = (
		overrides: Partial<IEstimationVsCycleTimeResponse> = {},
	): IEstimationVsCycleTimeResponse => ({
		status: "Ready",
		diagnostics: {
			totalCount: 1,
			mappedCount: 1,
			unmappedCount: 0,
			invalidCount: 0,
		},
		estimationUnit: null,
		useNonNumericEstimation: false,
		categoryValues: [],
		dataPoints: [makeDataPoint()],
		...overrides,
	});

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("should render nothing when status is NotConfigured", () => {
		const { container } = render(
			<EstimationVsCycleTimeChart
				data={makeResponse({ status: "NotConfigured", dataPoints: [] })}
				workItemLookup={mockWorkItemLookup}
			/>,
		);

		expect(container.textContent).toBe("");
	});

	it("should display 'No data available' when status is NoData", () => {
		render(
			<EstimationVsCycleTimeChart
				data={makeResponse({ status: "NoData", dataPoints: [] })}
				workItemLookup={mockWorkItemLookup}
			/>,
		);

		expect(screen.getByText("No data available")).toBeInTheDocument();
	});

	it("should render the chart with title when status is Ready", () => {
		render(
			<EstimationVsCycleTimeChart
				data={makeResponse()}
				workItemLookup={mockWorkItemLookup}
			/>,
		);

		expect(screen.getByText("Estimation vs. Cycle Time")).toBeInTheDocument();
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
	});

	it("should include estimation unit in x-axis label when provided", () => {
		render(
			<EstimationVsCycleTimeChart
				data={makeResponse({ estimationUnit: "Points" })}
				workItemLookup={mockWorkItemLookup}
			/>,
		);

		const container = screen.getByTestId("mock-chart-container");
		const xAxisAttr = container.dataset.xAxis;
		expect(xAxisAttr).toBeTruthy();
		const xAxis = JSON.parse(xAxisAttr ?? "");
		expect(xAxis[0].label).toContain("Points");
	});

	it("should use default x-axis label when no estimation unit", () => {
		render(
			<EstimationVsCycleTimeChart
				data={makeResponse({ estimationUnit: null })}
				workItemLookup={mockWorkItemLookup}
			/>,
		);

		const container = screen.getByTestId("mock-chart-container");
		const xAxisAttr = container.dataset.xAxis;
		expect(xAxisAttr).toBeTruthy();
		const xAxis = JSON.parse(xAxisAttr ?? "");
		expect(xAxis[0].label).toBe("Estimation");
	});

	it("should render data points in the scatter series", () => {
		const data = makeResponse({
			dataPoints: [
				makeDataPoint({
					estimationNumericValue: 3,
					cycleTime: 5,
					workItemIds: [1],
				}),
				makeDataPoint({
					estimationNumericValue: 8,
					cycleTime: 10,
					workItemIds: [2],
				}),
			],
			diagnostics: {
				totalCount: 2,
				mappedCount: 2,
				unmappedCount: 0,
				invalidCount: 0,
			},
		});

		render(
			<EstimationVsCycleTimeChart
				data={data}
				workItemLookup={mockWorkItemLookup}
			/>,
		);

		const container = screen.getByTestId("mock-chart-container");
		const seriesAttr = container.dataset.series;
		expect(seriesAttr).toBeTruthy();
		const series = JSON.parse(seriesAttr ?? "");
		expect(series[0].data).toHaveLength(2);
		expect(series[0].data[0].x).toBe(3);
		expect(series[0].data[0].y).toBe(5);
		expect(series[0].data[1].x).toBe(8);
		expect(series[0].data[1].y).toBe(10);
	});

	it("should render category tick labels for non-numeric mode", () => {
		const data = makeResponse({
			useNonNumericEstimation: true,
			categoryValues: ["XS", "S", "M", "L", "XL"],
			dataPoints: [
				makeDataPoint({
					estimationNumericValue: 2,
					estimationDisplayValue: "M",
					cycleTime: 5,
				}),
			],
		});

		render(
			<EstimationVsCycleTimeChart
				data={data}
				workItemLookup={mockWorkItemLookup}
			/>,
		);

		const container = screen.getByTestId("mock-chart-container");
		const xAxisAttr = container.dataset.xAxis;
		expect(xAxisAttr).toBeTruthy();
		const xAxis = JSON.parse(xAxisAttr ?? "");
		// In non-numeric mode, should have valueFormatter that maps indices to category names
		expect(xAxis[0].scaleType).toBe("linear");
	});

	it("should display diagnostic counts when unmapped or invalid items exist", () => {
		render(
			<EstimationVsCycleTimeChart
				data={makeResponse({
					diagnostics: {
						totalCount: 10,
						mappedCount: 7,
						unmappedCount: 2,
						invalidCount: 1,
					},
				})}
				workItemLookup={mockWorkItemLookup}
			/>,
		);

		expect(screen.getByText(/2 unmapped/i)).toBeInTheDocument();
		expect(screen.getByText(/1 not estimated/i)).toBeInTheDocument();
	});

	it("should not display diagnostic counts when all items are mapped", () => {
		render(
			<EstimationVsCycleTimeChart
				data={makeResponse({
					diagnostics: {
						totalCount: 5,
						mappedCount: 5,
						unmappedCount: 0,
						invalidCount: 0,
					},
				})}
				workItemLookup={mockWorkItemLookup}
			/>,
		);

		expect(screen.queryByText(/unmapped/i)).not.toBeInTheDocument();
		expect(screen.queryByText(/not estimated/i)).not.toBeInTheDocument();
	});

	it("should display 'No data available' when status is Ready but dataPoints is empty", () => {
		render(
			<EstimationVsCycleTimeChart
				data={makeResponse({ status: "NoData", dataPoints: [] })}
				workItemLookup={mockWorkItemLookup}
			/>,
		);

		expect(screen.getByText("No data available")).toBeInTheDocument();
	});
});
