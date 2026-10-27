import { Box, Container, Typography } from "@mui/material";
import type React from "react";
import { WidgetBody } from "../Common/MetricsView/WidgetShell";
import { queryStatusOf } from "../Common/MetricsView/widgetStatus";
import ReportList from "../Common/Reports/ReportList";
import { useAllReports } from "./useAllReports";

const COULD_NOT_LOAD = "The reports couldn't be loaded.";
const LIST_MIN_HEIGHT = 120;

const ReportsPage: React.FC = () => {
	const { data, isFetching, isError, refetch } = useAllReports();
	const status = queryStatusOf(isFetching, isError);

	return (
		<Container maxWidth={false}>
			<Typography variant="h4" component="h1" sx={{ my: 2 }}>
				Reports
			</Typography>
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
					{data !== undefined && (
						<ReportList reports={data.reports} showOwner />
					)}
				</WidgetBody>
			</Box>
		</Container>
	);
};

export default ReportsPage;
