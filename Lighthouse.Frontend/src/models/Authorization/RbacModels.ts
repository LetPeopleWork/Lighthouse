export interface RbacStatus {
	enabled: boolean;
	premiumGateSatisfied: boolean;
	hasSystemAdmin: boolean;
	hasEmergencyAdminConfigured: boolean;
	readyForEnablement: boolean;
}

export interface RbacUser {
	id: number;
	subject: string;
	displayName?: string;
	email?: string;
	isSystemAdmin: boolean;
}

export interface UserAuthorizationSummary {
	isRbacEnabled: boolean;
	isSystemAdmin: boolean;
	canCreateTeam: boolean;
	canCreatePortfolio: boolean;
}

export type ScopedRbacRole = "Viewer" | "TeamAdmin" | "PortfolioAdmin";

export interface RbacScopedMemberSummary {
	userProfileId: number;
	subject: string;
	displayName?: string;
	email?: string;
	role?: ScopedRbacRole | null;
}
