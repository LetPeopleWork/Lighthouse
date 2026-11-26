import type { IReportNote } from "../../../models/Reports/ReportNote";

/** The line under a note's text: the day it was written, then who wrote it when somebody signed it. */
export const bylineOf = (note: IReportNote): string => {
	const written = note.authorDisplayName
		? `${note.createdOn} · ${note.authorDisplayName}`
		: note.createdOn;
	return note.lastEditedOn
		? `${written} · edited ${note.lastEditedOn}`
		: written;
};
