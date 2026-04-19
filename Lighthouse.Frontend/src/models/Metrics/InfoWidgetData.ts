import type { TrendPayload } from "../../pages/Common/MetricsView/trendTypes";

export interface IInfoWidgetComparison extends TrendPayload {}

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
