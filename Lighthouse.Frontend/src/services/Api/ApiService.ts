import axios, { AxiosInstance } from 'axios';
import { IProject, Project } from '../../models/Project/Project';
import { IApiService } from './IApiService';
import { ITeam, Team } from '../../models/Team/Team';
import { Feature, IFeature } from '../../models/Feature';
import { IWhenForecast, WhenForecast } from '../../models/Forecasts/WhenForecast';
import { Throughput } from '../../models/Forecasts/Throughput';
import { IManualForecast, ManualForecast } from '../../models/Forecasts/ManualForecast';
import { HowManyForecast, IHowManyForecast } from '../../models/Forecasts/HowManyForecast';
import { IMilestone, Milestone } from '../../models/Project/Milestone';
import { IWorkTrackingSystemConnection, WorkTrackingSystemConnection } from '../../models/WorkTracking/WorkTrackingSystemConnection';
import { IWorkTrackingSystemOption, WorkTrackingSystemOption } from '../../models/WorkTracking/WorkTrackingSystemOption';
import { ITeamSettings, TeamSettings } from '../../models/Team/TeamSettings';
import { IProjectSettings, ProjectSettings } from '../../models/Project/ProjectSettings';
import { IRefreshSettings } from '../../models/AppSettings/RefreshSettings';
import { IRelease, Release } from '../../models/Release/Release';
import { IReleaseAsset, ReleaseAsset } from '../../models/Release/ReleaseAsset';

export class ApiService implements IApiService {
    private apiService!: AxiosInstance;

    constructor(baseUrl: string = "/api") {
        this.apiService = axios.create({
            baseURL: baseUrl
        });
    }

    async getCurrentVersion(): Promise<string> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<string>('/version/current');
            return response.data;
        });
    }

    async isUpdateAvailable(): Promise<boolean> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<boolean>('/version/hasupdate');
            return response.data;
        });
    }

    async getLatestRelease(): Promise<IRelease> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<IRelease>(`/version/latest`);
            return this.deserializeRelease(response.data);
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
            const teams = this.deserializeTeams(response.data);
            return teams;
        });
    }

    async getTeam(id: number): Promise<Team | null> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<ITeam>(`/teams/${id}`);
            return this.deserializeTeam(response.data);
        });
    }

    async deleteTeam(id: number): Promise<void> {
        await this.withErrorHandling(async () => {
            await this.apiService.delete<void>(`/teams/${id}`);
        });
    }

    async getTeamSettings(id: number): Promise<ITeamSettings> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<ITeamSettings>(`/teams/${id}/settings`);
            return this.deserializeTeamSettings(response.data);
        });
    }

    async updateTeam(teamSettings: ITeamSettings): Promise<ITeamSettings> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.put<ITeamSettings>(`/teams/${teamSettings.id}`, teamSettings);
            return this.deserializeTeamSettings(response.data);
        });
    }

    async createTeam(teamSettings: ITeamSettings): Promise<ITeamSettings> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.post<ITeamSettings>(`/teams`, teamSettings);
            return this.deserializeTeamSettings(response.data);
        });
    }

    async deleteProject(id: number): Promise<void> {
        await this.withErrorHandling(async () => {
            await this.apiService.delete<void>(`/projects/${id}`);
        });
    }

    async getProject(id: number): Promise<Project | null> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.get<IProject>(`/projects/${id}`);

            return this.deserializeProject(response.data);
        });
    }

    async getProjectSettings(id: number): Promise<IProjectSettings> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<IProjectSettings>(`/projects/${id}/settings`);
            return this.deserializeProjectSettings(response.data);
        });
    }

    async updateProject(projectSettings: IProjectSettings): Promise<IProjectSettings> {
        return await this.withErrorHandling(async () => {
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

    async updateThroughput(teamId: number): Promise<void> {
        await this.withErrorHandling(async () => {
            await this.apiService.post<void>(`/throughput/${teamId}`);
        });
    }

    async getThroughput(teamId: number): Promise<Throughput> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.get<number[]>(`/throughput/${teamId}`);

            return new Throughput(response.data);
        });
    }

    async updateForecast(teamId: number): Promise<void> {
        await this.withErrorHandling(async () => {
            await this.apiService.post<void>(`/forecast/update/${teamId}`);
        });
    }

    async runManualForecast(teamId: number, remainingItems: number, targetDate: Date): Promise<ManualForecast> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.post<IManualForecast>(`/forecast/manual/${teamId}`, {
                remainingItems: remainingItems,
                targetDate: targetDate
            });

            return this.deserializeManualForecast(response.data);
        });
    }

    async getWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.get<IWorkTrackingSystemConnection[]>(`/worktrackingsystemconnections/supported`);

            return response.data.map((connection) => this.deserializeWorkTrackingSystemConnection(connection))
        });
    }

    async getConfiguredWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.get<IWorkTrackingSystemConnection[]>(`/worktrackingsystemconnections`);

            return response.data.map((connection) => this.deserializeWorkTrackingSystemConnection(connection))
        });
    }

    async addNewWorkTrackingSystemConnection(newWorkTrackingSystemConnection: IWorkTrackingSystemConnection): Promise<IWorkTrackingSystemConnection> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.post<IWorkTrackingSystemConnection>(`/worktrackingsystemconnections`, newWorkTrackingSystemConnection);

            return this.deserializeWorkTrackingSystemConnection(response.data);
        });
    }

    async updateWorkTrackingSystemConnection(modifiedConnection: IWorkTrackingSystemConnection): Promise<IWorkTrackingSystemConnection> {
        return await this.withErrorHandling(async () => {
            const response = await this.apiService.put<IWorkTrackingSystemConnection>(`/worktrackingsystemconnections/${modifiedConnection.id}`, modifiedConnection);

            return this.deserializeWorkTrackingSystemConnection(response.data);
        });
    }

    async deleteWorkTrackingSystemConnection(connectionId: number): Promise<void> {
        this.withErrorHandling(async () => {
            await this.apiService.delete(`/worktrackingsystemconnections/${connectionId}`);
        });
    }

    async validateWorkTrackingSystemConnection(connection: IWorkTrackingSystemConnection): Promise<boolean> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.post<boolean>(`/worktrackingsystemconnections/validate`, connection);

            return response.data;
        });
    }

    async getLogLevel(): Promise<string> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<string>(`/logs/level`);

            return response.data;
        });
    }

    async getSupportedLogLevels(): Promise<string[]> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<string[]>(`/logs/level/supported`);

            return response.data;
        });
    }

    async setLogLevel(logLevel: string): Promise<void> {
        await this.withErrorHandling(async () => {
            await this.apiService.post<void>('/logs/level', { level: logLevel });
        });
    }

    async getLogs(): Promise<string> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<string>(`/logs`);

            return response.data;
        });
    }

    async getRefreshSettings(settingName: string): Promise<IRefreshSettings> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<IRefreshSettings>(`/appsettings/${settingName}Refresh`);

            return response.data;
        });
    }

    async updateRefreshSettings(settingName: string, refreshSettings: IRefreshSettings): Promise<void> {
        this.withErrorHandling(async () => {
            await this.apiService.put<IRefreshSettings>(`/appsettings/${settingName}Refresh`, refreshSettings);
        });
    }

    async getDefaultTeamSettings(): Promise<ITeamSettings> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<ITeamSettings>(`/appsettings/defaultteamsettings`);

            return this.deserializeTeamSettings(response.data);
        });
    }

    async updateDefaultTeamSettings(teamSettings: ITeamSettings): Promise<void> {
        this.withErrorHandling(async () => {
            await this.apiService.put<ITeamSettings>(`/appsettings/defaultteamsettings`, teamSettings);
        });
    }



    async getDefaultProjectSettings(): Promise<IProjectSettings> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<IProjectSettings>(`/appsettings/defaultprojectsettings`);

            return this.deserializeProjectSettings(response.data);
        });
    }

    async updateDefaultProjectSettings(projecSettings: IProjectSettings): Promise<void> {
        this.withErrorHandling(async () => {
            await this.apiService.put<IProjectSettings>(`/appsettings/defaultprojectsettings`, projecSettings);
        });
    }

    private deserializeTeams(teams: ITeam[]) {
        return teams.map((item: ITeam) => {
            return this.deserializeTeam(item);
        });
    }

    private deserializeTeam(item: ITeam) {
        const projects = this.deserializeProjects(item.projects);
        const features: Feature[] = this.deserializeFeatures(item.features);
        return new Team(item.name, item.id, projects, features, item.featureWip);
    }

    private deserializeTeamSettings(item: ITeamSettings): TeamSettings {
        return new TeamSettings(
            item.id,
            item.name,
            item.throughputHistory,
            item.featureWIP,
            item.workItemQuery,
            item.workItemTypes,
            item.workTrackingSystemConnectionId,
            item.relationCustomField
        );
    }

    private deserializeProjectSettings(item: IProjectSettings): ProjectSettings {
        const milestones = item.milestones.map((milestone: IMilestone) => {
            return new Milestone(milestone.id, milestone.name, new Date(milestone.date))
        })

        return new ProjectSettings(item.id, item.name, item.workItemTypes, milestones, item.workItemQuery, item.unparentedItemsQuery, item.defaultAmountOfWorkItemsPerFeature, item.workTrackingSystemConnectionId, item.sizeEstimateField);
    }

    private deserializeFeatures(featureData: IFeature[]): Feature[] {
        return featureData.map((feature: IFeature) => {
            const forecasts: WhenForecast[] = feature.forecasts.map((forecast: IWhenForecast) => {
                return new WhenForecast(forecast.probability, new Date(forecast.expectedDate));
            });
            return new Feature(feature.name, feature.id, feature.url, new Date(feature.lastUpdated), feature.projects, feature.remainingWork, feature.milestoneLikelihood, forecasts);
        });
    }

    private deserializeRelease(release: IRelease): Release {
        const releaseAssets: IReleaseAsset[] = release.assets.map((asset: IReleaseAsset) => {
            return new ReleaseAsset(asset.name, asset.link);
        })

        return new Release(release.name, release.link, release.highlights, release.version, releaseAssets);
    }

    private deserializeProjects(projectData: IProject[]): Project[] {
        return projectData.map((item: IProject) => {
            return this.deserializeProject(item);
        });
    }

    private deserializeProject(item: IProject) {
        const features: Feature[] = this.deserializeFeatures(item.features);
        const teams: Team[] = item.involvedTeams.map((team: ITeam) => {
            return new Team(team.name, team.id, [], [], team.featureWip);
        });

        const milestones: Milestone[] = item.milestones.map((milestone: IMilestone) => {
            return new Milestone(milestone.id, milestone.name, new Date(milestone.date))
        })

        return new Project(item.name, item.id, teams, features, milestones, new Date(item.lastUpdated));
    }

    private deserializeWorkTrackingSystemConnectionOption(workTrackingSystemConnectionOption: IWorkTrackingSystemOption): WorkTrackingSystemOption {
        return new WorkTrackingSystemOption(workTrackingSystemConnectionOption.key, workTrackingSystemConnectionOption.value, workTrackingSystemConnectionOption.isSecret);
    }

    private deserializeWorkTrackingSystemConnection(workTrackingSystemConnection: IWorkTrackingSystemConnection) {
        const workTrackingSystemOptions = workTrackingSystemConnection.options.map((option: IWorkTrackingSystemOption) => {
            return this.deserializeWorkTrackingSystemConnectionOption(option);
        })

        return new WorkTrackingSystemConnection(workTrackingSystemConnection.id, workTrackingSystemConnection.name, workTrackingSystemConnection.workTrackingSystem, workTrackingSystemOptions);
    }

    private deserializeManualForecast(manualForecastData: IManualForecast) {
        const whenForecasts = manualForecastData.whenForecasts.map((forecast: IWhenForecast) => {
            return new WhenForecast(forecast.probability, new Date(forecast.expectedDate));
        })

        const howManyForecasts = manualForecastData.howManyForecasts.map((forecast: IHowManyForecast) => {
            return new HowManyForecast(forecast.probability, forecast.expectedItems);
        })

        return new ManualForecast(manualForecastData.remainingItems, new Date(manualForecastData.targetDate), whenForecasts, howManyForecasts, manualForecastData.likelihood);
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