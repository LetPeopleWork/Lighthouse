import type { HowManyForecast, IHowManyForecast } from "./HowManyForecast";

export interface IBacktestResult {
	startDate: string;
	endDate: string;
	historicalStartDate: string;
	historicalEndDate: string;
	percentiles: IHowManyForecast[];
	actualThroughput: number;
	filterApplied?: boolean;
	excludedSummary?: string;
}

export class BacktestResult {
	startDate: Date;
	endDate: Date;
	historicalStartDate: Date;
	historicalEndDate: Date;
	percentiles: HowManyForecast[];
	actualThroughput: number;
	filterApplied?: boolean;
	excludedSummary?: string;

	constructor(
		startDate: Date,
		endDate: Date,
		historicalStartDate: Date,
		historicalEndDate: Date,
		percentiles: HowManyForecast[],
		actualThroughput: number,
		filterApplied = false,
		excludedSummary?: string,
	) {
		this.startDate = startDate;
		this.endDate = endDate;
		this.historicalStartDate = historicalStartDate;
		this.historicalEndDate = historicalEndDate;
		this.percentiles = percentiles;
		this.actualThroughput = actualThroughput;
		this.filterApplied = filterApplied;
		this.excludedSummary = excludedSummary;
	}
}
