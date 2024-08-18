import { createContext } from "react";
import { ForecastService, IForecastService } from "./ForecastService";
import { ILogService, LogService } from "./LogService";
import { IProjectService, ProjectService } from "./ProjectService";
import { ISettingsService, SettingsService } from "./SettingsService";
import { ITeamService, TeamService } from "./TeamService";
import { IVersionService, VersionService } from "./VersionService";
import { IWorkTrackingSystemService, WorkTrackingSystemService } from "./WorkTrackingSystemService";
import { DemoApiService } from "./DemoApiService";

export interface IApiServiceContext {
    forecastService: IForecastService;
    logService: ILogService;
    projectService: IProjectService;
    settingsService: ISettingsService;
    teamService: ITeamService;
    versionService: IVersionService;
    workTrackingSystemService: IWorkTrackingSystemService;
}

const defaultServices: IApiServiceContext = {
    forecastService: new ForecastService(),
    logService: new LogService(),
    projectService: new ProjectService(),
    settingsService: new SettingsService(),
    teamService: new TeamService(),
    versionService: new VersionService(),
    workTrackingSystemService: new WorkTrackingSystemService(),
};

const useDelay : boolean = import.meta.env.VITE_API_SERVICE_DELAY === "TRUE";
const demoApiService = new DemoApiService(useDelay);

const demoServices: IApiServiceContext = {
    forecastService: demoApiService,
    logService: demoApiService,
    projectService: demoApiService,
    settingsService: demoApiService,
    teamService: demoApiService,
    versionService: demoApiService,
    workTrackingSystemService: demoApiService,
}

export function getApiServices(isDemo: boolean): IApiServiceContext {
    if (isDemo) {
        return demoServices;
    }

    return defaultServices;
}

export const ApiServiceContext = createContext<IApiServiceContext | null>(defaultServices);
