import { type RbacState, useRbac } from "../../../hooks/useRbac";
import type { IReportOwner } from "../../../models/Reports/Report";

/** Whether the viewer may create, change and delete each owner's reports; nobody may until the roles are known. */
export const mayEditReportsOf = (
	rbac: RbacState,
): {
	mayEdit: (owner: IReportOwner) => boolean;
	isLoading: boolean;
} => {
	const isAdminOf = (owner: IReportOwner) =>
		owner.kind === "team"
			? rbac.isTeamAdmin(owner.id)
			: rbac.isPortfolioAdmin(owner.id);
	return {
		mayEdit: (owner) => !rbac.isLoading && isAdminOf(owner),
		isLoading: rbac.isLoading,
	};
};

/** Whether the viewer may create, change and delete this owner's reports; nobody may until the roles are known. */
export const useMayEditReports = (
	owner: IReportOwner,
): { isEditor: boolean; isLoading: boolean } => {
	const { mayEdit, isLoading } = mayEditReportsOf(useRbac());
	return { isEditor: mayEdit(owner), isLoading };
};
