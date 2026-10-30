import { z } from "zod";
import {
	type INewReport,
	type IReportListItem,
	ownerKind,
	ReportListItemSchema,
	type ReportMetricKey,
	type ReportOwnerKind,
	type ReportTemplateKey,
} from "./Report";

/** A report on the Reports page, which lists every Team's and Portfolio's reports in one place. */
export interface IReportAcrossOwners extends IReportListItem {
	ownerName: string;
}

export interface IReportsAcrossOwners {
	reports: IReportAcrossOwners[];
}

export const ReportsAcrossOwnersSchema: z.ZodType<IReportsAcrossOwners> =
	z.object({
		reports: z.array(ReportListItemSchema.extend({ ownerName: z.string() })),
	});

/** An owner the caller is Admin of and so may create a report on; `earliestStart` is null when it keeps every finished item. */
export interface IReportCreationOwner {
	ownerKind: ReportOwnerKind;
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
		metricKeysByOwnerKind: Record<ReportOwnerKind, ReportMetricKey[]>;
	}[];
	owners: IReportCreationOwner[];
}

/** The most owners one creation run takes, and the most reports one selection acts on; the server refuses more. */
export const MOST_PER_BULK_RUN = 50;

export interface INewReportsForOwners extends INewReport {
	owners: { ownerKind: ReportOwnerKind; ownerId: number }[];
}

export interface IReportCreationOutcome {
	ownerKind: ReportOwnerKind;
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

const optionalOnTheWire = <T extends z.ZodType>(schema: T) =>
	schema.nullish().transform((value) => value ?? undefined);

/** The server keys the metrics by the owner kind's capitalised name; a kind it leaves out offers none. */
const metricKeysByOwnerKind = z
	.record(
		z.string(),
		z.array(z.custom<ReportMetricKey>((key) => typeof key === "string")),
	)
	.transform((byKind) => {
		const lowerCased = new Map(
			Object.entries(byKind).map(([kind, keys]) => [kind.toLowerCase(), keys]),
		);
		return {
			team: lowerCased.get("team") ?? [],
			portfolio: lowerCased.get("portfolio") ?? [],
		};
	});

export const ReportCreationOptionsSchema: z.ZodType<IReportCreationOptions> =
	z.object({
		today: z.string(),
		minimumLengthDays: z.number(),
		templates: z.array(
			z.object({
				templateKey: z.literal("then-and-now"),
				metricKeysByOwnerKind,
			}),
		),
		owners: z.array(
			z.object({
				ownerKind,
				ownerId: z.number(),
				ownerName: z.string(),
				earliestStart: z.string().nullable(),
			}),
		),
	});

export const ReportRemovalOutcomesSchema: z.ZodType<{
	outcomes: IReportRemovalOutcome[];
}> = z.object({
	outcomes: z.array(
		z.object({
			reportId: z.number(),
			status: z.enum(["removed", "notRemoved"]),
			code: optionalOnTheWire(z.string()),
		}),
	),
});

export const ReportCreationOutcomesSchema: z.ZodType<{
	outcomes: IReportCreationOutcome[];
}> = z.object({
	outcomes: z.array(
		z.object({
			ownerKind,
			ownerId: z.number(),
			ownerName: optionalOnTheWire(z.string()),
			status: z.enum(["created", "notCreated"]),
			reportId: optionalOnTheWire(z.number()),
			code: optionalOnTheWire(z.string()),
			earliestStart: z.string().nullish(),
		}),
	),
});
