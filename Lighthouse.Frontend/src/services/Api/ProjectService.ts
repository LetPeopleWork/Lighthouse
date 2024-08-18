import { BaseApiService } from './BaseApiService';
import { IProject, Project } from '../../models/Project/Project';
import { IProjectSettings } from '../../models/Project/ProjectSettings';

export interface IProjectService {
    getProjects(): Promise<Project[]>;
    deleteProject(id: number): Promise<void>;
    getProject(id: number): Promise<Project | null>;
    getProjectSettings(id: number): Promise<IProjectSettings>;
    updateProject(projectSettings: IProjectSettings): Promise<IProjectSettings>;
    createProject(projectSettings: IProjectSettings): Promise<IProjectSettings>;
    refreshFeaturesForProject(id: number): Promise<Project | null>;
    refreshForecastsForProject(id: number): Promise<Project | null>;
}

export class ProjectService extends BaseApiService implements IProjectService {
    async getProjects(): Promise<Project[]> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<IProject[]>('/projects');
            return response.data.map(this.deserializeProject);
        });
    }

    async getProject(id: number): Promise<Project | null> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<IProject>(`/projects/${id}`);
            return this.deserializeProject(response.data);
        });
    }

    async deleteProject(id: number): Promise<void> {
        return this.withErrorHandling(async () => {
            await this.apiService.delete<void>(`/projects/${id}`);
        });
    }

    async getProjectSettings(id: number): Promise<IProjectSettings> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<IProjectSettings>(`/projects/${id}/settings`);
            return this.deserializeProjectSettings(response.data);
        });
    }

    async updateProject(projectSettings: IProjectSettings): Promise<IProjectSettings> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.put<IProjectSettings>(`/projects/${projectSettings.id}`, projectSettings);
            return this.deserializeProjectSettings(response.data);
        });
    }

    async createProject(projectSettings: IProjectSettings): Promise<IProjectSettings> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.post<IProjectSettings>(`/projects`, projectSettings);
            return this.deserializeProjectSettings(response.data);
        });
    }


    async refreshFeaturesForProject(id: number): Promise<Project | null> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.post<IProject>(`/projects/refresh/${id}`);

            return this.deserializeProject(response.data);
        });
    }

    async refreshForecastsForProject(id: number): Promise<Project | null> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.post<IProject>(`/forecast/update/${id}`);

            return this.deserializeProject(response.data);
        });
    }
}