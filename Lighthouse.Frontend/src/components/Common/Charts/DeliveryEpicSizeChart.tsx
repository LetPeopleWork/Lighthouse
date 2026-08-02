import { Card, CardContent, Typography, useTheme } from "@mui/material";
import {
	BarPlot,
	ChartsContainer,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	LinePlot,
	MarkPlot,
} from "@mui/x-charts";
import type { ReactElement, ReactNode } from "react";
import type {
	DeliveryMetricsHistory,
	DeliveryMetricsHistoryPoint,
} from "../../../models/Delivery/DeliveryMetricsHistory";
import { getColorMapForKeys } from "../../../utils/theme/colors";

export interface DeliveryEpicSizeChartProps {
	history: DeliveryMetricsHistory;
	featuresTerm?: string;
	height?: number;
}

const FORWARD_ONLY_EMPTY_STATE =
	"This chart builds forward from today — no snapshots recorded yet.";

const COUNT_AXIS_ID = "count";
const COUNT_AXIS_LABEL = "Count";
const COUNT_SERIES_ID = "epic-count";
const COUNT_DATA_KEY = "epicCount";
const LABEL_DATA_KEY = "label";

const ITEMS_AXIS_ID = "items";
const ITEMS_AXIS_LABEL = "Items";
const ITEMS_STACK_ID = "epic-size";

interface EpicSeriesDescriptor {
	referenceId: string;
	name: string;
}

const sizeDataKey = (referenceId: string): string => `items:${referenceId}`;

const sizesOn = (point: DeliveryMetricsHistoryPoint): Map<string, number> => {
	const sizes = new Map<string, number>();
	for (const metric of point.featureBreakdown) {
		if (metric.totalItems !== null) {
			sizes.set(metric.referenceId, metric.totalItems);
		}
	}
	return sizes;
};

// Sorted so an epic keeps its band and its colour from day to day (DESIGN OQ-4).
const collectSizedEpics = (
	history: DeliveryMetricsHistory,
): EpicSeriesDescriptor[] => {
	const namesByReferenceId = new Map<string, string>();
	for (const point of history.points) {
		for (const metric of point.featureBreakdown) {
			if (metric.totalItems !== null) {
				namesByReferenceId.set(metric.referenceId, metric.name);
			}
		}
	}

	return [...namesByReferenceId]
		.map(([referenceId, name]) => ({ referenceId, name }))
		.sort((left, right) =>
			left.referenceId.localeCompare(right.referenceId, undefined, {
				sensitivity: "base",
			}),
		);
};

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
	const title = `${featuresTerm} over Time`;

	if (history.points.length === 0) {
		return (
			<ChartCard title={title}>
				<Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
					{FORWARD_ONLY_EMPTY_STATE}
				</Typography>
			</ChartCard>
		);
	}

	const epics = collectSizedEpics(history);
	const colorByReferenceId = getColorMapForKeys(
		epics.map((epic) => epic.referenceId),
	);
	const hasSizes = epics.length > 0;

	const dataset = history.points.map((point) => {
		const sizes = sizesOn(point);
		const row: Record<string, string | number | null> = {
			[LABEL_DATA_KEY]: point.date.toLocaleDateString(),
			[COUNT_DATA_KEY]: point.featureBreakdown.length,
		};

		// D7: a departed epic gets an explicit null, so its segments stop instead of vanishing.
		for (const epic of epics) {
			row[sizeDataKey(epic.referenceId)] = sizes.get(epic.referenceId) ?? null;
		}

		return row;
	});

	const sizeSeries = epics.map((epic) => ({
		id: epic.referenceId,
		type: "bar" as const,
		dataKey: sizeDataKey(epic.referenceId),
		label: epic.name,
		yAxisId: ITEMS_AXIS_ID,
		stack: ITEMS_STACK_ID,
		color: colorByReferenceId[epic.referenceId],
	}));

	const itemsAxis = hasSizes
		? [{ id: ITEMS_AXIS_ID, position: "left" as const }]
		: [];

	return (
		<ChartCard title={title}>
			<ChartsContainer
				dataset={dataset}
				xAxis={[{ scaleType: "band", dataKey: LABEL_DATA_KEY }]}
				yAxis={[...itemsAxis, { id: COUNT_AXIS_ID, position: "right" }]}
				series={[
					...sizeSeries,
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
				margin={{ left: 60, right: 60, top: 20, bottom: 80 }}
			>
				<BarPlot />
				<LinePlot />
				<MarkPlot />
				<ChartsXAxis />
				{hasSizes && (
					<ChartsYAxis axisId={ITEMS_AXIS_ID} label={ITEMS_AXIS_LABEL} />
				)}
				<ChartsYAxis axisId={COUNT_AXIS_ID} label={COUNT_AXIS_LABEL} />
				<ChartsTooltip />
			</ChartsContainer>
		</ChartCard>
	);
};

export default DeliveryEpicSizeChart;
