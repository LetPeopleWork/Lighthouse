import { Project } from "../../models/Project";

export interface IApiService {
    getProjectOverviewData(): Promise<Project[]>;

    getVersion() : Promise<string>;
}