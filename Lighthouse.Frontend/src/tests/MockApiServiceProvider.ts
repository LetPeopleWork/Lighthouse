import { vi } from "vitest";
import type { IApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IConfigurationService } from "../services/Api/ConfigurationService";
import { DemoApiService } from "../services/Api/DemoApiService";
import type { IFeatureService } from "../services/Api/FeatureService";
import type { ILicensingService } from "../services/Api/LicensingService";
import type { ILogService } from "../services/Api/LogService";
import type {
	IProjectMetricsService,
	ITeamMetricsService,
} from "../services/Api/MetricsService";
import type { IOptionalFeatureService } from "../services/Api/OptionalFeatureService";
import type { IProjectService } from "../services/Api/ProjectService";
import type { ISettingsService } from "../services/Api/SettingsService";
import type { ISuggestionService } from "../services/Api/SuggestionService";
import type { ITeamService } from "../services/Api/TeamService";
import type { ITerminologyService } from "../services/Api/TerminologyService";
import type { IWorkTrackingSystemService } from "../services/Api/WorkTrackingSystemService";
import type { IUpdateSubscriptionService } from "../services/UpdateSubscriptionService";

export const createMockApiServiceContext = (
	overrides: Partial<IApiServiceContext>,
): IApiServiceContext => {
	return {
		forecastService: null as unknown as IApiServiceContext["forecastService"],
		logService: null as unknown as IApiServiceContext["logService"],
		projectService: null as unknown as IApiServiceContext["projectService"],
		settingsService: null as unknown as IApiServiceContext["settingsService"],
		teamService: null as unknown as IApiServiceContext["teamService"],
		teamMetricsService:
			null as unknown as IApiServiceContext["teamMetricsService"],
		versionService: null as unknown as IApiServiceContext["versionService"],
		workTrackingSystemService:
			null as unknown as IApiServiceContext["workTrackingSystemService"],
		optionalFeatureService:
			null as unknown as IApiServiceContext["optionalFeatureService"],
		updateSubscriptionService: new DemoApiService(false, false),
		projectMetricsService:
			null as unknown as IApiServiceContext["projectMetricsService"],
		suggestionService:
			null as unknown as IApiServiceContext["suggestionService"],
		configurationService:
			null as unknown as IApiServiceContext["configurationService"],
		featureService: null as unknown as IApiServiceContext["featureService"],
		terminologyService:
			null as unknown as IApiServiceContext["terminologyService"],
		licensingService: null as unknown as IApiServiceContext["licensingService"],
		demoDataService: null as unknown as IApiServiceContext["demoDataService"],
		...overrides,
	};
};

export const createMockTerminologyService = (): ITerminologyService => {
	return {
		getAllTerminology: vi.fn(),
		updateTerminology: vi.fn(),
	};
};

export const createMockSuggestionService = (): ISuggestionService => {
	return {
		getTags: vi.fn(),
		getWorkItemTypesForTeams: vi.fn(),
		getWorkItemTypesForProjects: vi.fn(),
		getStatesForTeams: vi.fn(),
		getStatesForProjects: vi.fn(),
	};
};

export const createMockSettingsService = (): ISettingsService => {
	return {
		getDefaultProjectSettings: vi.fn(),
		getRefreshSettings: vi.fn(),
		updateRefreshSettings: vi.fn(),
		getDefaultTeamSettings: vi.fn(),
		updateDefaultTeamSettings: vi.fn(),
		updateDefaultProjectSettings: vi.fn(),
		getWorkTrackingSystemSettings: vi.fn(),
		updateWorkTrackingSystemSettings: vi.fn(),
	};
};

export const createMockProjectService = (): IProjectService => {
	return {
		getProjectSettings: vi.fn(),
		createProject: vi.fn(),
		updateProject: vi.fn(),
		getProjects: vi.fn(),
		deleteProject: vi.fn(),
		getProject: vi.fn(),
		refreshFeaturesForProject: vi.fn(),
		refreshForecastsForProject: vi.fn(),
		refreshFeaturesForAllProjects: vi.fn(),
		validateProjectSettings: vi.fn(),
	};
};

export const createMockTeamService = (): ITeamService => {
	return {
		getTeams: vi.fn(),
		getTeam: vi.fn(),
		deleteTeam: vi.fn(),
		getTeamSettings: vi.fn(),
		validateTeamSettings: vi.fn(),
		updateTeam: vi.fn(),
		createTeam: vi.fn(),
		updateTeamData: vi.fn(),
		updateAllTeamData: vi.fn(),
	};
};

export const createMockFeatureService = (): IFeatureService => {
	return {
		getFeaturesByReferences: vi.fn(),
		getFeaturesByIds: vi.fn(),
	};
};

export const createMockTeamMetricsService = (): ITeamMetricsService => {
	return {
		getThroughput: vi.fn(),
		getStartedItems: vi.fn(),
		getWorkInProgressOverTime: vi.fn(),
		getFeaturesInProgress: vi.fn(),
		getInProgressItems: vi.fn(),
		getCycleTimeData: vi.fn(),
		getCycleTimePercentiles: vi.fn(),
		getMultiItemForecastPredictabilityScore: vi.fn(),
	};
};

export const createMockProjectMetricsService = (): IProjectMetricsService => {
	return {
		getThroughput: vi.fn().mockResolvedValue({ data: [], total: 0 }),
		getStartedItems: vi.fn().mockResolvedValue({ data: [], total: 0 }),
		getWorkInProgressOverTime: vi
			.fn()
			.mockResolvedValue({ data: [], total: 0 }),
		getInProgressItems: vi.fn().mockResolvedValue([]),
		getCycleTimeData: vi.fn().mockResolvedValue([]),
		getCycleTimePercentiles: vi.fn().mockResolvedValue([]),
		getMultiItemForecastPredictabilityScore: vi.fn(),
		getSizePercentiles: vi.fn(),
	};
};

export const createMockWorkTrackingSystemService =
	(): IWorkTrackingSystemService => {
		return {
			getConfiguredWorkTrackingSystems: vi.fn().mockResolvedValue([{ id: 1 }]),
			getWorkTrackingSystems: vi.fn(),
			addNewWorkTrackingSystemConnection: vi.fn(),
			updateWorkTrackingSystemConnection: vi.fn(),
			deleteWorkTrackingSystemConnection: vi.fn(),
			validateWorkTrackingSystemConnection: vi.fn(),
		};
	};

export const createMockConfigurationService = (): IConfigurationService => {
	return {
		exportConfiguration: vi.fn(),
		clearConfiguration: vi.fn(),
		validateConfiguration: vi.fn(),
	};
};

export const createMockUpdateSubscriptionService =
	(): IUpdateSubscriptionService => {
		return {
			initialize: vi.fn(),
			subscribeToFeatureUpdates: vi.fn(),
			subscribeToForecastUpdates: vi.fn(),
			subscribeToTeamUpdates: vi.fn(),
			getUpdateStatus: vi.fn(),
			unsubscribeFromFeatureUpdates: vi.fn(),
			unsubscribeFromForecastUpdates: vi.fn(),
			unsubscribeFromTeamUpdates: vi.fn(),
		};
	};

export const createMockOptionalFeatureService = (): IOptionalFeatureService => {
	return {
		getAllFeatures: vi.fn(),
		getFeatureByKey: vi.fn(),
		updateFeature: vi.fn(),
	};
};

export const createMockLogService = (): ILogService => {
	return {
		getLogs: vi.fn(),
		getLogLevel: vi.fn(),
		getSupportedLogLevels: vi.fn(),
		setLogLevel: vi.fn(),
		downloadLogs: vi.fn(),
	};
};

export const createMockLicensingService = (): ILicensingService => {
	return {
		getLicenseStatus: vi.fn(),
		importLicense: vi.fn(),
	};
};
