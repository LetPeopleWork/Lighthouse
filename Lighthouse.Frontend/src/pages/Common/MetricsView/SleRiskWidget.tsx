import { Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";
import type { SleRiskAtRiskSummary } from "../../../utils/charts/sleRisk";

type SleRiskWidgetProps = {
	/**
	 * How much of the team's in-flight work is at risk. Absent when the team published no target —
	 * there is no promise for anything to be at risk of breaking, and the widget's status says so
	 * rather than the count pretending to be zero.
	 */
	readonly atRisk?: SleRiskAtRiskSummary;
	readonly title?: string;
};

const SleRiskWidget: React.FC<SleRiskWidgetProps> = ({
	atRisk,
	title = "At Risk",
}) => {
	const theme = useTheme();

	// A team with no target gets a dash rather than a zero. Zero is a measurement and would read as
	// good news; there is nothing here to measure until a target exists.
	const hasAnswer = atRisk != null;

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
					data-testid="sle-risk-count"
					sx={{ fontWeight: "bold" }}
					// The colour of the worst item counted, in the palette the dialog column and the
					// chart already use — so a reader who has learned it once has learned it here.
					// Falls back to the theme's own when nothing is at risk and there is no worst item.
					style={{ color: atRisk?.color ?? theme.palette.primary.main }}
				>
					{hasAnswer ? atRisk.count : "—"}
				</Typography>
			</CardContent>
		</Card>
	);
};

export default SleRiskWidget;
