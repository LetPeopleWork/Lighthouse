import { BaseApiService } from './BaseApiService';
import { IManualForecast, ManualForecast } from '../../models/Forecasts/ManualForecast';
import { WhenForecast } from '../../models/Forecasts/WhenForecast';
import { HowManyForecast } from '../../models/Forecasts/HowManyForecast';

export interface IForecastService {
    runManualForecast(teamId: number, remainingItems: number, targetDate: Date): Promise<ManualForecast>;
}

export class ForecastService extends BaseApiService implements IForecastService {
    
    async runManualForecast(teamId: number, remainingItems: number, targetDate: Date): Promise<ManualForecast> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.post<IManualForecast>(`/forecast/manual/${teamId}`, {
                remainingItems: remainingItems,
                targetDate: targetDate
            });
            return this.deserializeManualForecast(response.data);
        });
    }

    private deserializeManualForecast(manualForecastData: IManualForecast): ManualForecast {
        const whenForecasts = manualForecastData.whenForecasts.map(forecast => new WhenForecast(forecast.probability, new Date(forecast.expectedDate)));
        const howManyForecasts = manualForecastData.howManyForecasts.map(forecast => new HowManyForecast(forecast.probability, forecast.expectedItems));
        return new ManualForecast(manualForecastData.remainingItems, new Date(manualForecastData.targetDate), whenForecasts, howManyForecasts, manualForecastData.likelihood);
    }
}