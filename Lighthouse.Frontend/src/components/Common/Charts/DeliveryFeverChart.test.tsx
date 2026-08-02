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
import { deliveryEpicColors } from "./deliveryEpicColors";
import { useFeatureFeverReveal } from "./useFeatureFeverReveal";

// Children are rendered: the zone bands are passed as children and paint through the chart's scales,
// so a mock that drops them would leave the bands untested.
const scatterChartMock = vi.hoisted(() =>
	vi.fn((props: { series?: unknown; children?: React.ReactNode }) => (
		<svg data-testid="mock-scatter-chart">
			<title>Test</title>
			{props.children}
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
	valueFormatter?: (value: { x: number; y: number }) => string;
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

	it("shows a single bubble per feature at its latest snapshot by default", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		expect(seriesById("F-1")?.data).toEqual([{ x: 60, y: 5, id: 0 }]);
		expect(seriesById("F-2")?.data).toEqual([{ x: 40, y: 70, id: 0 }]);
	});

	it("labels each feature series with its name and a distinct colour", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		expect(seriesById("F-1")?.label).toBe("Checkout");
		expect(seriesById("F-2")?.label).toBe("Search");
		expect(seriesById("F-1")?.color).not.toBe(seriesById("F-2")?.color);
	});

	it("colours each feature from the delivery-wide epic palette", () => {
		// Review 2026-08-02: the size chart on the same tab colours the same epics. Both read this map,
		// so an epic keeps one colour across the tab instead of one per chart.
		//
		// F-0 is un-forecastable and so never plotted here — and sorts ahead of the two that are. A map
		// built from this chart's own features would skip it and shift F-1 and F-2 a slot, which is the
		// drift being fixed; the fixture fails against that map rather than agreeing with it by accident.
		const historyWithAnUnforecastableEpic: DeliveryMetricsHistory =
			parseDeliveryMetricsHistory({
				deliveryDate: "2026-06-21T00:00:00Z",
				firstSnapshotDate: "2026-06-01T00:00:00Z",
				points: [
					{
						...getMockPoint("2026-06-01T00:00:00Z", []),
						featureBreakdown: [
							{
								referenceId: "F-0",
								name: "Migration",
								completion: 5,
								likelihood: null,
							},
							{
								referenceId: "F-1",
								name: "Checkout",
								completion: 20,
								likelihood: 90,
							},
							{
								referenceId: "F-2",
								name: "Search",
								completion: 10,
								likelihood: 50,
							},
						],
					},
				],
			});
		render(<DeliveryFeverChart history={historyWithAnUnforecastableEpic} />);

		const colors = deliveryEpicColors(historyWithAnUnforecastableEpic);
		expect(colors["F-0"]).toBeDefined();
		expect(seriesById("F-1")?.color).toBe(colors["F-1"]);
		expect(seriesById("F-2")?.color).toBe(colors["F-2"]);
	});

	it("moves a single bubble per feature during the animation, never a growing trail", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		act(() => {
			fireEvent.click(screen.getByRole("button", { name: "Run" }));
		});

		expect(seriesById("F-1")?.data).toEqual([{ x: 20, y: 10, id: 0 }]);
		expect(seriesById("F-1")?.data).toHaveLength(1);

		act(() => {
			vi.advanceTimersByTime(60_000);
		});

		expect(seriesById("F-1")?.data).toEqual([{ x: 60, y: 5, id: 0 }]);
		expect(seriesById("F-1")?.data).toHaveLength(1);
	});

	it("formats the tooltip value as the feature's likelihood", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		expect(seriesById("F-1")?.valueFormatter?.({ x: 60, y: 5 })).toBe(
			"95% Likelihood",
		);
	});

	it("leaves only the picked feature on the chart, and restores the rest when it is unpicked", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		act(() => {
			fireEvent.click(screen.getByRole("button", { name: /legend/i }));
		});

		act(() => {
			fireEvent.click(screen.getByRole("button", { name: "Checkout" }));
		});
		expect(seriesById("F-1")).toBeDefined();
		expect(seriesById("F-2")).toBeUndefined();

		act(() => {
			fireEvent.click(screen.getByRole("button", { name: "Checkout" }));
		});
		expect(seriesById("F-1")).toBeDefined();
		expect(seriesById("F-2")).toBeDefined();
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

	it("paints the three fever zones behind the bubbles", () => {
		const { container } = render(
			<DeliveryFeverChart history={twoFeatureHistory} />,
		);

		// Scoped to the chart: the legend's expand icon is a path too.
		const bands =
			container.querySelectorAll('[data-testid="mock-scatter-chart"] path') ??
			[];
		const fills = [...bands].map((band) => band.getAttribute("fill"));
		expect(fills).toEqual([
			testTheme.palette.success.main,
			testTheme.palette.warning.main,
			testTheme.palette.error.main,
		]);
	});

	it("names itself Delivery Progress when the caller passes no title", () => {
		render(<DeliveryFeverChart history={twoFeatureHistory} />);

		expect(
			screen.getByRole("heading", { name: "Delivery Progress" }),
		).toBeInTheDocument();
	});

	it("offers no run control when a single snapshot leaves nothing to animate", () => {
		const oneSnapshot = parseDeliveryMetricsHistory({
			deliveryDate: "2026-06-21T00:00:00Z",
			firstSnapshotDate: "2026-06-01T00:00:00Z",
			points: [
				getMockPoint("2026-06-01T00:00:00Z", [
					{
						referenceId: "F-1",
						name: "Checkout",
						completion: 20,
						likelihood: 90,
					},
				]),
			],
		});
		render(<DeliveryFeverChart history={oneSnapshot} />);

		expect(screen.queryByRole("button", { name: /^Run$/ })).toBeNull();
		expect(screen.getByTestId("delivery-fever-chart")).toBeInTheDocument();
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

	it("stays on the latest without a timer when there is only one frame", () => {
		const { result } = renderHook(() => useFeatureFeverReveal(1));

		act(() => {
			result.current.run();
		});

		expect(result.current.frame).toBeNull();
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
