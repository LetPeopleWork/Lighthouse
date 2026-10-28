import AddIcon from "@mui/icons-material/Add";
import { Box, Button, Container, Typography } from "@mui/material";
import type React from "react";
import { useState } from "react";
import SnackbarErrorHandler from "../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import { WidgetBody } from "../Common/MetricsView/WidgetShell";
import { queryStatusOf } from "../Common/MetricsView/widgetStatus";
import ReportList, {
	type ReportListActions,
} from "../Common/Reports/ReportList";
import { ownerOfReport } from "../Common/Reports/reportOwner";
import { useMayEditReportsOf } from "../Common/Reports/useMayEditReports";
import { useReportDeletion } from "../Common/Reports/useReportDeletion";
import { useReportEditing } from "../Common/Reports/useReportEditing";
import { useReportPdfExport } from "../Common/Reports/useReportPdfExport";
import BulkReportWizard from "./BulkReportWizard";
import { useAllReports } from "./useAllReports";
import { useAllReportsListedReporter } from "./useAllReportsListedReporter";
import { useReportCreationOptions } from "./useReportCreationOptions";

const COULD_NOT_LOAD = "The reports couldn't be loaded.";
const EMPTY = "Reports are created on a Team's or Portfolio's Reports tab.";
const LIST_MIN_HEIGHT = 120;

/** Create reports, and the wizard it opens once what a report needs has arrived for this opening. */
const useBulkCreation = () => {
	const [opening, setOpening] = useState<number | null>(null);
	const options = useReportCreationOptions(opening);
	const close = () => setOpening(null);
	const button = (
		<Button
			variant="contained"
			startIcon={<AddIcon />}
			disabled={options.isFetching}
			onClick={() => setOpening((opening ?? 0) + 1)}
		>
			Create reports
		</Button>
	);
	const wizard =
		opening !== null && options.isSuccess && !options.isFetching ? (
			<BulkReportWizard key={opening} options={options.data} onClose={close} />
		) : null;
	return { button, wizard };
};

const ReportsPageContent: React.FC = () => {
	const bulkCreation = useBulkCreation();
	const { data, isFetching, isError, refetch } = useAllReports();
	const status = queryStatusOf(isFetching, isError);
	useAllReportsListedReporter(status === "ready" && data !== undefined);
	const { mayEdit } = useMayEditReportsOf();
	const deletion = useReportDeletion(undefined);
	const editing = useReportEditing(undefined);
	const pdfExport = useReportPdfExport(undefined);
	const actions: ReportListActions = {
		onExportPdf: (report) => pdfExport.exportPdf(report, ownerOfReport(report)),
		preparingId: pdfExport.preparingId,
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
			<Box sx={{ display: "flex", mb: 2 }}>{bulkCreation.button}</Box>
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
							<Typography color="text.secondary">{EMPTY}</Typography>
						) : (
							<ReportList reports={data.reports} actions={actions} showOwner />
						))}
				</WidgetBody>
			</Box>
			{editing.element}
			{deletion.dialog}
			{pdfExport.frame}
			{bulkCreation.wizard}
		</Container>
	);
};

const ReportsPage: React.FC = () => (
	<SnackbarErrorHandler>
		<ReportsPageContent />
	</SnackbarErrorHandler>
);

export default ReportsPage;
