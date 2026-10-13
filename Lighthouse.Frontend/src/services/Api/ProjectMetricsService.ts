import type { IFeature } from "../../models/Feature";
import type { IFeatureSizeEstimationResponse } from "../../models/Metrics/FeatureSizeEstimationData";
import type { ProcessBehaviourChartData } from "../../models/Metrics/ProcessBehaviourChartData";
import {
	BaseMetricsService,
	type IProjectMetricsService,
} from "./MetricsService";

export class ProjectMetricsService
	extends BaseMetricsService<IFeature>
	implements IProjectMetricsService
{
	constructor() {
		super("portfolios");
	}

	async getFeatureSizePbc(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<ProcessBehaviourChartData> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ProcessBehaviourChartData>(
				`/portfolios/${projectId}/metrics/featureSize/pbc?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}

	async getFeatureSizeEstimation(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeatureSizeEstimationResponse> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<IFeatureSizeEstimationResponse>(
					`/portfolios/${projectId}/metrics/featureSizeEstimation?${this.getDateFormatString(startDate, endDate)}`,
				);

			return response.data;
		});
	}
}
