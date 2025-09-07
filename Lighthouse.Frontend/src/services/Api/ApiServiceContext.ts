import { createContext } from "react";
import type { IDemoDataService } from "../../models/DemoData/IDemoData";
import {
	type IUpdateSubscriptionService,
	UpdateSubscriptionService,
} from "../UpdateSubscriptionService";
import {
	ConfigurationService,
	type IConfigurationService,
} from "./ConfigurationService";
import {
	DemoApiService,
	DemoProjectMetricsService,
	DemoTeamMetricsService,
} from "./DemoApiService";
import { DemoDataService } from "./DemoDataService";
import { FeatureService, type IFeatureService } from "./FeatureService";
import { ForecastService, type IForecastService } from "./ForecastService";
import { type ILicensingService, LicensingService } from "./LicensingService";
import { type ILogService, LogService } from "./LogService";
import type {
	IProjectMetricsService,
	ITeamMetricsService,
} from "./MetricsService";
import {
	type IOptionalFeatureService,
	OptionalFeatureService,
} from "./OptionalFeatureService";
import { ProjectMetricsService } from "./ProjectMetricsService";
import { type IProjectService, ProjectService } from "./ProjectService";
import { type ISettingsService, SettingsService } from "./SettingsService";
import {
	type ISuggestionService,
	SuggestionService,
} from "./SuggestionService";
import { TeamMetricsService } from "./TeamMetricsService";
import { type ITeamService, TeamService } from "./TeamService";
import {
	type ITerminologyService,
	TerminologyService,
} from "./TerminologyService";
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
	suggestionService: ISuggestionService;
	configurationService: IConfigurationService;
	featureService: IFeatureService;
	terminologyService: ITerminologyService;
	licensingService: ILicensingService;
	demoDataService: IDemoDataService;
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
	suggestionService: new SuggestionService(),
	configurationService: new ConfigurationService(),
	featureService: new FeatureService(),
	terminologyService: new TerminologyService(),
	licensingService: new LicensingService(),
	demoDataService: new DemoDataService(),
};

const useDelay: boolean = import.meta.env.VITE_API_SERVICE_DELAY === "TRUE";
const demoApiService = new DemoApiService(useDelay);
const demoTeamMetricsService = new DemoTeamMetricsService();
const demoProjectMetricsService = new DemoProjectMetricsService();

const demoServices: IApiServiceContext = {
	forecastService: demoApiService,
	logService: demoApiService,
	projectService: demoApiService,
	settingsService: demoApiService,
	teamService: demoApiService,
	teamMetricsService: demoTeamMetricsService,
	projectMetricsService: demoProjectMetricsService,
	versionService: demoApiService,
	workTrackingSystemService: demoApiService,
	optionalFeatureService: demoApiService,
	updateSubscriptionService: demoApiService,
	suggestionService: demoApiService,
	configurationService: demoApiService,
	featureService: demoApiService,
	terminologyService: demoApiService,
	licensingService: demoApiService,
	demoDataService: demoApiService,
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
