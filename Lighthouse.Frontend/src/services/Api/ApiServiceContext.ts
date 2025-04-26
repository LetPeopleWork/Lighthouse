import { createContext } from "react";
import {
	type IUpdateSubscriptionService,
	UpdateSubscriptionService,
} from "../UpdateSubscriptionService";
import { DemoApiService } from "./DemoApiService";
import { ForecastService, type IForecastService } from "./ForecastService";
import { type ILogService, LogService } from "./LogService";
import {
	type IOptionalFeatureService,
	OptionalFeatureService,
} from "./OptionalFeatureService";
import {
	type IProjectMetricsService,
	ProjectMetricsService,
} from "./ProjectMetricsService";
import { type IProjectService, ProjectService } from "./ProjectService";
import { type ISettingsService, SettingsService } from "./SettingsService";
import {
	type ITeamMetricsService,
	TeamMetricsService,
} from "./TeamMetricsService";
import { type ITeamService, TeamService } from "./TeamService";
import { type IVersionService, VersionService } from "./VersionService";
import {
	type IWorkTrackingSystemService,
	WorkTrackingSystemService,
} from "./WorkTrackingSystemService";

export interface IApiServiceContext {
	forecastService: IForecastService;
	logService: ILogService;
	projectService: IProjectService;
	settingsService: ISettingsService;
	teamService: ITeamService;
	teamMetricsService: ITeamMetricsService;
	projectMetricsService: IProjectMetricsService;
	versionService: IVersionService;
	workTrackingSystemService: IWorkTrackingSystemService;
	optionalFeatureService: IOptionalFeatureService;
	updateSubscriptionService: IUpdateSubscriptionService;
}

const initializeUpdateSubscriptionService = async () => {
	await defaultServices.updateSubscriptionService.initialize();
};

const defaultServices: IApiServiceContext = {
	forecastService: new ForecastService(),
	logService: new LogService(),
	projectService: new ProjectService(),
	settingsService: new SettingsService(),
	teamService: new TeamService(),
	teamMetricsService: new TeamMetricsService(),
	projectMetricsService: new ProjectMetricsService(),
	versionService: new VersionService(),
	workTrackingSystemService: new WorkTrackingSystemService(),
	optionalFeatureService: new OptionalFeatureService(),
	updateSubscriptionService: new UpdateSubscriptionService(),
};

const useDelay: boolean = import.meta.env.VITE_API_SERVICE_DELAY === "TRUE";
const demoApiService = new DemoApiService(useDelay);

const demoServices: IApiServiceContext = {
	forecastService: demoApiService,
	logService: demoApiService,
	projectService: demoApiService,
	settingsService: demoApiService,
	teamService: demoApiService,
	teamMetricsService: demoApiService,
	projectMetricsService: demoApiService,
	versionService: demoApiService,
	workTrackingSystemService: demoApiService,
	optionalFeatureService: demoApiService,
	updateSubscriptionService: demoApiService,
};

export function getApiServices(): IApiServiceContext {
	const isDemoMode = import.meta.env.VITE_API_SERVICE_TYPE === "DEMO";
	if (isDemoMode) {
		return demoServices;
	}

	initializeUpdateSubscriptionService();

	return defaultServices;
}

export const ApiServiceContext =
	createContext<IApiServiceContext>(defaultServices);
