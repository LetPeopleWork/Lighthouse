import type { IHowManyForecast } from "./HowManyForecast";
import type { IWhenForecast } from "./WhenForecast";

export interface IManualForecast {
	remainingItems: number;
	targetDate: Date;
	whenForecasts: IWhenForecast[];
	howManyForecasts: IHowManyForecast[];
	likelihood: number;
	filterApplied: boolean;
	excludedSummary?: string;
	hasSufficientData?: boolean;
}

export class ManualForecast implements IManualForecast {
	whenForecasts: IWhenForecast[];
	howManyForecasts: IHowManyForecast[];
	likelihood: number;
	remainingItems: number;
	targetDate: Date;
	filterApplied: boolean;
	excludedSummary?: string;
	hasSufficientData: boolean;

	constructor(
		remainingItems: number,
		targetDate: Date,
		whenForecasts: IWhenForecast[],
		howManyForecasts: IHowManyForecast[],
		likelihood = 0,
		filterApplied = false,
		excludedSummary?: string,
		hasSufficientData = true,
	) {
		this.remainingItems = remainingItems;
		this.targetDate = targetDate;
		this.whenForecasts = whenForecasts;
		this.howManyForecasts = howManyForecasts;
		this.likelihood = likelihood;
		this.filterApplied = filterApplied;
		this.excludedSummary = excludedSummary;
		this.hasSufficientData = hasSufficientData;
	}
}
