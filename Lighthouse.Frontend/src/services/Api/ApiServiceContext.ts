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
import { DeliveryService, type IDeliveryService } from "./DeliveryService";
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
import { type IPortfolioService, PortfolioService } from "./PortfolioService";
import { ProjectMetricsService } from "./ProjectMetricsService";
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
import { type IWizardService, WizardService } from "./WizardService";
import {
	type IWorkTrackingSystemService,
	WorkTrackingSystemService,
} from "./WorkTrackingSystemService";

export interface IApiServiceContext {
	forecastService: IForecastService;
	logService: ILogService;
	portfolioService: IPortfolioService;
	settingsService: ISettingsService;
	teamService: ITeamService;
	teamMetricsService: ITeamMetricsService;
	portfolioMetricsService: IProjectMetricsService;
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
	deliveryService: IDeliveryService;
	wizardService: IWizardService;
}

const initializeUpdateSubscriptionService = async () => {
	await defaultServices.updateSubscriptionService.initialize();
};

const defaultServices: IApiServiceContext = {
	forecastService: new ForecastService(),
	logService: new LogService(),
	portfolioService: new PortfolioService(),
	settingsService: new SettingsService(),
	teamService: new TeamService(),
	teamMetricsService: new TeamMetricsService(),
	portfolioMetricsService: new ProjectMetricsService(),
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
	deliveryService: new DeliveryService(),
	wizardService: new WizardService(),
};

export function getApiServices(): IApiServiceContext {
	initializeUpdateSubscriptionService();

	return defaultServices;
}

export const ApiServiceContext =
	createContext<IApiServiceContext>(defaultServices);
