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

/** The one cache entry for the page listing every owner's reports, which any change to a report must refresh. */
export const ALL_REPORTS_QUERY_KEY = ["reports", "all"] as const;

/** Every cache entry holding one of an owner's reports, so all of them can be dropped at once. */
export const ownerReportQueryKeysOf = (owner: IReportOwner) =>
	["report", owner.kind, owner.id] as const;

/** The one cache entry for a single report, so its page and its deletion agree on it. */
export const reportQueryKeyOf = (owner: IReportOwner, reportId: number) =>
	[...ownerReportQueryKeysOf(owner), reportId] as const;
