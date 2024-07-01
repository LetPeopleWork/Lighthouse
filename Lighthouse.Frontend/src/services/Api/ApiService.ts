import axios, { AxiosInstance } from 'axios';
import { Project } from '../../models/Project';
import { IApiService } from './IApiService';

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

    private async withErrorHandling<T>(asyncFunction: () => Promise<T>): Promise<T> {
        try {
            return await asyncFunction();
        } catch (error) {
            console.error('Error during async function execution:', error);
            throw error; 
        }
    }
}
