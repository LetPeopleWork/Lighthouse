import {
	type IRefreshSettings,
	RefreshSettingsSchema,
} from "../../models/AppSettings/RefreshSettings";
import { BaseApiService } from "./BaseApiService";

export interface ISettingsService {
	getRefreshSettings(settingName: string): Promise<IRefreshSettings>;
	updateRefreshSettings(
		settingName: string,
		refreshSettings: IRefreshSettings,
	): Promise<void>;
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
}
