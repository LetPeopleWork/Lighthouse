import * as MuiCharts from "@mui/x-charts";
import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IPerStatePercentileValues } from "../../../models/PerStatePercentileValues";
import type { IWorkItem } from "../../../models/WorkItem";
import { testTheme } from "../../../tests/testTheme";
import {
	confidentColor,
	errorColor,
	getColorMapForKeys,
} from "../../../utils/theme/colors";
import WorkItemAgingChart, {
	computePaceBandRects,
	PACE_BAND_COLORS_LOW_TO_HIGH,
	PaceBandOverlay,
	STATE_BAND_HALF_WIDTH,
} from "./WorkItemAgingChart";

vi.mock("@mui/x-charts/hooks", () => {
	const identity = (value: number) => value;
	const xScale = Object.assign(identity, {
		domain: () => [-0.5, 2.5] as [number, number],
	});
	const yScale = Object.assign(identity, {
		domain: () => [1, 30] as [number, number],
	});
	return {
		useXScale: () => xScale,
		useYScale: () => yScale,
	};
});

const getMockPerStatePercentileValues = (
	overrides?: Partial<IPerStatePercentileValues>,
): IPerStatePercentileValues => ({
	state: "In Progress",
	percentiles: [
		{ percentile: 50, value: 3 },
		{ percentile: 70, value: 5 },
		{ percentile: 85, value: 8 },
		{ percentile: 95, value: 12 },
	],
	...overrides,
});

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
	default: vi.fn(
		({
			title,
			items,
			open,
			onClose,
			timeInStateColumn,
		}: {
			title: string;
			items?: IWorkItem[];
			open: boolean;
			onClose: () => void;
			timeInStateColumn?: { stalenessThresholdDays?: number };
		}) => {
			if (!open) return null;
			return (
				<div
					data-testid="work-items-dialog"
					data-time-in-state-threshold={
						timeInStateColumn
							? String(timeInStateColumn.stalenessThresholdDays)
							: undefined
					}
				>
					<h2>{title}</h2>
					<button type="button" onClick={onClose} data-testid="close-dialog">
						Close
					</button>
					<table>
						<thead>
							<tr>
								<th>Name</th>
								<th>Type</th>
								<th>State</th>
								<th>Age</th>
							</tr>
						</thead>
						<tbody>
							{items?.map((item: IWorkItem) => (
								<tr key={item.id}>
									<td>{item.name}</td>
									<td>{item.type}</td>
									<td>{item.state}</td>
									<td>{item.workItemAge} days</td>
								</tr>
							))}
						</tbody>
					</table>
				</div>
			);
		},
	),
}));

// Mock the MUI-X Charts
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

describe("WorkItemAgingChart component", () => {
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
	const mockInProgressItems: IWorkItem[] = [
		{
			id: 1,
			referenceId: "ITEM-1",
			name: "Test Item 1",
			url: "https://example.com/item1",
			cycleTime: 0,
			startedDate: new Date(2023, 0, 10),
			closedDate: new Date(),
			workItemAge: 5,
			type: "Story",
			state: "In Progress",
			stateCategory: "Doing",
			parentWorkItemReference: "",
			isBlocked: false,
		},
		{
			id: 2,
			referenceId: "ITEM-2",
			name: "Test Item 2",
			url: "https://example.com/item2",
			cycleTime: 0,
			startedDate: new Date(2023, 0, 5),
			closedDate: new Date(),
			workItemAge: 10,
			type: "Bug",
			state: "Ready for Review",
			stateCategory: "Doing",
			parentWorkItemReference: "",
			isBlocked: false,
		},
		{
			id: 3,
			referenceId: "ITEM-3",
			name: "Test Item 3",
			url: "https://example.com/item3",
			cycleTime: 0,
			startedDate: new Date(2023, 0, 8),
			closedDate: new Date(),
			workItemAge: 5,
			type: "Task",
			state: "In Progress",
			stateCategory: "Doing",
			parentWorkItemReference: "",
			isBlocked: false,
		},
	];

	// Create mock work items with blocked items
	const mockBlockedItems: IWorkItem[] = [
		{
			id: 4,
			referenceId: "ITEM-4",
			name: "Blocked Item 1",
			url: "https://example.com/item4",
			cycleTime: 0,
			startedDate: new Date(2023, 0, 1),
			closedDate: new Date(),
			workItemAge: 15,
			type: "Story",
			state: "In Progress",
			stateCategory: "Doing",
			parentWorkItemReference: "",
			isBlocked: true,
		},
		{
			id: 5,
			referenceId: "ITEM-5",
			name: "Regular Item",
			url: "https://example.com/item5",
			cycleTime: 0,
			startedDate: new Date(2023, 0, 12),
			closedDate: new Date(),
			workItemAge: 3,
			type: "Bug",
			state: "Review",
			stateCategory: "Doing",
			parentWorkItemReference: "",
			isBlocked: false,
		},
		{
			id: 6,
			referenceId: "ITEM-6",
			name: "Another Blocked Item",
			url: "https://example.com/item6",
			cycleTime: 0,
			startedDate: new Date(2023, 0, 2),
			closedDate: new Date(),
			workItemAge: 8,
			type: "Task",
			state: "In Progress",
			stateCategory: "Doing",
			parentWorkItemReference: "",
			isBlocked: true,
		},
	];

	beforeEach(() => {
		vi.clearAllMocks();
		localStorage.clear();
	});

	it("renders correctly with in-progress items", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(screen.getByText("Work Item Aging")).toBeInTheDocument();
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
		expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
	});

	it("displays correct chart title", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(screen.getByText("Work Item Aging")).toBeInTheDocument();
	});

	it("renders fallback message when there are no items", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={[]}
				percentileValues={mockPercentileValues}
				doingStates={["To Do", "In Progress", "Review"]}
				serviceLevelExpectation={mockSLE}
			/>,
		);

		expect(
			screen.queryByTestId("mock-chart-container"),
		).not.toBeInTheDocument();
		expect(screen.getByText("No work items in progress")).toBeInTheDocument();
	});

	it("renders percentile chips with correct labels", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		const percentile50Elements = screen.getAllByText("50%");
		const percentile85Elements = screen.getAllByText("85%");
		const percentile95Elements = screen.getAllByText("95%");

		expect(percentile50Elements.length).toBeGreaterThan(0);
		expect(percentile85Elements.length).toBeGreaterThan(0);
		expect(percentile95Elements.length).toBeGreaterThan(0);
	});

	it("renders service level expectation chip when provided", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(screen.getByText("Service Level Expectation")).toBeInTheDocument();
	});

	it("does not render service level expectation chip when not provided", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={null}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(
			screen.queryByText("Service Level Expectation"),
		).not.toBeInTheDocument();
	});

	it("toggles percentile visibility when chip is clicked", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		const chips = screen.getAllByRole("button");
		const percentile50Chip = chips.find((chip) => chip.textContent === "50%");

		expect(percentile50Chip).toBeInTheDocument();
		expect(screen.getByTestId("reference-line-50%")).toBeInTheDocument();

		if (percentile50Chip) {
			fireEvent.click(percentile50Chip);
		}

		expect(percentile50Chip).toBeInTheDocument();
		expect(screen.queryByTestId("reference-line-50%")).not.toBeInTheDocument();
		expect(screen.getByTestId("reference-line-85%")).toBeInTheDocument();
	});

	it("toggles service level expectation visibility when chip is clicked", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		const sleChip = screen.getByText("Service Level Expectation");
		fireEvent.click(sleChip);
		expect(sleChip).toBeInTheDocument();
	});

	it("groups items correctly by state and age", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
	});

	it("handles empty percentile values array", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={[]}
				serviceLevelExpectation={null}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(screen.getByText("Work Item Aging")).toBeInTheDocument();
		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();

		expect(screen.queryByTestId("reference-line-50%")).not.toBeInTheDocument();
		expect(screen.queryByTestId("reference-line-85%")).not.toBeInTheDocument();
		expect(screen.queryByTestId("reference-line-95%")).not.toBeInTheDocument();
	});

	it("renders chart components correctly", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
		expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
		expect(screen.getByText("X Axis")).toBeInTheDocument();
		expect(screen.getByText("Y Axis")).toBeInTheDocument();
		expect(screen.getByText("Tooltip")).toBeInTheDocument();
	});

	it("renders reference lines for visible percentiles", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={null}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(screen.getByTestId("reference-line-50%")).toBeInTheDocument();
		expect(screen.getByTestId("reference-line-85%")).toBeInTheDocument();
		expect(screen.getByTestId("reference-line-95%")).toBeInTheDocument();
	});

	it("handles items with missing state gracefully", () => {
		const itemsWithMissingState: IWorkItem[] = [
			{
				...mockInProgressItems[0],
				state: "",
			},
		];

		render(
			<WorkItemAgingChart
				inProgressItems={itemsWithMissingState}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// Items with empty state are filtered out — fallback message shown
		expect(
			screen.queryByTestId("mock-chart-container"),
		).not.toBeInTheDocument();
		expect(screen.getByText("No work items in progress")).toBeInTheDocument();
	});

	it("handles items with states not in doingStates list", () => {
		const itemsWithNonDoingStates: IWorkItem[] = [
			{
				...mockInProgressItems[0],
				state: "Done",
			},
			{
				...mockInProgressItems[1],
				state: "Closed",
			},
		];

		render(
			<WorkItemAgingChart
				inProgressItems={itemsWithNonDoingStates}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		// No items match doingStates — fallback message shown
		expect(
			screen.queryByTestId("mock-chart-container"),
		).not.toBeInTheDocument();
		expect(screen.getByText("No work items in progress")).toBeInTheDocument();
	});

	it("correctly extracts age from work items", () => {
		const itemWithSpecificAge: IWorkItem[] = [
			{
				...mockInProgressItems[0],
				workItemAge: 15,
			},
		];

		render(
			<WorkItemAgingChart
				inProgressItems={itemWithSpecificAge}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
			/>,
		);

		expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
	});

	describe("Blocked items functionality", () => {
		it("renders blocked items with red color", () => {
			render(
				<WorkItemAgingChart
					inProgressItems={mockBlockedItems}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
			expect(screen.getByText("Work Item Aging")).toBeInTheDocument();
		});

		it("should color groups using the legend type color mapping", () => {
			const typeItems: IWorkItem[] = [
				{
					id: 7,
					referenceId: "ITEM-7",
					name: "Bug Item",
					url: "https://example.com/item7",
					cycleTime: 0,
					startedDate: new Date(2023, 0, 1),
					closedDate: new Date(),
					workItemAge: 2,
					type: "Bug",
					state: "In Progress",
					stateCategory: "Doing",
					parentWorkItemReference: "",
					isBlocked: false,
				},
				{
					id: 8,
					referenceId: "ITEM-8",
					name: "Story Item",
					url: "https://example.com/item8",
					cycleTime: 0,
					startedDate: new Date(2023, 0, 1),
					closedDate: new Date(),
					workItemAge: 3,
					type: "Story",
					state: "In Progress",
					stateCategory: "Doing",
					parentWorkItemReference: "",
					isBlocked: false,
				},
			];

			render(
				<WorkItemAgingChart
					inProgressItems={typeItems}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			const container = screen.getByTestId("mock-chart-container");
			const seriesAttr = container.dataset.series;
			expect(seriesAttr).toBeTruthy();
			const series = seriesAttr ? JSON.parse(seriesAttr) : [];

			const colorMap = getColorMapForKeys(["Bug", "Story"], true);
			expect(series?.[0]?.data?.[0]?.color).toBe(colorMap.Bug);
			expect(series?.[0]?.data?.[0]?.color).not.toBe(
				testTheme.palette.primary.main,
			);
		});

		it("groups blocked and regular items correctly", () => {
			render(
				<WorkItemAgingChart
					inProgressItems={mockBlockedItems}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
			expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
		});

		it("marks group as blocked if any item in group is blocked", () => {
			const mixedBlockedItems: IWorkItem[] = [
				{
					...mockInProgressItems[0],
					workItemAge: 10,
					isBlocked: false,
				},
				{
					...mockInProgressItems[1],
					state: "In Progress",
					workItemAge: 10,
					isBlocked: true,
				},
			];

			render(
				<WorkItemAgingChart
					inProgressItems={mixedBlockedItems}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
		});

		it("renders fallback message when blocked items array is empty", () => {
			render(
				<WorkItemAgingChart
					inProgressItems={[]}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			expect(
				screen.queryByTestId("mock-chart-container"),
			).not.toBeInTheDocument();
			expect(screen.getByText("No work items in progress")).toBeInTheDocument();
		});
	});

	describe("Case-insensitive state matching", () => {
		it("matches states regardless of casing", () => {
			const itemsWithDifferentCasing: IWorkItem[] = [
				{
					...mockInProgressItems[0],
					state: "IN PROGRESS",
					workItemAge: 5,
				},
				{
					...mockInProgressItems[1],
					state: "in progress",
					workItemAge: 8,
				},
				{
					...mockInProgressItems[2],
					state: "In Progress",
					workItemAge: 3,
				},
			];

			render(
				<WorkItemAgingChart
					inProgressItems={itemsWithDifferentCasing}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			expect(screen.getByText("Work Item Aging")).toBeInTheDocument();
			expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
			expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
		});

		it("handles mixed casing in doingStates configuration", () => {
			const items: IWorkItem[] = [
				{
					...mockInProgressItems[0],
					state: "ready for review",
					workItemAge: 5,
				},
			];

			render(
				<WorkItemAgingChart
					inProgressItems={items}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Ready For Review"]}
				/>,
			);

			expect(screen.getByText("Work Item Aging")).toBeInTheDocument();
			expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
		});
	});

	describe("Stale items functionality", () => {
		const now = new Date("2026-05-25T12:00:00Z");

		const seriesColorOf = (
			items: IWorkItem[],
			stalenessThresholdDays: number,
		): string | undefined => {
			render(
				<WorkItemAgingChart
					inProgressItems={items}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
					stalenessThresholdDays={stalenessThresholdDays}
					now={now}
				/>,
			);

			const container = screen.getByTestId("mock-chart-container");
			const seriesAttr = container.dataset.series;
			const series = seriesAttr ? JSON.parse(seriesAttr) : [];
			return series?.[0]?.data?.[0]?.color;
		};

		it("reds a stale, non-blocked bubble with the same emphasis as blocked", () => {
			const staleItem: IWorkItem = {
				...mockInProgressItems[0],
				isBlocked: false,
				currentStateEnteredAt: new Date("2026-05-23T23:00:00Z"),
			};

			expect(seriesColorOf([staleItem], 1)).toBe(errorColor);
		});

		it("does not red a stale bubble when staleness is disabled (threshold 0)", () => {
			const oldButNotStaleItem: IWorkItem = {
				...mockInProgressItems[0],
				isBlocked: false,
				currentStateEnteredAt: new Date("2026-05-23T23:00:00Z"),
			};

			expect(seriesColorOf([oldButNotStaleItem], 0)).not.toBe(errorColor);
		});

		it("gives the bubble dialog a Time in State column carrying the staleness threshold", async () => {
			const { default: WorkItemsDialogMock } = await import(
				"../WorkItemsDialog/WorkItemsDialog"
			);

			render(
				<WorkItemAgingChart
					inProgressItems={mockInProgressItems}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
					stalenessThresholdDays={7}
					now={now}
				/>,
			);

			const calls = (
				WorkItemsDialogMock as unknown as {
					mock: { calls: Array<[{ timeInStateColumn?: unknown }]> };
				}
			).mock.calls;
			const dialogProps = calls[calls.length - 1]?.[0];

			expect(dialogProps?.timeInStateColumn).toEqual({
				now,
				stalenessThresholdDays: 7,
			});
		});
	});

	it("uses explicit axis IDs and x-axis height for stable tick rendering", () => {
		render(
			<WorkItemAgingChart
				inProgressItems={mockInProgressItems}
				percentileValues={mockPercentileValues}
				serviceLevelExpectation={mockSLE}
				doingStates={["To Do", "In Progress", "Review"]}
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
		expect(xAxisConfig?.id).toBe("stateAxis");
		expect(xAxisConfig?.height).toBe(56);

		expect(
			xAxisMock.mock.calls.some(
				([props]) => (props as { axisId?: string })?.axisId === "stateAxis",
			),
		).toBe(true);

		expect(
			yAxisMock.mock.calls.some(
				([props]) => (props as { axisId?: string })?.axisId === "ageAxis",
			),
		).toBe(true);
	});

	describe("Per-state pace band overlay", () => {
		const identityScale = (value: number) => value;

		it("renders no pace-band rects by default even when perStatePercentileValues is provided", () => {
			render(
				<WorkItemAgingChart
					inProgressItems={mockInProgressItems}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
					perStatePercentileValues={[getMockPerStatePercentileValues()]}
				/>,
			);

			expect(screen.getByTestId("mock-chart-container")).toBeInTheDocument();
			expect(screen.queryAllByTestId("pace-band")).toHaveLength(0);
		});

		it("shows the pace-bands toggle only when band data is available", () => {
			const { rerender } = render(
				<WorkItemAgingChart
					inProgressItems={mockInProgressItems}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
				/>,
			);

			expect(screen.queryByTestId("pace-bands-toggle")).not.toBeInTheDocument();

			rerender(
				<WorkItemAgingChart
					inProgressItems={mockInProgressItems}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
					perStatePercentileValues={[getMockPerStatePercentileValues()]}
				/>,
			);

			expect(screen.getByTestId("pace-bands-toggle")).toBeInTheDocument();
		});

		it("toggles the pace bands on then off through the top-right icon button", () => {
			render(
				<WorkItemAgingChart
					inProgressItems={mockInProgressItems}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
					perStatePercentileValues={[getMockPerStatePercentileValues()]}
				/>,
			);

			expect(screen.queryAllByTestId("pace-band")).toHaveLength(0);

			fireEvent.click(screen.getByTestId("pace-bands-toggle"));
			expect(screen.getAllByTestId("pace-band").length).toBeGreaterThan(0);

			fireEvent.click(screen.getByTestId("pace-bands-toggle"));
			expect(screen.queryAllByTestId("pace-band")).toHaveLength(0);
		});

		it("keeps the work item type chips and percentile chips untouched by the toggle", () => {
			render(
				<WorkItemAgingChart
					inProgressItems={mockInProgressItems}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
					perStatePercentileValues={[getMockPerStatePercentileValues()]}
				/>,
			);

			const typeChipsBefore = screen.getAllByText(/^(Story|Task)$/).length;

			fireEvent.click(screen.getByTestId("pace-bands-toggle"));

			expect(screen.getAllByText("50%").length).toBeGreaterThan(0);
			expect(screen.getByText("Service Level Expectation")).toBeInTheDocument();
			expect(screen.getAllByText(/^(Story|Task)$/).length).toBe(
				typeChipsBefore,
			);
			expect(screen.queryByText("Pace percentiles")).not.toBeInTheDocument();
		});

		it("spans each state band rect across x in [stateIndex - HALF_WIDTH, stateIndex + HALF_WIDTH]", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({ state: "Review" }),
				],
				doingStates: ["To Do", "In Progress", "Review"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			expect(rects.length).toBeGreaterThan(0);
			const stateIndex = 2;
			for (const rect of rects) {
				expect(rect.x).toBe(stateIndex - STATE_BAND_HALF_WIDTH);
				expect(rect.width).toBeCloseTo(2 * STATE_BAND_HALF_WIDTH);
			}
		});

		it("anchors the bottom band at the axis minimum and caps the top band at the axis maximum", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "In Progress",
						percentiles: [
							{ percentile: 50, value: 3 },
							{ percentile: 70, value: 5 },
							{ percentile: 85, value: 8 },
							{ percentile: 95, value: 12 },
						],
					}),
				],
				doingStates: ["In Progress"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			const boundaries = rects
				.map((rect) => [rect.y, rect.y + rect.height])
				.sort((a, b) => a[0] - b[0]);

			expect(boundaries).toEqual([
				[1, 3],
				[3, 5],
				[5, 8],
				[8, 12],
				[12, 30],
			]);
		});

		it("colours the stack from greenest at the floor to red at the top by band position", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "In Progress",
						percentiles: [
							{ percentile: 50, value: 3 },
							{ percentile: 70, value: 5 },
							{ percentile: 85, value: 8 },
							{ percentile: 95, value: 12 },
						],
					}),
				],
				doingStates: ["In Progress"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			const fillsBottomToTop = [...rects]
				.sort((a, b) => a.y - b.y)
				.map((rect) => rect.fill);

			expect(fillsBottomToTop).toEqual(PACE_BAND_COLORS_LOW_TO_HIGH);
		});

		it("paints the floor band the greenest colour and the top band red", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "In Progress",
						percentiles: [
							{ percentile: 50, value: 3 },
							{ percentile: 70, value: 5 },
							{ percentile: 85, value: 8 },
							{ percentile: 95, value: 12 },
						],
					}),
				],
				doingStates: ["In Progress"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			const sortedBottomToTop = [...rects].sort((a, b) => a.y - b.y);
			const floorBand = sortedBottomToTop[0];
			const topBand = sortedBottomToTop[sortedBottomToTop.length - 1];

			expect(floorBand.y).toBe(1);
			expect(floorBand.fill).toBe(PACE_BAND_COLORS_LOW_TO_HIGH[0]);
			expect(topBand.y + topBand.height).toBe(30);
			expect(topBand.fill).toBe(errorColor);
		});

		it("clamps the colour of bands beyond the palette to the reddest non-top colour", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "In Progress",
						percentiles: [
							{ percentile: 50, value: 3 },
							{ percentile: 60, value: 5 },
							{ percentile: 70, value: 8 },
							{ percentile: 80, value: 12 },
							{ percentile: 90, value: 16 },
							{ percentile: 95, value: 20 },
						],
					}),
				],
				doingStates: ["In Progress"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			const sortedBottomToTop = [...rects].sort((a, b) => a.y - b.y);
			const lastColouredBand = sortedBottomToTop[sortedBottomToTop.length - 2];

			expect(lastColouredBand.fill).toBe(
				PACE_BAND_COLORS_LOW_TO_HIGH[PACE_BAND_COLORS_LOW_TO_HIGH.length - 1],
			);
		});

		it("paints the top band red even when fewer than four percentiles are returned", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "In Progress",
						percentiles: [
							{ percentile: 50, value: 3 },
							{ percentile: 95, value: 8 },
						],
					}),
				],
				doingStates: ["In Progress"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			const topBand = [...rects].sort((a, b) => a.y - b.y)[rects.length - 1];

			expect(topBand.y + topBand.height).toBe(30);
			expect(topBand.fill).toBe(errorColor);
		});

		it("leaves states before the first populated one bare while carrying it forward to the right", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({ state: "In Progress" }),
				],
				doingStates: ["To Do", "In Progress", "Review"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			const stateIndicesWithRects = new Set(
				rects.map((rect) => rect.x + STATE_BAND_HALF_WIDTH),
			);
			expect(stateIndicesWithRects).toEqual(new Set([1, 2]));
		});

		it("collects all pace-band rects into a single svg group so the overlay is one z-layer", () => {
			render(
				<svg aria-label="overlay-host">
					<title>overlay-host</title>
					<PaceBandOverlay
						perStatePercentileValues={[
							getMockPerStatePercentileValues({ state: "In Progress" }),
						]}
						doingStates={["To Do", "In Progress", "Review"]}
					/>
				</svg>,
			);

			const bands = screen.getAllByTestId("pace-band");
			expect(bands).toHaveLength(10);

			const group = bands[0].parentElement;
			expect(group?.tagName.toLowerCase()).toBe("g");
			for (const band of bands) {
				expect(band.parentElement).toBe(group);
			}
		});

		it("applies the scales when projecting data coordinates to pixels", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "In Progress",
						percentiles: [{ percentile: 50, value: 4 }],
					}),
				],
				doingStates: ["In Progress"],
				xScale: (value: number) => value * 100,
				yScale: (value: number) => value * 10,
				axisMin: 1,
				axisMax: 30,
			});

			expect(rects).toHaveLength(2);
			const floorBand = [...rects].sort((a, b) => a.y - b.y)[0];
			expect(floorBand.x).toBe((0 - STATE_BAND_HALF_WIDTH) * 100);
			expect(floorBand.width).toBe(2 * STATE_BAND_HALF_WIDTH * 100);
			expect(floorBand.y).toBe(10);
			expect(floorBand.height).toBe(30);
		});

		it("emits no rects for a mapped state whose percentile set is empty", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "In Progress",
						percentiles: [],
					}),
				],
				doingStates: ["To Do", "In Progress", "Review"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			expect(rects).toHaveLength(0);
		});

		it("emits no rects for a state that carries observations but is not in the workflow order", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "Archived",
						percentiles: [
							{ percentile: 50, value: 3 },
							{ percentile: 70, value: 5 },
						],
					}),
				],
				doingStates: ["To Do", "In Progress", "Review"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			expect(rects).toHaveLength(0);
		});

		it("emits a full-column stack of five bands when all four percentiles are present and distinct", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "In Progress",
						percentiles: [
							{ percentile: 50, value: 3 },
							{ percentile: 70, value: 5 },
							{ percentile: 85, value: 8 },
							{ percentile: 95, value: 12 },
						],
					}),
				],
				doingStates: ["In Progress"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			expect(rects).toHaveLength(5);
		});

		it("orders the stacked bands by ascending percentile value regardless of input order", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "In Progress",
						percentiles: [
							{ percentile: 95, value: 12 },
							{ percentile: 50, value: 3 },
							{ percentile: 85, value: 8 },
							{ percentile: 70, value: 5 },
						],
					}),
				],
				doingStates: ["In Progress"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			expect(rects.map((rect) => [rect.y, rect.y + rect.height])).toEqual([
				[1, 3],
				[3, 5],
				[5, 8],
				[8, 12],
				[12, 30],
			]);
		});

		it("keys each rect by its state and percentile boundary so React reconciles them stably", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "Review",
						percentiles: [
							{ percentile: 50, value: 3 },
							{ percentile: 70, value: 5 },
						],
					}),
				],
				doingStates: ["In Progress", "Review"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			expect(rects.map((rect) => rect.key)).toEqual([
				"Review-50",
				"Review-70",
				"Review-top",
			]);
		});

		// Color is keyed off band position, not percentile value, so when adjacent percentiles coincide the zero-height band drops and no surviving band shifts color.
		it("collapses zero-height bands and keeps the surviving 50-70 band green when upper percentiles coincide", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "In Progress",
						percentiles: [
							{ percentile: 50, value: 1 },
							{ percentile: 70, value: 2 },
							{ percentile: 85, value: 2 },
							{ percentile: 95, value: 2 },
						],
					}),
				],
				doingStates: ["In Progress"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 6,
			});

			expect(
				rects.map((rect) => [rect.y, rect.y + rect.height, rect.fill]),
			).toEqual([
				[1, 2, confidentColor],
				[2, 6, errorColor],
			]);
		});

		it("repeats the nearest populated left neighbour's bands for an empty state at the empty state's x position", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "A",
						percentiles: [
							{ percentile: 50, value: 3 },
							{ percentile: 70, value: 5 },
						],
					}),
					getMockPerStatePercentileValues({
						state: "C",
						percentiles: [{ percentile: 50, value: 4 }],
					}),
				],
				doingStates: ["A", "B", "C"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			const bandsFor = (centerIndex: number) =>
				rects
					.filter((rect) => rect.x === centerIndex - STATE_BAND_HALF_WIDTH)
					.sort((a, b) => a.y - b.y)
					.map((rect) => [rect.y, rect.y + rect.height, rect.fill]);

			expect(bandsFor(1)).toEqual(bandsFor(0));
			expect(bandsFor(1)).toEqual([
				[1, 3, PACE_BAND_COLORS_LOW_TO_HIGH[0]],
				[3, 5, PACE_BAND_COLORS_LOW_TO_HIGH[1]],
				[
					5,
					30,
					PACE_BAND_COLORS_LOW_TO_HIGH[PACE_BAND_COLORS_LOW_TO_HIGH.length - 1],
				],
			]);
		});

		it("renders nothing for a leading empty state with no populated state to its left", () => {
			const rects = computePaceBandRects({
				perStatePercentileValues: [
					getMockPerStatePercentileValues({
						state: "B",
						percentiles: [{ percentile: 50, value: 4 }],
					}),
				],
				doingStates: ["A", "B"],
				xScale: identityScale,
				yScale: identityScale,
				axisMin: 1,
				axisMax: 30,
			});

			const leadingStateRects = rects.filter(
				(rect) => rect.x === 0 - STATE_BAND_HALF_WIDTH,
			);
			expect(leadingStateRects).toHaveLength(0);
		});

		it("renders pace bands at a readable fill opacity", () => {
			render(
				<svg aria-label="opacity-host">
					<title>opacity-host</title>
					<PaceBandOverlay
						perStatePercentileValues={[
							getMockPerStatePercentileValues({ state: "In Progress" }),
						]}
						doingStates={["To Do", "In Progress", "Review"]}
					/>
				</svg>,
			);

			for (const band of screen.getAllByTestId("pace-band")) {
				expect(band.getAttribute("fill-opacity")).toBe("0.28");
			}
		});
	});

	describe("Cycle time / work item age reference-line selector", () => {
		const workItemAgePercentileValues: IPercentileValue[] = [
			{ percentile: 50, value: 4 },
			{ percentile: 70, value: 6 },
			{ percentile: 85, value: 9 },
			{ percentile: 95, value: 14 },
		];

		const renderWithSelector = (
			overrides?: Partial<{
				workItemAgePercentileValues: IPercentileValue[];
				inProgressItems: IWorkItem[];
			}>,
		) =>
			render(
				<WorkItemAgingChart
					inProgressItems={overrides?.inProgressItems ?? mockInProgressItems}
					percentileValues={mockPercentileValues}
					serviceLevelExpectation={mockSLE}
					doingStates={["To Do", "In Progress", "Review"]}
					perStatePercentileValues={[getMockPerStatePercentileValues()]}
					workItemAgePercentileValues={
						overrides?.workItemAgePercentileValues ??
						workItemAgePercentileValues
					}
				/>,
			);

		const selectSource = (name: string) =>
			fireEvent.click(screen.getByRole("button", { name }));

		it("draws the cycle time reference lines by default and no work item age lines", () => {
			renderWithSelector();

			expect(screen.getByTestId("reference-line-50%")).toBeInTheDocument();
			expect(screen.getByTestId("reference-line-85%")).toBeInTheDocument();
			expect(screen.getByTestId("reference-line-95%")).toBeInTheDocument();

			expect(
				screen.queryByTestId("reference-line-Work Item Age 50%"),
			).not.toBeInTheDocument();
		});

		it("swaps to work item age lines and removes the cycle time lines, then round-trips back to cycle time", () => {
			renderWithSelector();

			selectSource("Work Item Age");

			expect(
				screen.getByTestId("reference-line-Work Item Age 50%"),
			).toBeInTheDocument();
			expect(
				screen.getByTestId("reference-line-Work Item Age 95%"),
			).toBeInTheDocument();
			expect(
				screen.queryByTestId("reference-line-50%"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("reference-line-95%"),
			).not.toBeInTheDocument();

			selectSource("Cycle Time");

			expect(screen.getByTestId("reference-line-50%")).toBeInTheDocument();
			expect(screen.getByTestId("reference-line-95%")).toBeInTheDocument();
			expect(
				screen.queryByTestId("reference-line-Work Item Age 50%"),
			).not.toBeInTheDocument();
		});

		it("leaves the pace-band overlay untouched when swapping the reference-line source", () => {
			renderWithSelector();

			fireEvent.click(screen.getByTestId("pace-bands-toggle"));
			expect(screen.getAllByTestId("pace-band").length).toBeGreaterThan(0);

			selectSource("Work Item Age");

			expect(screen.getAllByTestId("pace-band").length).toBeGreaterThan(0);
		});

		it("renders no work item age lines when in-progress work item age percentiles are all zero", () => {
			renderWithSelector({
				workItemAgePercentileValues: [
					{ percentile: 50, value: 0 },
					{ percentile: 70, value: 0 },
					{ percentile: 85, value: 0 },
					{ percentile: 95, value: 0 },
				],
			});

			selectSource("Work Item Age");

			expect(
				screen.queryByTestId("reference-line-Work Item Age 50%"),
			).not.toBeInTheDocument();
			expect(
				screen.queryByTestId("reference-line-Work Item Age 95%"),
			).not.toBeInTheDocument();
			expect(screen.getByTestId("mock-scatter-plot")).toBeInTheDocument();
		});
	});
});
