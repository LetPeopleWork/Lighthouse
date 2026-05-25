import { Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";

type StaleOverviewWidgetProps = {
	readonly staleCount: number;
	readonly title?: string;
};

const StaleOverviewWidget: React.FC<StaleOverviewWidgetProps> = ({
	staleCount,
	title = "Stale Items",
}) => {
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
					{title}
				</Typography>

				<Typography
					variant="h3"
					data-testid="stale-overview-count"
					sx={{ color: theme.palette.primary.main, fontWeight: "bold" }}
				>
					{staleCount}
				</Typography>
			</CardContent>
		</Card>
	);
};

export default StaleOverviewWidget;
