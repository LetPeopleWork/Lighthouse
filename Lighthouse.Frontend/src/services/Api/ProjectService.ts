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
    refreshFeaturesForProject(id: number): Promise<void>;
    refreshForecastsForProject(id: number): Promise<void>;
    validateProjectSettings(projectSettings: IProjectSettings): Promise<boolean>;
}

export class ProjectService extends BaseApiService implements IProjectService {
    async getProjects(): Promise<Project[]> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<IProject[]>('/projects');
            return response.data.map(BaseApiService.deserializeProject);
        });
    }

    async getProject(id: number): Promise<Project | null> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<IProject>(`/projects/${id}`);
            return BaseApiService.deserializeProject(response.data);
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


    async refreshFeaturesForProject(id: number): Promise<void> {
        await this.withErrorHandling(async () => {
            await this.apiService.post<IProject>(`/projects/refresh/${id}`);
        });
    }

    async refreshForecastsForProject(id: number): Promise<void> {
        await this.withErrorHandling(async () => {
            await this.apiService.post<IProject>(`/forecast/update/${id}`);
        });
    }

    async validateProjectSettings(projectSettings: IProjectSettings): Promise<boolean> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.post<boolean>(`/projects/validate`, projectSettings);
            return response.data;
        });
    }
}