import type { IProject, Project } from "../../models/Project/Project";
import type { IProjectSettings } from "../../models/Project/ProjectSettings";
import { BaseApiService } from "./BaseApiService";

export interface IProjectService {
	getProjects(): Promise<Project[]>;
	deleteProject(id: number): Promise<void>;
	getProject(id: number): Promise<Project | null>;
	getProjectSettings(id: number): Promise<IProjectSettings>;
	updateProject(projectSettings: IProjectSettings): Promise<IProjectSettings>;
	createProject(projectSettings: IProjectSettings): Promise<IProjectSettings>;
	refreshFeaturesForProject(id: number): Promise<void>;
	refreshFeaturesForAllProjects(): Promise<void>;
	refreshForecastsForProject(id: number): Promise<void>;
	validateProjectSettings(projectSettings: IProjectSettings): Promise<boolean>;
}

export class ProjectService extends BaseApiService implements IProjectService {
	async getProjects(): Promise<Project[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IProject[]>("/portfolios");
			return response.data.map(BaseApiService.deserializeProject);
		});
	}

	async getProject(id: number): Promise<Project | null> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IProject>(`/portfolios/${id}`);
			return BaseApiService.deserializeProject(response.data);
		});
	}

	async deleteProject(id: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete<void>(`/portfolios/${id}`);
		});
	}

	async getProjectSettings(id: number): Promise<IProjectSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IProjectSettings>(
				`/portfolios/${id}/settings`,
			);
			return response.data;
		});
	}

	async updateProject(
		projectSettings: IProjectSettings,
	): Promise<IProjectSettings> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.put<IProjectSettings>(
				`/portfolios/${projectSettings.id}`,
				projectSettings,
			);
			return response.data;
		});
	}

	async createProject(
		projectSettings: IProjectSettings,
	): Promise<IProjectSettings> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.post<IProjectSettings>(
				"/portfolios",
				projectSettings,
			);
			return response.data;
		});
	}

	async refreshFeaturesForProject(id: number): Promise<void> {
		await this.withErrorHandling(async () => {
			await this.apiService.post<IProject>(`/portfolios/refresh/${id}`);
		});
	}

	async refreshFeaturesForAllProjects(): Promise<void> {
		await this.withErrorHandling(async () => {
			await this.apiService.post(`/portfolios/refresh-all`);
		});
	}

	async refreshForecastsForProject(id: number): Promise<void> {
		await this.withErrorHandling(async () => {
			await this.apiService.post<IProject>(`/forecast/update/${id}`);
		});
	}

	async validateProjectSettings(
		projectSettings: IProjectSettings,
	): Promise<boolean> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<boolean>(
				"/portfolios/validate",
				projectSettings,
			);
			return response.data;
		});
	}
}
