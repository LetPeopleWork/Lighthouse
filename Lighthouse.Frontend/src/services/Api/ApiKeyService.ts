import type {
	IApiKeyCreationResult,
	IApiKeyInfo,
	ICreateApiKeyRequest,
} from "../../models/ApiKey/ApiKey";
import { BaseApiService } from "./BaseApiService";

export interface IApiKeyService {
	getApiKeys(): Promise<IApiKeyInfo[]>;
	createApiKey(request: ICreateApiKeyRequest): Promise<IApiKeyCreationResult>;
	deleteApiKey(id: number): Promise<void>;
}

export class ApiKeyService extends BaseApiService implements IApiKeyService {
	async getApiKeys(): Promise<IApiKeyInfo[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<IApiKeyInfo[]>("/apikeys");
			return response.data;
		});
	}

	async createApiKey(
		request: ICreateApiKeyRequest,
	): Promise<IApiKeyCreationResult> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.post<IApiKeyCreationResult>(
				"/apikeys",
				request,
			);
			return response.data;
		});
	}

	async deleteApiKey(id: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete(`/apikeys/${id}`);
		});
	}
}
