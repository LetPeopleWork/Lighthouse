import { Throughput } from "../../models/Forecasts/Throughput";
import type { IPercentileValue } from "../../models/PercentileValue";
import type { IWorkItem } from "../../models/WorkItem";
import { BaseApiService } from "./BaseApiService";

export interface ITeamMetricsService {
	getThroughput(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<Throughput>;
	getFeaturesInProgress(teamId: number): Promise<IWorkItem[]>;
	getInProgressItems(teamId: number): Promise<IWorkItem[]>;
	getCycleTimePercentiles(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]>;
	getCycleTimeData(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IWorkItem[]>;
}

export class TeamMetricsService
	extends BaseApiService
	implements ITeamMetricsService
{
	async getThroughput(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<Throughput> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<Throughput>(
				`/teams/${teamId}/metrics/throughput?${this.getDateFormatString(startDate, endDate)}`,
			);

			return new Throughput(
				response.data.throughputPerUnitOfTime,
				response.data.history,
				response.data.totalThroughput,
			);
		});
	}

	async getFeaturesInProgress(teamId: number): Promise<IWorkItem[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IWorkItem[]>(
				`/teams/${teamId}/metrics/featuresInProgress`,
			);

			const workItems = response.data.map((workItem) => {
				workItem.startedDate = new Date(workItem.startedDate);
				workItem.closedDate = new Date(workItem.closedDate);
				return workItem;
			});

			return workItems;
		});
	}

	async getInProgressItems(teamId: number): Promise<IWorkItem[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IWorkItem[]>(
				`/teams/${teamId}/metrics/wip`,
			);

			const workItems = response.data.map((workItem) => {
				workItem.startedDate = new Date(workItem.startedDate);
				workItem.closedDate = new Date(workItem.closedDate);
				return workItem;
			});

			return workItems;
		});
	}

	async getCycleTimePercentiles(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPercentileValue[]>(
				`/teams/${teamId}/metrics/cycleTimePercentiles?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}

	async getCycleTimeData(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IWorkItem[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IWorkItem[]>(
				`/teams/${teamId}/metrics/cycleTimeData?${this.getDateFormatString(startDate, endDate)}`,
			);

			const workItems = response.data.map((workItem) => {
				workItem.startedDate = new Date(workItem.startedDate);
				workItem.closedDate = new Date(workItem.closedDate);
				return workItem;
			});

			return workItems;
		});
	}

	getDateFormatString(startDate: Date, endDate: Date): string {
		const formattedStartDate = startDate.toISOString().split("T")[0];
		const formattedEndDate = endDate.toISOString().split("T")[0];

		return `startDate=${formattedStartDate}&endDate=${formattedEndDate}`;
	}
}
