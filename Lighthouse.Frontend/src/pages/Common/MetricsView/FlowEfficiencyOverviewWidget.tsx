import { Card, CardContent, CircularProgress, Typography } from "@mui/material";
import type React from "react";
import type { IFlowEfficiencyInfo } from "../../../models/Metrics/FlowEfficiencyInfo";
import { computeFlowEfficiencyRag, type RagTerms } from "./ragRules";

interface FlowEfficiencyOverviewWidgetProps {
	readonly info: IFlowEfficiencyInfo | null;
}

const ragColorMap: Record<"red" | "amber" | "green", string> = {
	red: "#d32f2f",
	amber: "#ed6c02",
	green: "#2e7d32",
};

const efficiencyRagTerms: RagTerms = {
	workItem: "work item",
	workItems: "work items",
	feature: "feature",
	features: "features",
	cycleTime: "cycle time",
	throughput: "throughput",
	wip: "WIP",
	workItemAge: "work item age",
	blocked: "blocked",
	sle: "SLE",
};

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

	const rag = computeFlowEfficiencyRag(
		info.efficiencyPercent,
		efficiencyRagTerms,
	);

	return (
		<Typography
			variant="h3"
			data-testid="flow-efficiency-percent"
			sx={{ color: ragColorMap[rag.ragStatus], fontWeight: "bold" }}
		>
			{info.efficiencyPercent.toFixed(0)}%
		</Typography>
	);
};

export default FlowEfficiencyOverviewWidget;
