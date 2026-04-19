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

export interface IWipOverviewInfo {
	count: number;
	comparison: IInfoWidgetComparison;
}

export interface IFeaturesWorkedOnInfo {
	count: number;
	comparison: IInfoWidgetComparison;
}

export interface ITotalWorkItemAgeInfo {
	totalAge: number;
	comparison: IInfoWidgetComparison;
}

export interface IPredictabilityScoreInfo {
	score: number;
	comparison: IInfoWidgetComparison;
}

export interface ICycleTimePercentilesInfo {
	percentiles: IPercentileValueDto[];
	comparison: IInfoWidgetComparison;
}
