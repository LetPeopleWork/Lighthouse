import AddIcon from "@mui/icons-material/Add";
import { Box, Button, Stack, Typography } from "@mui/material";
import type React from "react";
import { useState } from "react";
import { LicenseTooltip } from "../../../components/App/License/LicenseToolTip";
import type {
	IReportListItem,
	IReportOwner,
} from "../../../models/Reports/Report";
import { useTerminology } from "../../../services/TerminologyContext";
import { WidgetBody } from "../MetricsView/WidgetShell";
import { queryStatusOf } from "../MetricsView/widgetStatus";
import ReportList, { type ReportListActions } from "./ReportList";
import { ownerTermKeyOf } from "./reportOwner";
import { useMayEditReports } from "./useMayEditReports";
import { useOwnerReports } from "./useOwnerReports";
import { useReportDeletion } from "./useReportDeletion";
import { useReportEditing } from "./useReportEditing";
import { useReportPdfExport } from "./useReportPdfExport";
import ReportWizard from "./wizard/ReportWizard";
import { capSentenceFor } from "./wizard/reportRefusalCopy";

const LIST_COULD_NOT_LOAD = "Reports couldn't be loaded. Reload to try again.";
const NO_REPORTS = "No reports yet.";
const LIST_MIN_HEIGHT = 120;

interface ReportsTabProps {
	owner: IReportOwner;
}

const ListOrEmpty: React.FC<{
	readonly owner: IReportOwner;
	readonly reports: IReportListItem[];
	readonly emptyMessage: string;
	readonly actions: ReportListActions;
}> = ({ owner, reports, emptyMessage, actions }) =>
	reports.length === 0 ? (
		<Typography color="text.secondary">{emptyMessage}</Typography>
	) : (
		<ReportList owner={owner} reports={reports} actions={actions} />
	);

const ReportsTab: React.FC<Readonly<ReportsTabProps>> = ({ owner }) => {
	const { getTerm } = useTerminology();
	const [isCreating, setIsCreating] = useState(false);
	const { isEditor, isLoading: rolesAreLoading } = useMayEditReports(owner);
	const deletion = useReportDeletion(owner);
	const editing = useReportEditing(owner);
	const pdfExport = useReportPdfExport(owner);

	const { data, isFetching, isError, refetch } = useOwnerReports(owner);

	const status = queryStatusOf(isFetching, isError);
	const isAtTheCap = data?.creationBlockedByCap === true;
	const ownerTerm = getTerm(ownerTermKeyOf(owner));
	const emptyMessage =
		isEditor || rolesAreLoading
			? NO_REPORTS
			: `${NO_REPORTS} A ${ownerTerm} Admin can create one.`;

	return (
		<Stack spacing={2}>
			{isEditor && (
				<Box
					sx={{
						display: "flex",
						flexDirection: "column",
						alignItems: "flex-end",
						gap: 1,
					}}
				>
					<LicenseTooltip
						canUseFeature={!isAtTheCap}
						defaultTooltip="Create a report"
					>
						<span>
							<Button
								variant="contained"
								startIcon={<AddIcon />}
								disabled={data === undefined || isAtTheCap}
								onClick={() => setIsCreating(true)}
							>
								Create Report
							</Button>
						</span>
					</LicenseTooltip>
					{isAtTheCap && (
						<Typography variant="body2" color="text.secondary">
							{capSentenceFor(ownerTerm)}
						</Typography>
					)}
				</Box>
			)}
			{isCreating && (
				<ReportWizard owner={owner} onClose={() => setIsCreating(false)} />
			)}
			{editing.element}
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
							actions={{
								onExportPdf: pdfExport.exportPdf,
								preparingId: pdfExport.preparingId,
								editor: isEditor
									? {
											onEdit: editing.ask,
											onDelete: deletion.ask,
											readingId: editing.readingId,
										}
									: undefined,
							}}
						/>
					)}
				</WidgetBody>
			</Box>
			{deletion.dialog}
			{pdfExport.frame}
		</Stack>
	);
};

export default ReportsTab;
