import {
	type IRefreshSettings,
	RefreshSettingsSchema,
} from "../../models/AppSettings/RefreshSettings";
import {
	type FeatureOrderingPolicy,
	FeatureOrderingSchema,
} from "../../models/FeatureOrdering";
import { BaseApiService } from "./BaseApiService";

export interface ISettingsService {
	getRefreshSettings(settingName: string): Promise<IRefreshSettings>;
	updateRefreshSettings(
		settingName: string,
		refreshSettings: IRefreshSettings,
	): Promise<void>;
	getFeatureOrdering(): Promise<FeatureOrderingPolicy>;
	updateFeatureOrdering(policy: FeatureOrderingPolicy): Promise<void>;
}

export class SettingsService
	extends BaseApiService
	implements ISettingsService
{
	async getRefreshSettings(settingName: string): Promise<IRefreshSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(
				`/appsettings/${settingName}Refresh`,
			);

			return BaseApiService.parse(RefreshSettingsSchema, response.data);
		});
	}

	async updateRefreshSettings(
		settingName: string,
		refreshSettings: IRefreshSettings,
	): Promise<void> {
		this.withErrorHandling(async () => {
			await this.apiService.put<IRefreshSettings>(
				`/appsettings/${settingName}Refresh`,
				refreshSettings,
			);
		});
	}

	async getFeatureOrdering(): Promise<FeatureOrderingPolicy> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<unknown>(
				"/appsettings/FeatureOrdering",
			);

			return BaseApiService.parse(FeatureOrderingSchema, response.data).policy;
		});
	}

	async updateFeatureOrdering(policy: FeatureOrderingPolicy): Promise<void> {
		await this.withErrorHandling(async () => {
			await this.apiService.put("/appsettings/FeatureOrdering", { policy });
		});
	}
}
