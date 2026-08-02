import { Card, CardContent, Typography, useTheme } from "@mui/material";
import {
	ChartsContainer,
	ChartsXAxis,
	ChartsYAxis,
	LinePlot,
	MarkPlot,
} from "@mui/x-charts";
import type { ReactElement, ReactNode } from "react";
import type { DeliveryMetricsHistory } from "../../../models/Delivery/DeliveryMetricsHistory";

export interface DeliveryEpicSizeChartProps {
	history: DeliveryMetricsHistory;
	featuresTerm?: string;
	height?: number;
}

const FORWARD_ONLY_EMPTY_STATE =
	"This chart builds forward from today — no snapshots recorded yet.";

// D3: a day's breakdown only lists what had items, so anything with none is invisible to the count.
const NOT_COUNTED_CAVEAT =
	"Each day counts only what had items recorded — anything with no items that day is left out.";

const COUNT_AXIS_ID = "count";
const COUNT_AXIS_LABEL = "Count";
const COUNT_SERIES_ID = "epic-count";
const COUNT_DATA_KEY = "epicCount";
const LABEL_DATA_KEY = "label";

const ChartCard = ({
	title,
	children,
}: {
	title: string;
	children: ReactNode;
}): ReactElement => (
	<Card
		data-testid="delivery-epic-size-chart"
		sx={{ p: 2, borderRadius: 2, height: "100%" }}
	>
		<CardContent
			sx={{ height: "100%", display: "flex", flexDirection: "column" }}
		>
			<Typography variant="h6">{title}</Typography>
			{children}
		</CardContent>
	</Card>
);

const DeliveryEpicSizeChart = ({
	history,
	featuresTerm = "Epics",
	height = 320,
}: DeliveryEpicSizeChartProps): ReactElement => {
	const theme = useTheme();
	const title = `${featuresTerm} Size & Count`;

	if (history.points.length === 0) {
		return (
			<ChartCard title={title}>
				<Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
					{FORWARD_ONLY_EMPTY_STATE}
				</Typography>
			</ChartCard>
		);
	}

	const dataset = history.points.map((point) => ({
		[LABEL_DATA_KEY]: point.date.toLocaleDateString(),
		[COUNT_DATA_KEY]: point.featureBreakdown.length,
	}));

	return (
		<ChartCard title={title}>
			<ChartsContainer
				dataset={dataset}
				xAxis={[{ scaleType: "band", dataKey: LABEL_DATA_KEY }]}
				yAxis={[{ id: COUNT_AXIS_ID, position: "right" }]}
				series={[
					{
						id: COUNT_SERIES_ID,
						type: "line",
						dataKey: COUNT_DATA_KEY,
						label: COUNT_AXIS_LABEL,
						yAxisId: COUNT_AXIS_ID,
						color: theme.palette.primary.main,
					},
				]}
				height={height}
				margin={{ left: 20, right: 60, top: 20, bottom: 80 }}
			>
				<LinePlot />
				<MarkPlot />
				<ChartsXAxis />
				<ChartsYAxis axisId={COUNT_AXIS_ID} label={COUNT_AXIS_LABEL} />
			</ChartsContainer>
			<Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
				{NOT_COUNTED_CAVEAT}
			</Typography>
		</ChartCard>
	);
};

export default DeliveryEpicSizeChart;
