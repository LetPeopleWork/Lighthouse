import { Box } from "@mui/material";
import { useCallback, useState } from "react";
import BarRunChart from "../../../components/Common/Charts/BarRunChart";
import ThroughputChartFilterToggle from "../../../components/Common/Charts/ThroughputChart/ThroughputChartFilterToggle";
import type { RunChartData } from "../../../models/Metrics/RunChartData";
import type { IWorkItem } from "../../../models/WorkItem";
import type { IMetricsService } from "../../../services/Api/MetricsService";

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
	const [filteredData, setFilteredData] = useState<RunChartData | null>(null);

	const handleChange = useCallback(
		async (next: boolean) => {
			setFiltered(next);
			if (next && !filteredData) {
				try {
					const data = await metricsService.getThroughput(
						entityId,
						startDate,
						endDate,
						"filtered",
					);
					setFilteredData(data);
				} catch (error) {
					console.error("Error fetching filtered throughput:", error);
				}
			}
		},
		[entityId, metricsService, startDate, endDate, filteredData],
	);

	const displayData = filtered && filteredData ? filteredData : rawData;

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
						onChange={handleChange}
					/>
				}
			/>
		</Box>
	);
};

export default ThroughputRunChartCard;
