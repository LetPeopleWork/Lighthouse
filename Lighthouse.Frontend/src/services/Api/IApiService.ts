import { Project } from "../../models/Project";
import { Team } from "../../models/Team";
import { Throughput } from "../../models/Forecasts/Throughput";
import { ManualForecast } from "../../models/Forecasts/ManualForecast";

export interface IApiService {
    deleteProject(id: number): Promise<void>;

    deleteTeam(id: number): Promise<void>;

    getTeams(): Promise<Team[]>;

    getTeam(id: number): Promise<Team | null>;

    getProjects(): Promise<Project[]>;

    getVersion(): Promise<string>;

    updateThroughput(teamId: number): Promise<void>;

    getThroughput(teamId: number) : Promise<Throughput>;

    updateForecast(teamId: number): Promise<void>;

    runManualForecast(teamId: number, remainingItems: number, targetDate: Date) : Promise<ManualForecast>;
}