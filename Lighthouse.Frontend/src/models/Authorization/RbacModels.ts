import { z } from "zod";

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
	isEmergencyAdmin?: boolean;
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

export const UserAuthorizationSummarySchema = z.object({
	isRbacEnabled: z.boolean(),
	isSystemAdmin: z.boolean(),
	canCreateTeam: z.boolean(),
	canCreatePortfolio: z.boolean(),
	systemAdminDisplayNames: z.array(z.string()).optional(),
	adminTeamIds: z.array(z.number()).optional(),
	adminPortfolioIds: z.array(z.number()).optional(),
});

export type ScopedRbacRole = "Viewer" | "TeamAdmin" | "PortfolioAdmin";

export interface RbacScopedMemberSummary {
	userProfileId: number;
	subject: string;
	displayName?: string;
	email?: string;
	role?: ScopedRbacRole | null;
}

export type GroupMappingScopeType =
	| "System"
	| "Team"
	| "Portfolio"
	| "AllUnits";

export type GroupMappingRole = "SystemAdmin" | ScopedRbacRole;

export interface SystemWideRule {
	role: GroupMappingRole;
	holder: string;
}

/** The holder the server names for the reach of the everyone-signed-in switch. */
export const EVERYONE_SIGNED_IN_HOLDER = "everyone signed in";

/** Who holds a rule, as a unit's admin reads it: everyone signed in, or a named SSO group. */
export const getHolderLabel = (rule: SystemWideRule): string =>
	rule.holder === EVERYONE_SIGNED_IN_HOLDER
		? EVERYONE_SIGNED_IN_HOLDER
		: `group ${rule.holder}`;

/** Whether everyone signed in can view all Teams and Portfolios, with the token a save must be based on. */
export interface EveryoneSignedIn {
	everyoneCanView: boolean;
	concurrencyToken: string;
}

const ROLE_LABELS: Record<GroupMappingRole, string> = {
	SystemAdmin: "System Admin",
	Viewer: "Viewer",
	TeamAdmin: "Team Admin",
	PortfolioAdmin: "Portfolio Admin",
};

export const getRoleLabel = (role: GroupMappingRole): string =>
	ROLE_LABELS[role] ?? "Viewer";

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
