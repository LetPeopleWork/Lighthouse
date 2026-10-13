import { Box, Button, Stack } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useContext } from "react";
import { Link as RouterLink } from "react-router";
import type { IReport, IReportOwner } from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { WidgetBody } from "../MetricsView/WidgetShell";
import { queryStatusOf } from "../MetricsView/widgetStatus";
import { panelFor } from "./panels/reportPanelRegistry";
import { reportsPathOf } from "./reportOwner";
import ThenAndNowHeader from "./ThenAndNowHeader";
import { useReportOpenedReporter } from "./useReportOpenedReporter";

const LOADING = "Loading report…";
const COULD_NOT_LOAD = "This report couldn't be loaded. Reload to try again.";
const MADE_BY_A_NEWER_VERSION =
	"This report was made by a newer version of Lighthouse and can't be shown here.";
const BODY_MIN_HEIGHT = 120;

interface ReportViewProps {
	owner: IReportOwner;
	reportId: number;
}

const isMadeByANewerVersion = (error: Error | null): boolean =>
	error instanceof ApiError &&
	error.code === 422 &&
	error.problemCode === "template-not-supported";

const ReportPanels: React.FC<{ readonly report: IReport }> = ({ report }) => {
	const { getTerm } = useTerminology();
	const workItems = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

	return report.shownMetricKeys.map((key) => {
		const panel = panelFor(key);
		const metric = report.thenAndNow.metrics[key];
		if (panel === undefined || !metric) return null;
		const Panel = panel.component;
		return (
			<Panel
				key={key}
				title={`${getTerm(panel.titleTermKey)}: Then & Now`}
				metric={metric}
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
	const {
		data: report,
		isFetching,
		isError,
		error,
		refetch,
	} = useQuery({
		queryKey: ["report", owner.kind, owner.id, reportId],
		queryFn: () => reportService.getReport(owner, reportId),
	});

	useReportOpenedReporter(isError ? undefined : report);

	const reportsPath = reportsPathOf(owner);
	const status = queryStatusOf(isFetching, isError);
	const madeByANewerVersion = isMadeByANewerVersion(error);

	return (
		<Stack spacing={2} data-report-status={status}>
			<ThenAndNowHeader reportsPath={reportsPath} name={report?.name} />
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
					failureMessage={
						madeByANewerVersion ? MADE_BY_A_NEWER_VERSION : COULD_NOT_LOAD
					}
					onRetry={madeByANewerVersion ? undefined : () => refetch()}
				>
					{report !== undefined && (
						<Stack spacing={2}>
							<ReportPanels report={report} />
						</Stack>
					)}
				</WidgetBody>
			</Box>
			{status === "error" && madeByANewerVersion && (
				<Box>
					<Button component={RouterLink} to={reportsPath}>
						Back to Reports
					</Button>
				</Box>
			)}
		</Stack>
	);
};

export default ReportView;
