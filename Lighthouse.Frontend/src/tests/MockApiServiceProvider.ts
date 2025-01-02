import type { IApiServiceContext } from "../services/Api/ApiServiceContext";
import type { IChartService } from "../services/Api/ChartService";
import { DemoApiService } from "../services/Api/DemoApiService";
import type { ILogService } from "../services/Api/LogService";
import type { IPreviewFeatureService } from "../services/Api/PreviewFeatureService";
import type { IProjectService } from "../services/Api/ProjectService";
import type { ISettingsService } from "../services/Api/SettingsService";
import type { ITeamService } from "../services/Api/TeamService";
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
		versionService: null as unknown as IApiServiceContext["versionService"],
		workTrackingSystemService:
			null as unknown as IApiServiceContext["workTrackingSystemService"],
		chartService: null as unknown as IApiServiceContext["chartService"],
		previewFeatureService:
			null as unknown as IApiServiceContext["previewFeatureService"],
		updateSubscriptionService: new DemoApiService(false, false),
		...overrides,
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
		getDataRetentionSettings: vi.fn(),
		updateDataRetentionSettings: vi.fn(),
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

export const createMockPreviewFeatureService = (): IPreviewFeatureService => {
	return {
		getAllFeatures: vi.fn(),
		getFeatureByKey: vi.fn(),
		updateFeature: vi.fn(),
	};
};

export const createMockChartService = (): IChartService => {
	return {
		getLighthouseChartData: vi.fn(),
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
