import type { IFeature } from "../../models/Feature";
import type { IPercentileValue } from "../../models/PercentileValue";
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

	async getSizePercentiles(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPercentileValue[]>(
				`/portfolios/${projectId}/metrics/sizePercentiles?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}

	async getAllFeaturesForSizeChart(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeature[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IFeature[]>(
				`/portfolios/${projectId}/metrics/allFeaturesForSizeChart?${this.getDateFormatString(startDate, endDate)}`,
			);

			const features = response.data.map((feature) => {
				feature.startedDate = new Date(feature.startedDate);
				feature.closedDate = new Date(feature.closedDate);
				return feature;
			});

			return features;
		});
	}
}
