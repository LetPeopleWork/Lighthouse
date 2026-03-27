import type { RefreshLog } from "../../models/SystemInfo/RefreshLog";
import type { SystemInfo } from "../../models/SystemInfo/SystemInfo";
import { BaseApiService } from "./BaseApiService";

export interface ISystemInfoService {
	getSystemInfo(): Promise<SystemInfo>;
	getRefreshLogs(): Promise<RefreshLog[]>;
}

export class SystemInfoService
	extends BaseApiService
	implements ISystemInfoService
{
	async getSystemInfo(): Promise<SystemInfo> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<SystemInfo>("/systeminfo");
			return response.data;
		});
	}

	async getRefreshLogs(): Promise<RefreshLog[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RefreshLog[]>(
				"/systeminfo/refreshlog",
			);
			return response.data;
		});
	}
}
