import { HowManyForecast } from "../../models/Forecasts/HowManyForecast";
import {
	type IManualForecast,
	ManualForecast,
} from "../../models/Forecasts/ManualForecast";
import { WhenForecast } from "../../models/Forecasts/WhenForecast";
import { BaseApiService } from "./BaseApiService";

export interface IForecastService {
	runManualForecast(
		teamId: number,
		remainingItems: number,
		targetDate: Date,
	): Promise<ManualForecast>;

	runItemPrediction(
		teamId: number,
		startDate: Date,
		endDate: Date,
		targetDate: Date,
		workItemTypes: string[],
	): Promise<ManualForecast>;
}

export class ForecastService
	extends BaseApiService
	implements IForecastService
{
	async runManualForecast(
		teamId: number,
		remainingItems: number,
		targetDate: Date,
	): Promise<ManualForecast> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IManualForecast>(
				`/forecast/manual/${teamId}`,
				{
					remainingItems: remainingItems,
					targetDate: targetDate,
				},
			);
			return this.deserializeManualForecast(response.data);
		});
	}

	async runItemPrediction(
		teamId: number,
		startDate: Date,
		endDate: Date,
		targetDate: Date,
		workItemTypes: string[],
	): Promise<ManualForecast> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IManualForecast>(
				`/forecast/itemprediction/${teamId}`,
				{
					startDate: startDate,
					endDate: endDate,
					targetDate: targetDate,
					workItemTypes: workItemTypes,
				},
			);

			return this.deserializeManualForecast(response.data);
		});
	}

	private deserializeManualForecast(
		manualForecastData: IManualForecast,
	): ManualForecast {
		const whenForecasts = manualForecastData.whenForecasts.map((forecast) => {
			return WhenForecast.new(
				forecast.probability,
				new Date(forecast.expectedDate),
			);
		});
		const howManyForecasts = manualForecastData.howManyForecasts.map(
			(forecast) => new HowManyForecast(forecast.probability, forecast.value),
		);
		return new ManualForecast(
			manualForecastData.remainingItems,
			new Date(manualForecastData.targetDate),
			whenForecasts,
			howManyForecasts,
			manualForecastData.likelihood,
		);
	}
}
