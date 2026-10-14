import { Box, Button, Stack } from "@mui/material";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type React from "react";
import { useContext } from "react";
import { Link as RouterLink, useNavigate } from "react-router";
import type {
	IReport,
	IReportMetric,
	IReportOwner,
} from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiError } from "../../../services/Api/ApiError";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { reportFailedNavigation } from "../../../utils/navigation";
import { WidgetBody } from "../MetricsView/WidgetShell";
import { queryStatusOf } from "../MetricsView/widgetStatus";
import {
	inCatalogOrder,
	metricNameTermKeyOf,
	panelFor,
} from "./panels/reportPanelRegistry";
import UnsupportedMetricPanel from "./panels/UnsupportedMetricPanel";
import { reportsPathOf } from "./reportOwner";
import ThenAndNowHeader from "./ThenAndNowHeader";
import ThenAndNowSummary from "./ThenAndNowSummary";
import { useMayEditReports } from "./useMayEditReports";
import { useReportDeletion } from "./useReportDeletion";
import { useReportOpenedReporter } from "./useReportOpenedReporter";

const LOADING = "Loading report…";
const COULD_NOT_LOAD = "This report couldn't be loaded. Reload to try again.";
const MADE_BY_A_NEWER_VERSION =
	"This report was made by a newer version of Lighthouse and can't be shown here.";
const NO_LONGER_EXISTS = "This report no longer exists.";
const BODY_MIN_HEIGHT = 120;

/** A metric shown on a report made before it was captured reads as not captured on both sides. */
const NOTHING_CAPTURED: IReportMetric = { atThen: {}, atNow: {} };

interface ReportViewProps {
	owner: IReportOwner;
	reportId: number;
}

const isMadeByANewerVersion = (error: Error | null): boolean =>
	error instanceof ApiError &&
	error.code === 422 &&
	error.problemCode === "template-not-supported";

/** What to say about an answer that asking again will not change; nothing for one that might. */
const finalFailureOf = (error: Error | null): string | undefined => {
	if (isMadeByANewerVersion(error)) return MADE_BY_A_NEWER_VERSION;
	if (error instanceof ApiError && error.code === 404) return NO_LONGER_EXISTS;
	return undefined;
};

const ANSWERS_THAT_DO_NOT_CHANGE = new Set<string | number>([404, 422]);

// A report that is gone, or made by a newer version, stays that way; asking again only delays saying so.
const retriesUnlessFinal =
	(usualRetry: unknown) =>
	(failureCount: number, error: Error): boolean => {
		if (error instanceof ApiError && ANSWERS_THAT_DO_NOT_CHANGE.has(error.code))
			return false;
		return typeof usualRetry === "number"
			? failureCount < usualRetry
			: usualRetry === true;
	};

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
		queryKey: ["report", owner.kind, owner.id, reportId],
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
	const status = queryStatusOf(isFetching, isError);
	const finalFailure = finalFailureOf(error);

	return (
		<Stack spacing={2} data-report-status={status}>
			<ThenAndNowHeader
				reportsPath={reportsPath}
				name={report?.name}
				onDelete={
					isEditor && report !== undefined
						? () => deletion.ask(report)
						: undefined
				}
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
					failureMessage={finalFailure ?? COULD_NOT_LOAD}
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
		</Stack>
	);
};

export default ReportView;
