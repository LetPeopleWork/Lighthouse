import { Project } from "../../models/Project/Project";
import { Team } from "../../models/Team/Team";
import { Throughput } from "../../models/Forecasts/Throughput";
import { ManualForecast } from "../../models/Forecasts/ManualForecast";
import { IWorkTrackingSystemConnection } from "../../models/WorkTracking/WorkTrackingSystemConnection";
import { ITeamSettings } from "../../models/Team/TeamSettings";
import { IProjectSettings } from "../../models/Project/ProjectSettings";
import { IRefreshSettings } from "../../models/AppSettings/RefreshSettings";

export interface IApiService {
    deleteTeam(id: number): Promise<void>;

    getTeams(): Promise<Team[]>;

    getTeam(id: number): Promise<Team | null>;

    getTeamSettings(id: number): Promise<ITeamSettings>;

    updateTeam(teamSettings: ITeamSettings): Promise<ITeamSettings>;

    createTeam(teamSettings: ITeamSettings): Promise<ITeamSettings>;

    getProjects(): Promise<Project[]>;

    getProject(id: number): Promise<Project | null>;

    refreshFeaturesForProject(id: number): Promise<Project | null>;

    refreshForecastsForProject(id: number): Promise<Project | null>;

    deleteProject(id: number): Promise<void>;

    getProjectSettings(id: number): Promise<IProjectSettings>;

    updateProject(projectSettings: IProjectSettings): Promise<IProjectSettings>;

    createProject(projectSettings: IProjectSettings): Promise<IProjectSettings>;

    updateThroughput(teamId: number): Promise<void>;

    getThroughput(teamId: number): Promise<Throughput>;

    updateForecast(teamId: number): Promise<void>;

    runManualForecast(teamId: number, remainingItems: number, targetDate: Date): Promise<ManualForecast>;

    getWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]>;

    getConfiguredWorkTrackingSystems(): Promise<IWorkTrackingSystemConnection[]>;

    addNewWorkTrackingSystemConnection(newWorkTrackingSystemConnection: IWorkTrackingSystemConnection): Promise<IWorkTrackingSystemConnection>;

    updateWorkTrackingSystemConnection(modifiedConnection: IWorkTrackingSystemConnection): Promise<IWorkTrackingSystemConnection>;

    deleteWorkTrackingSystemConnection(connectionId: number): Promise<void>;

    validateWorkTrackingSystemConnection(connection: IWorkTrackingSystemConnection): Promise<boolean>;

    getRefreshSettings(settingName: string): Promise<IRefreshSettings>;

    updateRefreshSettings(settingName: string, refreshSettings: IRefreshSettings): Promise<void>;

    getDefaultTeamSettings(): Promise<ITeamSettings>;

    updateDefaultTeamSettings(teamSettings: ITeamSettings) : Promise<void>;

    getDefaultProjectSettings(): Promise<IProjectSettings>;

    updateDefaultProjectSettings(projecSettings: IProjectSettings) : Promise<void>;
}