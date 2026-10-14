import { useRbac } from "../../../hooks/useRbac";
import type { IReportOwner } from "../../../models/Reports/Report";

/** Whether the viewer may create, change and delete this owner's reports; nobody may until the roles are known. */
export const useMayEditReports = (
	owner: IReportOwner,
): { isEditor: boolean; isLoading: boolean } => {
	const rbac = useRbac();
	const isAdmin =
		owner.kind === "team"
			? rbac.isTeamAdmin(owner.id)
			: rbac.isPortfolioAdmin(owner.id);
	return { isEditor: !rbac.isLoading && isAdmin, isLoading: rbac.isLoading };
};
