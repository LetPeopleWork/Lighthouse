import type { IForecast } from "./IForecast";

/**
 * Whether a percentile lies beyond the forecast horizon, and how many years that horizon spans.
 * An older backend sends neither, and its percentile reads as the date it carries.
 */
export interface PercentileHorizon {
	beyondHorizon?: boolean;
	horizonYears?: number | null;
}

export interface IWhenForecast extends IForecast {
	expectedDate: Date;
	filterApplied?: boolean;
	excludedSummary?: string;
	beyondHorizon?: boolean;
	horizonYears?: number;
}

export class WhenForecast implements IWhenForecast {
	probability!: number;
	expectedDate: Date = new Date();
	filterApplied?: boolean;
	excludedSummary?: string;
	beyondHorizon?: boolean;
	horizonYears?: number;

	static fromBackend(data: IWhenForecast): WhenForecast {
		const forecast = WhenForecast.new(
			data.probability,
			new Date(data.expectedDate),
			data,
		);
		forecast.filterApplied = data.filterApplied;
		forecast.excludedSummary = data.excludedSummary;
		return forecast;
	}

	static new(
		probability: number,
		expectedDate: Date,
		horizon: PercentileHorizon = {},
	): WhenForecast {
		const forecast = new WhenForecast();
		forecast.probability = probability;
		forecast.expectedDate = expectedDate;
		forecast.beyondHorizon = horizon.beyondHorizon;
		forecast.horizonYears = horizon.horizonYears ?? undefined;
		return forecast;
	}
}
