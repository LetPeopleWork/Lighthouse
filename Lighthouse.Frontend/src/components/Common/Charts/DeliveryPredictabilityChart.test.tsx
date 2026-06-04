import { fireEvent, render, screen } from "@testing-library/react";
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
import { FORECAST_LEVEL_THRESHOLDS } from "../Forecasts/ForecastLevel";

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

const referenceLineMock = vi.hoisted(() =>
	vi.fn(({ label }) => <div data-testid="mock-reference-line">{label}</div>),
);

vi.mock("@mui/x-charts", () => ({
	ChartsReferenceLine: referenceLineMock,
}));

import DeliveryPredictabilityChart from "./DeliveryPredictabilityChart";

interface SeriesEntry {
	id?: string;
	label?: string;
	data?: Array<number | null>;
	showMark?: boolean;
	color?: string;
	valueFormatter?: (value: number | null) => string;
}

interface AxisEntry {
	data?: Date[];
	scaleType?: string;
	label?: string;
	valueFormatter?: (value: number | null) => string;
	colorMap?: {
		type?: string;
		thresholds?: number[];
		colors?: string[];
	};
}

const LIKELIHOOD_SERIES_ID = "likelihood";
const WHEN_70_SERIES_ID = "when-70";

const getLatestReferenceLineProps = () => {
	const lastCall =
		referenceLineMock.mock.calls[referenceLineMock.mock.calls.length - 1];
	return lastCall?.[0] as
		| { y?: Date; lineStyle?: { strokeDasharray?: string } }
		| undefined;
};

const switchToWhenView = () => {
	fireEvent.click(screen.getByRole("button", { name: /when/i }));
};

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
		expect(colorMap?.thresholds).toEqual([...FORECAST_LEVEL_THRESHOLDS]);
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

const getMockWhenHistory = (
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
				whenDistribution: [
					{ probability: 50, expectedDate: "2026-06-08T00:00:00Z" },
					{ probability: 70, expectedDate: "2026-06-12T00:00:00Z" },
					{ probability: 85, expectedDate: "2026-06-15T00:00:00Z" },
					{ probability: 95, expectedDate: "2026-06-18T00:00:00Z" },
				],
			},
			{
				date: "2026-06-02T00:00:00Z",
				totalWork: 20,
				doneWork: 8,
				remainingWork: 12,
				estimatedItemCount: null,
				forecastHowMany: null,
				likelihoodPercentage: 92,
				whenDistribution: [
					{ probability: 50, expectedDate: "2026-06-07T00:00:00Z" },
					{ probability: 70, expectedDate: "2026-06-09T00:00:00Z" },
					{ probability: 85, expectedDate: "2026-06-11T00:00:00Z" },
					{ probability: 95, expectedDate: "2026-06-13T00:00:00Z" },
				],
			},
		],
	});
	return { ...history, ...overrides };
};

describe("DeliveryPredictabilityChart when view", () => {
	beforeEach(() => {
		lineChartMock.mockClear();
		referenceLineMock.mockClear();
	});

	it("plots the default 70th-percentile completion dates per snapshot when switched to the when view", () => {
		render(<DeliveryPredictabilityChart history={getMockWhenHistory()} />);

		switchToWhenView();

		const props = getLatestChartProps();
		const when70 = props?.series?.find(
			(entry) => entry.id === WHEN_70_SERIES_ID,
		);
		expect(when70?.data).toEqual([
			new Date("2026-06-12T00:00:00Z").getTime(),
			new Date("2026-06-09T00:00:00Z").getTime(),
		]);
	});

	it("plots the when view against a date y-axis", () => {
		render(<DeliveryPredictabilityChart history={getMockWhenHistory()} />);

		switchToWhenView();

		const props = getLatestChartProps();
		expect(props?.yAxis?.[0]?.scaleType).toBe("time");
	});

	it("draws a flat dashed reference line at the delivery target when no per-day target was recorded", () => {
		render(<DeliveryPredictabilityChart history={getMockWhenHistory()} />);

		switchToWhenView();

		const referenceLine = getLatestReferenceLineProps();
		expect(referenceLine?.y?.getTime()).toBe(
			new Date("2026-06-10T00:00:00Z").getTime(),
		);
		expect(referenceLine?.lineStyle?.strokeDasharray).toBeTruthy();
	});

	it("steps the recorded target as a series and drops the flat reference line when the target moved", () => {
		const base = getMockWhenHistory();
		const history = {
			...base,
			points: base.points.map((point, index) => ({
				...point,
				targetDateAtSnapshot:
					index === 0
						? new Date("2026-06-10T00:00:00Z")
						: new Date("2026-06-20T00:00:00Z"),
			})),
		};

		render(<DeliveryPredictabilityChart history={history} />);
		switchToWhenView();

		const target = getLatestChartProps()?.series?.find(
			(entry) => entry.id === "target",
		);
		expect(target?.data).toEqual([
			new Date("2026-06-10T00:00:00Z").getTime(),
			new Date("2026-06-20T00:00:00Z").getTime(),
		]);
		expect(referenceLineMock).not.toHaveBeenCalled();
	});

	it("toggles the rendered series between the likelihood and when views", () => {
		render(<DeliveryPredictabilityChart history={getMockWhenHistory()} />);

		const likelihoodProps = getLatestChartProps();
		expect(
			likelihoodProps?.series?.some(
				(entry) => entry.id === LIKELIHOOD_SERIES_ID,
			),
		).toBe(true);

		switchToWhenView();

		const whenProps = getLatestChartProps();
		expect(
			whenProps?.series?.some((entry) => entry.id === WHEN_70_SERIES_ID),
		).toBe(true);
		expect(
			whenProps?.series?.some((entry) => entry.id === LIKELIHOOD_SERIES_ID),
		).toBe(false);
	});

	it("shows the forward-only empty state in the when view when no point carries a whenDistribution", () => {
		const history = getMockHistory();

		render(<DeliveryPredictabilityChart history={history} />);

		switchToWhenView();

		expect(
			screen.getByText(
				/builds forward from today — no snapshots recorded yet/i,
			),
		).toBeInTheDocument();
	});

	it("shows the forward-only empty state after toggling to the when view on empty history", () => {
		const history = getMockHistory({ firstSnapshotDate: null, points: [] });

		render(<DeliveryPredictabilityChart history={history} />);
		switchToWhenView();

		expect(
			screen.getByText(
				/builds forward from today — no snapshots recorded yet/i,
			),
		).toBeInTheDocument();
	});
});

describe("DeliveryPredictabilityChart axis value formatters", () => {
	beforeEach(() => {
		lineChartMock.mockClear();
	});

	it("formats the likelihood series values as a percentage and blanks nulls", () => {
		render(<DeliveryPredictabilityChart history={getMockHistory()} />);

		const props = getLatestChartProps();
		const formatter = props?.series?.find(
			(entry) => entry.id === LIKELIHOOD_SERIES_ID,
		)?.valueFormatter;

		expect(formatter?.(72)).toBe("72%");
		expect(formatter?.(0)).toBe("0%");
		expect(formatter?.(null)).toBe("");
	});

	it("formats the when-view y-axis values as a date and blanks nulls", () => {
		render(<DeliveryPredictabilityChart history={getMockWhenHistory()} />);

		switchToWhenView();

		const props = getLatestChartProps();
		const formatter = props?.yAxis?.[0]?.valueFormatter;
		const epochMs = new Date("2026-06-09T00:00:00Z").getTime();

		expect(formatter?.(epochMs)).toBe(new Date(epochMs).toLocaleDateString());
		expect(formatter?.(null)).toBe("");
	});
});

describe("DeliveryPredictabilityChart when-view series construction", () => {
	beforeEach(() => {
		lineChartMock.mockClear();
	});

	it("labels one series per percentile and emphasises only the default 70th", () => {
		render(<DeliveryPredictabilityChart history={getMockWhenHistory()} />);

		switchToWhenView();

		const series = getLatestChartProps()?.series ?? [];
		expect(series.map((entry) => entry.label)).toEqual([
			"50%",
			"70%",
			"85%",
			"95%",
		]);
		expect(series.map((entry) => entry.showMark)).toEqual([
			false,
			true,
			false,
			false,
		]);
	});

	it("reads each percentile's completion date per point and gaps points with no distribution", () => {
		const history = getMockWhenHistory();
		history.points[1].whenDistribution = null;

		render(<DeliveryPredictabilityChart history={history} />);

		switchToWhenView();

		const when70 = getLatestChartProps()?.series?.find(
			(entry) => entry.id === WHEN_70_SERIES_ID,
		);
		expect(when70?.data).toEqual([
			new Date("2026-06-12T00:00:00Z").getTime(),
			null,
		]);
	});

	it("treats an empty whenDistribution array as no data and shows the empty state", () => {
		const history = getMockWhenHistory();
		history.points[0].whenDistribution = [];
		history.points[1].whenDistribution = [];

		render(<DeliveryPredictabilityChart history={history} />);

		lineChartMock.mockClear();
		switchToWhenView();

		expect(
			screen.getByText(
				/builds forward from today — no snapshots recorded yet/i,
			),
		).toBeInTheDocument();
		expect(lineChartMock).not.toHaveBeenCalled();
	});
});

describe("DeliveryPredictabilityChart view-toggle guard", () => {
	beforeEach(() => {
		lineChartMock.mockClear();
	});

	it("keeps the current view when the toggle deselects to null", () => {
		render(<DeliveryPredictabilityChart history={getMockWhenHistory()} />);

		switchToWhenView();
		fireEvent.click(screen.getByRole("button", { name: /when/i }));

		const series = getLatestChartProps()?.series ?? [];
		expect(series.some((entry) => entry.id === WHEN_70_SERIES_ID)).toBe(true);
		expect(series.some((entry) => entry.id === LIKELIHOOD_SERIES_ID)).toBe(
			false,
		);
	});
});
