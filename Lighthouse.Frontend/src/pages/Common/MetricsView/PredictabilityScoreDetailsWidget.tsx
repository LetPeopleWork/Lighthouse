import { Box, Card, CardContent, Typography } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import PredictabilityScore from "../../../components/Common/Charts/PredictabilityScore";
import ThroughputChartFilterToggle from "../../../components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle";
import { metricsQueryOptions } from "../../../hooks/useMetricsData";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { formatLocalDate } from "../../../utils/date/localDate";
import { fetchKeyStateOf, useReportWidgetStatus } from "./widgetStatus";

interface PredictabilityScoreDetailsWidgetProps<T extends IWorkItem> {
	readonly predictabilityData: IForecastPredictabilityScore | null;
	readonly entityId?: number;
	readonly metricsService?: IMetricsService<T>;
	readonly startDate?: Date;
	readonly endDate?: Date;
	readonly isPremium?: boolean;
	readonly hasForecastFilter?: boolean;
}

const PredictabilityScoreDetailsWidget = <T extends IWorkItem>({
	predictabilityData,
	entityId,
	metricsService,
	startDate,
	endDate,
	isPremium = false,
	hasForecastFilter = false,
}: PredictabilityScoreDetailsWidgetProps<T>) => {
	const [filtered, setFiltered] = useState(false);

	const canRefetch =
		entityId !== undefined &&
		metricsService !== undefined &&
		startDate !== undefined &&
		endDate !== undefined;

	const filteredScore = useQuery({
		...metricsQueryOptions,
		queryKey: [
			"metrics",
			"getMultiItemForecastPredictabilityScore",
			{
				view: "filtered",
				ownerId: entityId,
				from: startDate ? formatLocalDate(startDate) : null,
				to: endDate ? formatLocalDate(endDate) : null,
			},
		] as const,
		queryFn: async () => {
			if (!canRefetch) return null;
			try {
				return await metricsService.getMultiItemForecastPredictabilityScore(
					entityId,
					startDate,
					endDate,
					"filtered",
				);
			} catch (error) {
				console.error("Error fetching filtered predictability score:", error);
				throw error;
			}
		},
		// Turning the filter off and on again shows the answer this window already has, rather than
		// asking again; a window the filter has never been on for is never asked.
		enabled: (query) =>
			canRefetch && (filtered || query.state.data !== undefined),
	});

	// With the filter off the widget shows the page's own score, so a refetch of the filtered answer
	// it keeps for later holds nothing back.
	useReportWidgetStatus(fetchKeyStateOf([filteredScore], filtered).status);

	const displayData =
		filtered && filteredScore.data ? filteredScore.data : predictabilityData;

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
