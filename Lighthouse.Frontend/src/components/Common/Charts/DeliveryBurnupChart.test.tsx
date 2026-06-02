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
	label?: string;
	data?: Array<number | null>;
}

const getLatestChartProps = () => {
	const lastCall =
		lineChartMock.mock.calls[lineChartMock.mock.calls.length - 1];
	return lastCall?.[0] as
		| {
				series?: SeriesEntry[];
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
				estimatedTotalWork: null,
				forecastHowMany: null,
				likelihoodPercentage: null,
				whenDistribution: null,
			},
			{
				date: "2026-06-02T00:00:00Z",
				totalWork: 20,
				doneWork: 8,
				remainingWork: 12,
				estimatedTotalWork: null,
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
