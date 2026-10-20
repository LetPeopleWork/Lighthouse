import { Box, Button, Stack } from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import { Link as RouterLink, useNavigate } from "react-router";
import type {
	IReport,
	IReportMetric,
	IReportOwner,
} from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { usageDataReportTemplateOf } from "../../../services/UsageData/usageDataReporter";
import { reportFailedNavigation } from "../../../utils/navigation";
import { WidgetBody } from "../MetricsView/WidgetShell";
import { queryStatusOf } from "../MetricsView/widgetStatus";
import {
	inCatalogOrder,
	metricNameTermKeyOf,
	panelFor,
} from "./panels/reportPanelRegistry";
import UnsupportedMetricPanel from "./panels/UnsupportedMetricPanel";
import { reportQueryKeyOf, reportsPathOf } from "./reportOwner";
import ThenAndNowHeader from "./ThenAndNowHeader";
import ThenAndNowSummary from "./ThenAndNowSummary";
import { useMayEditReports } from "./useMayEditReports";
import { useReportDeletion } from "./useReportDeletion";
import { useReportOpenedReporter } from "./useReportOpenedReporter";
import { useReportPdfExport } from "./useReportPdfExport";
import ReportWizard from "./wizard/ReportWizard";
import {
	COULD_NOT_LOAD_REPORT,
	finalFailureOf,
	retriesUnlessFinal,
} from "./wizard/reportRefusalCopy";

const LOADING = "Loading report…";
const BODY_MIN_HEIGHT = 120;

/** A metric shown on a report made before it was captured reads as not captured on both sides. */
const NOTHING_CAPTURED: IReportMetric = { atThen: {}, atNow: {} };

interface ReportViewProps {
	owner: IReportOwner;
	reportId: number;
}

const ReportPanels: React.FC<{ readonly report: IReport }> = ({ report }) => {
	const { getTerm } = useTerminology();
	const workItem = getTerm(TERMINOLOGY_KEYS.WORK_ITEM);
	const workItems = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	return inCatalogOrder(report.shownMetricKeys).map((key) => {
		const panel = panelFor(key);
		if (panel === undefined)
			return (
				<UnsupportedMetricPanel
					key={key}
					name={getTerm(metricNameTermKeyOf(key))}
				/>
			);
		const metric = report.thenAndNow.metrics[key] ?? NOTHING_CAPTURED;
		const Panel = panel.component;
		return (
			<Panel
				key={key}
				title={panel.titleOf(getTerm)}
				metric={metric}
				workItem={workItem}
				workItems={workItems}
				directionOfGood={panel.directionOfGood}
			/>
		);
	});
};

const ReportView: React.FC<Readonly<ReportViewProps>> = ({
	owner,
	reportId,
}) => {
	const { reportService } = useContext(ApiServiceContext);
	const usualRetry = useQueryClient().getDefaultOptions().queries?.retry;
	const {
		data: report,
		isFetching,
		isError,
		error,
		refetch,
	} = useQuery({
		queryKey: reportQueryKeyOf(owner, reportId),
		queryFn: () => reportService.getReport(owner, reportId),
		retry: retriesUnlessFinal(usualRetry),
	});

	useReportOpenedReporter(isError ? undefined : report);

	const reportsPath = reportsPathOf(owner);
	const navigate = useNavigate();
	const { isEditor } = useMayEditReports(owner);
	const deletion = useReportDeletion(owner, () =>
		reportFailedNavigation(navigate(reportsPath)),
	);
	const [editing, setEditing] = useState<IReport | null>(null);
	const status = queryStatusOf(isFetching, isError);
	const finalFailure = finalFailureOf(error);
	const mayChange = isEditor && report !== undefined;
	const exportPdf = useReportPdfExport();
	// A report of a type this version cannot show has nothing to print.
	const mayExport =
		report !== undefined &&
		!isError &&
		usageDataReportTemplateOf(report.templateKey) !== undefined;

	return (
		<Stack spacing={2} data-report-status={status}>
			<ThenAndNowHeader
				reportsPath={reportsPath}
				name={report?.name}
				onExport={
					mayExport ? () => exportPdf(owner, reportId, "report") : undefined
				}
				onEdit={mayChange ? () => setEditing(report) : undefined}
				onDelete={mayChange ? () => deletion.ask(report) : undefined}
			/>
			<Box
				aria-busy={status === "loading" ? "true" : undefined}
				sx={{
					display: "flex",
					flexDirection: "column",
					minHeight: BODY_MIN_HEIGHT,
				}}
			>
				<WidgetBody
					widgetKey="report"
					status={status}
					hasContentToDim={report !== undefined}
					loadingMessage={LOADING}
					failureMessage={finalFailure ?? COULD_NOT_LOAD_REPORT}
					onRetry={finalFailure === undefined ? () => refetch() : undefined}
				>
					{report !== undefined && (
						<Stack spacing={2}>
							<ThenAndNowSummary
								thenAndNow={report.thenAndNow}
								shownMetricKeys={report.shownMetricKeys}
							/>
							<ReportPanels report={report} />
						</Stack>
					)}
				</WidgetBody>
			</Box>
			{status === "error" && finalFailure !== undefined && (
				<Box>
					<Button component={RouterLink} to={reportsPath}>
						Back to Reports
					</Button>
				</Box>
			)}
			{deletion.dialog}
			{editing !== null && (
				<ReportWizard
					owner={owner}
					editing={editing}
					onClose={() => setEditing(null)}
				/>
			)}
		</Stack>
	);
};

export default ReportView;
