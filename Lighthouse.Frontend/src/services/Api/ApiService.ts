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

    async getProjectOverviewData(): Promise<Project[]> {
        try {
            const response = await this.apiService.get<Project[]>('/projects/overview');
            return response.data;
        } catch (error) {
            console.error('Error fetching Project Overview Data:', error);
            throw error; 
        }
    }
}
