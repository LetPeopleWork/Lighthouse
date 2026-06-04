import { render, screen } from "@testing-library/react";
import { vi } from "vitest";
import {
	type DeliveryMetricsHistory,
	parseDeliveryMetricsHistory,
} from "../../../models/Delivery/DeliveryMetricsHistory";
import { testTheme } from "../../../tests/testTheme";
import {
	certainColor,
	confidentColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";

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

import DeliveryPredictabilityChart from "./DeliveryPredictabilityChart";

interface SeriesEntry {
	id?: string;
	label?: string;
	data?: Array<number | null>;
	showMark?: boolean;
	color?: string;
}

interface AxisEntry {
	data?: Date[];
	scaleType?: string;
	colorMap?: {
		type?: string;
		thresholds?: number[];
		colors?: string[];
	};
}

const LIKELIHOOD_SERIES_ID = "likelihood";

const getLatestChartProps = () => {
	const lastCall =
		lineChartMock.mock.calls[lineChartMock.mock.calls.length - 1];
	return lastCall?.[0] as
		| {
				series?: SeriesEntry[];
				xAxis?: AxisEntry[];
				yAxis?: AxisEntry[];
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
				likelihoodPercentage: 40,
				whenDistribution: null,
			},
			{
				date: "2026-06-02T00:00:00Z",
				totalWork: 20,
				doneWork: 8,
				remainingWork: 12,
				estimatedItemCount: null,
				forecastHowMany: null,
				likelihoodPercentage: 92,
				whenDistribution: null,
			},
		],
	});
	return { ...history, ...overrides };
};

describe("DeliveryPredictabilityChart likelihood view", () => {
	beforeEach(() => {
		lineChartMock.mockClear();
	});

	it("plots the likelihood-over-time line from each point's likelihoodPercentage", () => {
		render(<DeliveryPredictabilityChart history={getMockHistory()} />);

		const props = getLatestChartProps();
		const likelihood = props?.series?.find(
			(entry) => entry.id === LIKELIHOOD_SERIES_ID,
		);
		expect(likelihood?.data).toEqual([40, 92]);
	});

	it("gaps the line on points whose likelihoodPercentage is null", () => {
		const history = getMockHistory();
		history.points[0].likelihoodPercentage = null;

		render(<DeliveryPredictabilityChart history={history} />);

		const props = getLatestChartProps();
		const likelihood = props?.series?.find(
			(entry) => entry.id === LIKELIHOOD_SERIES_ID,
		);
		expect(likelihood?.data).toEqual([null, 92]);
	});

	it("RAG-bands the line via ForecastLevel thresholds rather than a re-implemented table", () => {
		render(<DeliveryPredictabilityChart history={getMockHistory()} />);

		const props = getLatestChartProps();
		const colorMap = props?.yAxis?.[0]?.colorMap;

		expect(colorMap?.type).toBe("piecewise");
		expect(colorMap?.thresholds).toEqual([50, 70, 85]);
		expect(colorMap?.colors).toEqual([
			riskyColor,
			realisticColor,
			confidentColor,
			certainColor,
		]);
	});

	it("shows the forward-only empty state when no point carries a likelihood", () => {
		const history = getMockHistory();
		history.points[0].likelihoodPercentage = null;
		history.points[1].likelihoodPercentage = null;

		render(<DeliveryPredictabilityChart history={history} />);

		expect(
			screen.getByText(
				/builds forward from today — no snapshots recorded yet/i,
			),
		).toBeInTheDocument();
		expect(lineChartMock).not.toHaveBeenCalled();
	});

	it("shows the forward-only empty state when there are no snapshots", () => {
		render(
			<DeliveryPredictabilityChart
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

	it("uses the default Delivery Predictability title when none is provided", () => {
		render(<DeliveryPredictabilityChart history={getMockHistory()} />);

		expect(screen.getByText("Delivery Predictability")).toBeInTheDocument();
	});
});
