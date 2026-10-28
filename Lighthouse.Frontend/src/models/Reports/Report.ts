import { z } from "zod";

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

/** Why a value cannot be shown; the value then reads as a dash. A reason this version has no words for reads as unknown. */
export type ReportAbsentReason = "no-finished-items" | "unknown";

export interface IReportValue {
	value: number | null;
	sampleSize: number | null;
	absentReason: ReportAbsentReason | null;
}

/** One side of a metric: only the members that metric has are present, and one never captured is missing or null. */
export type ReportMetricSide = Partial<
	Record<
		| "p95"
		| "p85"
		| "p70"
		| "p50"
		| "total"
		| "perDay"
		| "average"
		| "median"
		| "min"
		| "max",
		IReportValue | null
	>
>;

/**
 * The two sides of a metric, named as the server names them. Neither side is called `then`: an object
 * carrying a `then` member looks like a promise to anything that awaits it.
 */
export interface IReportMetric {
	atThen: ReportMetricSide;
	atNow: ReportMetricSide;
}

export interface IReportWindow {
	start: string;
	end: string;
	lengthDays: number;
}

export interface IThenAndNow {
	thenWindow: IReportWindow;
	nowWindow: IReportWindow;
	/** Not yet sent by the server; becomes required once it is. */
	daysSinceThenEnded?: number;
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

/** An edit as the wizard makes it; the token comes from the report as it was read. */
export type ReportChange = Omit<IReportEdit, "concurrencyToken">;

const day = z.string();
/** The server names the owner kind by its capitalised enum name; the browser reads it lower-case, as its routes do. */
export const ownerKind = z
	.enum(["Team", "Portfolio", "team", "portfolio"])
	.transform((kind) => kind.toLowerCase() as IReportOwner["kind"]);

/** A later version may show a metric this one has no name for; it is passed on so the view can say it cannot show it. */
const metricKey = z.custom<ReportMetricKey>((key) => typeof key === "string");

export const ReportTemplatesSchema: z.ZodType<IReportTemplates> = z.object({
	today: day,
	earliestStart: day.nullable(),
	minimumLengthDays: z.number(),
	templates: z.array(
		z.object({
			templateKey: z.literal("then-and-now"),
			metrics: z.array(z.object({ key: metricKey })),
		}),
	),
});

export const ReportListItemSchema = z.object({
	id: z.number(),
	name: z.string(),
	templateKey: z.string(),
	ownerKind,
	ownerId: z.number(),
	createdOn: day,
	thenAndNow: z.object({ thenRebuildable: z.boolean() }).optional(),
});

export const ReportListSchema: z.ZodType<IReportList> = z.object({
	reports: z.array(ReportListItemSchema),
	creationBlockedByCap: z.boolean(),
	communityCap: z.number(),
});

const KNOWN_ABSENT_REASONS: readonly ReportAbsentReason[] = [
	"no-finished-items",
];

/** A report frozen by a later version may give a reason this one does not know; refusing it would lose the whole report. */
const absentReason = z
	.string()
	.nullable()
	.transform((reason): ReportAbsentReason | null =>
		reason === null
			? null
			: (KNOWN_ABSENT_REASONS.find((known) => known === reason) ?? "unknown"),
	);

const ReportValueSchema: z.ZodType<IReportValue> = z.object({
	value: z.number().nullable(),
	sampleSize: z.number().nullable(),
	absentReason,
});

const ReportMetricSideSchema = z.record(
	z.string(),
	ReportValueSchema.nullable(),
);

const ReportWindowSchema: z.ZodType<IReportWindow> = z.object({
	start: day,
	end: day,
	lengthDays: z.number(),
});

export const ReportSchema: z.ZodType<IReport> = z.object({
	id: z.number(),
	name: z.string(),
	templateKey: z.string(),
	ownerKind,
	ownerId: z.number(),
	createdOn: day,
	concurrencyToken: z.string(),
	shownMetricKeys: z.array(metricKey),
	thenAndNow: z.object({
		thenWindow: ReportWindowSchema,
		nowWindow: ReportWindowSchema,
		daysSinceThenEnded: z.number().optional(),
		thenRebuildable: z.boolean(),
		metrics: z.record(
			z.string(),
			z
				.object({
					atThen: ReportMetricSideSchema,
					atNow: ReportMetricSideSchema,
				})
				.nullable(),
		),
	}),
});
