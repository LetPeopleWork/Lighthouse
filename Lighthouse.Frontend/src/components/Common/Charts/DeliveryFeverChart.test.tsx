import {
	act,
	fireEvent,
	render,
	renderHook,
	screen,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
	type DeliveryMetricsHistory,
	parseDeliveryMetricsHistory,
} from "../../../models/Delivery/DeliveryMetricsHistory";
import { testTheme } from "../../../tests/testTheme";
import { useFeatureFeverReveal } from "./useFeatureFeverReveal";

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
		| { series?: SeriesEntry[]; xAxis?: AxisEntry[]; yAxis?: AxisEntry[] }
		| undefined;
};

const seriesById = (id: string): SeriesEntry | undefined =>
	getLatestChartProps()?.series?.find((entry) => entry.id === id);

type RawFeature = {
	referenceId: string;
	name: string;
	completion: number;
	likelihood: number;
};

const getMockPoint = (date: string, featureBreakdown: RawFeature[]) => ({
	date,
	totalWork: 20,
	doneWork: 0,
	remainingWork: 20,
	estimatedItemCount: null,
	forecastHowMany: null,
	likelihoodPercentage: null,
	whenDistribution: null,
	featureBreakdown,
});

const twoFeatureHistory: DeliveryMetricsHistory = parseDeliveryMetricsHistory({
	deliveryDate: "2026-06-21T00:00:00Z",
	firstSnapshotDate: "2026-06-01T00:00:00Z",
	points: [
		getMockPoint("2026-06-01T00:00:00Z", [
			{ referenceId: "F-1", name: "Checkout", completion: 20, likelihood: 90 },
			{ referenceId: "F-2", name: "Search", completion: 10, likelihood: 50 },
		]),
		getMockPoint("2026-06-08T00:00:00Z", [
			{ referenceId: "F-1", name: "Checkout", completion: 60, likelihood: 95 },
			{ referenceId: "F-2", name: "Search", completion: 40, likelihood: 30 },
		]),
	],
});

const emptyHistory: DeliveryMetricsHistory = parseDeliveryMetricsHistory({
	deliveryDate: "2026-06-21T00:00:00Z",
	firstSnapshotDate: "2026-06-01T00:00:00Z",
	points: [getMockPoint("2026-06-01T00:00:00Z", [])],
});

describe("DeliveryFeverChart", () => {
	beforeEach(() => {
		scatterChartMock.mockClear();
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("shows one bubble per feature at its latest snapshot by default", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		const checkout = seriesById("F-1");
		const search = seriesById("F-2");
		expect(checkout?.data).toEqual([{ x: 60, y: 5, id: 0 }]);
		expect(search?.data).toEqual([{ x: 40, y: 70, id: 0 }]);
	});

	it("labels each feature series with its name and a distinct colour for the legend", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		const checkout = seriesById("F-1");
		const search = seriesById("F-2");
		expect(checkout?.label).toBe("Checkout");
		expect(search?.label).toBe("Search");
		expect(checkout?.color).not.toBe(search?.color);
	});

	it("reveals each feature's full trail when the animation is run", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		act(() => {
			fireEvent.click(screen.getByRole("button", { name: "Run" }));
		});
		act(() => {
			vi.advanceTimersByTime(60_000);
		});

		expect(seriesById("F-1")?.data).toHaveLength(2);
		expect(seriesById("F-2")?.data).toHaveLength(2);
	});

	it("returns to one bubble per feature when showing the latest again", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		act(() => {
			fireEvent.click(screen.getByRole("button", { name: "Run" }));
		});
		act(() => {
			vi.advanceTimersByTime(60_000);
		});
		act(() => {
			fireEvent.click(screen.getByRole("button", { name: "Show latest" }));
		});

		expect(seriesById("F-1")?.data).toHaveLength(1);
	});

	it("labels the axes as completion rate and chance of being late on a 0-100 scale", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		const props = getLatestChartProps();
		expect(props?.xAxis?.[0]?.min).toBe(0);
		expect(props?.xAxis?.[0]?.max).toBe(100);
		expect(props?.xAxis?.[0]?.label).toMatch(/completion/i);
		expect(props?.yAxis?.[0]?.label).toMatch(/late/i);
	});

	it("shows the forward-only empty state and no chart when no feature was recorded", () => {
		render(<DeliveryFeverChart history={emptyHistory} />);

		expect(
			screen.getByText(/no feature snapshots recorded yet/i),
		).toBeInTheDocument();
		expect(scatterChartMock).not.toHaveBeenCalled();
	});

	it("carries the delivery-fever-chart test id on its root", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		expect(screen.getByTestId("delivery-fever-chart")).toBeInTheDocument();
	});
});

describe("useFeatureFeverReveal", () => {
	beforeEach(() => {
		vi.useFakeTimers();
	});

	afterEach(() => {
		vi.useRealTimers();
	});

	it("is idle and showing the latest by default", () => {
		const { result } = renderHook(() => useFeatureFeverReveal(3));

		expect(result.current.frame).toBeNull();
		expect(result.current.isRunning).toBe(false);
	});

	it("advances the frame to the last index and then stops", () => {
		const { result } = renderHook(() => useFeatureFeverReveal(3));

		act(() => {
			result.current.run();
		});
		expect(result.current.frame).toBe(0);
		expect(result.current.isRunning).toBe(true);

		act(() => {
			vi.advanceTimersByTime(60_000);
		});
		expect(result.current.frame).toBe(2);
		expect(result.current.isRunning).toBe(false);
	});

	it("returns to the latest when showLatest is called", () => {
		const { result } = renderHook(() => useFeatureFeverReveal(3));

		act(() => {
			result.current.run();
		});
		act(() => {
			result.current.showLatest();
		});

		expect(result.current.frame).toBeNull();
		expect(result.current.isRunning).toBe(false);
	});

	it("reveals a single-frame chart immediately without a timer", () => {
		const { result } = renderHook(() => useFeatureFeverReveal(1));

		act(() => {
			result.current.run();
		});

		expect(result.current.frame).toBe(0);
		expect(vi.getTimerCount()).toBe(0);
	});

	it("clears the interval on unmount", () => {
		const { result, unmount } = renderHook(() => useFeatureFeverReveal(4));

		act(() => {
			result.current.run();
		});
		unmount();

		act(() => {
			vi.advanceTimersByTime(60_000);
		});
		expect(vi.getTimerCount()).toBe(0);
	});
});
