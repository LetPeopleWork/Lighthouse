import { render, screen } from "@testing-library/react";
import { vi } from "vitest";
import {
	type DeliveryMetricsHistory,
	parseDeliveryMetricsHistory,
} from "../../../models/Delivery/DeliveryMetricsHistory";
import { testTheme } from "../../../tests/testTheme";

const lineChartMock = vi.hoisted(() =>
	vi.fn(({ children }) => (
		<svg data-testid="mock-line-chart">
			<title>Test</title>
			{children}
		</svg>
	)),
);

vi.mock("@mui/material", async () => {
	const actual = await vi.importActual("@mui/material");
	return {
		...actual,
		useTheme: () => testTheme,
	};
});

vi.mock("@mui/x-charts/LineChart", () => ({
	LineChart: lineChartMock,
}));

vi.mock("@mui/x-charts", () => ({
	ChartsReferenceLine: vi.fn(({ label }) => (
		<div data-testid="mock-reference-line">{label}</div>
	)),
}));

import DeliveryBurnupChart from "./DeliveryBurnupChart";

interface SeriesEntry {
	id?: string;
	label?: string;
	data?: Array<number | null>;
	area?: boolean;
	showMark?: boolean;
	color?: string;
}

const ESTIMATED_SERIES_LABEL = "Estimated (not broken down)";
const ESTIMATED_LINE_SELECTOR = '& .MuiLineChart-line[data-series="estimated"]';

interface AxisEntry {
	data?: Date[];
	scaleType?: string;
}

const getLatestChartProps = () => {
	const lastCall =
		lineChartMock.mock.calls[lineChartMock.mock.calls.length - 1];
	return lastCall?.[0] as
		| {
				series?: SeriesEntry[];
				xAxis?: AxisEntry[];
				sx?: Record<string, { strokeDasharray?: string }>;
				children?: unknown;
		  }
		| undefined;
};

const getMockHistory = (
	overrides?: Partial<DeliveryMetricsHistory>,
): DeliveryMetricsHistory => {
	const history = parseDeliveryMetricsHistory({
		deliveryDate: "2026-06-10T00:00:00Z",
		firstSnapshotDate: "2026-06-01T00:00:00Z",
		points: [
			{
				date: "2026-06-01T00:00:00Z",
				totalWork: 20,
				doneWork: 0,
				remainingWork: 20,
				estimatedItemCount: null,
				forecastHowMany: null,
				likelihoodPercentage: null,
				whenDistribution: null,
			},
			{
				date: "2026-06-02T00:00:00Z",
				totalWork: 20,
				doneWork: 8,
				remainingWork: 12,
				estimatedItemCount: null,
				forecastHowMany: null,
				likelihoodPercentage: null,
				whenDistribution: null,
			},
		],
	});
	return { ...history, ...overrides };
};

describe("DeliveryBurnupChart", () => {
	beforeEach(() => {
		lineChartMock.mockClear();
	});

	it("renders no delivery-date reference marker on the burnup", () => {
		render(<DeliveryBurnupChart history={getMockHistory()} />);

		expect(screen.queryByTestId("mock-reference-line")).not.toBeInTheDocument();
	});

	it("renders a backlog series and a done series from the metrics history", () => {
		render(<DeliveryBurnupChart history={getMockHistory()} />);

		const props = getLatestChartProps();
		const labels = props?.series?.map((entry) => entry.label) ?? [];

		expect(labels).toContain("Backlog");
		expect(labels).toContain("Done");

		const backlog = props?.series?.find((entry) => entry.label === "Backlog");
		const done = props?.series?.find((entry) => entry.label === "Done");
		expect(backlog?.data).toEqual([20, 20]);
		expect(done?.data).toEqual([0, 8]);
	});

	it("draws Done as a filled area and Backlog as a plain line, both without point markers", () => {
		render(<DeliveryBurnupChart history={getMockHistory()} />);

		const props = getLatestChartProps();
		const backlog = props?.series?.find((entry) => entry.label === "Backlog");
		const done = props?.series?.find((entry) => entry.label === "Done");

		expect(done?.area).toBe(true);
		expect(backlog?.area).toBeFalsy();
		expect(backlog?.showMark).toBe(false);
		expect(done?.showMark).toBe(false);
		expect(backlog?.color).toBe(testTheme.palette.text.secondary);
		expect(done?.color).toBe(testTheme.palette.primary.main);
	});

	it("plots the snapshot dates on a time-scaled x-axis", () => {
		render(<DeliveryBurnupChart history={getMockHistory()} />);

		const props = getLatestChartProps();
		const axis = props?.xAxis?.[0];
		expect(axis?.scaleType).toBe("time");
		expect(axis?.data).toEqual([
			new Date("2026-06-01T00:00:00Z"),
			new Date("2026-06-02T00:00:00Z"),
		]);
	});

	it("uses the default Delivery Burnup title when none is provided", () => {
		render(<DeliveryBurnupChart history={getMockHistory()} />);

		expect(screen.getByText("Delivery Burnup")).toBeInTheDocument();
	});

	it("renders the provided title over the default", () => {
		render(
			<DeliveryBurnupChart history={getMockHistory()} title="Custom Title" />,
		);

		expect(screen.getByText("Custom Title")).toBeInTheDocument();
		expect(screen.queryByText("Delivery Burnup")).not.toBeInTheDocument();
	});

	it("plots the dotted estimated-items line when points carry an estimated count", () => {
		const history = getMockHistory();
		history.points[0].estimatedItemCount = 12;
		history.points[1].estimatedItemCount = 7;

		render(<DeliveryBurnupChart history={history} />);

		const props = getLatestChartProps();
		const estimated = props?.series?.find(
			(entry) => entry.label === ESTIMATED_SERIES_LABEL,
		);
		expect(estimated?.data).toEqual([12, 7]);
		expect(estimated?.id).toBe("estimated");
		expect(props?.sx?.[ESTIMATED_LINE_SELECTOR]?.strokeDasharray).toBeTruthy();
		expect(estimated?.color).not.toBe(testTheme.palette.text.secondary);
		expect(estimated?.color).not.toBe(testTheme.palette.primary.main);
	});

	it("gaps the dotted line where a point is fully broken down", () => {
		const history = getMockHistory();
		history.points[0].estimatedItemCount = 9;
		history.points[1].estimatedItemCount = 0;

		render(<DeliveryBurnupChart history={history} />);

		const props = getLatestChartProps();
		const estimated = props?.series?.find(
			(entry) => entry.label === ESTIMATED_SERIES_LABEL,
		);
		expect(estimated?.data).toEqual([9, null]);
	});

	it("renders no estimated series when no point carries an estimated count", () => {
		render(<DeliveryBurnupChart history={getMockHistory()} />);

		const props = getLatestChartProps();
		const labels = props?.series?.map((entry) => entry.label) ?? [];
		expect(labels).not.toContain(ESTIMATED_SERIES_LABEL);
	});

	it("shows the forward-only empty state when no snapshots exist", () => {
		render(
			<DeliveryBurnupChart
				history={getMockHistory({ firstSnapshotDate: null, points: [] })}
			/>,
		);

		expect(
			screen.getByText(
				/builds forward from today — no snapshots recorded yet/i,
			),
		).toBeInTheDocument();
		expect(lineChartMock).not.toHaveBeenCalled();
	});
});
