import type { BlockedCountSnapshot } from "../../models/BlockedCountSnapshot";
import type { IFeature } from "../../models/Feature";
import type { IForecastInputCandidates } from "../../models/Forecasts/ForecastInputCandidates";
import {
	ForecastPredictabilityScore,
	type IForecastPredictabilityScore,
} from "../../models/Forecasts/ForecastPredictabilityScore";
import type { ICumulativeStateTimeResponse } from "../../models/Metrics/CumulativeStateTime";
import type { ICumulativeStateTimeCandidatesResponse } from "../../models/Metrics/CumulativeStateTimeCandidates";
import type { ICumulativeStateTimeItemsResponse } from "../../models/Metrics/CumulativeStateTimeItems";
import type { IEstimationVsCycleTimeResponse } from "../../models/Metrics/EstimationVsCycleTimeData";
import type { IFeatureSizeEstimationResponse } from "../../models/Metrics/FeatureSizeEstimationData";
import type { IFlowEfficiencyInfo } from "../../models/Metrics/FlowEfficiencyInfo";
import type {
	IArrivalsInfo,
	ICycleTimePercentilesInfo,
	IFeatureSizePercentilesInfo,
	IFeaturesWorkedOnInfo,
	IPredictabilityScoreInfo,
	IThroughputInfo,
	ITotalWorkItemAgeInfo,
	IWipOverviewInfo,
} from "../../models/Metrics/InfoWidgetData";
import {
	type INamedCycleTimeValue,
	NamedCycleTimeValueSchema,
} from "../../models/Metrics/NamedCycleTime";
import type { ProcessBehaviourChartData } from "../../models/Metrics/ProcessBehaviourChartData";
import { RunChartData } from "../../models/Metrics/RunChartData";
import {
	type IPercentileValue,
	PercentileValueSchema,
} from "../../models/PercentileValue";
import type { IPerStatePercentileValues } from "../../models/PerStatePercentileValues";
import type { IWorkItem } from "../../models/WorkItem";
import { BaseApiService } from "./BaseApiService";

export interface IMetricsService<T extends IWorkItem | IFeature> {
	getThroughput(
		id: number,
		startDate: Date,
		endDate: Date,
		view?: "raw" | "filtered",
	): Promise<RunChartData>;

	getWorkInProgressOverTime(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData>;

	getInProgressItems(id: number, asOfDate: Date): Promise<IWorkItem[]>;

	getCycleTimeData(id: number, startDate: Date, endDate: Date): Promise<T[]>;

	getCycleTimePercentiles(
		id: number,
		startDate: Date,
		endDate: Date,
		definitionId?: number,
	): Promise<IPercentileValue[]>;

	getWorkItemAgePercentiles(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]>;

	getAgeInStatePercentiles(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPerStatePercentileValues[]>;

	getCumulativeStateTimeForTeam(
		id: number,
		startDate: Date,
		endDate: Date,
		itemIds?: number[],
		definitionId?: number,
	): Promise<ICumulativeStateTimeResponse>;

	getCumulativeStateTimeItemsForTeam(
		id: number,
		state: string,
		startDate: Date,
		endDate: Date,
		itemIds?: number[],
	): Promise<ICumulativeStateTimeItemsResponse>;

	getCumulativeStateTimeItemsForPortfolio(
		id: number,
		state: string,
		startDate: Date,
		endDate: Date,
		itemIds?: number[],
	): Promise<ICumulativeStateTimeItemsResponse>;

	getCumulativeStateTimeCandidatesForTeam(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ICumulativeStateTimeCandidatesResponse>;

	getCumulativeStateTimeCandidatesForPortfolio(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ICumulativeStateTimeCandidatesResponse>;

	getMultiItemForecastPredictabilityScore(
		id: number,
		startDate: Date,
		endDate: Date,
		view?: "raw" | "filtered",
	): Promise<ForecastPredictabilityScore>;

	getTotalWorkItemAge(id: number, asOfDate: Date): Promise<number>;

	getThroughputPbc(
		id: number,
		startDate: Date,
		endDate: Date,
		view?: "raw" | "filtered",
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

	getEstimationVsCycleTimeData(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IEstimationVsCycleTimeResponse>;

	getArrivals(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData>;

	getArrivalsPbc(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData>;

	getThroughputInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IThroughputInfo>;

	getArrivalsInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IArrivalsInfo>;

	getWipOverviewInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IWipOverviewInfo>;

	getTotalWorkItemAgeInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ITotalWorkItemAgeInfo>;

	getPredictabilityScoreInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPredictabilityScoreInfo>;

	getCycleTimePercentilesInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ICycleTimePercentilesInfo>;

	getBlockedCountHistory(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<BlockedCountSnapshot[]>;

	getBlockedItemsAtDate(id: number, date: Date | string): Promise<IWorkItem[]>;

	getFlowEfficiencyInfoForTeam(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFlowEfficiencyInfo>;

	getFlowEfficiencyInfoForPortfolio(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFlowEfficiencyInfo>;
}

export interface ITeamMetricsService extends IMetricsService<IWorkItem> {
	getFeaturesInProgress(teamId: number, asOfDate: Date): Promise<IWorkItem[]>;
	getForecastInputCandidates(teamId: number): Promise<IForecastInputCandidates>;
	getFeaturesWorkedOnInfo(
		teamId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeaturesWorkedOnInfo>;
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

	getFeatureSizeEstimation(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeatureSizeEstimationResponse>;

	getFeatureSizePercentilesInfo(
		portfolioId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeatureSizePercentilesInfo>;
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
		view?: "raw" | "filtered",
	): Promise<RunChartData> {
		return this.withErrorHandling(async () => {
			const viewSuffix = view === "filtered" ? "&view=filtered" : "";
			const response = await this.apiService.get<RunChartData>(
				`/${this.api}/${id}/metrics/throughput?${this.getDateFormatString(startDate, endDate)}${viewSuffix}`,
			);

			return new RunChartData(
				response.data.workItemsPerUnitOfTime,
				response.data.history,
				response.data.total,
				response.data.blackoutDayIndices,
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
				response.data.blackoutDayIndices,
			);
		});
	}

	async getInProgressItems(id: number, asOfDate: Date): Promise<IWorkItem[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IWorkItem[]>(
				`/${this.api}/${id}/metrics/wip?asOfDate=${this.formatLocalDate(asOfDate)}`,
			);

			const workItems = response.data.map((workItem) => {
				workItem.startedDate = new Date(workItem.startedDate);
				workItem.closedDate = new Date(workItem.closedDate);
				workItem.currentStateEnteredAt = workItem.currentStateEnteredAt
					? new Date(workItem.currentStateEnteredAt)
					: workItem.currentStateEnteredAt;
				return workItem;
			});

			return workItems;
		});
	}

	async getCycleTimePercentiles(
		id: number,
		startDate: Date,
		endDate: Date,
		definitionId?: number,
	): Promise<IPercentileValue[]> {
		return this.withErrorHandling(async () => {
			const definitionSuffix =
				definitionId === undefined ? "" : `&definitionId=${definitionId}`;
			const response = await this.apiService.get<IPercentileValue[]>(
				`/${this.api}/${id}/metrics/cycleTimePercentiles?${this.getDateFormatString(startDate, endDate)}${definitionSuffix}`,
			);

			return response.data;
		});
	}

	async getWorkItemAgePercentiles(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(
				`/${this.api}/${id}/metrics/workItemAgePercentiles?${this.getDateFormatString(startDate, endDate)}`,
			);

			return BaseMetricsService.parse(
				PercentileValueSchema.array(),
				response.data,
			);
		});
	}

	async getAgeInStatePercentiles(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPerStatePercentileValues[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPerStatePercentileValues[]>(
				`/${this.api}/${id}/metrics/ageInStatePercentiles?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}

	async getCumulativeStateTimeForTeam(
		id: number,
		startDate: Date,
		endDate: Date,
		itemIds?: number[],
		definitionId?: number,
	): Promise<ICumulativeStateTimeResponse> {
		return this.withErrorHandling(async () => {
			const itemIdsSuffix = (itemIds ?? [])
				.map((itemId) => `&itemIds=${itemId}`)
				.join("");
			const definitionSuffix =
				definitionId === undefined ? "" : `&definitionId=${definitionId}`;
			const response = await this.apiService.get<ICumulativeStateTimeResponse>(
				`/${this.api}/${id}/metrics/cumulativeStateTime?${this.getDateFormatString(startDate, endDate)}${itemIdsSuffix}${definitionSuffix}`,
			);

			return response.data;
		});
	}

	getCumulativeStateTimeItemsForTeam(
		id: number,
		state: string,
		startDate: Date,
		endDate: Date,
		itemIds?: number[],
	): Promise<ICumulativeStateTimeItemsResponse> {
		return this.fetchCumulativeStateTimeItems(
			id,
			state,
			startDate,
			endDate,
			itemIds,
		);
	}

	getCumulativeStateTimeItemsForPortfolio(
		id: number,
		state: string,
		startDate: Date,
		endDate: Date,
		itemIds?: number[],
	): Promise<ICumulativeStateTimeItemsResponse> {
		return this.getCumulativeStateTimeItemsForTeam(
			id,
			state,
			startDate,
			endDate,
			itemIds,
		);
	}

	private fetchCumulativeStateTimeItems(
		id: number,
		state: string,
		startDate: Date,
		endDate: Date,
		itemIds?: number[],
	): Promise<ICumulativeStateTimeItemsResponse> {
		return this.withErrorHandling(async () => {
			const itemIdsSuffix = (itemIds ?? [])
				.map((itemId) => `&itemIds=${itemId}`)
				.join("");
			const stateSuffix = `&state=${encodeURIComponent(state)}`;
			const response =
				await this.apiService.get<ICumulativeStateTimeItemsResponse>(
					`/${this.api}/${id}/metrics/cumulativeStateTime/items?${this.getDateFormatString(startDate, endDate)}${stateSuffix}${itemIdsSuffix}`,
				);

			return response.data;
		});
	}

	getCumulativeStateTimeCandidatesForTeam(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ICumulativeStateTimeCandidatesResponse> {
		return this.fetchCumulativeStateTimeCandidates(id, startDate, endDate);
	}

	getCumulativeStateTimeCandidatesForPortfolio(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ICumulativeStateTimeCandidatesResponse> {
		return this.getCumulativeStateTimeCandidatesForTeam(id, startDate, endDate);
	}

	private fetchCumulativeStateTimeCandidates(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ICumulativeStateTimeCandidatesResponse> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<ICumulativeStateTimeCandidatesResponse>(
					`/${this.api}/${id}/metrics/cumulativeStateTime/candidates?${this.getDateFormatString(startDate, endDate)}`,
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
				const parsedNamedCycleTimes =
					NamedCycleTimeValueSchema.array().safeParse(
						(workItem as { namedCycleTimes?: unknown }).namedCycleTimes,
					);
				(
					workItem as { namedCycleTimes?: INamedCycleTimeValue[] }
				).namedCycleTimes = parsedNamedCycleTimes.success
					? parsedNamedCycleTimes.data
					: [];
				return workItem;
			});

			return items;
		});
	}

	async getMultiItemForecastPredictabilityScore(
		id: number,
		startDate: Date,
		endDate: Date,
		view?: "raw" | "filtered",
	): Promise<IForecastPredictabilityScore> {
		return this.withErrorHandling(async () => {
			const viewSuffix = view ? `&view=${view}` : "";
			const response = await this.apiService.get<IForecastPredictabilityScore>(
				`/${this.api}/${id}/metrics/multiitemforecastpredictabilityscore?${this.getDateFormatString(startDate, endDate)}${viewSuffix}`,
			);

			return this.deserializeForecastAccuracy(response.data);
		});
	}

	async getTotalWorkItemAge(id: number, asOfDate: Date): Promise<number> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<number>(
				`/${this.api}/${id}/metrics/totalWorkItemAge?asOfDate=${this.formatLocalDate(asOfDate)}`,
			);

			return response.data;
		});
	}

	async getThroughputPbc(
		id: number,
		startDate: Date,
		endDate: Date,
		view?: "raw" | "filtered",
	): Promise<ProcessBehaviourChartData> {
		return this.withErrorHandling(async () => {
			const viewSuffix = view === "filtered" ? "&view=filtered" : "";
			const response = await this.apiService.get<ProcessBehaviourChartData>(
				`/${this.api}/${id}/metrics/throughput/pbc?${this.getDateFormatString(startDate, endDate)}${viewSuffix}`,
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

	async getEstimationVsCycleTimeData(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IEstimationVsCycleTimeResponse> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<IEstimationVsCycleTimeResponse>(
					`/${this.api}/${id}/metrics/estimationVsCycleTime?${this.getDateFormatString(startDate, endDate)}`,
				);

			return response.data;
		});
	}

	async getArrivals(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<RunChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RunChartData>(
				`/${this.api}/${id}/metrics/arrivals?${this.getDateFormatString(startDate, endDate)}`,
			);

			return new RunChartData(
				response.data.workItemsPerUnitOfTime,
				response.data.history,
				response.data.total,
				response.data.blackoutDayIndices,
			);
		});
	}

	async getArrivalsPbc(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ProcessBehaviourChartData>(
				`/${this.api}/${id}/metrics/arrivals/pbc?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}

	async getThroughputInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IThroughputInfo> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IThroughputInfo>(
				`/${this.api}/${id}/metrics/throughputInfo?${this.getDateFormatString(startDate, endDate)}`,
			);
			return response.data;
		});
	}

	async getArrivalsInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IArrivalsInfo> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IArrivalsInfo>(
				`/${this.api}/${id}/metrics/arrivalsInfo?${this.getDateFormatString(startDate, endDate)}`,
			);
			return response.data;
		});
	}

	async getWipOverviewInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IWipOverviewInfo> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IWipOverviewInfo>(
				`/${this.api}/${id}/metrics/wipOverviewInfo?${this.getDateFormatString(startDate, endDate)}`,
			);
			return response.data;
		});
	}

	async getTotalWorkItemAgeInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ITotalWorkItemAgeInfo> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ITotalWorkItemAgeInfo>(
				`/${this.api}/${id}/metrics/totalWorkItemAgeInfo?${this.getDateFormatString(startDate, endDate)}`,
			);
			return response.data;
		});
	}

	async getPredictabilityScoreInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPredictabilityScoreInfo> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPredictabilityScoreInfo>(
				`/${this.api}/${id}/metrics/predictabilityScoreInfo?${this.getDateFormatString(startDate, endDate)}`,
			);
			return response.data;
		});
	}

	async getCycleTimePercentilesInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<ICycleTimePercentilesInfo> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ICycleTimePercentilesInfo>(
				`/${this.api}/${id}/metrics/cycleTimePercentilesInfo?${this.getDateFormatString(startDate, endDate)}`,
			);
			return response.data;
		});
	}

	async getBlockedCountHistory(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<BlockedCountSnapshot[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<BlockedCountSnapshot[]>(
				`/${this.api}/${id}/metrics/blockedCountHistory?${this.getDateFormatString(startDate, endDate)}`,
			);
			return response.data;
		});
	}

	async getBlockedItemsAtDate(
		id: number,
		date: Date | string,
	): Promise<IWorkItem[]> {
		return this.withErrorHandling(async () => {
			const formattedDate =
				typeof date === "string" ? date : this.formatLocalDate(date);
			const response = await this.apiService.get<IWorkItem[]>(
				`/${this.api}/${id}/metrics/blockedItemsAtDate?date=${formattedDate}`,
			);

			const workItems = response.data.map((workItem) => {
				workItem.startedDate = new Date(workItem.startedDate);
				workItem.closedDate = new Date(workItem.closedDate);
				workItem.currentStateEnteredAt = workItem.currentStateEnteredAt
					? new Date(workItem.currentStateEnteredAt)
					: workItem.currentStateEnteredAt;
				return workItem;
			});

			return workItems;
		});
	}

	getFlowEfficiencyInfoForTeam(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFlowEfficiencyInfo> {
		return this.fetchFlowEfficiencyInfo(id, startDate, endDate);
	}

	getFlowEfficiencyInfoForPortfolio(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFlowEfficiencyInfo> {
		return this.fetchFlowEfficiencyInfo(id, startDate, endDate);
	}

	private fetchFlowEfficiencyInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFlowEfficiencyInfo> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IFlowEfficiencyInfo>(
				`/${this.api}/${id}/metrics/flowEfficiencyInfo?${this.getDateFormatString(startDate, endDate)}`,
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
		const formattedStartDate = this.formatLocalDate(startDate);
		const formattedEndDate = this.formatLocalDate(endDate);

		return `startDate=${formattedStartDate}&endDate=${formattedEndDate}`;
	}

	private formatLocalDate(date: Date): string {
		const year = date.getFullYear();
		const month = String(date.getMonth() + 1).padStart(2, "0");
		const day = String(date.getDate()).padStart(2, "0");
		return `${year}-${month}-${day}`;
	}
}
