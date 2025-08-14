import type { IRefreshSettings } from "../../models/AppSettings/RefreshSettings";
import type { IWorkTrackingSystemSettings } from "../../models/AppSettings/WorkTrackingSystemSettings";
import type { IProjectSettings } from "../../models/Project/ProjectSettings";
import type { ITeamSettings } from "../../models/Team/TeamSettings";
import { BaseApiService } from "./BaseApiService";

export interface ISettingsService {
	getRefreshSettings(settingName: string): Promise<IRefreshSettings>;
	updateRefreshSettings(
		settingName: string,
		refreshSettings: IRefreshSettings,
	): Promise<void>;
	getDefaultTeamSettings(): Promise<ITeamSettings>;
	updateDefaultTeamSettings(teamSettings: ITeamSettings): Promise<void>;
	getDefaultProjectSettings(): Promise<IProjectSettings>;
	updateDefaultProjectSettings(projecSettings: IProjectSettings): Promise<void>;
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

	async updateDefaultTeamSettings(teamSettings: ITeamSettings): Promise<void> {
		this.withErrorHandling(async () => {
			await this.apiService.put<ITeamSettings>(
				"/appsettings/defaultteamsettings",
				teamSettings,
			);
		});
	}

	async getDefaultProjectSettings(): Promise<IProjectSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IProjectSettings>(
				"/appsettings/defaultprojectsettings",
			);

			return this.deserializeProjectSettings(response.data);
		});
	}

	async updateDefaultProjectSettings(
		projecSettings: IProjectSettings,
	): Promise<void> {
		this.withErrorHandling(async () => {
			await this.apiService.put<IProjectSettings>(
				"/appsettings/defaultprojectsettings",
				projecSettings,
			);
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
