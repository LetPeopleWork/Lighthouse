import { act, render, renderHook, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	type DeliveryMetricsHistory,
	parseDeliveryMetricsHistory,
} from "../../../models/Delivery/DeliveryMetricsHistory";
import { testTheme } from "../../../tests/testTheme";
import { useFeverTrailAnimation } from "./useFeverTrailAnimation";

const scatterChartMock = vi.hoisted(() =>
	vi.fn((_props: { series?: unknown }) => (
		<svg data-testid="mock-scatter-chart">
			<title>Test</title>
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

vi.mock("@mui/x-charts/ScatterChart", () => ({
	ScatterChart: scatterChartMock,
}));

import DeliveryFeverChart from "./DeliveryFeverChart";

interface ScatterDatum {
	x: number;
	y: number;
	id: number | string;
}

interface SeriesEntry {
	id?: string;
	label?: string;
	color?: string;
	data?: ScatterDatum[];
}

interface AxisEntry {
	min?: number;
	max?: number;
	label?: string;
}

const getLatestChartProps = () => {
	const lastCall =
		scatterChartMock.mock.calls[scatterChartMock.mock.calls.length - 1];
	return lastCall?.[0] as
		| {
				series?: SeriesEntry[];
				xAxis?: AxisEntry[];
				yAxis?: AxisEntry[];
		  }
		| undefined;
};

const seriesById = (id: string): SeriesEntry | undefined =>
	getLatestChartProps()?.series?.find((entry) => entry.id === id);

type RawPoint = {
	date: string;
	totalWork?: number;
	remainingWork?: number;
};

const getMockPoint = (overrides: RawPoint) => ({
	date: overrides.date,
	totalWork: overrides.totalWork ?? 20,
	doneWork: 0,
	remainingWork: overrides.remainingWork ?? 20,
	estimatedItemCount: null,
	forecastHowMany: null,
	likelihoodPercentage: null,
	whenDistribution: null,
});

const getMockHistory = (overrides: {
	firstSnapshotDate?: string | null;
	deliveryDate?: string;
	points: RawPoint[];
}): DeliveryMetricsHistory =>
	parseDeliveryMetricsHistory({
		deliveryDate: overrides.deliveryDate ?? "2026-06-11T00:00:00Z",
		firstSnapshotDate:
			overrides.firstSnapshotDate === undefined
				? "2026-06-01T00:00:00Z"
				: overrides.firstSnapshotDate,
		points: overrides.points.map(getMockPoint),
	});

const onTrackHistory = getMockHistory({
	points: [
		{ date: "2026-06-01T00:00:00Z", totalWork: 20, remainingWork: 20 },
		{ date: "2026-06-04T00:00:00Z", totalWork: 20, remainingWork: 13 },
		{ date: "2026-06-08T00:00:00Z", totalWork: 20, remainingWork: 6 },
		{ date: "2026-06-11T00:00:00Z", totalWork: 20, remainingWork: 0 },
	],
});

const degradingHistory = getMockHistory({
	points: [
		{ date: "2026-06-01T00:00:00Z", totalWork: 20, remainingWork: 20 },
		{ date: "2026-06-04T00:00:00Z", totalWork: 20, remainingWork: 18 },
		{ date: "2026-06-08T00:00:00Z", totalWork: 20, remainingWork: 18 },
		{ date: "2026-06-11T00:00:00Z", totalWork: 20, remainingWork: 16 },
	],
});

const emptyHistory = getMockHistory({ firstSnapshotDate: null, points: [] });

const SETTLE_MS = 60_000;

const renderSettled = (history: DeliveryMetricsHistory): void => {
	render(<DeliveryFeverChart history={history} />);
	act(() => {
		vi.advanceTimersByTime(SETTLE_MS);
	});
};

describe("DeliveryFeverChart", () => {
	beforeEach(() => {
		scatterChartMock.mockClear();
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("partitions the degrading trail into green, amber and red series coloured by zone", () => {
		renderSettled(degradingHistory);

		const green = seriesById("green");
		const amber = seriesById("amber");
		const red = seriesById("red");

		expect(green?.color).toBe(testTheme.palette.success.main);
		expect(amber?.color).toBe(testTheme.palette.warning.main);
		expect(red?.color).toBe(testTheme.palette.error.main);

		const total =
			(green?.data?.length ?? 0) +
			(amber?.data?.length ?? 0) +
			(red?.data?.length ?? 0);
		expect(total).toBe(degradingHistory.points.length);
		expect(green?.data?.length).toBeGreaterThan(0);
		expect(amber?.data?.length).toBeGreaterThan(0);
		expect(red?.data?.length).toBeGreaterThan(0);
	});

	it("places every on-track bubble in the green series and none in red", () => {
		renderSettled(onTrackHistory);

		const green = seriesById("green");
		const red = seriesById("red");

		expect(green?.data?.length).toBe(onTrackHistory.points.length);
		expect(red?.data?.length ?? 0).toBe(0);
	});

	it("plots each bubble at schedule-consumed x and work-remaining y", () => {
		renderSettled(onTrackHistory);

		const allData = (getLatestChartProps()?.series ?? [])
			.filter((entry) => entry.id !== "latest")
			.flatMap((entry) => entry.data ?? []);
		const byX = [...allData].sort((left, right) => left.x - right.x);

		expect(byX[0]).toMatchObject({ x: 0, y: 100 });
		expect(byX[byX.length - 1]).toMatchObject({ x: 100, y: 0 });
	});

	it("emphasises the latest bubble as its own series", () => {
		renderSettled(degradingHistory);

		const latest = seriesById("latest");
		expect(latest?.data?.length).toBe(1);
		expect(latest?.data?.[0]).toMatchObject({ x: 100 });
	});

	it("fixes both axes to a zero-to-hundred percentage scale with labels", () => {
		render(<DeliveryFeverChart history={degradingHistory} />);

		const props = getLatestChartProps();
		const xAxis = props?.xAxis?.[0];
		const yAxis = props?.yAxis?.[0];

		expect(xAxis?.min).toBe(0);
		expect(xAxis?.max).toBe(100);
		expect(xAxis?.label).toBe("% schedule consumed");
		expect(yAxis?.min).toBe(0);
		expect(yAxis?.max).toBe(100);
		expect(yAxis?.label).toBe("% work remaining");
	});

	it("names the green, amber and red zones in a caption", () => {
		render(<DeliveryFeverChart history={degradingHistory} />);

		expect(screen.getByText(/green/i)).toBeInTheDocument();
		expect(screen.getByText(/amber/i)).toBeInTheDocument();
		expect(screen.getByText(/red/i)).toBeInTheDocument();
	});

	it("shows the forward-only empty state and no chart when the trail is empty", () => {
		render(<DeliveryFeverChart history={emptyHistory} />);

		expect(
			screen.getByText(
				/builds forward from today — no snapshots recorded yet/i,
			),
		).toBeInTheDocument();
		expect(scatterChartMock).not.toHaveBeenCalled();
	});

	it("carries the delivery-fever-chart test id on its root", () => {
		render(<DeliveryFeverChart history={degradingHistory} />);

		expect(screen.getByTestId("delivery-fever-chart")).toBeInTheDocument();
	});
});

const everySeriesCarriesData = (): boolean =>
	(getLatestChartProps()?.series ?? []).every(
		(entry) => (entry.data?.length ?? 0) > 0,
	);

describe("DeliveryFeverChart empty-series guard", () => {
	beforeEach(() => {
		scatterChartMock.mockClear();
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("never dispatches a zone series with no points mid-animation", () => {
		render(<DeliveryFeverChart history={degradingHistory} />);

		expect(everySeriesCarriesData()).toBe(true);
	});

	it("omits the amber and red series when every bubble is in the green zone", () => {
		renderSettled(onTrackHistory);

		expect(everySeriesCarriesData()).toBe(true);
		expect(seriesById("amber")).toBeUndefined();
		expect(seriesById("red")).toBeUndefined();
	});
});

const visibleBubbleCount = (): number =>
	(getLatestChartProps()?.series ?? [])
		.filter((entry) => entry.id !== "latest")
		.reduce((sum, entry) => sum + (entry.data?.length ?? 0), 0);

describe("DeliveryFeverChart animation", () => {
	beforeEach(() => {
		scatterChartMock.mockClear();
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("reveals the trail bubbles progressively over time and settles on the full trail", () => {
		render(<DeliveryFeverChart history={onTrackHistory} />);

		expect(visibleBubbleCount()).toBe(1);

		act(() => {
			vi.advanceTimersByTime(60_000);
		});

		expect(visibleBubbleCount()).toBe(onTrackHistory.points.length);
	});
});

describe("useFeverTrailAnimation", () => {
	beforeEach(() => {
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("advances the visible count one point at a time and stops at the total", () => {
		const { result } = renderHook(() => useFeverTrailAnimation(4));

		expect(result.current).toBe(1);

		act(() => {
			vi.advanceTimersToNextTimer();
		});
		expect(result.current).toBe(2);

		act(() => {
			vi.advanceTimersToNextTimer();
		});
		expect(result.current).toBe(3);

		act(() => {
			vi.advanceTimersToNextTimer();
		});
		expect(result.current).toBe(4);

		act(() => {
			vi.advanceTimersByTime(60_000);
		});
		expect(result.current).toBe(4);
	});

	it("returns zero and registers no timer for an empty trail", () => {
		const { result } = renderHook(() => useFeverTrailAnimation(0));

		expect(result.current).toBe(0);
		expect(vi.getTimerCount()).toBe(0);
	});

	it("reveals a single-point trail immediately without a timer", () => {
		const { result } = renderHook(() => useFeverTrailAnimation(1));

		expect(result.current).toBe(1);
		expect(vi.getTimerCount()).toBe(0);
	});

	it("clears the interval on unmount so the count never advances again", () => {
		const { result, unmount } = renderHook(() => useFeverTrailAnimation(4));

		expect(result.current).toBe(1);
		unmount();

		act(() => {
			vi.advanceTimersByTime(60_000);
		});
		expect(result.current).toBe(1);
		expect(vi.getTimerCount()).toBe(0);
	});
});
