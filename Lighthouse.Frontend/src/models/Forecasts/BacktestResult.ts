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

export type BacktestResultOptions = {
	startDate: Date;
	endDate: Date;
	historicalStartDate: Date;
	historicalEndDate: Date;
	percentiles: HowManyForecast[];
	actualThroughput: number;
	filterApplied?: boolean;
	excludedSummary?: string;
};

export class BacktestResult {
	startDate: Date;
	endDate: Date;
	historicalStartDate: Date;
	historicalEndDate: Date;
	percentiles: HowManyForecast[];
	actualThroughput: number;
	filterApplied?: boolean;
	excludedSummary?: string;

	constructor(options: BacktestResultOptions) {
		this.startDate = options.startDate;
		this.endDate = options.endDate;
		this.historicalStartDate = options.historicalStartDate;
		this.historicalEndDate = options.historicalEndDate;
		this.percentiles = options.percentiles;
		this.actualThroughput = options.actualThroughput;
		this.filterApplied = options.filterApplied ?? false;
		this.excludedSummary = options.excludedSummary;
	}
}
