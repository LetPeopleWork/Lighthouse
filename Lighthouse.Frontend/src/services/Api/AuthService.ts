import type {
	AuthSessionStatus,
	RuntimeAuthStatus,
} from "../../models/Auth/AuthModels";
import { BaseApiService } from "./BaseApiService";

export interface IAuthService {
	getRuntimeAuthStatus(): Promise<RuntimeAuthStatus>;
	getSession(): Promise<AuthSessionStatus>;
	getLoginUrl(): string;
	logout(): Promise<void>;
}

export class AuthService extends BaseApiService implements IAuthService {
	async getRuntimeAuthStatus(): Promise<RuntimeAuthStatus> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<RuntimeAuthStatus>("/auth/mode");
			return response.data;
		});
	}

	async getSession(): Promise<AuthSessionStatus> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<AuthSessionStatus>("/auth/session");
			return response.data;
		});
	}

	getLoginUrl(): string {
		const baseUrl = this.apiService.defaults.baseURL ?? "/api";
		return `${baseUrl}/auth/login`;
	}

	async logout(): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post("/auth/logout");
		});
	}
}
