import type { IFeature } from "../../models/Feature";
import { RunChartData } from "../../models/Forecasts/RunChartData";
import type { IPercentileValue } from "../../models/PercentileValue";
import type { IWorkItem } from "../../models/WorkItem";
import { BaseApiService } from "./BaseApiService";

export interface IProjectMetricsService {
	getThroughputForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData>;
	getFeaturesInProgressOverTimeForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData>;
	getInProgressFeaturesForProject(projectId: number): Promise<IWorkItem[]>;
	getCycleTimePercentilesForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]>;
	getCycleTimeDataForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeature[]>;
}

export class ProjectMetricsService
	extends BaseApiService
	implements IProjectMetricsService
{
	async getThroughputForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RunChartData>(
				`/projects/${projectId}/metrics/throughput?${this.getDateFormatString(startDate, endDate)}`,
			);

			return new RunChartData(
				response.data.valuePerUnitOfTime,
				response.data.history,
				response.data.total,
			);
		});
	}

	async getFeaturesInProgressOverTimeForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RunChartData>(
				`/projects/${projectId}/metrics/featuresInProgressOverTime?${this.getDateFormatString(startDate, endDate)}`,
			);

			return new RunChartData(
				response.data.valuePerUnitOfTime,
				response.data.history,
				response.data.total,
			);
		});
	}

	async getInProgressFeaturesForProject(
		projectId: number,
	): Promise<IWorkItem[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IWorkItem[]>(
				`/projects/${projectId}/metrics/inProgressFeatures`,
			);

			const workItems = response.data.map((workItem) => {
				workItem.startedDate = new Date(workItem.startedDate);
				workItem.closedDate = new Date(workItem.closedDate);
				return workItem;
			});

			return workItems;
		});
	}

	async getCycleTimePercentilesForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPercentileValue[]>(
				`/projects/${projectId}/metrics/cycleTimePercentiles?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}

	async getCycleTimeDataForProject(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeature[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IFeature[]>(
				`/projects/${projectId}/metrics/cycleTimeData?${this.getDateFormatString(startDate, endDate)}`,
			);

			const features = response.data.map((feature) => {
				if (feature.startedDate) {
					feature.startedDate = new Date(feature.startedDate);
				}
				if (feature.closedDate) {
					feature.closedDate = new Date(feature.closedDate);
				}
				return feature;
			});

			return features;
		});
	}

	getDateFormatString(startDate: Date, endDate: Date): string {
		const formattedStartDate = startDate.toISOString().split("T")[0];
		const formattedEndDate = endDate.toISOString().split("T")[0];

		return `startDate=${formattedStartDate}&endDate=${formattedEndDate}`;
	}
}
