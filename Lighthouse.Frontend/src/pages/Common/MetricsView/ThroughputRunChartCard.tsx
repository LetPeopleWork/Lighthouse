import { Box } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import ThroughputChartFilterToggle from "../../../components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle";
import { metricsQueryOptions } from "../../../hooks/useMetricsData";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { formatLocalDate } from "../../../utils/date/localDate";
import { fetchKeyStateOf, useReportWidgetStatus } from "./widgetStatus";

interface ThroughputRunChartCardProps<T extends IWorkItem> {
	readonly entityId: number;
	readonly metricsService: IMetricsService<T>;
	readonly startDate: Date;
	readonly endDate: Date;
	readonly rawData: RunChartData;
	readonly title: string;
	readonly isPremium: boolean;
	readonly hasForecastFilter: boolean;
}

const ThroughputRunChartCard = <T extends IWorkItem>({
	entityId,
	metricsService,
	startDate,
	endDate,
	rawData,
	title,
	isPremium,
	hasForecastFilter,
}: ThroughputRunChartCardProps<T>) => {
	const [filtered, setFiltered] = useState(false);

	const filteredSeries = useQuery({
		...metricsQueryOptions,
		queryKey: [
			"metrics",
			"getThroughput",
			{
				view: "filtered",
				ownerId: entityId,
				from: formatLocalDate(startDate),
				to: formatLocalDate(endDate),
			},
		] as const,
		queryFn: async () => {
			try {
				return await metricsService.getThroughput(
					entityId,
					startDate,
					endDate,
					"filtered",
				);
			} catch (error) {
				console.error("Error fetching filtered throughput:", error);
				throw error;
			}
		},
		// Turning the filter off and on again shows the answer this window already has, rather than
		// asking again; a window the filter has never been on for is never asked.
		enabled: (query) => filtered || query.state.data !== undefined,
	});

	// With the filter off the chart shows the page's own series, so a refetch of the filtered answer
	// it keeps for later holds nothing back. Until the filtered answer lands the chart keeps showing
	// a series, the page's own or the previous window's, so there is always something to dim.
	useReportWidgetStatus(
		fetchKeyStateOf([filteredSeries], filtered).status,
		true,
	);

	const displayData =
		filtered && filteredSeries.data ? filteredSeries.data : rawData;

	return (
		<Box sx={{ height: "100%", position: "relative" }}>
			<BarRunChart
				title={title}
				startDate={startDate}
				chartData={displayData}
				displayTotal={true}
				filterToggle={
					<ThroughputChartFilterToggle
						isPremium={isPremium}
						hasFilter={hasForecastFilter}
						onChange={setFiltered}
					/>
				}
			/>
		</Box>
	);
};

export default ThroughputRunChartCard;
