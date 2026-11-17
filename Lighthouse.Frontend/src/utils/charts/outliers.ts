import { getMaxYAxisHeight } from "./chartAxisUtils";

export interface OutlierPartition<T> {
	/** The plotted items the verdict names, longest first. */
	readonly outliers: T[];
	/** Every other item, in input order. */
	readonly typical: T[];
}

/**
 * Splits the plotted items into the outliers the backend's verdict names and the rest. The rule that
 * decides which items are outliers lives in the backend; the chart only follows its verdict, and an id
 * the verdict names that is not plotted is ignored.
 */
export function findOutliers<T>(
	items: readonly T[],
	daysOf: (item: T) => number,
	idOf: (item: T) => number,
	outlierIds: readonly number[],
): OutlierPartition<T> {
	const named = new Set(outlierIds);
	const outliers: T[] = [];
	const typical: T[] = [];
	for (const item of items) {
		(named.has(idOf(item)) ? outliers : typical).push(item);
	}
	outliers.sort((a, b) => daysOf(b) - daysOf(a));

	return { outliers, typical };
}

export interface AxisTopInput {
	readonly typicalValues: readonly number[];
	/** Every percentile and the SLE of the data shown, whether ticked or not. */
	readonly lineValues: readonly number[];
	/** The limit the outliers are above, or null without one. */
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
 * the typical items and the lines that lie within the limit, and stops at the limit, so the outliers
 * are above it; it never stops below a drawn item.
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
	const cappedAtTheLimit = Math.min(
		tallestWithHeadroom([...typicalValues, ...lineValuesWithinCut]),
		cut,
	);
	return Math.max(cappedAtTheLimit, ...typicalValues);
}
