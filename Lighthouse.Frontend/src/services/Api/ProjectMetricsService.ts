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
		super("projects");
	}

	async getSizePercentiles(
		projectId: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPercentileValue[]>(
				`/projects/${projectId}/metrics/sizePercentiles?${this.getDateFormatString(startDate, endDate)}`,
			);

			return response.data;
		});
	}
}
