import {
	type IPreviewFeature,
	PreviewFeature,
} from "../../models/Preview/PreviewFeature";
import { BaseApiService } from "./BaseApiService";

export interface IPreviewFeatureService {
	getAllFeatures(): Promise<PreviewFeature[]>;
	getFeatureByKey(key: string): Promise<PreviewFeature | null>;
	updateFeature(feature: PreviewFeature): Promise<void>;
}

export class PreviewFeatureService
	extends BaseApiService
	implements IPreviewFeatureService
{
	getAllFeatures(): Promise<PreviewFeature[]> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<IPreviewFeature[]>("/previewfeatures");

			return response.data.map(
				(f) =>
					new PreviewFeature(f.id, f.key, f.name, f.description, f.enabled),
			);
		});
	}

	getFeatureByKey(key: string): Promise<PreviewFeature | null> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPreviewFeature>(
				`/previewfeatures/${key}`,
			);

			if (response.data) {
				return new PreviewFeature(
					response.data.id,
					response.data.key,
					response.data.name,
					response.data.description,
					response.data.enabled,
				);
			}

			return null;
		});
	}

	updateFeature(feature: PreviewFeature): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post<PreviewFeature>(
				`/previewfeatures/${feature.id}`,
				feature,
			);
		});
	}
}
