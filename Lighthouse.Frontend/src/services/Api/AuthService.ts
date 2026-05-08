import type {
	AuthSessionStatus,
	CurrentUserProfileStatus,
	RuntimeAuthStatus,
} from "../../models/Auth/AuthModels";
import { BaseApiService } from "./BaseApiService";

export interface IAuthService {
	getRuntimeAuthStatus(): Promise<RuntimeAuthStatus>;
	getSession(): Promise<AuthSessionStatus>;
	getCurrentUserProfile(): Promise<CurrentUserProfileStatus>;
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

	async getCurrentUserProfile(): Promise<CurrentUserProfileStatus> {
		return this.withErrorHandling(async () => {
			const response =
				await this.apiService.get<CurrentUserProfileStatus>("/auth/me");
			return response.data;
		});
	}

	getLoginUrl(): string {
		const baseUrl = this.apiService.defaults.baseURL ?? "/api/latest";
		return `${baseUrl}/auth/login`;
	}

	async logout(): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post("/auth/logout");
		});
	}
}
