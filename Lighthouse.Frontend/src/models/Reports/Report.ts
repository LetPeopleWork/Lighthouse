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

/** One side of a metric: only the members that metric has are present, and one never captured is missing or null. */
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

const day = z.string();
const ownerKind = z.enum(["team", "portfolio"]);

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

export const ReportListSchema: z.ZodType<IReportList> = z.object({
	reports: z.array(
		z.object({
			id: z.number(),
			name: z.string(),
			templateKey: z.string(),
			ownerKind,
			ownerId: z.number(),
			createdOn: day,
			thenAndNow: z.object({ thenRebuildable: z.boolean() }).optional(),
		}),
	),
	creationBlockedByCap: z.boolean(),
	communityCap: z.number(),
});

const ReportValueSchema: z.ZodType<IReportValue> = z.object({
	value: z.number().nullable(),
	sampleSize: z.number().nullable(),
	absentReason: z
		.enum([
			"no-finished-items",
			"too-few-points",
			"no-process",
			"no-wip",
			"lower-limit-zero",
		])
		.nullable(),
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
		daysSinceThenEnded: z.number(),
		thenRebuildable: z.boolean(),
		metrics: z.record(
			z.string(),
			z
				.object({
					atThen: ReportMetricSideSchema,
					atNow: ReportMetricSideSchema,
					beyondThenLimits: z
						.object({ above: ReportValueSchema, below: ReportValueSchema })
						.nullable()
						.optional(),
				})
				.nullable(),
		),
	}),
});
