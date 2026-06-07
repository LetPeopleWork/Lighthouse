import { z } from "zod";
import {
	type IOptionalFeature,
	OptionalFeatureSchema,
} from "../../models/OptionalFeatures/OptionalFeature";
import { BaseApiService } from "./BaseApiService";

export interface IOptionalFeatureService {
	getAllFeatures(): Promise<IOptionalFeature[]>;
	getFeatureByKey(key: string): Promise<IOptionalFeature | null>;
	updateFeature(feature: IOptionalFeature): Promise<void>;
}

export class OptionalFeatureService
	extends BaseApiService
	implements IOptionalFeatureService
{
	getAllFeatures(): Promise<IOptionalFeature[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>("/optionalfeatures");

			return BaseApiService.parse(
				z.array(OptionalFeatureSchema),
				response.data,
			);
		});
	}

	getFeatureByKey(key: string): Promise<IOptionalFeature | null> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(
				`/optionalfeatures/${key}`,
			);

			return response.data == null
				? null
				: BaseApiService.parse(OptionalFeatureSchema, response.data);
		});
	}

	updateFeature(feature: IOptionalFeature): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post<IOptionalFeature>(
				`/optionalfeatures/${feature.id}`,
				feature,
			);
		});
	}
}
