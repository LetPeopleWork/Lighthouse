import { BurndownEntry, ILighthouseChartData, ILighthouseChartFeatureData, LighthouseChartData, LighthouseChartFeatureData } from '../../models/Charts/LighthouseChartData';
import { IMilestone, Milestone } from '../../models/Project/Milestone';
import { BaseApiService } from './BaseApiService';

export interface IChartService {
    getLighthouseChartData(projectId: number, startDate: Date, sampleRate: number): Promise<ILighthouseChartData>;
}

export class ChartService extends BaseApiService implements IChartService {
    async getLighthouseChartData(projectId: number, startDate: Date, sampleRate: number): Promise<ILighthouseChartData> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.post<ILighthouseChartData>(`/lighthousechart/${projectId}`, {
                startDate: startDate,
                sampleRate: sampleRate
            });
            return this.deserializeLighthouseChartData(response.data);
        });
    }

    private deserializeLighthouseChartData(lighthouseChartData: ILighthouseChartData): LighthouseChartData {
        const featureData = lighthouseChartData.features.map(this.deserializeLighthouseChartFeatureData);

        const milestones = lighthouseChartData.milestones.map((milestone: IMilestone) => {
            return new Milestone(milestone.id, milestone.name, new Date(milestone.date))
        })

        return new LighthouseChartData(featureData, milestones);
    }

    private deserializeLighthouseChartFeatureData(featureData: ILighthouseChartFeatureData) {
        const remainingItems = featureData.remainingItemsTrend.map((entry) => {
            return new BurndownEntry(new Date(entry.date), entry.remainingItems);
        });

        const forecasts = featureData.forecasts.map((forecast) => new Date(forecast));

        return new LighthouseChartFeatureData(featureData.name, forecasts, remainingItems);
    }
}