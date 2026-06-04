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

vi.mock("@mui/x-charts/hooks", () => ({
	useXScale: () => (value: number) => value,
	useYScale: () => (value: number) => value,
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
	markerSize?: number;
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
	doneWork?: number;
	likelihoodPercentage?: number | null;
};

const getMockPoint = (overrides: RawPoint) => ({
	date: overrides.date,
	totalWork: overrides.totalWork ?? 20,
	doneWork: overrides.doneWork ?? 0,
	remainingWork: (overrides.totalWork ?? 20) - (overrides.doneWork ?? 0),
	estimatedItemCount: null,
	forecastHowMany: null,
	likelihoodPercentage:
		overrides.likelihoodPercentage === undefined
			? 90
			: overrides.likelihoodPercentage,
	whenDistribution: null,
});

const getMockHistory = (overrides: {
	points: RawPoint[];
}): DeliveryMetricsHistory =>
	parseDeliveryMetricsHistory({
		deliveryDate: "2026-06-21T00:00:00Z",
		firstSnapshotDate: "2026-06-01T00:00:00Z",
		points: overrides.points.map(getMockPoint),
	});

const trailHistory = getMockHistory({
	points: [
		{ date: "2026-06-01T00:00:00Z", doneWork: 5, likelihoodPercentage: 98 },
		{ date: "2026-06-08T00:00:00Z", doneWork: 10, likelihoodPercentage: 60 },
		{ date: "2026-06-15T00:00:00Z", doneWork: 15, likelihoodPercentage: 10 },
	],
});

const noLikelihoodHistory = getMockHistory({
	points: [
		{ date: "2026-06-01T00:00:00Z", doneWork: 4, likelihoodPercentage: null },
	],
});

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

	it("plots the snapshots as a trail series mapping completion to x and chance of late to y", () => {
		renderSettled(trailHistory);

		const trail = seriesById("trail");
		expect(trail?.data).toEqual([
			{ x: 25, y: 2, id: 0 },
			{ x: 50, y: 40, id: 1 },
			{ x: 75, y: 90, id: 2 },
		]);
		expect(trail?.color).toBe(testTheme.palette.primary.main);
	});

	it("emphasises the latest snapshot as its own larger series", () => {
		renderSettled(trailHistory);

		const latest = seriesById("latest");
		const trail = seriesById("trail");
		expect(latest?.data).toEqual([{ x: 75, y: 90, id: 2 }]);
		expect((latest?.markerSize ?? 0) > (trail?.markerSize ?? 0)).toBe(true);
	});

	it("labels the axes as completion rate and chance of being late on a 0-100 scale", () => {
		render(<DeliveryFeverChart history={trailHistory} />);

		const props = getLatestChartProps();
		const xAxis = props?.xAxis?.[0];
		const yAxis = props?.yAxis?.[0];

		expect(xAxis?.min).toBe(0);
		expect(xAxis?.max).toBe(100);
		expect(xAxis?.label).toMatch(/completion/i);
		expect(yAxis?.min).toBe(0);
		expect(yAxis?.max).toBe(100);
		expect(yAxis?.label).toMatch(/late/i);
	});

	it("never dispatches a series with no points mid-animation", () => {
		render(<DeliveryFeverChart history={trailHistory} />);

		const everyHasData = (getLatestChartProps()?.series ?? []).every(
			(entry) => (entry.data?.length ?? 0) > 0,
		);
		expect(everyHasData).toBe(true);
	});

	it("shows the forward-only empty state and no chart when no likelihood was recorded", () => {
		render(<DeliveryFeverChart history={noLikelihoodHistory} />);

		expect(
			screen.getByText(/no likelihood snapshots recorded yet/i),
		).toBeInTheDocument();
		expect(scatterChartMock).not.toHaveBeenCalled();
	});

	it("carries the delivery-fever-chart test id on its root", () => {
		render(<DeliveryFeverChart history={trailHistory} />);

		expect(screen.getByTestId("delivery-fever-chart")).toBeInTheDocument();
	});
});

const visibleBubbleCount = (): number => seriesById("trail")?.data?.length ?? 0;

describe("DeliveryFeverChart animation", () => {
	beforeEach(() => {
		scatterChartMock.mockClear();
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("reveals the trail snapshots progressively over time and settles on the full trail", () => {
		render(<DeliveryFeverChart history={trailHistory} />);

		expect(visibleBubbleCount()).toBe(1);

		act(() => {
			vi.advanceTimersByTime(60_000);
		});

		expect(visibleBubbleCount()).toBe(trailHistory.points.length);
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

	it("restarts the reveal when the point count changes", () => {
		const { result, rerender } = renderHook(
			({ count }) => useFeverTrailAnimation(count),
			{ initialProps: { count: 1 } },
		);

		expect(result.current).toBe(1);

		rerender({ count: 4 });
		act(() => {
			vi.advanceTimersByTime(60_000);
		});

		expect(result.current).toBe(4);
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
