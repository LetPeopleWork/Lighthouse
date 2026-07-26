import type { Locator, Page } from "@playwright/test";

/**
 * The process-behaviour metric families the recorder persists a dated series
 * for. Only Throughput is recorded today; later slices append here rather than
 * restructuring the toggle row.
 */
export type PbcMetricType = "Throughput";

/**
 * The three limit lines the widget plots per recorded day, in the point-in-time
 * process-behaviour chart's vocabulary (UNPL / Average / LNPL).
 */
export const PBC_LIMIT_LINES = ["unpl", "average", "lnpl"] as const;

export type PbcLimitLine = (typeof PBC_LIMIT_LINES)[number];

/**
 * The honest forward-only empty state (D6) a fresh owner reads instead of a
 * broken axis — verbatim, so a copy change here fails loudly.
 */
export const PBC_OVER_TIME_EMPTY_COPY =
	"builds forward from today — no snapshots recorded yet";

/**
 * The other honest empty state (slice-03b, D10/DDD-13): the owner may well have
 * history, just not inside the selected window. Shown when the range ends before
 * today — verbatim, so a copy change here fails loudly.
 */
export const PBC_OVER_TIME_RANGE_EMPTY_COPY =
	"no data recorded in the selected range";

/**
 * Drives the PBC Over Time widget (Predictability category, team + portfolio).
 * The widget renders a metric-family toggle row (Throughput pressed by default)
 * above a MUI-X LineChart of the dated UNPL / Average / LNPL triple the
 * recorder persisted, one point per recorded day. A fresh owner legitimately
 * has no history and gets the forward-only copy instead of a fabricated axis.
 */
export class PbcOverTimeWidget {
	private readonly widget: Locator;

	constructor(public readonly page: Page) {
		this.widget = page.getByTestId("pbc-over-time-widget");
	}

	get Widget(): Locator {
		return this.widget;
	}

	get emptyState(): Locator {
		return this.widget.getByTestId("pbc-over-time-empty");
	}

	get legend(): Locator {
		return this.widget.getByTestId("pbc-over-time-legend");
	}

	/** Mirrors the widget's exported `processBehaviorMetricTestId` convention. */
	metricToggle(metricType: PbcMetricType): Locator {
		return this.widget.getByTestId(`pbc-metric-${metricType.toLowerCase()}`);
	}

	async isMetricSelected(metricType: PbcMetricType): Promise<boolean> {
		return (
			(await this.metricToggle(metricType).getAttribute("aria-pressed")) ===
			"true"
		);
	}

	async selectMetric(metricType: PbcMetricType): Promise<void> {
		await this.metricToggle(metricType).click();
	}

	private legendEntry(line: PbcLimitLine): Locator {
		return this.widget.getByTestId(`pbc-line-${line}`);
	}

	async countLegendEntries(): Promise<number> {
		return this.legend.getByTestId(/^pbc-line-/).count();
	}

	// MUI-X LineChart renders one <path class="MuiLineChart-line"> per series.
	private get chartLines(): Locator {
		return this.widget.locator("path.MuiLineChart-line");
	}

	async countChartLines(): Promise<number> {
		return this.chartLines.count();
	}

	// The dated x-axis renders one tick label per recorded day. A populated,
	// truly "dated" trend has more than one so the lines actually span a window.
	private get axisTickLabels(): Locator {
		return this.widget.locator(".MuiChartsAxis-tickLabel");
	}

	async countAxisTickLabels(): Promise<number> {
		return this.axisTickLabels.count();
	}

	/**
	 * How many days the chart actually plots, read off the rendered geometry of one
	 * limit line. These lines draw no marks (showMark: false), and MUI-X thins axis
	 * tick labels to fit, so neither marks nor ticks can be counted — the vertex
	 * count of the SVG path is the honest signal. One command per plotted point: an
	 * `M` for the first, then an `L` per subsequent point when the curve is linear or
	 * a `C` when it is interpolated (MUI-X defaults to a monotone curve, so in
	 * practice these are `C`s).
	 */
	async countPlottedDays(line: PbcLimitLine = "average"): Promise<number> {
		if ((await this.limitLinePath(line).count()) === 0) {
			return 0;
		}
		const path = await this.limitLinePath(line).getAttribute("d");
		if (path === null) {
			return 0;
		}
		return (path.match(/[MLC]/g) ?? []).length;
	}

	/**
	 * The plotted path for one limit line. MUI-X tags each rendered line with a
	 * data-series attribute carrying the series id and emits no per-series class,
	 * so an id-in-class selector would silently match nothing.
	 */
	private limitLinePath(line: PbcLimitLine): Locator {
		return this.widget.locator(`path.MuiLineChart-line[data-series="${line}"]`);
	}

	/**
	 * The rendered dash pattern of a limit line, in CSS pixels. Over time the
	 * three limits ARE the series, so they render SOLID and this is empty —
	 * a dash pattern here would mean the discarded neutral-band styling came
	 * back and colour stopped being the differentiating channel.
	 */
	async limitLineDashPattern(line: PbcLimitLine): Promise<number[]> {
		const dashArray = await this.limitLinePath(line).evaluate(
			(node) => getComputedStyle(node).strokeDasharray,
		);
		return dashArray
			.split(",")
			.map((segment) => Number.parseFloat(segment.trim()))
			.filter((segment) => !Number.isNaN(segment));
	}

	/**
	 * The rendered stroke of a limit line. Each limit carries its OWN theme
	 * colour, so the spec asserts the three disagree rather than pinning a
	 * theme hex that the light/dark palettes would each answer differently.
	 */
	async limitLineStrokeColor(line: PbcLimitLine): Promise<string> {
		return this.limitLinePath(line).evaluate(
			(node) => getComputedStyle(node).stroke,
		);
	}

	/**
	 * The legend swatch a limit line draws, as rendered. The legend has to show
	 * what is plotted, so the swatch is a solid rule in the line's own colour.
	 */
	async legendSwatchRule(
		line: PbcLimitLine,
	): Promise<{ color: string; style: string }> {
		return this.widget.getByTestId(`pbc-swatch-${line}`).evaluate((node) => ({
			color: getComputedStyle(node).borderTopColor,
			style: getComputedStyle(node).borderTopStyle,
		}));
	}

	/** The colour swatch the legend draws for a limit line. */
	async isLimitLineInLegend(line: PbcLimitLine): Promise<boolean> {
		return (await this.legendEntry(line).count()) === 1;
	}
}
