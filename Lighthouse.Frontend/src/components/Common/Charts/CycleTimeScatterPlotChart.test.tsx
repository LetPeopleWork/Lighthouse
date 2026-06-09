import * as MuiCharts from "@mui/x-charts";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
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

// Mock TimeBlackoutOverlay to avoid chart context dependency
vi.mock("./TimeBlackoutOverlay", () => ({
	default: vi.fn(() => null),
}));

// Mock the MUI-X Charts and expose series prop for assertions
vi.mock("@mui/x-charts", async () => {
	const actual = await vi.importActual("@mui/x-charts");
	return {
		...actual,
		ChartsContainer: vi.fn(({ series, children }) => (
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

		const colorMap = getColorMapForKeys(["Bug", "Feature"], true);

		// The groups are created in the same order we provided data; 'Bug' is first group -> color should match
		expect(series?.[0]?.data?.[0]?.color).toBe(colorMap.Bug);
		expect(series?.[0]?.data?.[0]?.color).not.toBe(
			testTheme.palette.primary.main,
		);
		// Should not be the same as the blocked/error color
		expect(colorMap.Bug).not.toBe(errorColor);
	});

	it("uses explicit axis IDs and x-axis height for stable tick rendering", () => {
		render(
			<CycleTimeScatterPlotChart
				percentileValues={mockPercentileValues}
				cycleTimeDataPoints={mockWorkItems}
			/>,
		);

		const chartsContainerMock = MuiCharts.ChartsContainer as unknown as {
			mock: { calls: Array<[Record<string, unknown>]> };
		};
		const xAxisMock = MuiCharts.ChartsXAxis as unknown as {
			mock: { calls: Array<[Record<string, unknown>]> };
		};
		const yAxisMock = MuiCharts.ChartsYAxis as unknown as {
			mock: { calls: Array<[Record<string, unknown>]> };
		};

		const containerProps =
			chartsContainerMock.mock.calls[
				chartsContainerMock.mock.calls.length - 1
			]?.[0];
		expect(containerProps).toBeTruthy();

		const xAxisConfig = (
			containerProps?.xAxis as Array<Record<string, unknown>>
		)?.[0];
		expect(xAxisConfig?.id).toBe("timeAxis");
		expect(xAxisConfig?.height).toBe(56);

		expect(
			xAxisMock.mock.calls.some(
				([props]) => (props as { axisId?: string })?.axisId === "timeAxis",
			),
		).toBe(true);

		expect(
			yAxisMock.mock.calls.some(
				([props]) => (props as { axisId?: string })?.axisId === "cycleTimeAxis",
			),
		).toBe(true);
	});

	describe("named cycle time selector", () => {
		const conceptToCash: INamedCycleTimeDefinition = {
			id: 1,
			name: "Concept to Cash",
		};

		const namedItems: IWorkItem[] = [
			{
				...mockWorkItems[0],
				cycleTime: 5,
				namedCycleTimes: [{ definitionId: 1, days: 20 }],
			},
			{
				...mockWorkItems[1],
				cycleTime: 10,
				namedCycleTimes: [],
			},
		];

		const seriesData = () => {
			const seriesAttr = screen.getByTestId("mock-chart-container").dataset
				.series;
			const series = seriesAttr ? JSON.parse(seriesAttr) : [];
			return (series?.[0]?.data ?? []) as Array<{ y: number }>;
		};

		it("does not render the selector when no named definitions are available", () => {
			render(
				<CycleTimeScatterPlotChart
					percentileValues={mockPercentileValues}
					cycleTimeDataPoints={namedItems}
				/>,
			);

			expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
		});

		it("lists Default plus each named cycle time definition", async () => {
			render(
				<CycleTimeScatterPlotChart
					percentileValues={mockPercentileValues}
					cycleTimeDataPoints={namedItems}
					namedCycleTimeDefinitions={[conceptToCash]}
				/>,
			);

			await userEvent.click(
				screen.getByRole("combobox", { name: /cycle time/i }),
			);

			const options = screen.getByRole("listbox");
			expect(within(options).getByText("Default")).toBeInTheDocument();
			expect(within(options).getByText("Concept to Cash")).toBeInTheDocument();
		});

		it("plots the default durations until a named definition is selected", () => {
			render(
				<CycleTimeScatterPlotChart
					percentileValues={mockPercentileValues}
					cycleTimeDataPoints={namedItems}
					namedCycleTimeDefinitions={[conceptToCash]}
				/>,
			);

			expect(
				seriesData()
					.map((point) => point.y)
					.sort((a, b) => a - b),
			).toEqual([5, 10]);
		});

		it("switches the dots to the named durations client-side and drops items without an entry", async () => {
			render(
				<CycleTimeScatterPlotChart
					percentileValues={mockPercentileValues}
					cycleTimeDataPoints={namedItems}
					namedCycleTimeDefinitions={[conceptToCash]}
				/>,
			);

			await userEvent.click(
				screen.getByRole("combobox", { name: /cycle time/i }),
			);
			await userEvent.click(
				within(screen.getByRole("listbox")).getByText("Concept to Cash"),
			);

			await waitFor(() => {
				expect(seriesData().map((point) => point.y)).toEqual([20]);
			});
		});

		it("re-fetches percentiles for the selected definition", async () => {
			const onFetchNamedCycleTimePercentiles = vi
				.fn<(definitionId: number) => Promise<IPercentileValue[]>>()
				.mockResolvedValue([
					{ percentile: 50, value: 18 },
					{ percentile: 85, value: 25 },
				]);

			render(
				<CycleTimeScatterPlotChart
					percentileValues={mockPercentileValues}
					cycleTimeDataPoints={namedItems}
					namedCycleTimeDefinitions={[conceptToCash]}
					onFetchNamedCycleTimePercentiles={onFetchNamedCycleTimePercentiles}
				/>,
			);

			await userEvent.click(
				screen.getByRole("combobox", { name: /cycle time/i }),
			);
			await userEvent.click(
				within(screen.getByRole("listbox")).getByText("Concept to Cash"),
			);

			await waitFor(() => {
				expect(onFetchNamedCycleTimePercentiles).toHaveBeenCalledWith(1);
			});
		});

		it("disables an invalid named cycle time and keeps it unselectable", async () => {
			render(
				<CycleTimeScatterPlotChart
					percentileValues={mockPercentileValues}
					cycleTimeDataPoints={namedItems}
					namedCycleTimeDefinitions={[
						{ ...conceptToCash, isValid: true },
						{ id: 2, name: "Broken", isValid: false },
					]}
				/>,
			);

			await userEvent.click(
				screen.getByRole("combobox", { name: /cycle time/i }),
			);

			const invalidOption = within(screen.getByRole("listbox")).getByText(
				/Broken \(invalid/,
			);
			expect(invalidOption.closest('[role="option"]')).toHaveAttribute(
				"aria-disabled",
				"true",
			);
		});

		it("falls back to Default when the selected definition becomes invalid", async () => {
			const { rerender } = render(
				<CycleTimeScatterPlotChart
					percentileValues={mockPercentileValues}
					cycleTimeDataPoints={namedItems}
					namedCycleTimeDefinitions={[{ ...conceptToCash, isValid: true }]}
				/>,
			);

			await userEvent.click(
				screen.getByRole("combobox", { name: /cycle time/i }),
			);
			await userEvent.click(
				within(screen.getByRole("listbox")).getByText("Concept to Cash"),
			);
			await waitFor(() => {
				expect(seriesData().map((point) => point.y)).toEqual([20]);
			});

			rerender(
				<CycleTimeScatterPlotChart
					percentileValues={mockPercentileValues}
					cycleTimeDataPoints={namedItems}
					namedCycleTimeDefinitions={[{ ...conceptToCash, isValid: false }]}
				/>,
			);

			await waitFor(() => {
				expect(
					seriesData()
						.map((point) => point.y)
						.sort((a, b) => a - b),
				).toEqual([5, 10]);
			});
		});
	});
});
