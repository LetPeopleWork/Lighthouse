import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { PercentilesOverTimeSnapshot } from "../../../models/Metrics/PercentilesOverTimeSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { certainColor, riskyColor } from "../../../utils/theme/colors";
import PercentilesOverTimeWidget from "./PercentilesOverTimeWidget";

// Mock MUI-X LineChart (same pattern as LineRunChart.test.tsx). Expose the
// series (colours + per-series point counts) and the x-axis dates so the test
// can assert four dated red-to-green percentile lines without reaching into
// the real SVG renderer.
vi.mock("@mui/x-charts", () => ({
	LineChart: vi.fn(
		({
			xAxis,
			series,
			hideLegend,
		}: {
			xAxis?: { data?: string[] }[];
			series?: {
				label?: string;
				color?: string;
				data?: number[];
				shape?: string;
				showMark?: boolean;
			}[];
			hideLegend?: boolean;
		}) => (
			<div data-testid="mock-line-chart">
				{/* The built-in MUI-X legend is suppressed (hideLegend) so only the
				    custom top-left legend renders — expose the prop to assert it. */}
				<div data-testid="chart-hide-legend">{String(hideLegend)}</div>
				<div data-testid="chart-xaxis">
					{JSON.stringify(xAxis?.[0]?.data ?? [])}
				</div>
				<div data-testid="chart-series">
					{JSON.stringify(
						series?.map((s) => ({
							label: s.label,
							color: s.color,
							points: s.data?.length ?? 0,
							data: s.data ?? [],
							shape: s.shape,
							showMark: s.showMark,
						})) ?? [],
					)}
				</div>
			</div>
		),
	),
}));

const OWNER_ID = 42;

const DATED_SERIES: PercentilesOverTimeSnapshot[] = [
	{
		recordedAt: "2026-05-23",
		metricType: "CycleTime",
		p50: 3,
		p70: 4,
		p85: 6,
		p95: 8,
	},
	{
		recordedAt: "2026-05-24",
		metricType: "CycleTime",
		p50: 3,
		p70: 5,
		p85: 7,
		p95: 9,
	},
	{
		recordedAt: "2026-05-25",
		metricType: "CycleTime",
		p50: 4,
		p70: 5,
		p85: 7,
		p95: 10,
	},
];

function createMetricsService(
	getPercentilesOverTime: ReturnType<typeof vi.fn>,
): IMetricsService<IWorkItem | IFeature> {
	return {
		getPercentilesOverTime,
	} as unknown as IMetricsService<IWorkItem | IFeature>;
}

describe("PercentilesOverTimeWidget", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("fetches and plots the 30-day horizon by default", async () => {
		const getPercentilesOverTime = vi.fn().mockResolvedValue(DATED_SERIES);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(OWNER_ID, 30),
		);

		// The 30-day chip is the pressed toggle on first paint (AC1). Selection is
		// set explicitly per button (not via ToggleButtonGroup injection), so the
		// Tooltip wrapper does not cost the pressed state.
		expect(screen.getByTestId("percentiles-horizon-30")).toHaveAttribute(
			"aria-pressed",
			"true",
		);
		expect(screen.getByTestId("percentiles-horizon-60")).toHaveAttribute(
			"aria-pressed",
			"false",
		);
		expect(screen.getByTestId("percentiles-horizon-90")).toHaveAttribute(
			"aria-pressed",
			"false",
		);
	});

	it("labels the horizon chips in days with an explanatory cycle-time tooltip", async () => {
		const getPercentilesOverTime = vi.fn().mockResolvedValue(DATED_SERIES);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(OWNER_ID, 30),
		);

		// Visible labels read "{30|60|90} days", not the old "CT-{n}" codes.
		const chip30 = screen.getByTestId("percentiles-horizon-30");
		expect(chip30).toHaveTextContent("30 days");
		expect(screen.getByTestId("percentiles-horizon-60")).toHaveTextContent(
			"60 days",
		);
		expect(screen.getByTestId("percentiles-horizon-90")).toHaveTextContent(
			"90 days",
		);

		// Hovering a chip surfaces the cycle-time explanation.
		fireEvent.mouseOver(chip30);
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			"Cycle Time over the last 30 days",
		);
	});

	it("renders a single legend by suppressing the chart's built-in legend", async () => {
		const getPercentilesOverTime = vi.fn().mockResolvedValue(DATED_SERIES);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await screen.findByTestId("mock-line-chart");

		// Only the custom top-left legend renders; the chart's built-in one is off.
		expect(
			screen.getByTestId("percentiles-over-time-legend"),
		).toBeInTheDocument();
		expect(screen.getByTestId("chart-hide-legend")).toHaveTextContent("true");
	});

	it("gives every percentile line the same default circle marker", async () => {
		const getPercentilesOverTime = vi.fn().mockResolvedValue(DATED_SERIES);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await screen.findByTestId("mock-line-chart");

		const seriesInfo = JSON.parse(
			screen.getByTestId("chart-series").textContent ?? "[]",
		) as { shape: string }[];

		// Uniform marker: colour is the only differentiator, no per-series shape cycle.
		expect(seriesInfo).toHaveLength(4);
		for (const s of seriesInfo) {
			expect(s.shape).toBe("circle");
		}
	});

	it("renders four dated percentile lines with the red-to-green ramp", async () => {
		const getPercentilesOverTime = vi.fn().mockResolvedValue(DATED_SERIES);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await screen.findByTestId("mock-line-chart");

		// One point per calendar day in range (AC2).
		const xAxis = JSON.parse(
			screen.getByTestId("chart-xaxis").textContent ?? "[]",
		);
		expect(xAxis).toEqual(["2026-05-23", "2026-05-24", "2026-05-25"]);

		const seriesInfo = JSON.parse(
			screen.getByTestId("chart-series").textContent ?? "[]",
		) as {
			label: string;
			color: string;
			points: number;
			data: number[];
			showMark: boolean;
		}[];

		// 50/70/85/95, one series each, three points each.
		expect(seriesInfo).toHaveLength(4);
		expect(seriesInfo.map((s) => s.label)).toEqual([
			"50th",
			"70th",
			"85th",
			"95th",
		]);
		for (const s of seriesInfo) {
			expect(s.points).toBe(3);
			// Every percentile line opts into markers (uniform circles).
			expect(s.showMark).toBe(true);
		}
		// Each series plots ITS OWN percentile accessor, in recordedAt order.
		expect(seriesInfo[0].data).toEqual([3, 3, 4]); // p50 across the three days
		expect(seriesInfo[1].data).toEqual([4, 5, 5]); // p70
		expect(seriesInfo[2].data).toEqual([6, 7, 7]); // p85
		expect(seriesInfo[3].data).toEqual([8, 9, 10]); // p95
		// Red at the 50th end, green at the 95th end (D7 ramp).
		expect(seriesInfo[0].color).toBe(riskyColor);
		expect(seriesInfo[3].color).toBe(certainColor);
	});

	it("renders the default title and one swatch per percentile line", async () => {
		const getPercentilesOverTime = vi.fn().mockResolvedValue(DATED_SERIES);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await screen.findByTestId("mock-line-chart");

		// Default title (no title prop supplied).
		expect(screen.getByText("Percentiles Over Time")).toBeInTheDocument();
		// One legend swatch per percentile, keyed by its own test id.
		for (const percentile of [50, 70, 85, 95]) {
			expect(
				screen.getByTestId(`percentile-line-${percentile}`),
			).toBeInTheDocument();
		}
	});

	it("shows neither the chart nor the empty state while the series is still loading", async () => {
		// A promise that never resolves keeps the hook's series === null (loading).
		const getPercentilesOverTime = vi
			.fn()
			.mockReturnValue(new Promise<PercentilesOverTimeSnapshot[]>(() => {}));
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(OWNER_ID, 30),
		);

		// While loading we must render neither the chart nor the honest empty copy —
		// the empty state is reserved for a loaded-but-empty series (series === []).
		expect(screen.queryByTestId("mock-line-chart")).not.toBeInTheDocument();
		expect(
			screen.queryByTestId("percentiles-over-time-empty"),
		).not.toBeInTheDocument();
	});

	it("logs and recovers when the series fetch rejects, showing no chart", async () => {
		const consoleError = vi
			.spyOn(console, "error")
			.mockImplementation(() => {});
		const getPercentilesOverTime = vi.fn().mockRejectedValue(new Error("boom"));
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() => expect(consoleError).toHaveBeenCalled());
		expect(consoleError).toHaveBeenCalledWith(
			"Error fetching percentiles over time:",
			expect.any(Error),
		);
		// A failed fetch leaves the series null → no chart, no crash.
		expect(screen.queryByTestId("mock-line-chart")).not.toBeInTheDocument();
		consoleError.mockRestore();
	});

	it("shows the honest empty-state copy and no chart when no snapshots exist", async () => {
		const getPercentilesOverTime = vi.fn().mockResolvedValue([]);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await screen.findByTestId("percentiles-over-time-empty");
		expect(screen.getByTestId("percentiles-over-time-empty")).toHaveTextContent(
			"builds forward from today — no snapshots recorded yet",
		);
		expect(screen.queryByTestId("mock-line-chart")).not.toBeInTheDocument();
	});

	it("re-plots a persisted horizon on toggle without a second recompute fetch", async () => {
		const getPercentilesOverTime = vi
			.fn()
			.mockImplementation((_ownerId: number, horizon: number) =>
				Promise.resolve(
					DATED_SERIES.map((s) => ({ ...s, metricType: `CT-${horizon}` })),
				),
			);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(OWNER_ID, 30),
		);

		// Switch to CT-60 → one fetch for that horizon (AC5: read-only, per horizon).
		fireEvent.click(screen.getByTestId("percentiles-horizon-60"));
		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(OWNER_ID, 60),
		);
		expect(getPercentilesOverTime).toHaveBeenCalledTimes(2);

		// Switch back to CT-30 → re-plots from the already-fetched series, no new fetch.
		fireEvent.click(screen.getByTestId("percentiles-horizon-30"));
		await waitFor(() =>
			expect(screen.getByTestId("percentiles-horizon-30")).toHaveAttribute(
				"aria-pressed",
				"true",
			),
		);
		expect(getPercentilesOverTime).toHaveBeenCalledTimes(2);
	});
});
