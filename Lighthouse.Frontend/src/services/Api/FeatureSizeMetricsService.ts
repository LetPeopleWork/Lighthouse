import type { IFeature } from "../../models/Feature";
import type { IFeatureSizePercentilesInfo } from "../../models/Metrics/InfoWidgetData";
import type { IPercentileValue } from "../../models/PercentileValue";

/**
 * Feature sizes, for any owner that has them. A Team counts only its own Work Items in each
 * Feature, so it answers with the same shapes as a Portfolio but different sizes.
 */
export interface IFeatureSizeMetricsService {
	getSizePercentiles(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IPercentileValue[]>;

	getAllFeaturesForSizeChart(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeature[]>;

	getFeatureSizePercentilesInfo(
		id: number,
		startDate: Date,
		endDate: Date,
	): Promise<IFeatureSizePercentilesInfo>;
}
