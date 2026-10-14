import type { IReportOwner } from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";

export const reportsPathOf = (owner: IReportOwner): string =>
	`/${owner.kind}s/${owner.id}/reports`;

export const reportPathOf = (owner: IReportOwner, reportId: number): string =>
	`${reportsPathOf(owner)}/${reportId}`;

export const ownerTermKeyOf = (owner: IReportOwner): string =>
	owner.kind === "team" ? TERMINOLOGY_KEYS.TEAM : TERMINOLOGY_KEYS.PORTFOLIO;

/** The one cache entry for an owner's report list, so every reader and every change agree on it. */
export const reportsQueryKeyOf = (owner: IReportOwner) =>
	["reports", owner.kind, owner.id] as const;
