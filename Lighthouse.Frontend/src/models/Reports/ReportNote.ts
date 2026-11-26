import { z } from "zod";

/** A note on a report. The days are the instance's calendar days as yyyy-MM-dd; the instants are UTC. */
export interface IReportNote {
	id: number;
	text: string;
	createdAt: string;
	createdOn: string;
	lastEditedAt?: string | null;
	lastEditedOn?: string | null;
	authorDisplayName?: string | null;
	canModify?: boolean;
}

export const ReportNoteSchema: z.ZodType<IReportNote> = z.object({
	id: z.number(),
	text: z.string(),
	createdAt: z.string(),
	createdOn: z.string(),
	lastEditedAt: z.string().nullish(),
	lastEditedOn: z.string().nullish(),
	authorDisplayName: z.string().nullish(),
	canModify: z.boolean().optional(),
});

export const ReportNotesSchema = z.array(ReportNoteSchema);
