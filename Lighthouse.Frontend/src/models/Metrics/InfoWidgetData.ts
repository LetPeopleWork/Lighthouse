import type { TrendPayload } from "../../pages/Common/MetricsView/trendTypes";

export interface IInfoWidgetComparison extends TrendPayload {}

export interface IPercentileValueDto {
	percentile: number;
	value: number;
}

export interface IThroughputInfo {
	total: number;
	dailyAverage: number;
	comparison: IInfoWidgetComparison;
}

export interface IArrivalsInfo {
	total: number;
	dailyAverage: number;
	comparison: IInfoWidgetComparison;
}

export interface IFeatureSizePercentilesInfo {
	percentiles: IPercentileValueDto[];
	comparison: IInfoWidgetComparison;
}
