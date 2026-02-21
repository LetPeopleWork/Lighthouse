import { vi } from "vitest";
import type { IApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IConfigurationService } from "../services/Api/ConfigurationService";
import type { IDeliveryService } from "../services/Api/DeliveryService";
import type { IFeatureService } from "../services/Api/FeatureService";
import type { ILicensingService } from "../services/Api/LicensingService";
import type { ILogService } from "../services/Api/LogService";
import type {
	IProjectMetricsService,
	ITeamMetricsService,
} from "../services/Api/MetricsService";
import type { IOptionalFeatureService } from "../services/Api/OptionalFeatureService";
import type { IPortfolioService } from "../services/Api/PortfolioService";
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
		portfolioService: null as unknown as IApiServiceContext["portfolioService"],
		settingsService: null as unknown as IApiServiceContext["settingsService"],
		teamService: null as unknown as IApiServiceContext["teamService"],
		teamMetricsService:
			null as unknown as IApiServiceContext["teamMetricsService"],
		versionService: null as unknown as IApiServiceContext["versionService"],
		workTrackingSystemService:
			null as unknown as IApiServiceContext["workTrackingSystemService"],
		optionalFeatureService:
			null as unknown as IApiServiceContext["optionalFeatureService"],
		updateSubscriptionService:
			null as unknown as IApiServiceContext["updateSubscriptionService"],
		portfolioMetricsService:
			null as unknown as IApiServiceContext["portfolioMetricsService"],
		suggestionService:
			null as unknown as IApiServiceContext["suggestionService"],
		configurationService:
			null as unknown as IApiServiceContext["configurationService"],
		featureService: null as unknown as IApiServiceContext["featureService"],
		terminologyService:
			null as unknown as IApiServiceContext["terminologyService"],
		licensingService: null as unknown as IApiServiceContext["licensingService"],
		demoDataService: null as unknown as IApiServiceContext["demoDataService"],
		deliveryService: null as unknown as IApiServiceContext["deliveryService"],
		wizardService: null as unknown as IApiServiceContext["wizardService"],
		...overrides,
	};
};

export const createMockTerminologyService = (): ITerminologyService => {
	return {
		getAllTerminology: vi.fn().mockResolvedValue([
			{
				id: 1,
				key: "portfolio",
				defaultValue: "Portfolio",
				description: "Term used for individual portfolios",
				value: "Portfolio",
			},
			{
				id: 2,
				key: "portfolios",
				defaultValue: "Portfolios",
				description: "Term used for multiple portfolios",
				value: "Portfolios",
			},
		]),
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
		getRefreshSettings: vi.fn(),
		updateRefreshSettings: vi.fn(),
	};
};

export const createMockPortfolioService = (): IPortfolioService => {
	return {
		getPortfolioSettings: vi.fn(),
		createPortfolio: vi.fn(),
		updatePortfolio: vi.fn(),
		getPortfolios: vi.fn(),
		deletePortfolio: vi.fn(),
		getPortfolio: vi.fn(),
		refreshFeaturesForPortfolio: vi.fn(),
		refreshForecastsForPortfolio: vi.fn(),
		refreshFeaturesForAllPortfolios: vi.fn(),
		validatePortfolioSettings: vi.fn(),
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
		updateForecastsForTeamPortfolios: vi.fn(),
	};
};

export const createMockFeatureService = (): IFeatureService => {
	return {
		getFeaturesByReferences: vi.fn(),
		getFeaturesByIds: vi.fn(),
		getFeatureWorkItems: vi.fn(),
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
		getTotalWorkItemAge: vi.fn(),
		getThroughputPbc: vi.fn(),
		getWipPbc: vi.fn(),
		getTotalWorkItemAgePbc: vi.fn(),
		getCycleTimePbc: vi.fn(),
		getEstimationVsCycleTimeData: vi.fn(),
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
		getTotalWorkItemAge: vi.fn(),
		getAllFeaturesForSizeChart: vi.fn().mockResolvedValue([]),
		getThroughputPbc: vi.fn(),
		getWipPbc: vi.fn(),
		getTotalWorkItemAgePbc: vi.fn(),
		getCycleTimePbc: vi.fn(),
		getFeatureSizePbc: vi.fn(),
		getEstimationVsCycleTimeData: vi.fn(),
		getFeatureSizeEstimation: vi.fn(),
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
			subscribeToAllUpdates: vi.fn(),
			unsubscribeFromAllUpdates: vi.fn(),
			subscribeToFeatureUpdates: vi.fn(),
			subscribeToForecastUpdates: vi.fn(),
			subscribeToTeamUpdates: vi.fn(),
			getUpdateStatus: vi.fn(),
			getGlobalUpdateStatus: vi.fn(),
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
		clearLicense: vi.fn(),
	};
};

export const createMockDeliveryService = (): IDeliveryService => {
	return {
		getByPortfolio: vi.fn().mockResolvedValue([]),
		create: vi.fn().mockResolvedValue(undefined),
		update: vi.fn().mockResolvedValue(undefined),
		delete: vi.fn().mockResolvedValue(undefined),
		getRuleSchema: vi.fn().mockResolvedValue({ fields: [] }),
		validateRules: vi.fn().mockResolvedValue([]),
	};
};
