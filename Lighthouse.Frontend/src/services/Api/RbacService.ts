import type {
	CreateRbacGroupMappingRequest,
	RbacGroupMapping,
	RbacScopedMemberSummary,
	RbacStatus,
	RbacUser,
	ScopedRbacRole,
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
	getTeamMembers(teamId: number): Promise<RbacScopedMemberSummary[]>;
	upsertTeamMember(
		teamId: number,
		userProfileId: number,
		role: ScopedRbacRole,
	): Promise<void>;
	removeTeamMember(teamId: number, userProfileId: number): Promise<void>;
	getPortfolioMembers(portfolioId: number): Promise<RbacScopedMemberSummary[]>;
	upsertPortfolioMember(
		portfolioId: number,
		userProfileId: number,
		role: ScopedRbacRole,
	): Promise<void>;
	removePortfolioMember(
		portfolioId: number,
		userProfileId: number,
	): Promise<void>;
	getGroupMappings(): Promise<RbacGroupMapping[]>;
	createGroupMapping(request: CreateRbacGroupMappingRequest): Promise<void>;
	removeGroupMapping(mappingId: number): Promise<void>;
	deleteUser(userId: number): Promise<void>;
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

	getTeamMembers(teamId: number): Promise<RbacScopedMemberSummary[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RbacScopedMemberSummary[]>(
				`/authorization/teams/${teamId}/members`,
			);
			return response.data;
		});
	}

	upsertTeamMember(
		teamId: number,
		userProfileId: number,
		role: ScopedRbacRole,
	): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.put(
				`/authorization/teams/${teamId}/members/${userProfileId}`,
				{ role },
			);
		});
	}

	removeTeamMember(teamId: number, userProfileId: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete(
				`/authorization/teams/${teamId}/members/${userProfileId}`,
			);
		});
	}

	getPortfolioMembers(portfolioId: number): Promise<RbacScopedMemberSummary[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RbacScopedMemberSummary[]>(
				`/authorization/portfolios/${portfolioId}/members`,
			);
			return response.data;
		});
	}

	upsertPortfolioMember(
		portfolioId: number,
		userProfileId: number,
		role: ScopedRbacRole,
	): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.put(
				`/authorization/portfolios/${portfolioId}/members/${userProfileId}`,
				{ role },
			);
		});
	}

	removePortfolioMember(
		portfolioId: number,
		userProfileId: number,
	): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete(
				`/authorization/portfolios/${portfolioId}/members/${userProfileId}`,
			);
		});
	}

	getGroupMappings(): Promise<RbacGroupMapping[]> {
		return this.withErrorHandling(async () => {
			const response = await this.apiService.get<RbacGroupMapping[]>(
				"/authorization/group-mappings",
			);
			return response.data;
		});
	}

	createGroupMapping(request: CreateRbacGroupMappingRequest): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.post("/authorization/group-mappings", request);
		});
	}

	removeGroupMapping(mappingId: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete(
				`/authorization/group-mappings/${mappingId}`,
			);
		});
	}

	deleteUser(userId: number): Promise<void> {
		return this.withErrorHandling(async () => {
			await this.apiService.delete(`/authorization/users/${userId}`);
		});
	}
}
