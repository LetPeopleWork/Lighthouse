import axios, { AxiosInstance } from 'axios';
import { IProject, Project } from '../../models/Project';
import { IApiService } from './IApiService';
import { ITeam, Team } from '../../models/Team';
import { Feature, IFeature } from '../../models/Feature';
import { IWhenForecast, WhenForecast } from '../../models/WhenForecast';

export class ApiService implements IApiService {
    private apiService!: AxiosInstance;

    constructor(baseUrl: string = "/api") {
        this.apiService = axios.create({
            baseURL: baseUrl
        });
    }

    async getVersion(): Promise<string> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<string>('/version');
            return response.data;
        });
    }

    async getProjects(): Promise<Project[]> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<IProject[]>('/projects');
            return this.deserializeProjects(response.data);
        });
    }

    async getTeams(): Promise<Team[]> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<ITeam[]>('/teams');
            const teams = response.data.map((item: ITeam) => {
                const projects = this.deserializeProjects(item.projects);
                const features: Feature[] = this.deserializeFeatures(item.features);
                return new Team(item.name, item.id, projects, features);
            });
            return teams;
        });
    }

    async deleteTeam(id: number): Promise<void> {
        await this.withErrorHandling(async () => {
            await this.apiService.delete<void>(`/teams/${id}`);
        });
    }

    async deleteProject(id: number): Promise<void> {
        await this.withErrorHandling(async () => {
            await this.apiService.delete<void>(`/projects/${id}`);
        });
    }

    private deserializeFeatures(featureData: IFeature[]): Feature[] {
        return featureData.map((feature: IFeature) => {
            const forecasts: WhenForecast[] = feature.forecasts.map((forecast: IWhenForecast) => {
                return new WhenForecast(forecast.probability, new Date(forecast.expectedDate));
            });
            return new Feature(feature.name, feature.id, new Date(feature.lastUpdated), feature.remainingWork, forecasts);
        });
    }

    private deserializeProjects(projectData: IProject[]): Project[] {
        return projectData.map((item: IProject) => {
            const features: Feature[] = this.deserializeFeatures(item.features);
            const teams: Team[] = item.involvedTeams.map((team: ITeam) => {
                return new Team(team.name, team.id, [], []);
            });
            return new Project(item.name, item.id, teams, features, new Date(item.lastUpdated));
        });
    }

    private async withErrorHandling<T>(asyncFunction: () => Promise<T>): Promise<T> {
        try {
            return await asyncFunction();
        } catch (error) {
            console.error('Error during async function execution:', error);
            throw error;
        }
    }
}