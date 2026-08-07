import {
	type IRefreshSettings,
	RefreshSettingsSchema,
} from "../../models/AppSettings/RefreshSettings";
import type { FeatureOrderingPolicy } from "../../models/FeatureOrdering";
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

	// __SCAFFOLD__ — Epic 5375 slice 02
	async getFeatureOrdering(): Promise<FeatureOrderingPolicy> {
		throw new Error("Not yet implemented — RED scaffold");
	}

	// __SCAFFOLD__ — Epic 5375 slice 02
	async updateFeatureOrdering(_policy: FeatureOrderingPolicy): Promise<void> {
		throw new Error("Not yet implemented — RED scaffold");
	}
}
