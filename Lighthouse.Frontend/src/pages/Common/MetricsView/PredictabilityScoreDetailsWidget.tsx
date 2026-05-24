import { Box, Card, CardContent, Typography } from "@mui/material";
import { useCallback, useState } from "react";
import PredictabilityScore from "../../../components/Common/Charts/PredictabilityScore";
import ThroughputChartFilterToggle from "../../../components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle";
import type { IForecastPredictabilityScore } from "../../../models/Forecasts/ForecastPredictabilityScore";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";

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
	const [filteredData, setFilteredData] =
		useState<IForecastPredictabilityScore | null>(null);

	const canRefetch =
		entityId !== undefined &&
		metricsService !== undefined &&
		startDate !== undefined &&
		endDate !== undefined;

	const handleChange = useCallback(
		async (next: boolean) => {
			setFiltered(next);
			if (next && !filteredData && canRefetch) {
				try {
					const data =
						await metricsService.getMultiItemForecastPredictabilityScore(
							entityId,
							startDate,
							endDate,
							"filtered",
						);
					setFilteredData(data);
				} catch (error) {
					console.error("Error fetching filtered predictability score:", error);
				}
			}
		},
		[entityId, metricsService, startDate, endDate, filteredData, canRefetch],
	);

	const displayData =
		filtered && filteredData ? filteredData : predictabilityData;

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
							onChange={handleChange}
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
