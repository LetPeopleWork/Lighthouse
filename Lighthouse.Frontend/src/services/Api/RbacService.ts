import type {
	RbacStatus,
	RbacUser,
	UserAuthorizationSummary,
} from "../../models/Authorization/RbacModels";
import { BaseApiService } from "./BaseApiService";

export interface IRbacService {
	getStatus(): Promise<RbacStatus>;
	getUsers(): Promise<RbacUser[]>;
	getAuthorizationSummary(): Promise<UserAuthorizationSummary>;
	bootstrapCurrentUserAsSystemAdmin(): Promise<void>;
	grantSystemAdmin(userId: number): Promise<void>;
	revokeSystemAdmin(userId: number): Promise<void>;
}

export class RbacService extends BaseApiService implements IRbacService {
	getStatus(): Promise<RbacStatus> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RbacStatus>(
				"/authorization/status",
			);
			return response.data;
		});
	}

	getUsers(): Promise<RbacUser[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RbacUser[]>(
				"/authorization/users",
			);
			return response.data;
		});
	}

	getAuthorizationSummary(): Promise<UserAuthorizationSummary> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<UserAuthorizationSummary>(
				"/authorization/my-summary",
			);
			return response.data;
		});
	}

	bootstrapCurrentUserAsSystemAdmin(): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post("/authorization/bootstrap/system-admin");
		});
	}

	grantSystemAdmin(userId: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post(`/authorization/system-admins/${userId}`);
		});
	}

	revokeSystemAdmin(userId: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete(`/authorization/system-admins/${userId}`);
		});
	}
}
