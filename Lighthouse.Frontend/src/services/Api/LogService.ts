import { BaseApiService } from "./BaseApiService";

export interface ILogService {
	getSupportedLogLevels(): Promise<string[]>;
	getLogLevel(): Promise<string>;
	setLogLevel(logLevel: string): Promise<void>;
	getLogs(): Promise<string>;
	downloadLogs(): Promise<void>;
}

export class LogService extends BaseApiService implements ILogService {
	async getSupportedLogLevels(): Promise<string[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<string[]>(
				"/logs/level/supported",
			);

			return response.data;
		});
	}

	async getLogLevel(): Promise<string> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<string>("/logs/level");

			return response.data;
		});
	}

	async setLogLevel(logLevel: string): Promise<void> {
		await this.withErrorHandling(async () => {
			await this.apiService.post<void>("/logs/level", { level: logLevel });
		});
	}

	async getLogs(): Promise<string> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<string>("/logs");

			return response.data;
		});
	}

	async downloadLogs(): Promise<void> {
		const response = await this.apiService.get<Blob>("/logs/download", {
			responseType: "blob",
		});
		const fileUrl = URL.createObjectURL(response.data);
		const link = document.createElement("a");
		link.href = fileUrl;
		link.download = `Lighthouse_Log_${new Date().toISOString().split("T")[0]}.txt`;
		document.body.appendChild(link);
		link.click();
		link.remove();
		URL.revokeObjectURL(fileUrl);
	}
}
