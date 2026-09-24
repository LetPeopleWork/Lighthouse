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
import { OptionalFeatureService } from "../../../services/Api/OptionalFeatureService";
import { certainColor, riskyColor } from "../../../utils/theme/colors";
import PercentilesOverTimeWidget, {
	PERCENTILES_OVER_TIME_EMPTY_COPY,
} from "./PercentilesOverTimeWidget";

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

// The dashboard's default range always ends today: BaseMetricsView seeds endDate from
// `new Date()`, so it carries a time of day.
const RANGE_START = new Date(2026, 6, 1);
const RANGE_END = todayAtNoon();

const PAST_RANGE_START = new Date(2026, 4, 1);
const PAST_RANGE_END = new Date(2026, 4, 15);

// An empty chart cannot tell why it is empty: the same empty answer comes back when the
// range predates everything held, when nothing is held at all, when syncing stopped before
// the range, and when the past days are still being filled in the background. Nor can it
// tell whether this instance fills in past days at all, because the widget never asks. The
// copy has to be true of every one of those, whichever range was asked for. Pinned as a
// literal so a blanked or reworded constant fails here.
const HONEST_EMPTY_COPY =
	"Nothing to show for the selected range. Days appear here as Lighthouse records them.";

// The chart no longer only builds forward, so this sentence would now be false.
const RETIRED_FORWARD_ONLY_COPY = "builds forward from today";

// False on an instance that leaves filling in past days switched off, which is every instance
// until an administrator opts in.
const RETIRED_LATER_VISIT_COPY = "fill in on a later visit";

function todayAtNoon(): Date {
	const today = new Date();
	return new Date(today.getFullYear(), today.getMonth(), today.getDate(), 12);
}

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
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				30,
				RANGE_START,
				RANGE_END,
			),
		);

		// The 30-day chip is the pressed toggle on first paint. Selection is
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
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				30,
				RANGE_START,
				RANGE_END,
			),
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
				startDate={RANGE_START}
				endDate={RANGE_END}
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
				startDate={RANGE_START}
				endDate={RANGE_END}
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
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await screen.findByTestId("mock-line-chart");

		// One point per calendar day in range.
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
		// Red at the 50th end, green at the 95th end, as on the point-in-time chart.
		expect(seriesInfo[0].color).toBe(riskyColor);
		expect(seriesInfo[3].color).toBe(certainColor);
	});

	it("renders the default title and one swatch per percentile line", async () => {
		const getPercentilesOverTime = vi.fn().mockResolvedValue(DATED_SERIES);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				startDate={RANGE_START}
				endDate={RANGE_END}
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
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				30,
				RANGE_START,
				RANGE_END,
			),
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
				startDate={RANGE_START}
				endDate={RANGE_END}
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

	it.each([
		{ range: "ending today", startDate: RANGE_START, endDate: RANGE_END },
		{
			range: "ending in the past",
			startDate: PAST_RANGE_START,
			endDate: PAST_RANGE_END,
		},
	])(
		"says only what is true of every empty chart, for a range $range, and draws no chart",
		async ({ startDate, endDate }) => {
			const getPercentilesOverTime = vi.fn().mockResolvedValue([]);
			render(
				<PercentilesOverTimeWidget
					ownerId={OWNER_ID}
					startDate={startDate}
					endDate={endDate}
					metricsService={createMetricsService(getPercentilesOverTime)}
				/>,
			);

			const empty = await screen.findByTestId("percentiles-over-time-empty");
			expect(empty.textContent).toBe(HONEST_EMPTY_COPY);
			expect(empty).not.toHaveTextContent(RETIRED_FORWARD_ONLY_COPY);
			expect(empty).not.toHaveTextContent(RETIRED_LATER_VISIT_COPY);
			expect(screen.queryByTestId("mock-line-chart")).not.toBeInTheDocument();
		},
	);

	it("exports the empty copy verbatim so the end-to-end test asserts the shipped string", () => {
		expect(PERCENTILES_OVER_TIME_EMPTY_COPY).toBe(HONEST_EMPTY_COPY);
	});

	// Whether this instance fills in past days is a backend matter. The widget gets one sentence
	// that is true either way, so it has no reason to ask, and asking would make removing the
	// switch later a frontend change too.
	it("never asks whether this instance fills in past days", async () => {
		const getAllFeatures = vi.spyOn(
			OptionalFeatureService.prototype,
			"getAllFeatures",
		);
		const getFeatureByKey = vi.spyOn(
			OptionalFeatureService.prototype,
			"getFeatureByKey",
		);
		const getPercentilesOverTime = vi.fn().mockResolvedValue([]);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await screen.findByTestId("percentiles-over-time-empty");
		expect(getPercentilesOverTime).toHaveBeenCalled();
		expect(getAllFeatures).not.toHaveBeenCalled();
		expect(getFeatureByKey).not.toHaveBeenCalled();
		getAllFeatures.mockRestore();
		getFeatureByKey.mockRestore();
	});

	it("refetches instead of replaying the cached series when the range changes", async () => {
		const IN_RANGE_SERIES: PercentilesOverTimeSnapshot[] = [
			{
				recordedAt: "2026-05-01",
				metricType: "CycleTime",
				p50: 1,
				p70: 2,
				p85: 3,
				p95: 4,
			},
		];
		const getPercentilesOverTime = vi
			.fn()
			.mockResolvedValueOnce(DATED_SERIES)
			.mockResolvedValueOnce(IN_RANGE_SERIES);

		const { rerender } = render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				30,
				RANGE_START,
				RANGE_END,
			),
		);

		rerender(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				startDate={PAST_RANGE_START}
				endDate={PAST_RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		// A range change is a cache MISS: the previous range's series must never be
		// replayed against the new range — the likeliest bug in a per-selection cache.
		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				30,
				PAST_RANGE_START,
				PAST_RANGE_END,
			),
		);
		await waitFor(() => {
			const seriesInfo = JSON.parse(
				screen.getByTestId("chart-series").textContent ?? "[]",
			);
			expect(seriesInfo[0].points).toBe(IN_RANGE_SERIES.length);
		});
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
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				30,
				RANGE_START,
				RANGE_END,
			),
		);

		// Switch to CT-60 → one read-only fetch for that horizon.
		fireEvent.click(screen.getByTestId("percentiles-horizon-60"));
		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				60,
				RANGE_START,
				RANGE_END,
			),
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
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				30,
				RANGE_START,
				RANGE_END,
			),
		);

		// [ Age | 30 days | 60 days | 90 days ] — Age leads the row.
		const chips = within(screen.getByRole("group")).getAllByRole("button");
		expect(chips.map((chip) => chip.getAttribute("data-testid"))).toEqual([
			"percentiles-selection-age",
			"percentiles-horizon-30",
			"percentiles-horizon-60",
			"percentiles-horizon-90",
		]);
		expect(chips[0]).toHaveTextContent("Age");

		// Age leads visually but 30 days stays the default selection — no earlier
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
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				30,
				RANGE_START,
				RANGE_END,
			),
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
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				30,
				RANGE_START,
				RANGE_END,
			),
		);

		fireEvent.click(screen.getByTestId("percentiles-selection-age"));

		// Age carries no horizon dimension — the request is the bare age selection.
		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				"age",
				RANGE_START,
				RANGE_END,
			),
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
		// red→green ramp, custom legend with the built-in one suppressed.
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

	it("shows the same honest empty copy on the Age tab when no age values exist", async () => {
		const getPercentilesOverTime = vi
			.fn()
			.mockImplementation((_ownerId: number, selection: string | number) =>
				Promise.resolve(selection === "age" ? [] : DATED_SERIES),
			);
		render(
			<PercentilesOverTimeWidget
				ownerId={OWNER_ID}
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await screen.findByTestId("mock-line-chart");

		fireEvent.click(screen.getByTestId("percentiles-selection-age"));

		// Never a broken axis — the same copy as the cycle-time tabs.
		const empty = await screen.findByTestId("percentiles-over-time-empty");
		expect(empty.textContent).toBe(HONEST_EMPTY_COPY);
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
				startDate={RANGE_START}
				endDate={RANGE_END}
				metricsService={createMetricsService(getPercentilesOverTime)}
			/>,
		);

		await waitFor(() =>
			expect(getPercentilesOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				30,
				RANGE_START,
				RANGE_END,
			),
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
