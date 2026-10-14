import { useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import DeleteConfirmationDialog from "../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import type { IReportOwner } from "../../../models/Reports/Report";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import { reportQueryKeyOf, reportsQueryKeyOf } from "./reportOwner";

const CANNOT_BE_REBUILT = "This Then window can no longer be rebuilt.";
const COULD_NOT_DELETE = "This report couldn't be deleted. Try again.";

/** What deleting needs to know of a report, whether it comes from the list or from its own page. */
interface DeletableReport {
	id: number;
	name: string;
	thenAndNow?: { thenRebuildable: boolean };
}

export interface ReportDeletion {
	/** Opens the confirmation for this report; nothing is sent until somebody confirms. */
	ask: (report: DeletableReport) => void;
	dialog: React.ReactNode;
}

type DeleteOutcome = "deleted" | "already-gone" | "failed";

/** A Then whose days have fallen out of the kept history is gone for good once its report is. */
const rebuildWarningOf = (report: DeletableReport): string | undefined =>
	report.thenAndNow?.thenRebuildable === false ? CANNOT_BE_REBUILT : undefined;

const isNotFound = (error: unknown): boolean =>
	error instanceof ApiError && error.code === 404;

const ignore = () => undefined;

/**
 * Deleting one of an owner's reports after asking. A delete the server agreed to is reported; a report
 * somebody else deleted first is gone just the same, but nobody here deleted it, so nothing is reported.
 * Either way the report is forgotten and the owner's list re-read. A failed delete keeps the dialog open
 * and says so, and while a delete is on its way the dialog can be neither confirmed again nor dismissed.
 */
export const useReportDeletion = (
	owner: IReportOwner,
	onDeleted: () => void = () => undefined,
): ReportDeletion => {
	const { reportService } = useContext(ApiServiceContext);
	const queryClient = useQueryClient();
	const reportUsage = useUsageDataReporter();
	const [toDelete, setToDelete] = useState<DeletableReport | null>(null);
	const [failure, setFailure] = useState<string | undefined>();
	const [isDeleting, setIsDeleting] = useState(false);

	const ask = (report: DeletableReport) => {
		setFailure(undefined);
		setToDelete(report);
	};

	const close = () => {
		setToDelete(null);
		setFailure(undefined);
		setIsDeleting(false);
	};

	const send = async (reportId: number): Promise<DeleteOutcome> => {
		try {
			await reportService.deleteReport(owner, reportId);
			return "deleted";
		} catch (error_) {
			return isNotFound(error_) ? "already-gone" : "failed";
		}
	};

	const confirm = async () => {
		if (toDelete === null || isDeleting) {
			return;
		}
		setIsDeleting(true);
		const outcome = await send(toDelete.id);
		if (outcome === "failed") {
			setFailure(COULD_NOT_DELETE);
			setIsDeleting(false);
			return;
		}
		if (outcome === "deleted") {
			reportUsage({ name: UsageDataEventName.ReportDeleted });
		}
		queryClient.removeQueries({
			queryKey: reportQueryKeyOf(owner, toDelete.id),
			exact: true,
		});
		close();
		onDeleted();
		await queryClient.invalidateQueries({
			queryKey: reportsQueryKeyOf(owner),
		});
	};

	const dialog =
		toDelete === null ? null : (
			<DeleteConfirmationDialog
				open
				itemName={toDelete.name}
				warning={rebuildWarningOf(toDelete)}
				errorMessage={failure}
				confirmDisabled={isDeleting}
				onConfirm={confirm}
				onCancel={isDeleting ? ignore : close}
			/>
		);

	return { ask, dialog };
};
