import { Box, Card, CardContent, Typography } from "@mui/material";
import PredictabilityScore from "../../../components/Common/Charts/PredictabilityScore";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";

interface PredictabilityScoreDetailsWidgetProps {
	predictabilityData: IForecastPredictabilityScore | null;
}

const PredictabilityScoreDetailsWidget: React.FC<
	PredictabilityScoreDetailsWidgetProps
> = ({ predictabilityData }) => {
	return (
		<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Typography variant="h6" sx={{ mb: 2 }}>
					Predictability Score
				</Typography>
				<Box sx={{ flex: 1, width: "100%" }}>
					{predictabilityData ? (
						<PredictabilityScore data={predictabilityData} title="" />
					) : (
						<Typography variant="body2">No data available</Typography>
					)}
				</Box>
			</CardContent>
		</Card>
	);
};

export default PredictabilityScoreDetailsWidget;
