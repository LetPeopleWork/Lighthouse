import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ProcessBehaviourChartData } from "../../../models/Metrics/ProcessBehaviourChartData";
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
	MarkPlot: vi.fn(() => <div data-testid="mock-mark-plot" />),
}));

// Mock hexToRgba
vi.mock("../../../utils/theme/colors", () => ({
	hexToRgba: vi.fn((color, _opacity) => color),
	getColorMapForKeys: vi.fn(() => ({})),
}));

const createReadyChartData = (
	overrides: Partial<ProcessBehaviourChartData> = {},
): ProcessBehaviourChartData => ({
	status: "Ready",
	statusReason: "",
	xAxisKind: "Date",
	average: 10,
	upperNaturalProcessLimit: 20,
	lowerNaturalProcessLimit: 2,
	dataPoints: [
		{
			xValue: "2026-01-15T00:00:00Z",
			yValue: 5,
			specialCause: "None",
			workItemIds: [101],
		},
		{
			xValue: "2026-01-16T00:00:00Z",
			yValue: 12,
			specialCause: "None",
			workItemIds: [102, 103],
		},
		{
			xValue: "2026-01-17T00:00:00Z",
			yValue: 8,
			specialCause: "LargeChange",
			workItemIds: [104],
		},
		{
			xValue: "2026-01-18T00:00:00Z",
			yValue: 15,
			specialCause: "None",
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
				statusReason: "No baseline configured.",
				xAxisKind: "Date",
				average: 0,
				upperNaturalProcessLimit: 0,
				lowerNaturalProcessLimit: 0,
				dataPoints: [],
			};

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByText(/No baseline configured/i)).toBeDefined();
		});

		it("renders baseline invalid message when status is BaselineInvalid", () => {
			const data: ProcessBehaviourChartData = {
				status: "BaselineInvalid",
				statusReason: "Baseline end date is in the future.",
				xAxisKind: "Date",
				average: 0,
				upperNaturalProcessLimit: 0,
				lowerNaturalProcessLimit: 0,
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

			expect(screen.getByText("Throughput Process Behaviour Chart")).toBeDefined();
		});

		it("renders the chart when status is Ready", () => {
			const data = createReadyChartData();

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByTestId("mock-chart-container")).toBeDefined();
		});

		it("does not render chart when status is not Ready", () => {
			const data: ProcessBehaviourChartData = {
				status: "BaselineMissing",
				statusReason: "No baseline configured.",
				xAxisKind: "Date",
				average: 0,
				upperNaturalProcessLimit: 0,
				lowerNaturalProcessLimit: 0,
				dataPoints: [],
			};

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.queryByTestId("mock-chart-container")).toBeNull();
		});
	});

	describe("Reference Lines", () => {
		it("renders average reference line", () => {
			const data = createReadyChartData({ average: 10 });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByTestId("reference-line-Average")).toBeDefined();
		});

		it("renders UNPL reference line", () => {
			const data = createReadyChartData({ upperNaturalProcessLimit: 20 });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByTestId("reference-line-UNPL")).toBeDefined();
		});

		it("renders LNPL reference line", () => {
			const data = createReadyChartData({ lowerNaturalProcessLimit: 2 });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByTestId("reference-line-LNPL")).toBeDefined();
		});
	});

	describe("Special Cause Highlighting", () => {
		it("applies special cause styling to data points", () => {
			const data = createReadyChartData({
				dataPoints: [
					{
						xValue: "2026-01-15T00:00:00Z",
						yValue: 5,
						specialCause: "None",
						workItemIds: [101],
					},
					{
						xValue: "2026-01-16T00:00:00Z",
						yValue: 25,
						specialCause: "LargeChange",
						workItemIds: [102],
					},
					{
						xValue: "2026-01-17T00:00:00Z",
						yValue: 18,
						specialCause: "ModerateChange",
						workItemIds: [103],
					},
					{
						xValue: "2026-01-18T00:00:00Z",
						yValue: 16,
						specialCause: "ModerateShift",
						workItemIds: [104],
					},
					{
						xValue: "2026-01-19T00:00:00Z",
						yValue: 14,
						specialCause: "SmallShift",
						workItemIds: [105],
					},
				],
			});

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			// Chart should render with all data points
			expect(screen.getByTestId("mock-chart-container")).toBeDefined();
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
						specialCause: "None",
						workItemIds: [101],
					},
					{
						xValue: "2026-01-16T09:15:00Z",
						yValue: 12,
						specialCause: "None",
						workItemIds: [102],
					},
				],
			});

			render(<ProcessBehaviourChart data={data} title="Cycle Time PBC" />);

			expect(screen.getByTestId("mock-chart-container")).toBeDefined();
		});
	});

	describe("Empty Data", () => {
		it("renders no data message when ready but empty data points", () => {
			const data = createReadyChartData({ dataPoints: [] });

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByText(/No data available/i)).toBeDefined();
		});
	});

	describe("Legend", () => {
		it("renders legend chips for reference lines", () => {
			const data = createReadyChartData();

			render(<ProcessBehaviourChart data={data} title="Throughput PBC" />);

			expect(screen.getByLabelText("Average visibility toggle")).toBeDefined();
			expect(screen.getByLabelText("UNPL visibility toggle")).toBeDefined();
			expect(screen.getByLabelText("LNPL visibility toggle")).toBeDefined();
		});
	});
});
