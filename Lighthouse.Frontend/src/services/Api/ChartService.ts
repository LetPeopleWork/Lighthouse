import { ILighthouseChart, LighthouseChart } from '../../models/Charts/LighthouseChart';
import { BaseApiService } from './BaseApiService';

export type TimeInterval = "day" | "week" | "month";

export interface IChartService {
    getLighthouseChartData(projectId: number, interval: TimeInterval): Promise<ILighthouseChart>;
}

export class ChartService extends BaseApiService implements IChartService {
    async getLighthouseChartData(projectId: number, interval: TimeInterval): Promise<ILighthouseChart> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<ILighthouseChart>(`/charts/lighthouse?projectId=${projectId}&interval=${interval}`);
            return this.deserializeLighthouseChart(response.data);
        });
    }

    private deserializeLighthouseChart(lighthouseChartData: ILighthouseChart): LighthouseChart {
        return new LighthouseChart();
    }
}