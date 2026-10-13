import AddIcon from "@mui/icons-material/Add";
import { Box, Button, Stack, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useContext, useState } from "react";
import { useRbac } from "../../../hooks/useRbac";
import type {
	IReportListItem,
	IReportOwner,
} from "../../../models/Reports/Report";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { WidgetBody } from "../MetricsView/WidgetShell";
import type { WidgetStatus } from "../MetricsView/widgetStatus";
import ReportList from "./ReportList";
import ReportWizard from "./wizard/ReportWizard";

const LIST_COULD_NOT_LOAD = "Reports couldn't be loaded. Reload to try again.";
const NO_REPORTS = "No reports yet.";
const LIST_MIN_HEIGHT = 120;

interface ReportsTabProps {
	owner: IReportOwner;
}

function statusOf(isFetching: boolean, isError: boolean): WidgetStatus {
	if (isFetching) return "loading";
	return isError ? "error" : "ready";
}

const ListOrEmpty: React.FC<{
	readonly owner: IReportOwner;
	readonly reports: IReportListItem[];
	readonly emptyMessage: string;
}> = ({ owner, reports, emptyMessage }) =>
	reports.length === 0 ? (
		<Typography color="text.secondary">{emptyMessage}</Typography>
	) : (
		<ReportList owner={owner} reports={reports} />
	);

const ReportsTab: React.FC<Readonly<ReportsTabProps>> = ({ owner }) => {
	const { reportService } = useContext(ApiServiceContext);
	const rbac = useRbac();
	const { getTerm } = useTerminology();
	const [isCreating, setIsCreating] = useState(false);

	const { data, isFetching, isError, refetch } = useQuery({
		queryKey: ["reports", owner.kind, owner.id],
		queryFn: () => reportService.getReports(owner),
	});

	const isEditor =
		!rbac.isLoading &&
		(owner.kind === "team"
			? rbac.isTeamAdmin(owner.id)
			: rbac.isPortfolioAdmin(owner.id));

	const status = statusOf(isFetching, isError);
	const ownerTerm = getTerm(
		owner.kind === "team" ? TERMINOLOGY_KEYS.TEAM : TERMINOLOGY_KEYS.PORTFOLIO,
	);
	const emptyMessage =
		isEditor || rbac.isLoading
			? NO_REPORTS
			: `${NO_REPORTS} A ${ownerTerm} Admin can create one.`;

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
						/>
					)}
				</WidgetBody>
			</Box>
		</Stack>
	);
};

export default ReportsTab;
