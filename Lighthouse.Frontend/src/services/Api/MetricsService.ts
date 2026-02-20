import type { IFeature } from "../../models/Feature";
import {
	ForecastPredictabilityScore,
	type IForecastPredictabilityScore,
} from "../../models/Forecasts/ForecastPredictabilityScore";
import type { ProcessBehaviourChartData } from "../../models/Metrics/ProcessBehaviourChartData";
import { RunChartData } from "../../models/Metrics/RunChartData";
import type { IPercentileValue } from "../../models/PercentileValue";
import type { IWorkItem } from "../../models/WorkItem";
import { BaseApiService } from "./BaseApiService";

export interface IMetricsService<T extends IWorkItem | IFeature> {
	getThroughput(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData>;

	getStartedItems(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData>;

	getWorkInProgressOverTime(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData>;

	getInProgressItems(id: number): Promise<IWorkItem[]>;

	getCycleTimeData(id: number, startDate: Date, endDate: Date): Promise<T[]>;

	getCycleTimePercentiles(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]>;

	getMultiItemForecastPredictabilityScore(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ForecastPredictabilityScore>;

	getTotalWorkItemAge(id: number): Promise<number>;

	getThroughputPbc(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData>;

	getWipPbc(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData>;

	getTotalWorkItemAgePbc(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData>;

	getCycleTimePbc(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData>;
}

export interface ITeamMetricsService extends IMetricsService<IWorkItem> {
	getFeaturesInProgress(teamId: number): Promise<IWorkItem[]>;
}

export interface IProjectMetricsService extends IMetricsService<IFeature> {
	getSizePercentiles(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]>;

	getAllFeaturesForSizeChart(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeature[]>;

	getFeatureSizePbc(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData>;
}

export abstract class BaseMetricsService<T extends IWorkItem | IFeature>
	extends BaseApiService
	implements IMetricsService<T>
{
	private readonly api: string;

	constructor(api: "teams" | "portfolios") {
		super();
		this.api = api;
	}

	async getThroughput(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RunChartData>(
				`/${this.api}/${id}/metrics/throughput?${this.getDateFormatString(startDate, endDate)}`,
			);

			return new RunChartData(
				response.data.workItemsPerUnitOfTime,
				response.data.history,
				response.data.total,
			);
		});
	}

	async getStartedItems(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RunChartData>(
				`/${this.api}/${id}/metrics/started?${this.getDateFormatString(startDate, endDate)}`,
			);

			return new RunChartData(
				response.data.workItemsPerUnitOfTime,
				response.data.history,
				response.data.total,
			);
		});
	}

	async getWorkInProgressOverTime(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RunChartData>(
				`/${this.api}/${id}/metrics/wipOverTime?${this.getDateFormatString(startDate, endDate)}`,
			);

			return new RunChartData(
				response.data.workItemsPerUnitOfTime,
				response.data.history,
				response.data.total,
			);
		});
	}

	async getInProgressItems(id: number): Promise<IWorkItem[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IWorkItem[]>(
				`/${this.api}/${id}/metrics/currentwip`,
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
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPercentileValue[]>(
				`/${this.api}/${id}/metrics/cycleTimePercentiles?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}

	async getCycleTimeData(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<T[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<T[]>(
				`/${this.api}/${id}/metrics/cycleTimeData?${this.getDateFormatString(startDate, endDate)}`,
			);

			const items = response.data.map((workItem) => {
				workItem.startedDate = new Date(workItem.startedDate);
				workItem.closedDate = new Date(workItem.closedDate);
				return workItem;
			});

			return items;
		});
	}

	async getMultiItemForecastPredictabilityScore(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IForecastPredictabilityScore> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IForecastPredictabilityScore>(
				`/${this.api}/${id}/metrics/multiitemforecastpredictabilityscore?${this.getDateFormatString(startDate, endDate)}`,
			);

			return this.deserializeForecastAccuracy(response.data);
		});
	}

	async getTotalWorkItemAge(id: number): Promise<number> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<number>(
				`/${this.api}/${id}/metrics/totalWorkItemAge`,
			);

			return response.data;
		});
	}

	async getThroughputPbc(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ProcessBehaviourChartData>(
				`/${this.api}/${id}/metrics/throughput/pbc?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}

	async getWipPbc(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ProcessBehaviourChartData>(
				`/${this.api}/${id}/metrics/wipOverTime/pbc?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}

	async getTotalWorkItemAgePbc(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ProcessBehaviourChartData>(
				`/${this.api}/${id}/metrics/totalWorkItemAge/pbc?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}

	async getCycleTimePbc(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ProcessBehaviourChartData>(
				`/${this.api}/${id}/metrics/cycleTime/pbc?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}

	private deserializeForecastAccuracy(
		forecastPredictabilityScoreData: IForecastPredictabilityScore,
	): ForecastPredictabilityScore {
		const forecastResults = new Map<number, number>(
			Object.entries(forecastPredictabilityScoreData.forecastResults).map(
				([key, value]) => [Number(key), value as number],
			),
		);

		return new ForecastPredictabilityScore(
			forecastPredictabilityScoreData.percentiles,
			forecastPredictabilityScoreData.predictabilityScore,
			forecastResults,
		);
	}

	getDateFormatString(startDate: Date, endDate: Date): string {
		const formattedStartDate = startDate.toISOString().split("T")[0];
		const formattedEndDate = endDate.toISOString().split("T")[0];

		return `startDate=${formattedStartDate}&endDate=${formattedEndDate}`;
	}
}
