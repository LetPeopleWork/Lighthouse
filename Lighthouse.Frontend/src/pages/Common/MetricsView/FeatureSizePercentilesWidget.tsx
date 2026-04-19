import { Box, Card, CardContent, Typography, useTheme } from "@mui/material";
import type React from "react";
import type { IFeatureSizePercentilesInfo } from "../../../models/Metrics/InfoWidgetData";
import type { TrendPayload } from "./trendTypes";

interface FeatureSizePercentilesWidgetProps {
	readonly data: IFeatureSizePercentilesInfo;
}

const FeatureSizePercentilesWidget: React.FC<FeatureSizePercentilesWidgetProps> & {
	getTrendPayload: (data: IFeatureSizePercentilesInfo) => {
		trendPayload: TrendPayload;
	};
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
					Feature Size Percentiles
				</Typography>

				{data.percentiles.length === 0 ? (
					<Typography variant="body2" color="text.secondary">
						No data
					</Typography>
				) : (
					<Box sx={{ width: "100%" }}>
						{data.percentiles.map((p) => (
							<Box
								key={p.percentile}
								sx={{
									display: "flex",
									justifyContent: "space-between",
									py: 0.5,
								}}
							>
								<Typography variant="body2" color="text.secondary">
									{p.percentile}th
								</Typography>
								<Typography
									variant="body2"
									data-testid={`percentile-row-${p.percentile}`}
									sx={{ color: theme.palette.primary.main, fontWeight: "bold" }}
								>
									{p.value}
								</Typography>
							</Box>
						))}
					</Box>
				)}
			</CardContent>
		</Card>
	);
};

FeatureSizePercentilesWidget.getTrendPayload = (
	data: IFeatureSizePercentilesInfo,
): { trendPayload: TrendPayload } => ({
	trendPayload: data.comparison,
});

export default FeatureSizePercentilesWidget;
