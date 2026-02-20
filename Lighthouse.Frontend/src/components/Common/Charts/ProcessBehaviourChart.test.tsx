import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ProcessBehaviourChartData } from "../../../models/Metrics/ProcessBehaviourChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import { testTheme } from "../../../tests/testTheme";
import ProcessBehaviourChart from "./ProcessBehaviourChart";

// Mock Material UI
vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

// Mock MUI X Charts
vi.mock("@mui/x-charts", () => ({
	ChartsReferenceLine: vi.fn(({ y, label }) => (
		<div data-testid={`reference-line-${label}`} data-y={y}>
			{label}
		</div>
	)),
	LineChart: vi.fn(({ xAxis, series, children }) => (
		<div data-testid="mock-line-chart">
			{xAxis?.[0]?.data?.map((label: string, index: number) => (
				<span
					key={`point-${label}-${String(index)}`}
					data-testid={`chart-point-${index}`}
				>
					{label}: {series?.[0]?.data?.[index]}
				</span>
			))}
			{children}
		</div>
	)),
	ScatterChart: vi.fn(({ series, children }) => (
		<div data-testid="mock-scatter-chart">
			{series?.[0]?.data?.map(
				(point: { x: number; y: number; id: number }, index: number) => (
					<span
						key={`scatter-${point.id}-${String(index)}`}
						data-testid={`scatter-point-${index}`}
					>
						{point.x}: {point.y}
					</span>
				),
			)}
			{children}
		</div>
	)),
	ChartContainer: vi.fn(({ children }) => (
		<div data-testid="mock-chart-container">{children}</div>
	)),
	ChartsXAxis: vi.fn(() => <div data-testid="mock-x-axis" />),
	ChartsYAxis: vi.fn(() => <div data-testid="mock-y-axis" />),
	ChartsTooltip: vi.fn(() => <div data-testid="mock-tooltip" />),
	LinePlot: vi.fn(() => <div data-testid="mock-line-plot" />),
	MarkPlot: vi.fn(
		({
			onItemClick,
		}: {
			onItemClick?: (
				event: React.MouseEvent,
				params: { dataIndex: number },
			) => void;
		}) => (
			<button
				type="button"
				data-testid="mock-mark-plot"
				onClick={(e) =>
					onItemClick?.(e as unknown as React.MouseEvent, { dataIndex: 0 })
				}
			/>
		),
	),
}));

// Mock WorkItemsDialog
vi.mock("../WorkItemsDialog/WorkItemsDialog", () => ({
	default: vi.fn(
		({
			title,
			items,
			open,
		}: {
			title: string;
			items: IWorkItem[];
			open: boolean;
		}) =>
			open ? (
				<div data-testid="work-items-dialog" data-title={title}>
					{items.map((item) => (
						<span key={item.id} data-testid={`dialog-item-${item.id}`}>
							{item.name}
						</span>
					))}
				</div>
			) : null,
	),
}));

// Mock hexToRgba
vi.mock("../../../utils/theme/colors", () => ({
	hexToRgba: vi.fn((color, _opacity) => color),
	getColorMapForKeys: vi.fn(() => ({})),
}));

const createMockWorkItem = (overrides: Partial<IWorkItem> = {}): IWorkItem => ({
	id: 1,
	name: "Test Item",
	state: "Done",
	stateCategory: "Done",
	type: "User Story",
	referenceId: "US-1",
	url: null,
	startedDate: new Date("2026-01-01"),
	closedDate: new Date("2026-01-15"),
	cycleTime: 14,
	workItemAge: 14,
	parentWorkItemReference: "",
	isBlocked: false,
	...overrides,
});

const createReadyChartData = (
	overrides: Partial<ProcessBehaviourChartData> = {},
): ProcessBehaviourChartData => ({
	status: "Ready",
	statusReason: "",
	xAxisKind: "Date",
	average: 10,
	upperNaturalProcessLimit: 20,
	lowerNaturalProcessLimit: 2,
	baselineConfigured: true,
	dataPoints: [
		{
			xValue: "2026-01-15T00:00:00Z",
			yValue: 5,
			specialCauses: [],
			workItemIds: [101],
		},
		{
			xValue: "2026-01-16T00:00:00Z",
			yValue: 12,
			specialCauses: [],
			workItemIds: [102, 103],
		},
		{
			xValue: "2026-01-17T00:00:00Z",
			yValue: 8,
			specialCauses: ["LargeChange"],
			workItemIds: [104],
		},
		{
			xValue: "2026-01-18T00:00:00Z",
			yValue: 15,
			specialCauses: [],
			workItemIds: [105],
		},
	],
	...overrides,
});

describe("ProcessBehaviourChart", () => {
	describe("Baseline Status Handling", () => {
		it("renders baseline missing message when status is BaselineMissing", () => {
			const data: ProcessBehaviourChartData = {
				status: "BaselineMissing",
				statusReason: "No Baseline Configured.",
				xAxisKind: "Date",
				average: 0,
				upperNaturalProcessLimit: 0,
				lowerNaturalProcessLimit: 0,
				baselineConfigured: false,
				dataPoints: [],
			};

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByText(/No Baseline Configured/i)).toBeDefined();
		});

		it("renders baseline invalid message when status is BaselineInvalid", () => {
			const data: ProcessBehaviourChartData = {
				status: "BaselineInvalid",
				statusReason: "Baseline end date is in the future.",
				xAxisKind: "Date",
				average: 0,
				upperNaturalProcessLimit: 0,
				lowerNaturalProcessLimit: 0,
				baselineConfigured: true,
				dataPoints: [],
			};

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(
				screen.getByText(/Baseline end date is in the future/i),
			).toBeDefined();
		});

		it("renders insufficient data message when status is InsufficientData", () => {
			const data: ProcessBehaviourChartData = {
				status: "InsufficientData",
				statusReason: "Not enough data points.",
				xAxisKind: "Date",
				average: 0,
				upperNaturalProcessLimit: 0,
				lowerNaturalProcessLimit: 0,
				baselineConfigured: true,
				dataPoints: [],
			};

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByText(/Not enough data points/i)).toBeDefined();
		});
	});

	describe("Chart Rendering", () => {
		it("renders chart title", () => {
			const data = createReadyChartData();

			render(<ProcessBehaviourChart data={data} title="Throughput" />);

			expect(
				screen.getByText("Throughput Process Behaviour Chart"),
			).toBeDefined();
		});

		it("renders the chart when status is Ready", () => {
			const data = createReadyChartData();

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByTestId("mock-chart-container")).toBeDefined();
		});

		it("does not render chart when status is not Ready", () => {
			const data: ProcessBehaviourChartData = {
				status: "BaselineMissing",
				statusReason: "No Baseline Configured",
				xAxisKind: "Date",
				average: 0,
				upperNaturalProcessLimit: 0,
				lowerNaturalProcessLimit: 0,
				baselineConfigured: false,
				dataPoints: [],
			};

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.queryByTestId("mock-chart-container")).toBeNull();
		});
	});

	describe("Reference Lines", () => {
		it("renders average reference line always without toggle", () => {
			const data = createReadyChartData({ average: 10 });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByTestId("reference-line-Average = 10.0")).toBeDefined();
			expect(screen.queryByLabelText("Average visibility toggle")).toBeNull();
		});

		it("renders UNPL reference line always without toggle", () => {
			const data = createReadyChartData({ upperNaturalProcessLimit: 20 });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByTestId("reference-line-UNPL = 20.0")).toBeDefined();
			expect(screen.queryByLabelText("UNPL visibility toggle")).toBeNull();
		});

		it("renders LNPL reference line when value is greater than 0", () => {
			const data = createReadyChartData({ lowerNaturalProcessLimit: 2 });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByTestId("reference-line-LNPL = 2.0")).toBeDefined();
		});

		it("does not render LNPL reference line when value equals 0", () => {
			const data = createReadyChartData({ lowerNaturalProcessLimit: 0 });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.queryByTestId("reference-line-LNPL")).toBeNull();
		});

		it("displays reference line labels with values", () => {
			const data = createReadyChartData({
				average: 10.5,
				upperNaturalProcessLimit: 20.3,
				lowerNaturalProcessLimit: 2.1,
			});

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByText(/Average/)).toBeDefined();
			expect(screen.getByText(/UNPL/)).toBeDefined();
			expect(screen.getByText(/LNPL/)).toBeDefined();
		});
	});

	describe("Special Cause Chips", () => {
		it("renders special cause chips in upper-right area", () => {
			const data = createReadyChartData({
				dataPoints: [
					{
						xValue: "2026-01-15T00:00:00Z",
						yValue: 5,
						specialCauses: ["LargeChange"],
						workItemIds: [101],
					},
					{
						xValue: "2026-01-16T00:00:00Z",
						yValue: 25,
						specialCauses: [],
						workItemIds: [102],
					},
				],
			});

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByText("Large Change")).toBeDefined();
		});

		it("defaults to the highest available special cause in priority order", () => {
			const data = createReadyChartData({
				dataPoints: [
					{
						xValue: "2026-01-15T00:00:00Z",
						yValue: 5,
						specialCauses: ["SmallShift"],
						workItemIds: [101],
					},
					{
						xValue: "2026-01-16T00:00:00Z",
						yValue: 25,
						specialCauses: ["ModerateShift"],
						workItemIds: [102],
					},
				],
			});

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			const moderateShiftChip = screen.getByText("Moderate Shift");
			expect(moderateShiftChip.closest("[aria-pressed='true']")).toBeDefined();
		});

		it("disables chips with no matching data points", () => {
			const data = createReadyChartData({
				dataPoints: [
					{
						xValue: "2026-01-15T00:00:00Z",
						yValue: 5,
						specialCauses: ["LargeChange"],
						workItemIds: [101],
					},
				],
			});

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			const smallShiftChip = screen.getByText("Small Shift");
			expect(
				smallShiftChip.closest("[aria-disabled='true']") ??
					smallShiftChip.closest("button[disabled]") ??
					smallShiftChip.closest(".Mui-disabled"),
			).toBeTruthy();
		});

		it("toggles to no-highlight state when clicking the selected chip", () => {
			const data = createReadyChartData({
				dataPoints: [
					{
						xValue: "2026-01-15T00:00:00Z",
						yValue: 5,
						specialCauses: ["LargeChange"],
						workItemIds: [101],
					},
				],
			});

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			const chip = screen.getByText("Large Change");
			// Initially selected
			expect(chip.closest("[aria-pressed='true']")).toBeDefined();
			// Click to deselect
			fireEvent.click(chip);
			expect(chip.closest("[aria-pressed='false']")).toBeDefined();
		});

		it("switches selection when clicking a different chip (single-select)", () => {
			const data = createReadyChartData({
				dataPoints: [
					{
						xValue: "2026-01-15T00:00:00Z",
						yValue: 5,
						specialCauses: ["LargeChange"],
						workItemIds: [101],
					},
					{
						xValue: "2026-01-16T00:00:00Z",
						yValue: 8,
						specialCauses: ["SmallShift"],
						workItemIds: [102],
					},
				],
			});

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			const largeChip = screen.getByText("Large Change");
			const smallChip = screen.getByText("Small Shift");

			// Large Change starts selected (highest available)
			expect(largeChip.closest("[aria-pressed='true']")).toBeDefined();
			expect(smallChip.closest("[aria-pressed='false']")).toBeDefined();

			// Click Small Shift
			fireEvent.click(smallChip);
			expect(smallChip.closest("[aria-pressed='true']")).toBeDefined();
			expect(largeChip.closest("[aria-pressed='false']")).toBeDefined();
		});

		it("does not render special cause chips when no data points have special causes", () => {
			const data = createReadyChartData({
				dataPoints: [
					{
						xValue: "2026-01-15T00:00:00Z",
						yValue: 5,
						specialCauses: [],
						workItemIds: [101],
					},
				],
			});

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.queryByText("Large Change")).toBeNull();
			expect(screen.queryByText("Small Shift")).toBeNull();
		});
	});

	describe("Dot Click Drill-In", () => {
		it("opens WorkItemsDialog when clicking a dot with resolved items", () => {
			const mockItems = [
				createMockWorkItem({ id: 101, name: "Item 101" }),
				createMockWorkItem({ id: 102, name: "Item 102" }),
			];

			const data = createReadyChartData({
				dataPoints: [
					{
						xValue: "2026-01-15T00:00:00Z",
						yValue: 5,
						specialCauses: [],
						workItemIds: [101, 102],
					},
				],
			});

			render(
				<ProcessBehaviourChart
					data={data}
					title="Throughput PBC"
					workItemLookup={new Map(mockItems.map((item) => [item.id, item]))}
				/>,
			);

			const markPlot = screen.getByTestId("mock-mark-plot");
			fireEvent.click(markPlot);

			expect(screen.getByTestId("work-items-dialog")).toBeDefined();
			expect(screen.getByTestId("dialog-item-101")).toBeDefined();
			expect(screen.getByTestId("dialog-item-102")).toBeDefined();
		});

		it("does not open dialog when no items can be resolved", () => {
			const data = createReadyChartData({
				dataPoints: [
					{
						xValue: "2026-01-15T00:00:00Z",
						yValue: 5,
						specialCauses: [],
						workItemIds: [999],
					},
				],
			});

			render(
				<ProcessBehaviourChart
					data={data}
					title="Throughput PBC"
					workItemLookup={new Map()}
				/>,
			);

			const markPlot = screen.getByTestId("mock-mark-plot");
			fireEvent.click(markPlot);

			expect(screen.queryByTestId("work-items-dialog")).toBeNull();
		});

		it("does not open dialog when workItemIds is empty", () => {
			const data = createReadyChartData({
				dataPoints: [
					{
						xValue: "2026-01-15T00:00:00Z",
						yValue: 5,
						specialCauses: [],
						workItemIds: [],
					},
				],
			});

			render(
				<ProcessBehaviourChart
					data={data}
					title="Throughput PBC"
					workItemLookup={new Map()}
				/>,
			);

			const markPlot = screen.getByTestId("mock-mark-plot");
			fireEvent.click(markPlot);

			expect(screen.queryByTestId("work-items-dialog")).toBeNull();
		});
	});

	describe("X-Axis Kind Support", () => {
		it("renders with date x-axis kind", () => {
			const data = createReadyChartData({ xAxisKind: "Date" });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByTestId("mock-chart-container")).toBeDefined();
		});

		it("renders with datetime x-axis kind", () => {
			const data = createReadyChartData({
				xAxisKind: "DateTime",
				dataPoints: [
					{
						xValue: "2026-01-15T14:30:00Z",
						yValue: 5,
						specialCauses: [],
						workItemIds: [101],
					},
					{
						xValue: "2026-01-16T09:15:00Z",
						yValue: 12,
						specialCauses: [],
						workItemIds: [102],
					},
				],
			});

			render(<ProcessBehaviourChart data={data} title="Cycle Time PBC" />);

			expect(screen.getByTestId("mock-chart-container")).toBeDefined();
		});
	});

	describe("No Baseline Configured Info Icon", () => {
		it("shows info icon with tooltip when status is Ready and baselineConfigured is false", () => {
			const data = createReadyChartData({ baselineConfigured: false });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByLabelText("No Baseline Configured")).toBeDefined();
		});

		it("does not show info icon when status is Ready and baselineConfigured is true", () => {
			const data = createReadyChartData({ baselineConfigured: true });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.queryByLabelText("No Baseline Configured")).toBeNull();
		});
	});

	describe("Empty Data", () => {
		it("renders no data message when ready but empty data points", () => {
			const data = createReadyChartData({ dataPoints: [] });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByText(/No data available/i)).toBeDefined();
		});
	});

	describe("Cycle Time Equal Spacing", () => {
		it("uses sequential index spacing for DateTime x-axis kind", () => {
			const data = createReadyChartData({
				xAxisKind: "DateTime",
				dataPoints: [
					{
						xValue: "2026-01-10T14:30:00Z",
						yValue: 5,
						specialCauses: [],
						workItemIds: [101],
					},
					{
						xValue: "2026-01-20T09:15:00Z",
						yValue: 12,
						specialCauses: [],
						workItemIds: [102],
					},
					{
						xValue: "2026-01-21T16:45:00Z",
						yValue: 8,
						specialCauses: [],
						workItemIds: [103],
					},
				],
			});

			render(
				<ProcessBehaviourChart
					data={data}
					title="Cycle Time PBC"
					useEqualSpacing={true}
				/>,
			);

			expect(screen.getByTestId("mock-chart-container")).toBeDefined();
		});
	});
});
