import { Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";
import type { IArrivalsInfo } from "../../../models/Metrics/InfoWidgetData";
import type { TrendPayload } from "./trendTypes";

interface TotalArrivalsWidgetProps {
	readonly data: IArrivalsInfo;
}

const TotalArrivalsWidget: React.FC<TotalArrivalsWidgetProps> & {
	getTrendPayload: (data: IArrivalsInfo) => { trendPayload: TrendPayload };
} = ({ data }) => {
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
					Total Arrivals
				</Typography>

				<Typography
					variant="h3"
					data-testid="arrivals-info-total"
					sx={{ color: theme.palette.primary.main, fontWeight: "bold" }}
				>
					{data.total}
				</Typography>

				<Typography
					variant="body2"
					color="text.secondary"
					data-testid="arrivals-info-average"
					sx={{ mt: 0.5 }}
				>
					{data.dailyAverage.toFixed(1)} / day
				</Typography>
			</CardContent>
		</Card>
	);
};

TotalArrivalsWidget.getTrendPayload = (
	data: IArrivalsInfo,
): { trendPayload: TrendPayload } => ({
	trendPayload: data.comparison,
});

export default TotalArrivalsWidget;
