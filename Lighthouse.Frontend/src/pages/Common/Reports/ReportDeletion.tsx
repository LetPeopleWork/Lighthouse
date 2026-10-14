import { useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import DeleteConfirmationDialog from "../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import { useRbac } from "../../../hooks/useRbac";
import type { IReportOwner } from "../../../models/Reports/Report";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";

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

/** Whether the viewer may create, change and delete this owner's reports; nobody may until the roles are known. */
export const useMayEditReports = (
	owner: IReportOwner,
): { isEditor: boolean; isLoading: boolean } => {
	const rbac = useRbac();
	const isAdmin =
		owner.kind === "team"
			? rbac.isTeamAdmin(owner.id)
			: rbac.isPortfolioAdmin(owner.id);
	return { isEditor: !rbac.isLoading && isAdmin, isLoading: rbac.isLoading };
};

/** A Then whose days have fallen out of the kept history is gone for good once its report is. */
const rebuildWarningOf = (report: DeletableReport): string | undefined =>
	report.thenAndNow?.thenRebuildable === false ? CANNOT_BE_REBUILT : undefined;

/**
 * Deleting one of an owner's reports after asking. Only a delete the server agreed to is reported and
 * re-reads the owner's list; a failed one keeps the dialog open and says so.
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

	const ask = (report: DeletableReport) => {
		setFailure(undefined);
		setToDelete(report);
	};

	const close = () => {
		setToDelete(null);
		setFailure(undefined);
	};

	const confirm = async () => {
		if (toDelete === null) {
			return;
		}
		try {
			await reportService.deleteReport(owner, toDelete.id);
		} catch {
			setFailure(COULD_NOT_DELETE);
			return;
		}
		reportUsage({ name: UsageDataEventName.ReportDeleted });
		close();
		onDeleted();
		await queryClient.invalidateQueries({
			queryKey: ["reports", owner.kind, owner.id],
		});
	};

	const dialog =
		toDelete === null ? null : (
			<DeleteConfirmationDialog
				open
				itemName={toDelete.name}
				warning={rebuildWarningOf(toDelete)}
				errorMessage={failure}
				onConfirm={confirm}
				onCancel={close}
			/>
		);

	return { ask, dialog };
};
