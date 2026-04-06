import {
	Card,
	CardContent,
	CircularProgress,
	Typography,
	useTheme,
} from "@mui/material";
import type React from "react";

type PredictabilityScoreOverviewWidgetProps = {
	readonly score: number | null;
};

const PredictabilityScoreOverviewWidget: React.FC<
	PredictabilityScoreOverviewWidgetProps
> = ({ score }) => {
	const theme = useTheme();

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
					Predictability Score
				</Typography>

				{score === null ? (
					<CircularProgress />
				) : (
					<Typography
						variant="h3"
						data-testid="predictability-score-value"
						sx={{ color: theme.palette.primary.main, fontWeight: "bold" }}
					>
						{Math.round(score * 100)}%
					</Typography>
				)}
			</CardContent>
		</Card>
	);
};

export default PredictabilityScoreOverviewWidget;
