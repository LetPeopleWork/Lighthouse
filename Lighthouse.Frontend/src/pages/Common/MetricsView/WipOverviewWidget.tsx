import { Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";
import type { SleRiskAtRiskSummary } from "../../../utils/charts/sleRisk";

type WipOverviewWidgetProps = {
	readonly wipCount: number;
	readonly systemWipLimit?: number;
	readonly title?: string;
	/**
	 * How much of this work is more likely than not to miss the team's target. Absent when the team
	 * published no target, in which case nothing here changes.
	 */
	readonly atRisk?: SleRiskAtRiskSummary;
};

const WipOverviewWidget: React.FC<WipOverviewWidgetProps> = ({
	wipCount,
	systemWipLimit,
	title = "In Progress",
	atRisk,
}) => {
	const theme = useTheme();
	const hasLimit = systemWipLimit != null && systemWipLimit > 0;
	// Never "0 at risk": a line that is always there stops being read, and the plain count above
	// already says that nothing is wrong.
	const hasRisk = atRisk != null && atRisk.count > 0;

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
					{title}
				</Typography>

				<Typography
					variant="h3"
					data-testid="wip-overview-count"
					sx={{ color: theme.palette.primary.main, fontWeight: "bold" }}
				>
					{wipCount}
				</Typography>

				{hasLimit && (
					<Typography
						variant="body2"
						color="text.secondary"
						data-testid="wip-overview-limit"
						sx={{ mt: 0.5 }}
					>
						Limit: {systemWipLimit}
					</Typography>
				)}

				{hasRisk && (
					<Typography
						variant="body2"
						data-testid="wip-overview-at-risk"
						sx={{ mt: 0.5, fontWeight: 600 }}
						style={{ color: atRisk.color }}
					>
						{atRisk.count} at risk
					</Typography>
				)}
			</CardContent>
		</Card>
	);
};

export default WipOverviewWidget;
