import { Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";

type WipOverviewWidgetProps = {
	readonly wipCount: number;
	readonly systemWipLimit?: number;
	readonly title?: string;
};

const WipOverviewWidget: React.FC<WipOverviewWidgetProps> = ({
	wipCount,
	systemWipLimit,
	title = "In Progress",
}) => {
	const theme = useTheme();
	const hasLimit = systemWipLimit != null && systemWipLimit > 0;

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
			</CardContent>
		</Card>
	);
};

export default WipOverviewWidget;
