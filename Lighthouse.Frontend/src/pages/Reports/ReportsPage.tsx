import { Box, Container, Typography } from "@mui/material";
import type React from "react";
import SnackbarErrorHandler from "../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { useRbac } from "../../hooks/useRbac";
import type { IReportAcrossOwners } from "../../models/Reports/ReportsAcrossOwners";
import { TERMINOLOGY_KEYS } from "../../models/TerminologyKeys";
import { useTerminology } from "../../services/TerminologyContext";
import { WidgetBody } from "../Common/MetricsView/WidgetShell";
import { queryStatusOf } from "../Common/MetricsView/widgetStatus";
import ReportList, {
	type ReportListActions,
} from "../Common/Reports/ReportList";
import { ownerOfReport } from "../Common/Reports/reportOwner";
import { mayEditReportsOf } from "../Common/Reports/useMayEditReports";
import { useReportDeletion } from "../Common/Reports/useReportDeletion";
import { useReportEditing } from "../Common/Reports/useReportEditing";
import { useAllReports } from "./useAllReports";
import { useAllReportsListedReporter } from "./useAllReportsListedReporter";
import { useBulkCreation } from "./useBulkCreation";
import { useReportsPdfExport } from "./useReportsPdfExport";
import { useRowPicks } from "./useRowPicks";
import { useSelectionExport, useSelectionRemoval } from "./useSelectionActions";

const COULD_NOT_LOAD = "The reports couldn't be loaded.";

/** Portfolios first, then Teams, each by owner name; a sort the reader picks in the table still overrides it. */
const byOwner = (reports: IReportAcrossOwners[]): IReportAcrossOwners[] =>
	[...reports].sort(
		(a, b) =>
			Number(a.ownerKind === "team") - Number(b.ownerKind === "team") ||
			a.ownerName.localeCompare(b.ownerName),
	);
const LIST_MIN_HEIGHT = 120;

const ReportsPageContent: React.FC = () => {
	const rbac = useRbac();
	const { getTerm } = useTerminology();
	const bulkCreation = useBulkCreation(rbac);
	const { data, isFetching, isError, refetch } = useAllReports();
	const status = queryStatusOf(isFetching, isError);
	useAllReportsListedReporter(status === "ready" && data !== undefined);
	const picks = useRowPicks(data?.reports);
	const removal = useSelectionRemoval(picks, rbac);
	const pdfExport = useReportsPdfExport(picks.unpick);
	const selectionExport = useSelectionExport(picks, pdfExport);
	const { mayEdit } = mayEditReportsOf(rbac);
	const deletion = useReportDeletion(undefined);
	const editing = useReportEditing(undefined);
	const actions: ReportListActions = {
		onExportPdf: pdfExport.exportOne,
		preparingId: pdfExport.preparingId,
		exportHeldBack: pdfExport.preparing,
		editor: {
			onEdit: (report) => editing.ask(report, ownerOfReport(report)),
			onDelete: (report) => deletion.ask(report, ownerOfReport(report)),
			readingId: editing.readingId,
		},
		mayEditRow: (report) => mayEdit(ownerOfReport(report)),
	};

	return (
		<Container maxWidth={false}>
			<Typography variant="h4" component="h1" sx={{ my: 2 }}>
				Reports
			</Typography>
			<Box sx={{ display: "flex", alignItems: "center", gap: 2, mb: 2 }}>
				{bulkCreation.button}
				{removal.button}
				{selectionExport.button}
				<Typography color="text.secondary">{`${picks.picked.length} selected`}</Typography>
			</Box>
			<Box
				data-testid="all-reports"
				data-widget-status={status}
				aria-busy={status === "loading" ? "true" : undefined}
				sx={{
					display: "flex",
					flexDirection: "column",
					minHeight: LIST_MIN_HEIGHT,
				}}
			>
				<WidgetBody
					widgetKey="all-reports"
					status={status}
					hasContentToDim={data !== undefined}
					failureMessage={COULD_NOT_LOAD}
					onRetry={() => refetch()}
				>
					{data !== undefined &&
						(data.reports.length === 0 ? (
							<Typography color="text.secondary">
								{`Reports are created on a ${getTerm(TERMINOLOGY_KEYS.TEAM)}'s or ${getTerm(TERMINOLOGY_KEYS.PORTFOLIO)}'s Reports tab, or here with Create reports.`}
							</Typography>
						) : (
							<ReportList
								reports={byOwner(data.reports)}
								actions={actions}
								showOwner
								picking={picks.picking}
							/>
						))}
				</WidgetBody>
			</Box>
			{editing.element}
			{deletion.dialog}
			{pdfExport.frame}
			{bulkCreation.wizard}
			{removal.dialog}
		</Container>
	);
};

const ReportsPage: React.FC = () => (
	<SnackbarErrorHandler>
		<ReportsPageContent />
	</SnackbarErrorHandler>
);

export default ReportsPage;
