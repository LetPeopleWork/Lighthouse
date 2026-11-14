import { getMaxYAxisHeight } from "./chartAxisUtils";

/** A closed Work Item longer than this many times the median is an outlier. */
export const OUTLIER_MEDIAN_MULTIPLE = 5;

const MEDIAN_FRACTION = 0.5;

/**
 * The median of the values, as the backend computes its 50th percentile: sort ascending and take the
 * value at index floor(0.5 x N) - 1, kept within the list. It is always one of the values, which is
 * why the chart's "50%" line and the median named for outliers can never disagree. A change to the
 * backend's percentile rule has to be made here too. Values that are not finite are ignored.
 */
export function fiftiethPercentile(values: readonly number[]): number | null {
	const sorted = values.filter(Number.isFinite).sort((a, b) => a - b);
	if (sorted.length === 0) return null;

	const index = Math.floor(MEDIAN_FRACTION * sorted.length) - 1;
	return sorted[Math.min(Math.max(index, 0), sorted.length - 1)];
}

export interface OutlierAnalysis<T> {
	/** The median of the finite values, or null when no outlier rule applies. */
	readonly median: number | null;
	/** The longest value that is still typical: the median times the multiple, or null. */
	readonly cut: number | null;
	/** Items strictly above the cut, longest first. */
	readonly outliers: T[];
	/** Every other item, in input order. */
	readonly typical: T[];
}

/**
 * Splits the items into those that are exceptionally long and the rest. The rule needs a median
 * above zero; without one every item is typical.
 */
export function findOutliers<T>(
	items: readonly T[],
	daysOf: (item: T) => number,
): OutlierAnalysis<T> {
	const median = fiftiethPercentile(items.map(daysOf));
	if (median === null || median <= 0) {
		return { median: null, cut: null, outliers: [], typical: [...items] };
	}

	const cut = median * OUTLIER_MEDIAN_MULTIPLE;
	const outliers: T[] = [];
	const typical: T[] = [];
	for (const item of items) {
		(daysOf(item) > cut ? outliers : typical).push(item);
	}
	outliers.sort((a, b) => daysOf(b) - daysOf(a));

	return { median, cut, outliers, typical };
}

export interface AxisTopInput {
	readonly typicalValues: readonly number[];
	/** Every percentile and the SLE of the data shown, whether ticked or not. */
	readonly lineValues: readonly number[];
	readonly cut: number | null;
	readonly hasOutliers: boolean;
}

const identity = (value: number): number => value;

const tallestWithHeadroom = (values: readonly number[]): number =>
	getMaxYAxisHeight({
		percentiles: [],
		dataPoints: [...values],
		getDataValue: identity,
	});

/**
 * The top of the y-axis. Without outliers it is what the chart always had. With them, the axis fits
 * the typical items and the lines that lie within the cut, and never rises above the cut, so every
 * outlier is above the axis.
 */
export function axisTop({
	typicalValues,
	lineValues,
	cut,
	hasOutliers,
}: AxisTopInput): number {
	if (!hasOutliers || cut === null) {
		return tallestWithHeadroom([...typicalValues, ...lineValues]);
	}

	const lineValuesWithinCut = lineValues.filter((value) => value <= cut);
	return Math.min(
		tallestWithHeadroom([...typicalValues, ...lineValuesWithinCut]),
		cut,
	);
}

const TENTHS = 10;
const WHOLE_NUMBERS_FROM_TENTHS = 100;

/** How many times the median the value is, rounded up to a tenth below 10 and half-up to a whole number from 10. */
export function timesTheMedian(value: number, median: number): number {
	const tenths = Math.ceil((value * TENTHS) / median);
	if (tenths < WHOLE_NUMBERS_FROM_TENTHS) return tenths / TENTHS;

	return Math.floor((2 * value + median) / (2 * median));
}

export interface TimesTheMedianText {
	/** "9.7x" */
	readonly short: string;
	/** "9.7 times the median" */
	readonly spoken: string;
}

export function formatTimesTheMedian(
	value: number,
	median: number,
): TimesTheMedianText {
	const times = timesTheMedian(value, median);
	const shown = times < TENTHS ? times.toFixed(1) : String(times);

	return { short: `${shown}x`, spoken: `${shown} times the median` };
}
