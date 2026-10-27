import { Alert } from "@mui/material";
import { useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import type { IReport, IReportOwner } from "../../../models/Reports/Report";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { reportQueryKeyOf, reportsQueryKeyOf } from "./reportOwner";
import ReportWizard from "./wizard/ReportWizard";
import {
	COULD_NOT_LOAD_REPORT,
	NO_LONGER_EXISTS,
} from "./wizard/reportRefusalCopy";

export interface ReportEditing {
	/** Reads the report, then opens Edit report on it; a second press while it is being read does nothing. */
	ask: (report: { id: number }, forOwner?: IReportOwner) => void;
	/** The report being read before its edit can open, if any. */
	readingId: number | undefined;
	/** The edit wizard, or what went wrong reading the report for it. */
	element: React.ReactNode;
}

const isNotFound = (error: unknown): boolean =>
	error instanceof ApiError && error.code === 404;

/**
 * Editing one of an owner's reports from somewhere that only knows which report it is. The report is read
 * afresh first, since an edit is saved over the report as it was read; one that is gone is forgotten and the
 * owner's list read again.
 */
export const useReportEditing = (
	owner: IReportOwner | undefined,
): ReportEditing => {
	const { reportService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();
	const [readingId, setReadingId] = useState<number | undefined>();
	const [editing, setEditing] = useState<{
		report: IReport;
		owner: IReportOwner;
	} | null>(null);
	const [failure, setFailure] = useState<string | undefined>();

	const read = async (reportOwner: IReportOwner, reportId: number) => {
		const queryKey = reportQueryKeyOf(reportOwner, reportId);
		try {
			const report = await queryClient.query({
				queryKey,
				queryFn: () => reportService.getReport(reportOwner, reportId),
			});
			setEditing({ report, owner: reportOwner });
		} catch (error) {
			if (!isNotFound(error)) {
				setFailure(COULD_NOT_LOAD_REPORT);
				return;
			}
			setFailure(NO_LONGER_EXISTS);
			queryClient.removeQueries({ queryKey, exact: true });
			await queryClient.invalidateQueries({
				queryKey: reportsQueryKeyOf(reportOwner),
			});
		} finally {
			setReadingId(undefined);
		}
	};

	const ask = (report: { id: number }, forOwner = owner) => {
		if (forOwner === undefined || readingId !== undefined) {
			return;
		}
		setFailure(undefined);
		setReadingId(report.id);
		read(forOwner, report.id).catch(() => undefined);
	};

	const failureAlert =
		failure === undefined ? null : (
			<Alert severity="error" onClose={() => setFailure(undefined)}>
				{failure}
			</Alert>
		);

	const element =
		editing === null ? (
			failureAlert
		) : (
			<ReportWizard
				owner={editing.owner}
				editing={editing.report}
				onClose={() => setEditing(null)}
			/>
		);

	return { ask, readingId, element };
};
