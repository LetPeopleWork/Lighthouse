import type { IRefreshSettings } from "../../models/AppSettings/RefreshSettings";
import type { IWorkTrackingSystemSettings } from "../../models/AppSettings/WorkTrackingSystemSettings";
import type { IPortfolioSettings } from "../../models/Portfolio/PortfolioSettings";
import type { ITeamSettings } from "../../models/Team/TeamSettings";
import { BaseApiService } from "./BaseApiService";

export interface ISettingsService {
	getRefreshSettings(settingName: string): Promise<IRefreshSettings>;
	updateRefreshSettings(
		settingName: string,
		refreshSettings: IRefreshSettings,
	): Promise<void>;
	getDefaultTeamSettings(): Promise<ITeamSettings>;
	getDefaultProjectSettings(): Promise<IPortfolioSettings>;
	getWorkTrackingSystemSettings(): Promise<IWorkTrackingSystemSettings>;
	updateWorkTrackingSystemSettings(
		workTrackingSystemSettings: IWorkTrackingSystemSettings,
	): Promise<void>;
}

export class SettingsService
	extends BaseApiService
	implements ISettingsService
{
	async getRefreshSettings(settingName: string): Promise<IRefreshSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IRefreshSettings>(
				`/appsettings/${settingName}Refresh`,
			);

			return response.data;
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

	async getDefaultTeamSettings(): Promise<ITeamSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<ITeamSettings>(
				"/appsettings/defaultteamsettings",
			);

			const teamSettings = response.data;
			teamSettings.throughputHistoryStartDate = new Date(
				teamSettings.throughputHistoryStartDate,
			);
			teamSettings.throughputHistoryEndDate = new Date(
				teamSettings.throughputHistoryEndDate,
			);

			return teamSettings;
		});
	}

	async getDefaultProjectSettings(): Promise<IPortfolioSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IPortfolioSettings>(
				"/appsettings/defaultprojectsettings",
			);

			return response.data;
		});
	}

	async getWorkTrackingSystemSettings(): Promise<IWorkTrackingSystemSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IWorkTrackingSystemSettings>(
				"/appsettings/workTrackingSystemSettings",
			);

			return response.data;
		});
	}

	async updateWorkTrackingSystemSettings(
		workTrackingSystemSettings: IWorkTrackingSystemSettings,
	): Promise<void> {
		this.withErrorHandling(async () => {
			await this.apiService.put<IWorkTrackingSystemSettings>(
				"/appsettings/workTrackingSystemSettings",
				workTrackingSystemSettings,
			);
		});
	}
}
