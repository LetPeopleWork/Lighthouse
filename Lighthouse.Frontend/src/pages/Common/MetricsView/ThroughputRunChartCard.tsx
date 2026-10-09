import { Box } from "@mui/material";
import { useState } from "react";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import ThroughputChartFilterToggle from "../../../components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle";
import type { MetricsOwnerRequest } from "../../../hooks/useMetricsData";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";
import { useFilteredView } from "./useFilteredView";
import { useReportWidgetStatus } from "./widgetStatus";

interface ThroughputRunChartCardProps<T extends IWorkItem> {
	readonly owner: MetricsOwnerRequest;
	readonly metricsService: IMetricsService<T>;
	readonly startDate: Date;
	readonly endDate: Date;
	readonly rawData: RunChartData;
	readonly title: string;
	readonly isPremium: boolean;
	readonly hasForecastFilter: boolean;
}

const ThroughputRunChartCard = <T extends IWorkItem>({
	owner,
	metricsService,
	startDate,
	endDate,
	rawData,
	title,
	isPremium,
	hasForecastFilter,
}: ThroughputRunChartCardProps<T>) => {
	const [filtered, setFiltered] = useState(false);

	const filteredSeries = useFilteredView({
		fetchName: "getThroughput",
		owner,
		startDate,
		endDate,
		filtered,
		ask: () =>
			metricsService.getThroughput(
				owner.ownerId,
				startDate,
				endDate,
				"filtered",
			),
		failureMessage: "Error fetching filtered throughput:",
	});

	// Until the filtered answer lands the chart keeps showing a series, the page's own or the
	// previous window's, so there is always something to dim.
	useReportWidgetStatus(filteredSeries.status, true);

	const displayData =
		filtered && filteredSeries.answer ? filteredSeries.answer : rawData;

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
