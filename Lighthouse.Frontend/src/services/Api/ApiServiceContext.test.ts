import { getApiServices, IApiServiceContext } from './ApiServiceContext';
import { DemoApiService } from './DemoApiService';

const mockImportMetaEnv = (envVars: Record<string, string>) => {
    Object.defineProperty(import.meta, 'env', {
        value: envVars,
        writable: true
    });
};

describe('ApiServiceContext', () => {

    afterEach(() => {
        mockImportMetaEnv({
            VITE_API_SERVICE_TYPE: '',
            VITE_API_SERVICE_DELAY: ''
        });
    });

    it('should return demo services when in demo mode', () => {
        mockImportMetaEnv({ VITE_API_SERVICE_TYPE: 'DEMO', VITE_API_SERVICE_DELAY: 'FALSE' });

        const apiServices: IApiServiceContext = getApiServices();

        expect(apiServices.forecastService).toBeInstanceOf(DemoApiService);
        expect(apiServices.logService).toBeInstanceOf(DemoApiService);
        expect(apiServices.projectService).toBeInstanceOf(DemoApiService);
        expect(apiServices.settingsService).toBeInstanceOf(DemoApiService);
        expect(apiServices.teamService).toBeInstanceOf(DemoApiService);
        expect(apiServices.versionService).toBeInstanceOf(DemoApiService);
        expect(apiServices.workTrackingSystemService).toBeInstanceOf(DemoApiService);
    });
});
