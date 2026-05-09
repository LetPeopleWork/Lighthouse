export interface RbacStatus {
	enabled: boolean;
	premiumGateSatisfied: boolean;
	hasSystemAdmin: boolean;
	hasEmergencyAdminConfigured: boolean;
	readyForEnablement: boolean;
	unassignedUserCount?: number;
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
}

export type ScopedRbacRole = "Viewer" | "TeamAdmin" | "PortfolioAdmin";

export interface RbacScopedMemberSummary {
	userProfileId: number;
	subject: string;
	displayName?: string;
	email?: string;
	role?: ScopedRbacRole | null;
}
