import {
	fireEvent,
	render,
	screen,
	waitFor,
	within,
} from "@testing-library/react";
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

// Work Item Age is as-of-today, so its series carries no horizon dimension.
const AGE_SERIES: PercentilesOverTimeSnapshot[] = [
	{
		recordedAt: "2026-06-01",
		metricType: "WorkItemAge",
		p50: 2,
		p70: 5,
		p85: 9,
		p95: 14,
	},
	{
		recordedAt: "2026-06-02",
		metricType: "WorkItemAge",
		p50: 3,
		p70: 6,
		p85: 10,
		p95: 15,
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

	it("offers the Age tab first while keeping 30 days as the default selection", async () => {
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

		// [ Age | 30 days | 60 days | 90 days ] — Age leads the row (US-03 AC1).
		const chips = within(screen.getByRole("group")).getAllByRole("button");
		expect(chips.map((chip) => chip.getAttribute("data-testid"))).toEqual([
			"percentiles-selection-age",
			"percentiles-horizon-30",
			"percentiles-horizon-60",
			"percentiles-horizon-90",
		]);
		expect(chips[0]).toHaveTextContent("Age");

		// Age leads visually but 30 days stays the default selection — no slice-01
		// assertion (Vitest or E2E) regresses.
		expect(screen.getByTestId("percentiles-selection-age")).toHaveAttribute(
			"aria-pressed",
			"false",
		);
		expect(screen.getByTestId("percentiles-horizon-30")).toHaveAttribute(
			"aria-pressed",
			"true",
		);
	});

	it("explains the Age tab with its own work-item-age tooltip", async () => {
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

		fireEvent.mouseOver(screen.getByTestId("percentiles-selection-age"));
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			"Work Item Age of items in progress today",
		);
	});

	it("requests the age series with no horizon and plots four dated ramp lines", async () => {
		const getPercentilesOverTime = vi
			.fn()
			.mockImplementation((_ownerId: number, selection: string | number) =>
				Promise.resolve(selection === "age" ? AGE_SERIES : DATED_SERIES),
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

		fireEvent.click(screen.getByTestId("percentiles-selection-age"));

		// Age carries no horizon dimension — the request is the bare age selection.
		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(OWNER_ID, "age"),
		);
		expect(screen.getByTestId("percentiles-selection-age")).toHaveAttribute(
			"aria-pressed",
			"true",
		);

		// The Age tab offers no horizon sub-choice — the row keeps exactly the four
		// tabs, no extra horizon control appears for age.
		expect(
			within(screen.getByRole("group")).getAllByRole("button"),
		).toHaveLength(4);

		await waitFor(() =>
			expect(screen.getByTestId("chart-xaxis")).toHaveTextContent("2026-06-01"),
		);
		expect(
			JSON.parse(screen.getByTestId("chart-xaxis").textContent ?? "[]"),
		).toEqual(["2026-06-01", "2026-06-02"]);

		const seriesInfo = JSON.parse(
			screen.getByTestId("chart-series").textContent ?? "[]",
		) as {
			label: string;
			color: string;
			data: number[];
			shape: string;
			showMark: boolean;
		}[];

		// Identical shape to the CT tabs: four percentile lines, uniform circles,
		// red→green ramp, custom legend with the built-in one suppressed (D7).
		expect(seriesInfo.map((s) => s.label)).toEqual([
			"50th",
			"70th",
			"85th",
			"95th",
		]);
		expect(seriesInfo[0].data).toEqual([2, 3]);
		expect(seriesInfo[3].data).toEqual([14, 15]);
		expect(seriesInfo[0].color).toBe(riskyColor);
		expect(seriesInfo[3].color).toBe(certainColor);
		for (const s of seriesInfo) {
			expect(s.shape).toBe("circle");
			expect(s.showMark).toBe(true);
		}
		expect(screen.getByTestId("chart-hide-legend")).toHaveTextContent("true");
		expect(
			screen.getByTestId("percentiles-over-time-legend"),
		).toBeInTheDocument();
	});

	it("shows the honest forward-only empty state on the Age tab when no age snapshots exist", async () => {
		const getPercentilesOverTime = vi
			.fn()
			.mockImplementation((_ownerId: number, selection: string | number) =>
				Promise.resolve(selection === "age" ? [] : DATED_SERIES),
			);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await screen.findByTestId("mock-line-chart");

		fireEvent.click(screen.getByTestId("percentiles-selection-age"));

		// Never a broken axis — the same verbatim D6 copy as the cycle-time tabs.
		await screen.findByTestId("percentiles-over-time-empty");
		expect(screen.getByTestId("percentiles-over-time-empty")).toHaveTextContent(
			"builds forward from today — no snapshots recorded yet",
		);
		expect(screen.queryByTestId("mock-line-chart")).not.toBeInTheDocument();
	});

	it("re-plots Age and the horizons from the per-selection cache without refetching", async () => {
		const getPercentilesOverTime = vi
			.fn()
			.mockImplementation((_ownerId: number, selection: string | number) =>
				Promise.resolve(
					DATED_SERIES.map((s) => ({ ...s, metricType: String(selection) })),
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

		// Age → 60 → 90: one fetch each, cached per SELECTION not per horizon.
		for (const testId of [
			"percentiles-selection-age",
			"percentiles-horizon-60",
			"percentiles-horizon-90",
		]) {
			fireEvent.click(screen.getByTestId(testId));
			await waitFor(() =>
				expect(screen.getByTestId(testId)).toHaveAttribute(
					"aria-pressed",
					"true",
				),
			);
		}
		expect(getPercentilesOverTime).toHaveBeenCalledTimes(4);

		// Back to Age and back to 30: both already cached, so no further fetches.
		fireEvent.click(screen.getByTestId("percentiles-selection-age"));
		await waitFor(() =>
			expect(screen.getByTestId("percentiles-selection-age")).toHaveAttribute(
				"aria-pressed",
				"true",
			),
		);
		fireEvent.click(screen.getByTestId("percentiles-horizon-30"));
		await waitFor(() =>
			expect(screen.getByTestId("percentiles-horizon-30")).toHaveAttribute(
				"aria-pressed",
				"true",
			),
		);
		expect(getPercentilesOverTime).toHaveBeenCalledTimes(4);
	});
});
