import type {
	IReportListItem,
	IReportOwner,
} from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";

/** The owner a listed report, or a bulk run's answer about one, is on. */
export const ownerOfReport = (
	report: Pick<IReportListItem, "ownerKind" | "ownerId">,
): IReportOwner => ({
	kind: report.ownerKind,
	id: report.ownerId,
});

/** Where an owner's own pages live; its Reports tab and its settings sit beneath it. */
export const ownerPathOf = (owner: IReportOwner): string =>
	`/${owner.kind}s/${owner.id}`;

export const reportsPathOf = (owner: IReportOwner): string =>
	`${ownerPathOf(owner)}/reports`;

export const reportPathOf = (owner: IReportOwner, reportId: number): string =>
	`${reportsPathOf(owner)}/${reportId}`;

export const ownerTermKeyOf = (owner: IReportOwner): string =>
	owner.kind === "team" ? TERMINOLOGY_KEYS.TEAM : TERMINOLOGY_KEYS.PORTFOLIO;

/** Every report list, each owner's and the page listing them all, so a change to many reports refreshes them at once. */
export const EVERY_REPORT_LIST_QUERY_KEY = ["reports"] as const;

/** The one cache entry for an owner's report list, so every reader and every change agree on it. */
export const reportsQueryKeyOf = (owner: IReportOwner) =>
	[...EVERY_REPORT_LIST_QUERY_KEY, owner.kind, owner.id] as const;

/** The one cache entry for the page listing every owner's reports, which any change to a report must refresh. */
export const ALL_REPORTS_QUERY_KEY = [
	...EVERY_REPORT_LIST_QUERY_KEY,
	"all",
] as const;

/** Every cache entry holding one of an owner's reports, so all of them can be dropped at once. */
export const ownerReportQueryKeysOf = (owner: IReportOwner) =>
	["report", owner.kind, owner.id] as const;

/** The one cache entry for a single report, so its page and its deletion agree on it. */
export const reportQueryKeyOf = (owner: IReportOwner, reportId: number) =>
	[...ownerReportQueryKeysOf(owner), reportId] as const;
