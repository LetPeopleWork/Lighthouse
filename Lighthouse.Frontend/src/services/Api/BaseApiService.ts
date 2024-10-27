import axios, { AxiosInstance } from 'axios';
import { Feature, IFeature } from '../../models/Feature';
import { IWhenForecast, WhenForecast } from '../../models/Forecasts/WhenForecast';
import { IProject, Project } from '../../models/Project/Project';
import { ITeam, Team } from '../../models/Team/Team';
import { IMilestone, Milestone } from '../../models/Project/Milestone';
import { IProjectSettings } from '../../models/Project/ProjectSettings';

export class BaseApiService {
    protected apiService: AxiosInstance;

    constructor() {
        let baseUrl = "/api";
        if (import.meta.env.VITE_API_BASE_URL !== undefined) {
            baseUrl = import.meta.env.VITE_API_BASE_URL
        }

        this.apiService = axios.create({
            baseURL: baseUrl
        });
    }

    protected async withErrorHandling<T>(asyncFunction: () => Promise<T>): Promise<T> {
        try {
            return await asyncFunction();
        } catch (error) {
            console.error('Error during async function execution:', error);
            throw error;
        }
    }

    protected static deserializeTeam(item: ITeam) {
        if (item == null){
            return null;
        }
        
        const projects = item.projects.map(project => BaseApiService.deserializeProject(project)) ?? [];
        const features: Feature[] = BaseApiService.deserializeFeatures(item.features ?? []);
        return new Team(item.name, item.id, projects, features, item.featureWip, new Date(item.lastUpdated));
    }

    protected static deserializeProject(item: IProject): Project {
        const features = BaseApiService.deserializeFeatures(item.features);
        const teams = item.involvedTeams.map(team => new Team(team.name, team.id, [], [], team.featureWip, new Date(item.lastUpdated)));
        const milestones = item.milestones.map(milestone => new Milestone(milestone.id, milestone.name, new Date(milestone.date)));
        return new Project(item.name, item.id, teams, features, milestones, new Date(item.lastUpdated));
    }

    protected static deserializeFeatures(featureData: IFeature[]): Feature[] {
        return featureData.map((feature: IFeature) => {
            const forecasts: WhenForecast[] = feature.forecasts.map((forecast: IWhenForecast) => {
                return new WhenForecast(forecast.probability, new Date(forecast.expectedDate));
            });
            return new Feature(feature.name, feature.id, feature.url, new Date(feature.lastUpdated), feature.isUsingDefaultFeatureSize, feature.projects, feature.remainingWork, feature.totalWork, feature.milestoneLikelihood, forecasts);
        });
    }

    protected deserializeProjectSettings(item: IProjectSettings): IProjectSettings {
        const milestones = item.milestones.map((milestone: IMilestone) => {
            return new Milestone(milestone.id, milestone.name, new Date(milestone.date))
        })

        item.milestones = milestones;

        return item;
    }
}