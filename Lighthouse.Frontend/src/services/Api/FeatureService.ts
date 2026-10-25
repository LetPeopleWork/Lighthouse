import { Feature, type IFeature } from "../../models/Feature";
import {
	type FeatureListOwner,
	FeatureListSchema,
	type IFeatureList,
} from "../../models/FeatureList";
import type { FeatureMoveTarget } from "../../models/FeatureOrdering";
import type { IWorkItem } from "../../models/WorkItem";
import { BaseApiService } from "./BaseApiService";

export interface IFeatureService {
	getAllFeatures(): Promise<IFeature[]>;
	getFeaturesByIds(featureIds: number[]): Promise<IFeature[]>;
	getFeaturesByReferences(featureReferences: string[]): Promise<IFeature[]>;
	getFeatureWorkItems(featureId: number): Promise<IWorkItem[]>;
	/** Every way of reordering a Feature reduces to this one call. */
	moveFeature(featureId: number, target: FeatureMoveTarget): Promise<void>;
	/** Everything one Features tab shows, in one answer. */
	getFeatureList(owner: FeatureListOwner): Promise<IFeatureList>;
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

	getFeaturesByReferences(
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

	getFeatureList(owner: FeatureListOwner): Promise<IFeatureList> {
		return this.withErrorHandling(async () => {
			const collection = owner.kind === "portfolio" ? "portfolios" : "teams";
			const response = await this.apiService.get<unknown>(
				`/${collection}/${owner.id}/feature-list`,
			);

			const answer = BaseApiService.parse(FeatureListSchema, response.data);

			return { ...answer, features: answer.features.map(Feature.fromParsed) };
		});
	}

	moveFeature(featureId: number, target: FeatureMoveTarget): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.patch(`/features/${featureId}/rank`, target);
		});
	}

	getFeatureWorkItems(featureId: number): Promise<IWorkItem[]> {
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
