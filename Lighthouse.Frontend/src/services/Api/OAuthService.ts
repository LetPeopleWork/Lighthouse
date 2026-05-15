import { getBackendUrl } from "../../utils/backendUrl";
import { BaseApiService } from "./BaseApiService";

export interface IOAuthInitiateResponse {
	authorizationUrl: string;
}

export interface IOAuthHealthMetric {
	value: number | null;
	unavailableReason: string | null;
}

export interface IOAuthHealthDto {
	setupSuccessRate30d: IOAuthHealthMetric;
	refreshSuccessRate7d: IOAuthHealthMetric;
	staleRefreshFailedCount24h: number;
	staleRefreshFailedCount7d: number;
}

export interface IOAuthService {
	initiateConnect(
		providerKey: string,
		connectionId: number,
	): Promise<IOAuthInitiateResponse>;
	disconnect(providerKey: string, connectionId: number): Promise<void>;
	getHealth(): Promise<IOAuthHealthDto>;
}

export class OAuthService extends BaseApiService implements IOAuthService {
	async initiateConnect(
		providerKey: string,
		connectionId: number,
	): Promise<IOAuthInitiateResponse> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IOAuthInitiateResponse>(
				`/oauth/${providerKey}/connect`,
				{ connectionId },
				{ baseURL: getBackendUrl() },
			);
			return response.data;
		});
	}

	async disconnect(providerKey: string, connectionId: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post(
				`/oauth/${providerKey}/disconnect`,
				{ connectionId },
				{ baseURL: getBackendUrl() },
			);
		});
	}

	async getHealth(): Promise<IOAuthHealthDto> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IOAuthHealthDto>(
				"/oauth/health",
				{ baseURL: getBackendUrl() },
			);
			return response.data;
		});
	}
}
