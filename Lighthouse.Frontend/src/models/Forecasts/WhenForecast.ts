import type { IForecast } from "./IForecast";

export interface IWhenForecast extends IForecast {
	expectedDate: Date;
	filterApplied?: boolean;
	excludedSummary?: string;
}

export class WhenForecast implements IWhenForecast {
	probability!: number;
	expectedDate: Date = new Date();
	filterApplied?: boolean;
	excludedSummary?: string;

	static fromBackend(data: IWhenForecast): WhenForecast {
		const forecast = WhenForecast.new(
			data.probability,
			new Date(data.expectedDate),
		);
		forecast.filterApplied = data.filterApplied;
		forecast.excludedSummary = data.excludedSummary;
		return forecast;
	}

	static new(probability: number, expectedDate: Date): WhenForecast {
		const forecast = new WhenForecast();
		forecast.probability = probability;
		forecast.expectedDate = expectedDate;
		return forecast;
	}
}
