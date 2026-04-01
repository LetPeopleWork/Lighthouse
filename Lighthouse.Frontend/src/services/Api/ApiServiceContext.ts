import { createContext } from "react";
import type { IDemoDataService } from "../../models/DemoData/IDemoData";
import {
	type IUpdateSubscriptionService,
	UpdateSubscriptionService,
} from "../UpdateSubscriptionService";
import { AuthService, type IAuthService } from "./AuthService";
import {
	BlackoutPeriodService,
	type IBlackoutPeriodService,
} from "./BlackoutPeriodService";
import {
	DatabaseManagementService,
	type IDatabaseManagementService,
} from "./DatabaseManagementService";
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
import {
	type ISystemInfoService,
	SystemInfoService,
} from "./SystemInfoService";
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
	authService: IAuthService;
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
	featureService: IFeatureService;
	terminologyService: ITerminologyService;
	licensingService: ILicensingService;
	demoDataService: IDemoDataService;
	deliveryService: IDeliveryService;
	wizardService: IWizardService;
	systemInfoService: ISystemInfoService;
	blackoutPeriodService: IBlackoutPeriodService;
	databaseManagementService: IDatabaseManagementService;
}

const defaultServices: IApiServiceContext = {
	authService: new AuthService(),
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
	featureService: new FeatureService(),
	terminologyService: new TerminologyService(),
	licensingService: new LicensingService(),
	demoDataService: new DemoDataService(),
	deliveryService: new DeliveryService(),
	wizardService: new WizardService(),
	systemInfoService: new SystemInfoService(),
	blackoutPeriodService: new BlackoutPeriodService(),
	databaseManagementService: new DatabaseManagementService(),
};

export function getApiServices(): IApiServiceContext {
	return defaultServices;
}

export const ApiServiceContext =
	createContext<IApiServiceContext>(defaultServices);
