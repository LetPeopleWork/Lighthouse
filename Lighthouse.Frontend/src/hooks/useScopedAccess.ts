import { useCallback, useContext, useState } from "react";
import type {
	RbacGroupMapping,
	RbacScopedMemberSummary,
	ScopedRbacRole,
} from "../models/Authorization/RbacModels";
import { TERMINOLOGY_KEYS } from "../models/TerminologyKeys";
import { ApiServiceContext } from "../services/Api/ApiServiceContext";
import { useTerminology } from "../services/TerminologyContext";

export type ScopedAccessKind = "Team" | "Portfolio";

export interface ScopedAccess {
	members: RbacScopedMemberSummary[];
	membersLoading: boolean;
	membersError: string | undefined;
	loadMembers: () => Promise<void>;
	assignRole: (userProfileId: number, role: ScopedRbacRole) => Promise<void>;
	removeMember: (userProfileId: number) => Promise<void>;
	loadGroupMappings: () => Promise<RbacGroupMapping[]>;
	createGroupMapping: (
		groupValue: string,
		role: ScopedRbacRole,
	) => Promise<void>;
	removeGroupMapping: (mappingId: number) => Promise<void>;
}

/**
 * The members and group mappings of one Team or Portfolio. Who may see or change them stays
 * with the page, through `useRbac`; this hook only talks to the service for that scope.
 */
export const useScopedAccess = (
	scope: ScopedAccessKind,
	scopeId: number | undefined,
): ScopedAccess => {
	const { rbacService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();
	const scopeWord = getTerm(
		scope === "Team" ? TERMINOLOGY_KEYS.TEAM : TERMINOLOGY_KEYS.PORTFOLIO,
	).toLowerCase();
	const [members, setMembers] = useState<RbacScopedMemberSummary[]>([]);
	const [membersLoading, setMembersLoading] = useState(false);
	const [membersError, setMembersError] = useState<string>();

	const loadMembers = useCallback(async () => {
		if (scopeId === undefined) {
			return;
		}

		setMembersError(undefined);
		setMembersLoading(true);
		try {
			setMembers(
				scope === "Team"
					? await rbacService.getTeamMembers(scopeId)
					: await rbacService.getPortfolioMembers(scopeId),
			);
		} catch {
			setMembersError(`Failed to load ${scopeWord} members.`);
		} finally {
			setMembersLoading(false);
		}
	}, [rbacService, scope, scopeId, scopeWord]);

	const assignRole = useCallback(
		async (userProfileId: number, role: ScopedRbacRole) => {
			if (scopeId === undefined) {
				return;
			}

			setMembersError(undefined);
			try {
				await (scope === "Team"
					? rbacService.upsertTeamMember(scopeId, userProfileId, role)
					: rbacService.upsertPortfolioMember(scopeId, userProfileId, role));
				await loadMembers();
			} catch {
				setMembersError(`Failed to update ${scopeWord} member role.`);
			}
		},
		[rbacService, scope, scopeId, scopeWord, loadMembers],
	);

	const removeMember = useCallback(
		async (userProfileId: number) => {
			if (scopeId === undefined) {
				return;
			}

			setMembersError(undefined);
			try {
				await (scope === "Team"
					? rbacService.removeTeamMember(scopeId, userProfileId)
					: rbacService.removePortfolioMember(scopeId, userProfileId));
				await loadMembers();
			} catch {
				setMembersError(`Failed to remove ${scopeWord} member.`);
			}
		},
		[rbacService, scope, scopeId, scopeWord, loadMembers],
	);

	const loadGroupMappings = useCallback(() => {
		if (scopeId === undefined) {
			return Promise.resolve([]);
		}

		return scope === "Team"
			? rbacService.getTeamGroupMappings(scopeId)
			: rbacService.getPortfolioGroupMappings(scopeId);
	}, [rbacService, scope, scopeId]);

	const createGroupMapping = useCallback(
		async (groupValue: string, role: ScopedRbacRole) => {
			if (scopeId === undefined) {
				return;
			}

			await rbacService.createGroupMapping({
				groupValue,
				role,
				scopeType: scope,
				scopeId,
			});
		},
		[rbacService, scope, scopeId],
	);

	const removeGroupMapping = useCallback(
		async (mappingId: number) => {
			if (scopeId === undefined) {
				return;
			}

			await rbacService.removeGroupMapping(mappingId);
		},
		[rbacService, scopeId],
	);

	return {
		members,
		membersLoading,
		membersError,
		loadMembers,
		assignRole,
		removeMember,
		loadGroupMappings,
		createGroupMapping,
		removeGroupMapping,
	};
};
