import { Box, Card, CardContent, Typography } from "@mui/material";
import { useState } from "react";
import PredictabilityScore from "../../../components/Common/Charts/PredictabilityScore";
import ThroughputChartFilterToggle from "../../../components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle";
import type { MetricsOwnerRequest } from "../../../hooks/useMetricsData";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { useFilteredView } from "./useFilteredView";
import { useReportWidgetStatus } from "./widgetStatus";

interface PredictabilityScoreDetailsWidgetProps<T extends IWorkItem> {
	readonly predictabilityData: IForecastPredictabilityScore | null;
	readonly owner?: MetricsOwnerRequest;
	readonly metricsService?: IMetricsService<T>;
	readonly startDate?: Date;
	readonly endDate?: Date;
	readonly isPremium?: boolean;
	readonly hasForecastFilter?: boolean;
}

const PredictabilityScoreDetailsWidget = <T extends IWorkItem>({
	predictabilityData,
	owner,
	metricsService,
	startDate,
	endDate,
	isPremium = false,
	hasForecastFilter = false,
}: PredictabilityScoreDetailsWidgetProps<T>) => {
	const [filtered, setFiltered] = useState(false);

	const canRefetch =
		owner !== undefined &&
		metricsService !== undefined &&
		startDate !== undefined &&
		endDate !== undefined;

	const filteredScore = useFilteredView({
		fetchName: "getMultiItemForecastPredictabilityScore",
		owner,
		startDate,
		endDate,
		filtered,
		canAsk: canRefetch,
		ask: async () => {
			if (!canRefetch) return null;
			return metricsService.getMultiItemForecastPredictabilityScore(
				owner.ownerId,
				startDate,
				endDate,
				"filtered",
			);
		},
		failureMessage: "Error fetching filtered predictability score:",
	});

	const displayData =
		filtered && filteredScore.answer
			? filteredScore.answer
			: predictabilityData;

	// Until the filtered answer lands the widget keeps showing whichever score it already has, and
	// that is what the frame dims.
	useReportWidgetStatus(filteredScore.status, !!displayData);

	return (
		<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Box
					sx={{
						display: "flex",
						flexDirection: "row",
						justifyContent: "space-between",
						alignItems: "center",
						mb: 2,
					}}
				>
					<Typography variant="h6">Predictability Score</Typography>
					{canRefetch && (
						<ThroughputChartFilterToggle
							isPremium={isPremium}
							hasFilter={hasForecastFilter}
							onChange={setFiltered}
						/>
					)}
				</Box>
				<Box sx={{ flex: 1, width: "100%" }}>
					{displayData ? (
						<PredictabilityScore data={displayData} title="" />
					) : (
						<Typography variant="body2">No data available</Typography>
					)}
				</Box>
			</CardContent>
		</Card>
	);
};

export default PredictabilityScoreDetailsWidget;
