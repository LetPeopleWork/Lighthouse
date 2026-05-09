export interface RbacStatus {
	enabled: boolean;
	premiumGateSatisfied: boolean;
	hasSystemAdmin: boolean;
	hasEmergencyAdminConfigured: boolean;
	readyForEnablement: boolean;
	unassignedUserCount?: number;
	groupClaimName?: string | null;
}

export interface RbacUser {
	id: number;
	subject: string;
	displayName?: string;
	email?: string;
	isSystemAdmin: boolean;
	isUnassigned?: boolean;
}

export interface UserAuthorizationSummary {
	isRbacEnabled: boolean;
	isSystemAdmin: boolean;
	canCreateTeam: boolean;
	canCreatePortfolio: boolean;
	systemAdminDisplayNames?: string[];
	/** Team IDs where the current user has admin (write) rights. Empty when RBAC is disabled or user is System Admin. */
	adminTeamIds?: number[];
	/** Portfolio IDs where the current user has admin (write) rights. Empty when RBAC is disabled or user is System Admin. */
	adminPortfolioIds?: number[];
}

export type ScopedRbacRole = "Viewer" | "TeamAdmin" | "PortfolioAdmin";

export interface RbacScopedMemberSummary {
	userProfileId: number;
	subject: string;
	displayName?: string;
	email?: string;
	role?: ScopedRbacRole | null;
}

export type GroupMappingScopeType = "System" | "Team" | "Portfolio";

export type GroupMappingRole = "SystemAdmin" | ScopedRbacRole;

export interface RbacGroupMapping {
	id: number;
	groupValue: string;
	role: GroupMappingRole;
	scopeType: GroupMappingScopeType;
	scopeId?: number | null;
}

export interface CreateRbacGroupMappingRequest {
	groupValue: string;
	role: GroupMappingRole;
	scopeType: GroupMappingScopeType;
	scopeId?: number | null;
}
