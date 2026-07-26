import { createTheme, ThemeProvider } from "@mui/material/styles";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { ProcessBehaviorSnapshot } from "../../../models/Metrics/ProcessBehaviorSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import PbcOverTimeWidget, {
	PBC_OVER_TIME_EMPTY_COPY,
	PBC_OVER_TIME_RANGE_EMPTY_COPY,
} from "./PbcOverTimeWidget";

// Mock MUI-X LineChart (same pattern as PercentilesOverTimeWidget.test.tsx).
// Exposes the series identity/colour and the x-axis dates so the specs can
// assert three dated limit lines without reaching into the real SVG renderer.
vi.mock("@mui/x-charts", () => ({
	LineChart: vi.fn(
		({
			xAxis,
			series,
			hideLegend,
		}: {
			xAxis?: { data?: string[] }[];
			series?: {
				id?: string;
				label?: string;
				color?: string;
				data?: number[];
				showMark?: boolean;
			}[];
			hideLegend?: boolean;
		}) => (
			<div data-testid="mock-line-chart">
				<div data-testid="chart-hide-legend">{String(hideLegend)}</div>
				<div data-testid="chart-xaxis">
					{JSON.stringify(xAxis?.[0]?.data ?? [])}
				</div>
				<div data-testid="chart-series">
					{JSON.stringify(
						series?.map((s) => ({
							id: s.id,
							label: s.label,
							color: s.color,
							points: s.data?.length ?? 0,
							data: s.data ?? [],
							showMark: s.showMark,
						})) ?? [],
					)}
				</div>
			</div>
		),
	),
}));

const OWNER_ID = 42;

// The dashboard's default range always ends today (BaseMetricsView seeds endDate from
// `new Date()`), which is the state the shipped forward-only empty-state assertions run in.
const RANGE_START = new Date(2026, 6, 1);
const RANGE_END = todayAtNoon();

/** A past window: ends before today, so an empty series is an in-range emptiness (DDD-13). */
const PAST_RANGE_START = new Date(2026, 4, 1);
const PAST_RANGE_END = new Date(2026, 4, 15);

function todayAtNoon(): Date {
	const today = new Date();
	return new Date(today.getFullYear(), today.getMonth(), today.getDate(), 12);
}

const THREE_DAY_SERIES: ProcessBehaviorSnapshot[] = [
	{ recordedAt: "2026-07-20", unpl: 14, average: 8, lnpl: 2 },
	{ recordedAt: "2026-07-21", unpl: 15, average: 9, lnpl: 3 },
	{ recordedAt: "2026-07-22", unpl: 16, average: 9, lnpl: 2 },
];

const ONE_DAY_SERIES: ProcessBehaviorSnapshot[] = [
	{ recordedAt: "2026-07-22", unpl: 16, average: 9, lnpl: 2 },
];

type SeriesInfo = {
	id: string;
	label: string;
	color: string;
	points: number;
	data: number[];
	showMark: boolean;
};

function createMetricsService(
	getProcessBehaviorOverTime: ReturnType<typeof vi.fn>,
): IMetricsService<IWorkItem | IFeature> {
	return {
		getProcessBehaviorOverTime,
	} as unknown as IMetricsService<IWorkItem | IFeature>;
}

// Colour is now the only channel that separates the three limits, so the specs
// resolve the expected values from a real theme rather than pinning hexes.
const theme = createTheme();

const EXPECTED_LIMIT_COLORS = {
	unpl: theme.palette.error.main,
	average: theme.palette.info.main,
	lnpl: theme.palette.warning.main,
} as const;

function renderWidget(
	getProcessBehaviorOverTime: ReturnType<typeof vi.fn>,
	startDate: Date = RANGE_START,
	endDate: Date = RANGE_END,
) {
	return render(
		<ThemeProvider theme={theme}>
			<PbcOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getProcessBehaviorOverTime)}
				startDate={startDate}
				endDate={endDate}
			/>
		</ThemeProvider>,
	);
}

function readSeries(): SeriesInfo[] {
	return JSON.parse(
		screen.getByTestId("chart-series").textContent ?? "[]",
	) as SeriesInfo[];
}

describe("PbcOverTimeWidget", () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it("fetches the Throughput limits series for the owner on first paint", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		renderWidget(getProcessBehaviorOverTime);

		await waitFor(() =>
			expect(getProcessBehaviorOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				"Throughput",
				RANGE_START,
				RANGE_END,
			),
		);

		// Throughput is the pressed toggle on first paint (Scenario 10). Selection
		// is set explicitly per button so the Tooltip wrapper does not cost the
		// pressed state — same accessibility surface as the percentiles widget.
		expect(screen.getByTestId("pbc-metric-throughput")).toHaveAttribute(
			"aria-pressed",
			"true",
		);
		expect(screen.getByTestId("pbc-metric-throughput")).toHaveTextContent(
			"Throughput",
		);
	});

	it("explains the Throughput tab with its own tooltip", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		renderWidget(getProcessBehaviorOverTime);

		await screen.findByTestId("mock-line-chart");

		fireEvent.mouseOver(screen.getByTestId("pbc-metric-throughput"));
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			"Throughput natural process limits per recorded day",
		);
	});

	it("renders the default title and one legend swatch per limit line", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		renderWidget(getProcessBehaviorOverTime);

		await screen.findByTestId("mock-line-chart");

		expect(screen.getByText("PBC Over Time")).toBeInTheDocument();
		expect(screen.getByTestId("pbc-over-time-legend")).toBeInTheDocument();
		// The legend has to show what is plotted: each swatch is a SOLID rule in
		// its own line's colour, never a shared neutral dash.
		for (const [key, color] of Object.entries(EXPECTED_LIMIT_COLORS)) {
			expect(screen.getByTestId(`pbc-line-${key}`)).toBeInTheDocument();
			expect(screen.getByTestId(`pbc-swatch-${key}`)).toHaveStyle({
				borderTopStyle: "solid",
				borderTopWidth: "2px",
				borderTopColor: color,
			});
		}
		// Only the custom legend renders; the chart's built-in one stays off.
		expect(screen.getByTestId("chart-hide-legend")).toHaveTextContent("true");
	});

	it("plots three dated limit lines in the point-in-time PBC vocabulary", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		renderWidget(getProcessBehaviorOverTime);

		await screen.findByTestId("mock-line-chart");

		expect(
			JSON.parse(screen.getByTestId("chart-xaxis").textContent ?? "[]"),
		).toEqual(["2026-07-20", "2026-07-21", "2026-07-22"]);

		const seriesInfo = readSeries();
		expect(seriesInfo.map((s) => s.id)).toEqual(["unpl", "average", "lnpl"]);
		expect(seriesInfo.map((s) => s.label)).toEqual(["UNPL", "Average", "LNPL"]);
		// Each line plots ITS OWN accessor, in recordedAt order.
		expect(seriesInfo[0].data).toEqual([14, 15, 16]);
		expect(seriesInfo[1].data).toEqual([8, 9, 9]);
		expect(seriesInfo[2].data).toEqual([2, 3, 2]);
		for (const s of seriesInfo) {
			expect(s.points).toBe(3);
			expect(s.showMark).toBe(false);
		}
		// Deliberate D7 deviation: over time the three limits ARE the series, so
		// colour — not a dash pattern — is what tells them apart.
		expect(seriesInfo.map((s) => s.color)).toEqual([
			EXPECTED_LIMIT_COLORS.unpl,
			EXPECTED_LIMIT_COLORS.average,
			EXPECTED_LIMIT_COLORS.lnpl,
		]);
		// Three DISTINCT colours — a shared colour is what made the band
		// unreadable in dark mode in the first place.
		expect(new Set(seriesInfo.map((s) => s.color)).size).toBe(3);
	});

	it("plots a single recorded day without a degenerate axis", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(ONE_DAY_SERIES);
		renderWidget(getProcessBehaviorOverTime);

		await screen.findByTestId("mock-line-chart");

		expect(
			JSON.parse(screen.getByTestId("chart-xaxis").textContent ?? "[]"),
		).toEqual(["2026-07-22"]);
		const seriesInfo = readSeries();
		expect(seriesInfo).toHaveLength(3);
		expect(seriesInfo.map((s) => s.data)).toEqual([[16], [9], [2]]);
		// A one-day series is still an honest chart — the empty state stays away.
		expect(screen.queryByTestId("pbc-over-time-empty")).not.toBeInTheDocument();
	});

	it("shows the honest forward-only empty state and no chart on a fresh owner", async () => {
		const getProcessBehaviorOverTime = vi.fn().mockResolvedValue([]);
		renderWidget(getProcessBehaviorOverTime);

		await screen.findByTestId("pbc-over-time-empty");
		expect(screen.getByTestId("pbc-over-time-empty")).toHaveTextContent(
			PBC_OVER_TIME_EMPTY_COPY,
		);
		// Scenario 12 — never a broken chart: no axis is rendered at all.
		expect(screen.queryByTestId("mock-line-chart")).not.toBeInTheDocument();
	});

	it("says the range is empty, not that nothing was ever recorded, for a window that ended before today", async () => {
		const getProcessBehaviorOverTime = vi.fn().mockResolvedValue([]);
		renderWidget(getProcessBehaviorOverTime, PAST_RANGE_START, PAST_RANGE_END);

		const empty = await screen.findByTestId("pbc-over-time-empty");
		expect(empty).toHaveTextContent(PBC_OVER_TIME_RANGE_EMPTY_COPY);
		// The forward-only sentence would be a lie about a past window on an owner
		// that may well have history outside it (D10 / DDD-13).
		expect(empty).not.toHaveTextContent(PBC_OVER_TIME_EMPTY_COPY);
		expect(screen.queryByTestId("mock-line-chart")).not.toBeInTheDocument();
	});

	it("keeps the forward-only copy when the window still includes today", async () => {
		const getProcessBehaviorOverTime = vi.fn().mockResolvedValue([]);
		renderWidget(getProcessBehaviorOverTime, RANGE_START, RANGE_END);

		const empty = await screen.findByTestId("pbc-over-time-empty");
		expect(empty).toHaveTextContent(PBC_OVER_TIME_EMPTY_COPY);
	});

	it("exports the D6 empty copy verbatim so the E2E asserts the shipped string", () => {
		expect(PBC_OVER_TIME_EMPTY_COPY).toBe(
			"builds forward from today — no snapshots recorded yet",
		);
	});

	it("shows neither the chart nor the empty state while the series is loading", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockReturnValue(new Promise<ProcessBehaviorSnapshot[]>(() => {}));
		renderWidget(getProcessBehaviorOverTime);

		await waitFor(() =>
			expect(getProcessBehaviorOverTime).toHaveBeenCalledWith(
				OWNER_ID,
				"Throughput",
				RANGE_START,
				RANGE_END,
			),
		);

		// The empty state is reserved for a loaded-but-empty series (series === []).
		expect(screen.queryByTestId("mock-line-chart")).not.toBeInTheDocument();
		expect(screen.queryByTestId("pbc-over-time-empty")).not.toBeInTheDocument();
	});

	it("logs and recovers when the series fetch rejects, showing no chart", async () => {
		const consoleError = vi
			.spyOn(console, "error")
			.mockImplementation(() => {});
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockRejectedValue(new Error("boom"));
		renderWidget(getProcessBehaviorOverTime);

		await waitFor(() => expect(consoleError).toHaveBeenCalled());
		expect(consoleError).toHaveBeenCalledWith(
			"Error fetching process behavior over time:",
			expect.any(Error),
		);
		expect(screen.queryByTestId("mock-line-chart")).not.toBeInTheDocument();
		consoleError.mockRestore();
	});

	it("re-plots an already fetched metric family without a second fetch", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		renderWidget(getProcessBehaviorOverTime);

		await screen.findByTestId("mock-line-chart");
		expect(getProcessBehaviorOverTime).toHaveBeenCalledTimes(1);

		// Clicking the already-selected family re-plots from the cache (read-only).
		fireEvent.click(screen.getByTestId("pbc-metric-throughput"));
		await waitFor(() =>
			expect(screen.getByTestId("pbc-metric-throughput")).toHaveAttribute(
				"aria-pressed",
				"true",
			),
		);
		expect(getProcessBehaviorOverTime).toHaveBeenCalledTimes(1);
	});
});
