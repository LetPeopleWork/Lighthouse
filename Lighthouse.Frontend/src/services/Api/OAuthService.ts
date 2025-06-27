import { BaseApiService } from "./BaseApiService";

export interface OAuthTokenResponse {
	accessToken: string;
	refreshToken: string;
	expiresIn: number;
	tokenType: string;
}

export interface AccessibleResource {
	id: string;
	name: string;
	url: string;
	scopes: string[];
	avatarUrl: string;
}

export interface IOAuthService {
	getAuthorizationUrl(
		clientId: string,
		redirectUri: string,
		state: string,
	): Promise<string>;
	exchangeCodeForToken(
		clientId: string,
		clientSecret: string,
		code: string,
		redirectUri: string,
	): Promise<OAuthTokenResponse>;
	refreshToken(
		clientId: string,
		clientSecret: string,
		refreshToken: string,
	): Promise<OAuthTokenResponse>;
	validateToken(accessToken: string, jiraUrl: string): Promise<boolean>;
	getAccessibleResources(accessToken: string): Promise<AccessibleResource[]>;
}

export class OAuthService extends BaseApiService implements IOAuthService {
	async getAuthorizationUrl(
		clientId: string,
		redirectUri: string,
		state: string,
	): Promise<string> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.get<{ authorizationUrl: string }>(
				"/oauth/authorize",
				{
					params: {
						clientId,
						redirectUri,
						state,
					},
				},
			);
			return response.data.authorizationUrl;
		});
	}

	async exchangeCodeForToken(
		clientId: string,
		clientSecret: string,
		code: string,
		redirectUri: string,
	): Promise<OAuthTokenResponse> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.post<OAuthTokenResponse>(
				"/oauth/token",
				{
					clientId,
					clientSecret,
					code,
					redirectUri,
				},
			);
			return response.data;
		});
	}

	async refreshToken(
		clientId: string,
		clientSecret: string,
		refreshToken: string,
	): Promise<OAuthTokenResponse> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.post<OAuthTokenResponse>(
				"/oauth/refresh",
				{
					clientId,
					clientSecret,
					refreshToken,
				},
			);
			return response.data;
		});
	}

	async validateToken(accessToken: string, jiraUrl: string): Promise<boolean> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.post<{ isValid: boolean }>(
				"/oauth/validate",
				{
					accessToken,
					jiraUrl,
				},
			);
			return response.data.isValid;
		});
	}

	async getAccessibleResources(
		accessToken: string,
	): Promise<AccessibleResource[]> {
		return await this.withErrorHandling(async () => {
			const response = await this.apiService.post<AccessibleResource[]>(
				"/oauth/accessible-resources",
				{
					accessToken,
				},
			);
			return response.data || [];
		});
	}
}
