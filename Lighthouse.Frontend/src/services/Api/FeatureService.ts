import type { IFeature } from "../../models/Feature";
import { BaseApiService } from "./BaseApiService";

export interface IFeatureService {
	getFeaturesByIds(featureIds: number[]): Promise<IFeature[]>;
	getFeaturesByReferences(featureReferences: string[]): Promise<IFeature[]>;
}

export class FeatureService extends BaseApiService implements IFeatureService {
	getFeaturesByIds(featureIds: number[]): Promise<IFeature[]> {
		return this.withErrorHandling(async () => {
			// Return empty array if no feature IDs are provided
			if (!featureIds || featureIds.length === 0) {
				return [];
			}

			const params = new URLSearchParams();
			for (const id of featureIds) {
				params.append("featureIds", `${id}`);
			}

			const response = await this.apiService.get<IFeature[]>(
				`/features/ids?${params.toString()}`,
			);

			return BaseApiService.deserializeFeatures(response.data);
		});
	}

	async getFeaturesByReferences(
		parentFeatureReferenceIds: string[],
	): Promise<IFeature[]> {
		return this.withErrorHandling(async () => {
			const params = new URLSearchParams();
			for (const id of parentFeatureReferenceIds) {
				params.append("featureReferences", id);
			}

			const response = await this.apiService.get<IFeature[]>(
				`/features/references?${params.toString()}`,
			);

			return BaseApiService.deserializeFeatures(response.data);
		});
	}
}
