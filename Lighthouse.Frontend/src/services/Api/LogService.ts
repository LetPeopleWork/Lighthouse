import { BaseApiService } from './BaseApiService';

export interface ILogService {
    getSupportedLogLevels(): Promise<string[]>;
    getLogLevel(): Promise<string>;
    setLogLevel(logLevel: string): Promise<void>;
    getLogs(): Promise<string>;
}

export class LogService extends BaseApiService implements ILogService {
    async getSupportedLogLevels(): Promise<string[]> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<string[]>(`/logs/level/supported`);

            return response.data;
        });
    }

    async getLogLevel(): Promise<string> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<string>(`/logs/level`);

            return response.data;
        });
    }

    async setLogLevel(logLevel: string): Promise<void> {
        await this.withErrorHandling(async () => {
            await this.apiService.post<void>('/logs/level', { level: logLevel });
        });
    }


    async getLogs(): Promise<string> {
        return this.withErrorHandling(async () => {
            const response = await this.apiService.get<string>(`/logs`);

            return response.data;
        });
    }
}