import { ILighthouseChartData, LighthouseChartData } from '../../models/Charts/LighthouseChartData';
import { BaseApiService } from './BaseApiService';

export interface IChartService {
    getLighthouseChartData(projectId: number): Promise<ILighthouseChartData>;
}

export class ChartService extends BaseApiService implements IChartService {
    async getLighthouseChartData(projectId: number): Promise<ILighthouseChartData> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<ILighthouseChartData>(`/charts/lighthouse?projectId=${projectId}`);
            return this.deserializeLighthouseChart(response.data);
        });
    }

    private deserializeLighthouseChart(lighthouseChartData: ILighthouseChartData): LighthouseChartData {
        return new LighthouseChartData([], []);
    }
}