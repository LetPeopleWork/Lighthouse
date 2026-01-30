import type { HowManyForecast, IHowManyForecast } from "./HowManyForecast";

export interface IBacktestResult {
	startDate: string;
	endDate: string;
	historicalWindowDays: number;
	percentiles: IHowManyForecast[];
	actualThroughput: number;
}

export class BacktestResult {
	startDate: Date;
	endDate: Date;
	historicalWindowDays: number;
	percentiles: HowManyForecast[];
	actualThroughput: number;

	constructor(
		startDate: Date,
		endDate: Date,
		historicalWindowDays: number,
		percentiles: HowManyForecast[],
		actualThroughput: number,
	) {
		this.startDate = startDate;
		this.endDate = endDate;
		this.historicalWindowDays = historicalWindowDays;
		this.percentiles = percentiles;
		this.actualThroughput = actualThroughput;
	}
}
