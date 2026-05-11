import { useRbac } from "./useRbac";

export type RbacGateRequirement =
	| { kind: "systemAdmin" }
	| { kind: "teamAdmin"; teamId: number }
	| { kind: "portfolioAdmin"; portfolioId: number };

export interface RbacGateResult {
	allowed: boolean;
	isLoading: boolean;
}

export function useRbacGate(requirement: RbacGateRequirement): RbacGateResult {
	const rbac = useRbac();

	const allowed = ((): boolean => {
		switch (requirement.kind) {
			case "systemAdmin":
				return rbac.isSystemAdmin;
			case "teamAdmin":
				return rbac.isTeamAdmin(requirement.teamId);
			case "portfolioAdmin":
				return rbac.isPortfolioAdmin(requirement.portfolioId);
		}
	})();

	return {
		allowed,
		isLoading: rbac.isLoading,
	};
}
