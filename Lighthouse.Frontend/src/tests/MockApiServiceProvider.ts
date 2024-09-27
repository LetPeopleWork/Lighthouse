import { IApiServiceContext } from '../services/Api/ApiServiceContext';
import { IProjectService } from '../services/Api/ProjectService';
import { ISettingsService } from '../services/Api/SettingsService';
import { ITeamService } from '../services/Api/TeamService';
import { IWorkTrackingSystemService } from '../services/Api/WorkTrackingSystemService';

export const createMockApiServiceContext = (overrides: Partial<IApiServiceContext>): IApiServiceContext => {
    return {
        forecastService: null as unknown as IApiServiceContext['forecastService'],
        logService: null as unknown as IApiServiceContext['logService'],
        projectService: null as unknown as IApiServiceContext['projectService'],
        settingsService: null as unknown as IApiServiceContext['settingsService'],
        teamService: null as unknown as IApiServiceContext['teamService'],
        versionService: null as unknown as IApiServiceContext['versionService'],
        workTrackingSystemService: null as unknown as IApiServiceContext['workTrackingSystemService'],
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
    };
}

export const createMockTeamService = (): ITeamService => {
    return {
        getTeams: vi.fn(),
        getTeam: vi.fn(),
        deleteTeam: vi.fn(),
        getTeamSettings: vi.fn(),
        updateTeam: vi.fn(),
        createTeam: vi.fn(),
        updateThroughput: vi.fn(),
        getThroughput: vi.fn(),    
    }
}

export const createMockWorkTrackingSystemService = (): IWorkTrackingSystemService => {
    return {
        getConfiguredWorkTrackingSystems: vi.fn(),
        getWorkTrackingSystems: vi.fn(),
        addNewWorkTrackingSystemConnection: vi.fn(),
        updateWorkTrackingSystemConnection: vi.fn(),
        deleteWorkTrackingSystemConnection: vi.fn(),
        validateWorkTrackingSystemConnection: vi.fn(),
    };
}