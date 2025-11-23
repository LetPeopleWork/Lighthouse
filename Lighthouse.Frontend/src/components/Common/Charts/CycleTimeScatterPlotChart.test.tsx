import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import { testTheme } from "../../../tests/testTheme";
import { errorColor, getColorMapForKeys } from "../../../utils/theme/colors";
import CycleTimeScatterPlotChart from "./CycleTimeScatterPlotChart";

// Mock the Material-UI theme
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

// Mock WorkItemsDialog component
vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(({ title, items, open, onClose }) => {
		if (!open) return null;
		return (
			<div data-testid="work-items-dialog">
				<h2>{title}</h2>
				<button type="button" onClick={onClose}>
					Close
				</button>
				<table>
					<thead>
						<tr>
							<th>Name</th>
							<th>Type</th>
							<th>State</th>
							<th>Cycle Time</th>
						</tr>
					</thead>
					<tbody>
						{items?.map((item: IWorkItem) => (
							<tr key={item.id}>
								<td>{item.name}</td>
								<td>{item.type}</td>
								<td>{item.state}</td>
								<td>{item.cycleTime} days</td>
							</tr>
						))}
					</tbody>
				</table>
			</div>
		);
	}),
}));

// Mock the MUI-X Charts and expose series prop for assertions
vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		ChartContainer: vi.fn(({ series, children }) => (
			<div
				data-testid="mock-chart-container"
				data-series={series ? JSON.stringify(series) : undefined}
			>
				{children}
			</div>
		)),
		ScatterPlot: vi.fn(() => {
			return (
				<div data-testid="mock-scatter-plot">
					{/* We don't use slots here as we'll test handleShowItems differently */}
					<div>Scatter Plot Content</div>
				</div>
			);
		}),
		ChartsXAxis: vi.fn(() => <div>X Axis</div>),
		ChartsYAxis: vi.fn(() => <div>Y Axis</div>),
		ChartsTooltip: vi.fn(() => <div>Tooltip</div>),
		ChartsReferenceLine: vi.fn(({ label }) => (
			<div data-testid={`reference-line-${label}`}>{label}</div>
		)),
	};
});

describe("CycleTimeScatterPlotChart component", () => {
	// Mock data for tests
	const mockPercentileValues: IPercentileValue[] = [
		{ percentile: 50, value: 3 },
		{ percentile: 85, value: 7 },
		{ percentile: 95, value: 12 },
	];

	const mockSLE: IPercentileValue = {
		percentile: 85,
		value: 7,
	};

	// Create mock work items with proper date objects
	const mockWorkItems: IWorkItem[] = [
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
			type: "Task",
			parentWorkItemReference: "",
			isBlocked: false,
		},
	];

	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("should display 'No data available' when no work items are provided", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={[]}
			/>,
		);

		expect(screen.getByText("No data available")).toBeInTheDocument();
	});

	it("should render the chart with correct title when data is provided", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={mockWorkItems}
			/>,
		);

		expect(screen.getByText("Cycle Time")).toBeInTheDocument();
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
	});

	it("should render percentile chips for each percentile", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={mockWorkItems}
			/>,
		);

		// All percentiles should be represented by reference lines
		for (const p of mockPercentileValues) {
			expect(
				screen.getByTestId(`reference-line-${p.percentile}%`),
			).toBeInTheDocument();
		}
	});

	it("should render SLE reference line when serviceLevelExpectation is provided", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={mockWorkItems}
				serviceLevelExpectation={mockSLE}
			/>,
		);

		expect(screen.getByText("Service Level Expectation")).toBeInTheDocument();
	});

	it("should color group with blocked item using the error color", () => {
		const blockedItems: IWorkItem[] = [
			{
				id: 3,
				referenceId: "ITEM-3",
				name: "Blocked Item",
				url: "https://example.com/item3",
				cycleTime: 5,
				startedDate: new Date(2023, 0, 10),
				closedDate: new Date(2023, 0, 15),
				workItemAge: 5,
				state: "Done",
				stateCategory: "Done",
				type: "Task",
				parentWorkItemReference: "",
				isBlocked: true,
			},
			{
				id: 4,
				referenceId: "ITEM-4",
				name: "Unblocked Item",
				url: "https://example.com/item4",
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
		];

		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={blockedItems}
			/>,
		);

		const container = screen.getByTestId("mock-chart-container");
		const seriesAttr = container.dataset.series;
		expect(seriesAttr).toBeTruthy();
		const series = seriesAttr ? JSON.parse(seriesAttr) : [];
		// series[0].data[0].color should be the errorColor (blocked color)
		expect(series?.[0]?.data?.[0]?.color).toBe(errorColor);
	});
	it("should color groups using the legend type color mapping", () => {
		const typeItems: IWorkItem[] = [
			{
				id: 5,
				referenceId: "ITEM-5",
				name: "Bug Item",
				url: "https://example.com/item5",
				cycleTime: 2,
				startedDate: new Date(2023, 0, 1),
				closedDate: new Date(2023, 0, 3),
				workItemAge: 2,
				state: "Done",
				stateCategory: "Done",
				type: "Bug",
				parentWorkItemReference: "",
				isBlocked: false,
			},
			{
				id: 6,
				referenceId: "ITEM-6",
				name: "Feature Item",
				url: "https://example.com/item6",
				cycleTime: 3,
				startedDate: new Date(2023, 0, 2),
				closedDate: new Date(2023, 0, 5),
				workItemAge: 3,
				state: "Done",
				stateCategory: "Done",
				type: "Feature",
				parentWorkItemReference: "",
				isBlocked: false,
			},
		];

		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={typeItems}
			/>,
		);

		const container = screen.getByTestId("mock-chart-container");
		const seriesAttr = container.dataset.series;
		expect(seriesAttr).toBeTruthy();
		const series = seriesAttr ? JSON.parse(seriesAttr) : [];

		const colorMap = getColorMapForKeys(
			["Bug", "Feature"],
			testTheme.palette.primary.main,
		);

		// The groups are created in the same order we provided data; 'Bug' is first group -> color should match
		expect(series?.[0]?.data?.[0]?.color).toBe(colorMap.Bug);
		expect(series?.[0]?.data?.[0]?.color).not.toBe(
			testTheme.palette.primary.main,
		);
		// Should not be the same as the blocked/error color
		expect(colorMap.Bug).not.toBe(errorColor);
	});
});
