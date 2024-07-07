import axios, { AxiosInstance } from 'axios';
import { Project } from '../../models/Project';
import { IApiService } from './IApiService';
import { Team } from '../../models/Team';

export class ApiService implements IApiService {    
    private apiService!: AxiosInstance;

    constructor(baseUrl: string = "/api"){
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

    async getProjectOverviewData(): Promise<Project[]> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<Project[]>('/projects/overview');
            return response.data;
        });
    }
    
    async getTeams(): Promise<Team[]> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<Team[]>('/teams');
            return response.data;
        });
    }
    
    async deleteTeam(id: number): Promise<void> {
        await this.withErrorHandling(async () => {
            const response = await this.apiService.delete<void>(`/teams/${id}`);
            return response.data;
        });
    }

    async deleteProject(id: number): Promise<void> {
        await this.withErrorHandling(async () => {
            const response = await this.apiService.delete<void>(`/projects/${id}`);
            return response.data;
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
