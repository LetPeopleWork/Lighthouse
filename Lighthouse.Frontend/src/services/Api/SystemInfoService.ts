import type { SystemInfo } from "../../models/SystemInfo/SystemInfo";
import { BaseApiService } from "./BaseApiService";

export interface ISystemInfoService {
	getSystemInfo(): Promise<SystemInfo>;
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
}
