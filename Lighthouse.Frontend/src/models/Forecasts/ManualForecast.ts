import type { IHowManyForecast } from "./HowManyForecast";
import type { IWhenForecast } from "./WhenForecast";

export interface IManualForecast {
	remainingItems: number;
	targetDate: Date;
	whenForecasts: IWhenForecast[];
	howManyForecasts: IHowManyForecast[];
	likelihood: number;
}

export class ManualForecast implements IManualForecast {
	whenForecasts: IWhenForecast[];
	howManyForecasts: IHowManyForecast[];
	likelihood: number;
	remainingItems: number;
	targetDate: Date;

	constructor(
		remainingItems: number,
		targetDate: Date,
		whenForecasts: IWhenForecast[],
		howManyForecasts: IHowManyForecast[],
		likelihood = 0,
	) {
		this.remainingItems = remainingItems;
		this.targetDate = targetDate;
		this.whenForecasts = whenForecasts;
		this.howManyForecasts = howManyForecasts;
		this.likelihood = likelihood;
	}
}
