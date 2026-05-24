import { Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";
import type { IThroughputInfo } from "../../../models/Metrics/InfoWidgetData";
import type { TrendPayload } from "./trendTypes";

interface TotalThroughputWidgetProps {
	readonly data: IThroughputInfo;
}

const TotalThroughputWidget: React.FC<TotalThroughputWidgetProps> & {
	getTrendPayload: (data: IThroughputInfo) => { trendPayload: TrendPayload };
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
					Total Throughput
				</Typography>

				<Typography
					variant="h3"
					data-testid="throughput-info-total"
					sx={{ color: theme.palette.primary.main, fontWeight: "bold" }}
				>
					{data.total}
				</Typography>

				<Typography
					variant="body2"
					color="text.secondary"
					data-testid="throughput-info-average"
					sx={{ mt: 0.5 }}
				>
					{data.dailyAverage.toFixed(1)} / day
				</Typography>
			</CardContent>
		</Card>
	);
};

TotalThroughputWidget.getTrendPayload = (
	data: IThroughputInfo,
): { trendPayload: TrendPayload } => ({
	trendPayload: data.comparison,
});

export default TotalThroughputWidget;
