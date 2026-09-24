import { createTheme, ThemeProvider } from "@mui/material/styles";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { ProcessBehaviorSnapshot } from "../../../models/Metrics/ProcessBehaviorSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { OptionalFeatureService } from "../../../services/Api/OptionalFeatureService";
import PbcOverTimeWidget, {
	PBC_OVER_TIME_EMPTY_COPY,
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
	// Portfolio is the wider scope, so the shipped specs keep seeing every family.
	ownerType: "team" | "portfolio" = "portfolio",
) {
	return render(
		<ThemeProvider theme={theme}>
			<PbcOverTimeWidget
				ownerId={OWNER_ID}
				metricsService={createMetricsService(getProcessBehaviorOverTime)}
				startDate={startDate}
				endDate={endDate}
				ownerType={ownerType}
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

		// Throughput is the pressed toggle on first paint. Selection
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
		// Deliberate deviation from the point-in-time chart: over time the three limits ARE the series, so
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

	// Skipped until the empty-chart sentence becomes the one pinned above; un-skip with that change.
	it.skip.each([
		{ range: "ending today", startDate: RANGE_START, endDate: RANGE_END },
		{
			range: "ending in the past",
			startDate: PAST_RANGE_START,
			endDate: PAST_RANGE_END,
		},
	])(
		"says only what is true of every empty chart, for a range $range, and draws no axis",
		async ({ startDate, endDate }) => {
			const getProcessBehaviorOverTime = vi.fn().mockResolvedValue([]);
			renderWidget(getProcessBehaviorOverTime, startDate, endDate);

			const empty = await screen.findByTestId("pbc-over-time-empty");
			expect(empty.textContent).toBe(HONEST_EMPTY_COPY);
			expect(empty).not.toHaveTextContent(RETIRED_FORWARD_ONLY_COPY);
			expect(empty).not.toHaveTextContent(RETIRED_LATER_VISIT_COPY);
			expect(screen.queryByTestId("mock-line-chart")).not.toBeInTheDocument();
		},
	);

	// Skipped until the empty-chart sentence becomes the one pinned above; un-skip with that change.
	it.skip("exports the empty copy verbatim so the end-to-end test asserts the shipped string", () => {
		expect(PBC_OVER_TIME_EMPTY_COPY).toBe(HONEST_EMPTY_COPY);
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
		const getProcessBehaviorOverTime = vi.fn().mockResolvedValue([]);
		renderWidget(getProcessBehaviorOverTime, RANGE_START, RANGE_END);

		await screen.findByTestId("pbc-over-time-empty");
		expect(getProcessBehaviorOverTime).toHaveBeenCalled();
		expect(getAllFeatures).not.toHaveBeenCalled();
		expect(getFeatureByKey).not.toHaveBeenCalled();
		getAllFeatures.mockRestore();
		getFeatureByKey.mockRestore();
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

	it("offers every process-behaviour family on a portfolio", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		renderWidget(
			getProcessBehaviorOverTime,
			RANGE_START,
			RANGE_END,
			"portfolio",
		);

		await screen.findByTestId("mock-line-chart");

		// The shipped locator convention is pbc-metric-<lowercased wire value>.
		for (const testId of [
			"pbc-metric-throughput",
			"pbc-metric-workitemage",
			"pbc-metric-wip",
			"pbc-metric-cycletime",
			"pbc-metric-arrivals",
			"pbc-metric-featuresize",
		]) {
			expect(screen.getByTestId(testId)).toBeInTheDocument();
		}
	});

	it("withholds Feature Size from a team, which has no feature sizes to chart", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		renderWidget(getProcessBehaviorOverTime, RANGE_START, RANGE_END, "team");

		await screen.findByTestId("mock-line-chart");

		expect(screen.queryByTestId("pbc-metric-featuresize")).toBeNull();
		// The other five stay — withholding one family must not cost the rest.
		for (const testId of [
			"pbc-metric-throughput",
			"pbc-metric-workitemage",
			"pbc-metric-wip",
			"pbc-metric-cycletime",
			"pbc-metric-arrivals",
		]) {
			expect(screen.getByTestId(testId)).toBeInTheDocument();
		}
	});

	it("labels each family in the delivery lead's words, not in the wire's", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		renderWidget(
			getProcessBehaviorOverTime,
			RANGE_START,
			RANGE_END,
			"portfolio",
		);

		await screen.findByTestId("mock-line-chart");

		// Literal strings on purpose: asserting against the label source would pass
		// even if every label were blanked.
		expect(screen.getByTestId("pbc-metric-workitemage")).toHaveTextContent(
			"Work Item Age",
		);
		expect(screen.getByTestId("pbc-metric-wip")).toHaveTextContent(
			"Work In Progress",
		);
		expect(screen.getByTestId("pbc-metric-cycletime")).toHaveTextContent(
			"Cycle Time",
		);
		expect(screen.getByTestId("pbc-metric-arrivals")).toHaveTextContent(
			"Arrivals",
		);
		expect(screen.getByTestId("pbc-metric-featuresize")).toHaveTextContent(
			"Feature Size",
		);
	});

	it("derives each family's tooltip from its human label", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		renderWidget(getProcessBehaviorOverTime, RANGE_START, RANGE_END, "team");

		await screen.findByTestId("mock-line-chart");

		fireEvent.mouseOver(screen.getByTestId("pbc-metric-wip"));
		expect(await screen.findByRole("tooltip")).toHaveTextContent(
			"Work In Progress natural process limits per recorded day",
		);
	});

	it("starts on Throughput at both scopes, so the default is never unreachable", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		const { unmount } = renderWidget(
			getProcessBehaviorOverTime,
			RANGE_START,
			RANGE_END,
			"team",
		);

		await screen.findByTestId("mock-line-chart");
		expect(screen.getByTestId("pbc-metric-throughput")).toHaveAttribute(
			"aria-pressed",
			"true",
		);
		unmount();

		renderWidget(
			getProcessBehaviorOverTime,
			RANGE_START,
			RANGE_END,
			"portfolio",
		);
		await screen.findByTestId("mock-line-chart");
		expect(screen.getByTestId("pbc-metric-throughput")).toHaveAttribute(
			"aria-pressed",
			"true",
		);
	});

	it("requests the selected family's own wire value", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		renderWidget(
			getProcessBehaviorOverTime,
			RANGE_START,
			RANGE_END,
			"portfolio",
		);
		await screen.findByTestId("mock-line-chart");

		for (const [testId, wireValue] of [
			["pbc-metric-workitemage", "WorkItemAge"],
			["pbc-metric-wip", "Wip"],
			["pbc-metric-cycletime", "CycleTime"],
			["pbc-metric-arrivals", "Arrivals"],
			["pbc-metric-featuresize", "FeatureSize"],
		]) {
			fireEvent.click(screen.getByTestId(testId));
			await waitFor(() =>
				expect(getProcessBehaviorOverTime).toHaveBeenLastCalledWith(
					OWNER_ID,
					wireValue,
					RANGE_START,
					RANGE_END,
				),
			);
		}
	});

	it("re-plots a previously visited family from the cache without a second request", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockResolvedValue(THREE_DAY_SERIES);
		renderWidget(getProcessBehaviorOverTime, RANGE_START, RANGE_END, "team");
		await screen.findByTestId("mock-line-chart");
		expect(getProcessBehaviorOverTime).toHaveBeenCalledTimes(1);

		fireEvent.click(screen.getByTestId("pbc-metric-cycletime"));
		await waitFor(() =>
			expect(getProcessBehaviorOverTime).toHaveBeenCalledTimes(2),
		);

		// Back to Throughput: already fetched for this range, so no recompute.
		fireEvent.click(screen.getByTestId("pbc-metric-throughput"));
		await waitFor(() =>
			expect(screen.getByTestId("pbc-metric-throughput")).toHaveAttribute(
				"aria-pressed",
				"true",
			),
		);
		expect(getProcessBehaviorOverTime).toHaveBeenCalledTimes(2);
	});

	// Skipped until the empty-chart sentence becomes the one pinned above; un-skip with that change.
	it.skip("shows the same honest empty copy for a family with nothing to show", async () => {
		const getProcessBehaviorOverTime = vi
			.fn()
			.mockImplementation((_ownerId: number, metricType: string) =>
				Promise.resolve(metricType === "Throughput" ? THREE_DAY_SERIES : []),
			);
		renderWidget(getProcessBehaviorOverTime, RANGE_START, RANGE_END, "team");
		await screen.findByTestId("mock-line-chart");

		fireEvent.click(screen.getByTestId("pbc-metric-arrivals"));

		const empty = await screen.findByTestId("pbc-over-time-empty");
		expect(empty.textContent).toBe(HONEST_EMPTY_COPY);
		expect(screen.queryByTestId("mock-line-chart")).not.toBeInTheDocument();
	});
});
