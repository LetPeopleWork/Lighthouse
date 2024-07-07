import { Project } from "../../models/Project";
import { Team } from "../../models/Team";

export interface IApiService {
    deleteProject(id: number): Promise<void>;
    
    deleteTeam(id: number): Promise<void>;

    getTeams(): Promise<Team[]>;

    getProjectOverviewData(): Promise<Project[]>;

    getVersion() : Promise<string>;
}