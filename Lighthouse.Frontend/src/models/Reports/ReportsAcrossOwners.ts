import type {
	INewReport,
	IReportListItem,
	ReportMetricKey,
	ReportTemplateKey,
} from "./Report";

/** A report on the Reports page, which lists every Team's and Portfolio's reports in one place. */
export interface IReportAcrossOwners extends IReportListItem {
	ownerName: string;
}

export interface IReportsAcrossOwners {
	reports: IReportAcrossOwners[];
}

/** An owner the caller is Admin of and so may create a report on; `earliestStart` is null when it keeps every finished item. */
export interface IReportCreationOwner {
	ownerKind: "team" | "portfolio";
	ownerId: number;
	ownerName: string;
	earliestStart: string | null;
}

/** What can be created for several owners at once, answered by the server so the browser holds no copy of the rules. */
export interface IReportCreationOptions {
	today: string;
	minimumLengthDays: number;
	templates: {
		templateKey: ReportTemplateKey;
		metricKeysByOwnerKind: Record<"team" | "portfolio", ReportMetricKey[]>;
	}[];
	owners: IReportCreationOwner[];
}

export interface INewReportsForOwners extends INewReport {
	owners: { ownerKind: "team" | "portfolio"; ownerId: number }[];
}

export interface IReportCreationOutcome {
	ownerKind: "team" | "portfolio";
	ownerId: number;
	ownerName?: string;
	status: "created" | "notCreated";
	reportId?: number;
	code?: string;
	earliestStart?: string | null;
}

export interface IReportRemovalOutcome {
	reportId: number;
	status: "removed" | "notRemoved";
	code?: string;
}
