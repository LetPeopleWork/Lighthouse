import AddIcon from "@mui/icons-material/Add";
import { Box, Button, Stack, Typography } from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import DeleteConfirmationDialog from "../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import { useRbac } from "../../../hooks/useRbac";
import type {
	IReportListItem,
	IReportOwner,
} from "../../../models/Reports/Report";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useTerminology } from "../../../services/TerminologyContext";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import { WidgetBody } from "../MetricsView/WidgetShell";
import { queryStatusOf } from "../MetricsView/widgetStatus";
import ReportList from "./ReportList";
import { rebuildWarningOf } from "./rebuildWarning";
import { ownerTermKeyOf } from "./reportOwner";
import ReportWizard from "./wizard/ReportWizard";

const LIST_COULD_NOT_LOAD = "Reports couldn't be loaded. Reload to try again.";
const NO_REPORTS = "No reports yet.";
const LIST_MIN_HEIGHT = 120;
const COULD_NOT_DELETE = "This report couldn't be deleted. Try again.";

interface ReportsTabProps {
	owner: IReportOwner;
}

const ListOrEmpty: React.FC<{
	readonly owner: IReportOwner;
	readonly reports: IReportListItem[];
	readonly emptyMessage: string;
	readonly onDelete?: (report: IReportListItem) => void;
}> = ({ owner, reports, emptyMessage, onDelete }) =>
	reports.length === 0 ? (
		<Typography color="text.secondary">{emptyMessage}</Typography>
	) : (
		<ReportList owner={owner} reports={reports} onDelete={onDelete} />
	);

const ReportsTab: React.FC<Readonly<ReportsTabProps>> = ({ owner }) => {
	const { reportService } = useContext(ApiServiceContext);
	const rbac = useRbac();
	const { getTerm } = useTerminology();
	const queryClient = useQueryClient();
	const reportUsage = useUsageDataReporter();
	const [isCreating, setIsCreating] = useState(false);
	const [toDelete, setToDelete] = useState<IReportListItem | null>(null);
	const [deleteError, setDeleteError] = useState<string | undefined>();

	const listKey = ["reports", owner.kind, owner.id];
	const { data, isFetching, isError, refetch } = useQuery({
		queryKey: listKey,
		queryFn: () => reportService.getReports(owner),
	});

	const isEditor =
		!rbac.isLoading &&
		(owner.kind === "team"
			? rbac.isTeamAdmin(owner.id)
			: rbac.isPortfolioAdmin(owner.id));

	const status = queryStatusOf(isFetching, isError);
	const ownerTerm = getTerm(ownerTermKeyOf(owner));
	const emptyMessage =
		isEditor || rbac.isLoading
			? NO_REPORTS
			: `${NO_REPORTS} A ${ownerTerm} Admin can create one.`;

	const askToDelete = (report: IReportListItem) => {
		setDeleteError(undefined);
		setToDelete(report);
	};

	const closeDeleteDialog = () => {
		setToDelete(null);
		setDeleteError(undefined);
	};

	const confirmDelete = async () => {
		if (toDelete === null) {
			return;
		}
		try {
			await reportService.deleteReport(owner, toDelete.id);
		} catch {
			setDeleteError(COULD_NOT_DELETE);
			return;
		}
		reportUsage({ name: UsageDataEventName.ReportDeleted });
		closeDeleteDialog();
		await queryClient.invalidateQueries({ queryKey: listKey });
	};

	return (
		<Stack spacing={2}>
			{isEditor && (
				<Box sx={{ display: "flex", justifyContent: "flex-end" }}>
					<Button
						variant="contained"
						startIcon={<AddIcon />}
						disabled={data === undefined}
						onClick={() => setIsCreating(true)}
					>
						Create Report
					</Button>
				</Box>
			)}
			{isCreating && (
				<ReportWizard owner={owner} onClose={() => setIsCreating(false)} />
			)}
			<Box
				data-testid="reports-list"
				data-widget-status={status}
				aria-busy={status === "loading" ? "true" : undefined}
				sx={{
					display: "flex",
					flexDirection: "column",
					minHeight: LIST_MIN_HEIGHT,
				}}
			>
				<WidgetBody
					widgetKey="reports-list"
					status={status}
					hasContentToDim={data !== undefined}
					failureMessage={LIST_COULD_NOT_LOAD}
					onRetry={() => refetch()}
				>
					{data !== undefined && (
						<ListOrEmpty
							owner={owner}
							reports={data.reports}
							emptyMessage={emptyMessage}
							onDelete={isEditor ? askToDelete : undefined}
						/>
					)}
				</WidgetBody>
			</Box>
			{toDelete !== null && (
				<DeleteConfirmationDialog
					open
					itemName={toDelete.name}
					warning={rebuildWarningOf(toDelete)}
					errorMessage={deleteError}
					onConfirm={confirmDelete}
					onCancel={closeDeleteDialog}
				/>
			)}
		</Stack>
	);
};

export default ReportsTab;
