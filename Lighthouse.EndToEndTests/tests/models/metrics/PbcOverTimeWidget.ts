import type { Locator, Page } from "@playwright/test";

/**
 * The process-behaviour metric families the recorder persists a dated series
 * for — the backend enum MEMBER names verbatim, because the value is what the
 * widget lowercases into its locator. Ordered as the toggle row renders them,
 * so an offered-families assertion reads in visual order.
 */
export const PBC_METRIC_TYPES = [
	"Throughput",
	"WorkItemAge",
	"Wip",
	"CycleTime",
	"Arrivals",
	"FeatureSize",
] as const;

export type PbcMetricType = (typeof PBC_METRIC_TYPES)[number];

/** The families a portfolio dashboard offers — every one of them (D8). */
export const PBC_PORTFOLIO_METRIC_TYPES: readonly PbcMetricType[] =
	PBC_METRIC_TYPES;

/**
 * The families a TEAM dashboard offers: a team has no feature sizes to chart,
 * so Feature Size is withheld there (D8).
 */
export const PBC_TEAM_METRIC_TYPES: readonly PbcMetricType[] =
	PBC_METRIC_TYPES.filter((type) => type !== "FeatureSize");

/** The widget's locator convention: `pbc-metric-<lowercased wire value>`. */
function metricTestId(metricType: PbcMetricType): string {
	return `pbc-metric-${metricType.toLowerCase()}`;
}

const METRIC_TYPE_BY_TEST_ID: ReadonlyMap<string, PbcMetricType> = new Map(
	PBC_METRIC_TYPES.map((type) => [metricTestId(type), type]),
);

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
		return this.widget.getByTestId(metricTestId(metricType));
	}

	/**
	 * Which families the toggle row currently OFFERS, in rendered order. Scoped
	 * to the widget because the Predictability category also hosts six
	 * point-in-time PBC widgets. Reading the offered set makes ABSENCE directly
	 * assertable — a click on a withheld family would only ever time out, which
	 * is indistinguishable from a broken locator.
	 *
	 * Returns `[]` while the widget is still mounting, so callers must poll for
	 * the expected set rather than one-sidedly bounding the length.
	 */
	async offeredMetricTypes(): Promise<PbcMetricType[]> {
		const testIds = await this.widget
			.getByTestId(/^pbc-metric-/)
			.evaluateAll((buttons) =>
				buttons.map((button) => button.getAttribute("data-testid") ?? ""),
			);
		// An unmapped id means the widget invented a family the POM does not know;
		// surfacing the raw id fails the assertion loudly instead of dropping it.
		return testIds.map((testId) => {
			const metricType = METRIC_TYPE_BY_TEST_ID.get(testId);
			if (metricType === undefined) {
				throw new Error(`Unknown process-behaviour metric toggle: ${testId}`);
			}
			return metricType;
		});
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
