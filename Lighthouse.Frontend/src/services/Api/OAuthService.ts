import { getBackendUrl } from "../../utils/backendUrl";
import { BaseApiService } from "./BaseApiService";

export interface IOAuthInitiateResponse {
	authorizationUrl: string;
}

export interface IOAuthService {
	initiateConnect(
		providerKey: string,
		connectionId: number,
	): Promise<IOAuthInitiateResponse>;
	disconnect(providerKey: string, connectionId: number): Promise<void>;
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
}
