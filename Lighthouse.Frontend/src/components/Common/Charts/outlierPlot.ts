import type { IPercentileValue } from "../../../models/PercentileValue";
import type { IWorkItem } from "../../../models/WorkItem";
import type { ChartLine } from "../../../utils/charts/offScaleLines";
import type { AboveAxisLine } from "../../../utils/charts/outlierWording";
import { ForecastLevel } from "../Forecasts/ForecastLevel";

export interface IPlotLine extends ChartLine {
	readonly aboveAxis: AboveAxisLine;
}

export interface INamedAnswer {
	readonly definitionId: number;
	readonly fetcher: (definitionId: number) => Promise<IPercentileValue[]>;
	readonly percentiles: IPercentileValue[];
}

const NO_PERCENTILES: IPercentileValue[] = [];

/** An answer fetched for another definition, or by an older callback, is not the current one. */
export function currentNamedPercentiles(
	answer: INamedAnswer | null,
	definitionId: number | null,
	fetcher: INamedAnswer["fetcher"] | undefined,
): IPercentileValue[] {
	const isCurrent =
		answer !== null &&
		answer.definitionId === definitionId &&
		answer.fetcher === fetcher;
	return isCurrent ? answer.percentiles : NO_PERCENTILES;
}

export const isOfVisibleType = (
	item: IWorkItem,
	visibleTypes: Record<string, boolean>,
): boolean => !item.type || visibleTypes[item.type] !== false;

interface PlotLineInput {
	readonly percentiles: readonly IPercentileValue[];
	readonly visiblePercentiles: Record<number, boolean>;
	readonly sle: IPercentileValue | null;
	readonly sleVisible: boolean;
	readonly sleTerm: string;
	readonly sleColor: string;
}

/** The percentile and SLE lines the reader has switched on, with the words an edge marker uses for each. */
export function buildPlotLines({
	percentiles,
	visiblePercentiles,
	sle,
	sleVisible,
	sleTerm,
	sleColor,
}: PlotLineInput): IPlotLine[] {
	const lines: IPlotLine[] = percentiles
		.filter((p) => visiblePercentiles[p.percentile])
		.map((p) => ({
			key: `percentile-${p.percentile}`,
			value: p.value,
			color: new ForecastLevel(p.percentile).color,
			markerText: `${p.percentile}%: ${p.value} days`,
			aboveAxis: {
				kind: "percentile",
				percentile: p.percentile,
				value: p.value,
			},
		}));
	if (sleVisible && sle) {
		lines.push({
			key: "sle-reference-line",
			value: sle.value,
			color: sleColor,
			markerText: `${sleTerm}: ${sle.percentile}% @ ${sle.value} days or less`,
			aboveAxis: { kind: "sle", value: sle.value },
		});
	}
	return lines;
}

/** Every percentile and the SLE of the data shown, whether ticked or not. */
export const allLineValues = (
	percentiles: readonly IPercentileValue[],
	sle: IPercentileValue | null,
): number[] => [
	...percentiles.map((p) => p.value),
	...(sle ? [sle.value] : []),
];

/**
 * How many lines the axis leaves out, ticked or not, whenever an outlier shapes the axis. The margin
 * is kept for all of them, so ticking one never moves the plot.
 */
export function reservedMarkerRows(
	percentiles: readonly IPercentileValue[],
	sle: IPercentileValue | null,
	axisMax: number,
	hasOutliers: boolean,
): number {
	if (!hasOutliers) return 0;
	return allLineValues(percentiles, sle).filter((value) => value > axisMax)
		.length;
}
