import { Throughput } from "../../models/Forecasts/Throughput";
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
			const formattedStartDate = startDate.toISOString().split("T")[0];
			const formattedEndDate = endDate.toISOString().split("T")[0];
			const response = await this.apiService.get<Throughput>(
				`/teams/${teamId}/metrics/throughput?startDate=${formattedStartDate}&endDate=${formattedEndDate}`,
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
}
