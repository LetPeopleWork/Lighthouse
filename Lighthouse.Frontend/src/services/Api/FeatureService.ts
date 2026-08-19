import type { IFeature } from "../../models/Feature";
import type { FeatureMoveTarget } from "../../models/FeatureOrdering";
import type { IWorkItem } from "../../models/WorkItem";
import { BaseApiService } from "./BaseApiService";

export interface IFeatureService {
	getAllFeatures(): Promise<IFeature[]>;
	getFeaturesByIds(featureIds: number[]): Promise<IFeature[]>;
	getFeaturesByReferences(featureReferences: string[]): Promise<IFeature[]>;
	getFeatureWorkItems(featureId: number): Promise<IWorkItem[]>;
	/** Every gesture in D18 reduces to this one call (US-03, US-04). */
	moveFeature(featureId: number, target: FeatureMoveTarget): Promise<void>;
}

export class FeatureService extends BaseApiService implements IFeatureService {
	// Every Feature the caller may read, across every Portfolio, in forecast order (US-01).
	getAllFeatures(): Promise<IFeature[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>("/features");

			return BaseApiService.deserializeFeatures(response.data);
		});
	}

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

			const response = await this.apiService.get<unknown>(
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

			const response = await this.apiService.get<unknown>(
				`/features/references?${params.toString()}`,
			);

			return BaseApiService.deserializeFeatures(response.data);
		});
	}

	// Every gesture reduces to this one call; they differ only in which row is named as the target (D18).
	moveFeature(featureId: number, target: FeatureMoveTarget): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.patch(`/features/${featureId}/rank`, target);
		});
	}

	async getFeatureWorkItems(featureId: number): Promise<IWorkItem[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IWorkItem[]>(
				`/features/${featureId}/workitems`,
			);

			return BaseApiService.asArray(
				response.data,
				`/features/${featureId}/workitems`,
			).map((workItem) => {
				workItem.startedDate = new Date(workItem.startedDate);
				workItem.closedDate = new Date(workItem.closedDate);
				return workItem;
			});
		});
	}
}
