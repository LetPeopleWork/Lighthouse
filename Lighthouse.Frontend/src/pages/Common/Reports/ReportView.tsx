import { Stack } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useContext } from "react";
import type { IReport, IReportOwner } from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { panelFor } from "./panels/reportPanelRegistry";
import ThenAndNowHeader from "./ThenAndNowHeader";

interface ReportViewProps {
	owner: IReportOwner;
	reportId: number;
}

type ReportStatus = "loading" | "error" | "ready";

function statusOf(hasReport: boolean, isError: boolean): ReportStatus {
	if (isError) return "error";
	return hasReport ? "ready" : "loading";
}

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
	const { data: report, isError } = useQuery({
		queryKey: ["report", owner.kind, owner.id, reportId],
		queryFn: () => reportService.getReport(owner, reportId),
	});

	return (
		<Stack
			spacing={2}
			data-report-status={statusOf(report !== undefined, isError)}
		>
			<ThenAndNowHeader
				reportsPath={`/${owner.kind}s/${owner.id}/reports`}
				name={report?.name}
			/>
			{report !== undefined && <ReportPanels report={report} />}
		</Stack>
	);
};

export default ReportView;
