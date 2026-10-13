/** Whose report it is. A report belongs to exactly one Team or one Portfolio and is deleted with it. */
export interface IReportOwner {
	readonly kind: "team" | "portfolio";
	readonly id: number;
}

/** The type of a report, as the server names it. Users read it as the report's Type. */
export type ReportTemplateKey = "then-and-now";

export type ReportMetricKey =
	| "cycle-time"
	| "throughput"
	| "wip"
	| "work-item-age";

export interface IReportTemplate {
	templateKey: ReportTemplateKey;
	metrics: { key: ReportMetricKey }[];
}

/**
 * What can be created today, and the bounds every window entered in the wizard is held to. The days are the
 * instance's calendar days as yyyy-MM-dd; `earliestStart` is null when the owner keeps every finished item.
 * The server answers them so the browser never holds a copy of the rule.
 */
export interface IReportTemplates {
	today: string;
	earliestStart: string | null;
	minimumLengthDays: number;
	templates: IReportTemplate[];
}

export interface IReportListItem {
	id: number;
	name: string;
	templateKey: string;
	ownerKind: "team" | "portfolio";
	ownerId: number;
	createdOn: string;
	/** Missing on a report of a type this version cannot read. */
	thenAndNow?: {
		/** False once Then reaches back past the oldest finished Work Item the owner still keeps. */
		thenRebuildable: boolean;
	};
}

export interface IReportList {
	reports: IReportListItem[];
	creationBlockedByCap: boolean;
	communityCap: number;
}

/** Why a value cannot be shown; the value then reads as a dash. */
export type ReportAbsentReason =
	| "no-finished-items"
	| "too-few-points"
	| "no-process"
	| "no-wip"
	| "lower-limit-zero";

export interface IReportValue {
	value: number | null;
	sampleSize: number | null;
	absentReason: ReportAbsentReason | null;
}

/** One side of a metric: only the members that metric has are present. */
export type ReportMetricSide = Partial<
	Record<
		| "p85"
		| "p70"
		| "p50"
		| "total"
		| "perDay"
		| "average"
		| "min"
		| "max"
		| "totalWindowAverage"
		| "totalLastDay"
		| "averageWindowAverage"
		| "averageLastDay",
		IReportValue
	>
>;

/**
 * The two sides of a metric, named as the server names them. Neither side is called `then`: an object
 * carrying a `then` member looks like a promise to anything that awaits it.
 */
export interface IReportMetric {
	atThen: ReportMetricSide;
	atNow: ReportMetricSide;
	beyondThenLimits?: { above: IReportValue; below: IReportValue } | null;
}

export interface IReportWindow {
	start: string;
	end: string;
	lengthDays: number;
}

export interface IThenAndNow {
	thenWindow: IReportWindow;
	nowWindow: IReportWindow;
	daysSinceThenEnded: number;
	thenRebuildable: boolean;
	/** A metric the report never captured is missing here. */
	metrics: Partial<Record<ReportMetricKey, IReportMetric | null>>;
}

export interface IReport {
	id: number;
	name: string;
	templateKey: string;
	ownerKind: "team" | "portfolio";
	ownerId: number;
	createdOn: string;
	concurrencyToken: string;
	shownMetricKeys: ReportMetricKey[];
	thenAndNow: IThenAndNow;
}

export interface INewReport {
	templateKey: ReportTemplateKey;
	name: string;
	shownMetricKeys: ReportMetricKey[];
	thenAndNow: {
		thenEndDate: string;
		thenLengthDays: number;
		nowLengthDays: number | null;
	};
}

/** Then is never part of an edit: it stays as it was frozen. */
export interface IReportEdit {
	name: string;
	shownMetricKeys: ReportMetricKey[];
	concurrencyToken: string;
	thenAndNow: { nowLengthDays: number };
}
