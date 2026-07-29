import type { IHowManyForecast } from "./HowManyForecast";
import type { IWhenForecast } from "./WhenForecast";

export interface IManualForecast {
	remainingItems: number;
	targetDate: Date;
	whenForecasts: IWhenForecast[];
	howManyForecasts: IHowManyForecast[];
	/** Null when the forecast has no trials behind it - see Bug #5586. */
	likelihood: number | null;
	filterApplied: boolean;
	excludedSummary?: string;
	hasSufficientData?: boolean;
}

export class ManualForecast implements IManualForecast {
	whenForecasts: IWhenForecast[];
	howManyForecasts: IHowManyForecast[];
	likelihood: number | null;
	remainingItems: number;
	targetDate: Date;
	filterApplied: boolean;
	excludedSummary?: string;
	hasSufficientData = true;

	constructor(
		remainingItems: number,
		targetDate: Date,
		whenForecasts: IWhenForecast[],
		howManyForecasts: IHowManyForecast[],
		likelihood: number | null = null,
		filterApplied = false,
		excludedSummary?: string,
	) {
		this.remainingItems = remainingItems;
		this.targetDate = targetDate;
		this.whenForecasts = whenForecasts;
		this.howManyForecasts = howManyForecasts;
		this.likelihood = likelihood;
		this.filterApplied = filterApplied;
		this.excludedSummary = excludedSummary;
	}
}
