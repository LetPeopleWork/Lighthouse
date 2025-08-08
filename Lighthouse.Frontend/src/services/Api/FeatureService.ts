import type { Feature, IFeature } from "../../models/Feature";
import { BaseApiService } from "./BaseApiService";

export interface IFeatureService {
	getParentFeatures(parentFeatureReferenceIds: string[]): Promise<Feature[]>;
}

export class FeatureService extends BaseApiService implements IFeatureService {
	async getParentFeatures(
		parentFeatureReferenceIds: string[],
	): Promise<Feature[]> {
		return this.withErrorHandling(async () => {
			const params = new URLSearchParams();
			for (const id of parentFeatureReferenceIds) {
				params.append("parentFeatureReferenceIds", id);
			}

			const response = await this.apiService.get<IFeature[]>(
				`/features/parent?${params.toString()}`,
			);

			return BaseApiService.deserializeFeatures(response.data);
		});
	}
}
