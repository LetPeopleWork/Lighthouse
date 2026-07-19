import { Card, CardContent, CircularProgress, Typography } from "@mui/material";
import type React from "react";
import type { IFlowEfficiencyInfo } from "../../../models/Metrics/FlowEfficiencyInfo";

interface FlowEfficiencyOverviewWidgetProps {
	readonly info: IFlowEfficiencyInfo | null;
}

const FlowEfficiencyOverviewWidget: React.FC<
	FlowEfficiencyOverviewWidgetProps
> = ({ info }) => {
	return (
		<Card sx={{ borderRadius: 2, height: "100%", width: "100%" }}>
			<CardContent
				sx={{
					display: "flex",
					flexDirection: "column",
					alignItems: "center",
					justifyContent: "center",
					height: "100%",
					p: 2,
				}}
			>
				<Typography variant="h6" gutterBottom sx={{ textAlign: "center" }}>
					Flow Efficiency
				</Typography>

				<FlowEfficiencyBody info={info} />
			</CardContent>
		</Card>
	);
};

const FlowEfficiencyBody: React.FC<{
	readonly info: IFlowEfficiencyInfo | null;
}> = ({ info }) => {
	if (info === null) {
		return <CircularProgress />;
	}

	if (!info.isConfigured) {
		return (
			<Typography
				variant="body2"
				color="text.secondary"
				data-testid="flow-efficiency-not-configured"
				sx={{ textAlign: "center" }}
			>
				Not configured — define wait states in settings to measure flow
				efficiency.
			</Typography>
		);
	}

	if (!info.hasDataInScope) {
		return (
			<Typography
				variant="body2"
				color="text.secondary"
				data-testid="flow-efficiency-no-data"
				sx={{ textAlign: "center" }}
			>
				No data in scope — adjust the date range or filter to bring work into
				scope.
			</Typography>
		);
	}

	// The RAG chip rendered by WidgetShell is the single carrier of status. Colouring the number
	// too would state the same thing twice, in two conventions.
	return (
		<Typography
			variant="h3"
			data-testid="flow-efficiency-percent"
			sx={{ fontWeight: "bold" }}
		>
			{info.efficiencyPercent.toFixed(0)}%
		</Typography>
	);
};

export default FlowEfficiencyOverviewWidget;
