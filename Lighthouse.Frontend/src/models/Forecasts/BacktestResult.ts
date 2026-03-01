import type { HowManyForecast, IHowManyForecast } from "./HowManyForecast";

export interface IBacktestResult {
	startDate: string;
	endDate: string;
	historicalStartDate: string;
	historicalEndDate: string;
	percentiles: IHowManyForecast[];
	actualThroughput: number;
}

export class BacktestResult {
	startDate: Date;
	endDate: Date;
	historicalStartDate: Date;
	historicalEndDate: Date;
	percentiles: HowManyForecast[];
	actualThroughput: number;

	constructor(
		startDate: Date,
		endDate: Date,
		historicalStartDate: Date,
		historicalEndDate: Date,
		percentiles: HowManyForecast[],
		actualThroughput: number,
	) {
		this.startDate = startDate;
		this.endDate = endDate;
		this.historicalStartDate = historicalStartDate;
		this.historicalEndDate = historicalEndDate;
		this.percentiles = percentiles;
		this.actualThroughput = actualThroughput;
	}
}
