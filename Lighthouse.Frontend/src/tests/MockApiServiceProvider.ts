import { IApiServiceContext } from '../services/Api/ApiServiceContext';

export const createMockApiServiceContext = (    overrides: Partial<IApiServiceContext>): IApiServiceContext => {
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