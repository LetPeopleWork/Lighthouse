import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IFeature } from "../../../models/Feature";
import type { ProcessBehaviorSnapshot } from "../../../models/Metrics/ProcessBehaviorSnapshot";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import PbcOverTimeWidget, {
	PBC_OVER_TIME_EMPTY_COPY,
} from "./PbcOverTimeWidget";

// Mock MUI-X LineChart (same pattern as PercentilesOverTimeWidget.test.tsx).
// Exposes the series identity/colour/dash and the x-axis dates so the specs can
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

function renderWidget(getProcessBehaviorOverTime: ReturnType<typeof vi.fn>) {
	return render(
		<PbcOverTimeWidget
			ownerId={OWNER_ID}
			metricsService={createMetricsService(getProcessBehaviorOverTime)}
		/>,
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
		for (const key of ["unpl", "average", "lnpl"]) {
			expect(screen.getByTestId(`pbc-line-${key}`)).toBeInTheDocument();
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
		// D7 — the point-in-time chart draws its limits in the neutral secondary
		// text colour, so the over-time limits read as the same concept.
		const neutral = seriesInfo[0].color;
		expect(neutral).toBeTruthy();
		expect(seriesInfo.map((s) => s.color)).toEqual([neutral, neutral, neutral]);
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
