import {
	Card,
	CardContent,
	ToggleButton,
	ToggleButtonGroup,
	Typography,
	useTheme,
} from "@mui/material";
import { ChartsReferenceLine } from "@mui/x-charts";
import { LineChart } from "@mui/x-charts/LineChart";
import { type ReactElement, useState } from "react";
import type {
	DeliveryMetricsHistory,
	DeliveryMetricsHistoryPoint,
} from "../../../models/Delivery/DeliveryMetricsHistory";
import {
	steppedTargetData,
	type TargetChange,
	targetChanges,
} from "../../../models/Delivery/deliveryTargetHistory";
import {
	certainColor,
	confidentColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";
import { FORECAST_LEVEL_THRESHOLDS } from "../Forecasts/ForecastLevel";

interface DeliveryPredictabilityChartProps {
	history: DeliveryMetricsHistory;
	title?: string;
	height?: number;
}

type PredictabilityView = "likelihood" | "when";

const FORWARD_ONLY_EMPTY_STATE =
	"This chart builds forward from today — no snapshots recorded yet.";

const LIKELIHOOD_SERIES_ID = "likelihood";
const LIKELIHOOD_SERIES_LABEL = "Likelihood";

const TARGET_CHANGE_SERIES_ID = "target-change";
const TARGET_CHANGE_SERIES_LABEL = "Target moved";

const RAG_BAND_COLORS = [
	riskyColor,
	realisticColor,
	confidentColor,
	certainColor,
];

const WHEN_PERCENTILES = [50, 70, 85, 95];
const DEFAULT_WHEN_PERCENTILE = 70;
const TARGET_LINE_DASH = "6 4";

const TARGET_SERIES_ID = "target";
const TARGET_SERIES_LABEL = "Delivery Target";
const TARGET_STEP_SX = {
	[`& .MuiLineChart-line[data-series="${TARGET_SERIES_ID}"]`]: {
		strokeDasharray: TARGET_LINE_DASH,
		strokeWidth: 2,
	},
};

const WHEN_PALETTE: Record<number, string> = {
	50: riskyColor,
	70: realisticColor,
	85: confidentColor,
	95: certainColor,
};

const formatDate = (date: Date): string => date.toLocaleDateString();

const formatPercentage = (value: number | null): string =>
	value === null ? "" : `${value}%`;

const formatDateValue = (value: number | null): string =>
	value === null ? "" : new Date(value).toLocaleDateString();

const formatTargetMove = (change: TargetChange): string =>
	`Target moved: ${formatDate(change.previousTarget)} → ${formatDate(change.newTarget)}`;

const hasLikelihood = (point: DeliveryMetricsHistoryPoint): boolean =>
	point.likelihoodPercentage !== null;

const hasWhenDistribution = (point: DeliveryMetricsHistoryPoint): boolean =>
	point.whenDistribution !== null && point.whenDistribution.length > 0;

const completionDateAt = (
	point: DeliveryMetricsHistoryPoint,
	percentile: number,
): number | null => {
	const match = point.whenDistribution?.find(
		(entry) => entry.probability === percentile,
	);
	return match ? match.expectedDate.getTime() : null;
};

interface WhenSeriesConfig {
	id: string;
	label: string;
	data: Array<number | null>;
	color: string;
	emphasized: boolean;
}

const buildWhenSeries = (
	points: DeliveryMetricsHistoryPoint[],
	palette: Record<number, string>,
): WhenSeriesConfig[] =>
	WHEN_PERCENTILES.map((percentile) => ({
		id: `when-${percentile}`,
		label: `${percentile}%`,
		data: points.map((point) => completionDateAt(point, percentile)),
		color: palette[percentile],
		emphasized: percentile === DEFAULT_WHEN_PERCENTILE,
	}));

const EmptyBody = (): ReactElement => (
	<Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
		{FORWARD_ONLY_EMPTY_STATE}
	</Typography>
);

const LikelihoodView = ({
	points,
	lineColor,
	changeColor,
	height,
}: {
	points: DeliveryMetricsHistoryPoint[];
	lineColor: string;
	changeColor: string;
	height: number;
}): ReactElement => {
	const changes = targetChanges(points);
	const changeByIndex = new Map(
		changes.map((change) => [change.index, change]),
	);

	const likelihoodSeries = {
		id: LIKELIHOOD_SERIES_ID,
		label: LIKELIHOOD_SERIES_LABEL,
		data: points.map((point) => point.likelihoodPercentage),
		showMark: true,
		color: lineColor,
		valueFormatter: (value: number | null): string => formatPercentage(value),
	};

	const changeSeries = {
		id: TARGET_CHANGE_SERIES_ID,
		label: TARGET_CHANGE_SERIES_LABEL,
		data: points.map((point, index) =>
			changeByIndex.has(index) ? point.likelihoodPercentage : null,
		),
		showMark: true,
		color: changeColor,
		valueFormatter: (
			_value: number | null,
			context: { dataIndex: number },
		): string => {
			const change = changeByIndex.get(context.dataIndex);
			return change ? formatTargetMove(change) : "";
		},
	};

	const series =
		changes.length === 0
			? [likelihoodSeries]
			: [likelihoodSeries, changeSeries];

	return (
		<LineChart
			xAxis={[
				{
					scaleType: "time",
					data: points.map((point) => point.date),
					label: "Date",
					height: 56,
					valueFormatter: formatDate,
				},
			]}
			yAxis={[
				{
					min: 0,
					max: 100,
					label: "Likelihood (%)",
					colorMap: {
						type: "piecewise",
						thresholds: [...FORECAST_LEVEL_THRESHOLDS],
						colors: RAG_BAND_COLORS,
					},
				},
			]}
			series={series}
			height={height}
			slotProps={{
				legend: {
					direction: "horizontal",
					position: { vertical: "top", horizontal: "end" },
				},
			}}
		/>
	);
};

const WhenView = ({
	history,
	palette,
	targetColor,
	height,
}: {
	history: DeliveryMetricsHistory;
	palette: Record<number, string>;
	targetColor: string;
	height: number;
}): ReactElement => {
	const points = history.points;
	const whenSeries = buildWhenSeries(points, palette);
	const targetData = steppedTargetData(points);

	const percentileSeries = whenSeries.map((entry) => ({
		id: entry.id,
		label: entry.label,
		data: entry.data,
		showMark: entry.emphasized,
		color: entry.color,
		valueFormatter: formatDateValue,
	}));

	const series =
		targetData === null
			? percentileSeries
			: [
					...percentileSeries,
					{
						id: TARGET_SERIES_ID,
						label: TARGET_SERIES_LABEL,
						data: targetData,
						showMark: false,
						color: targetColor,
						curve: "stepAfter" as const,
						valueFormatter: formatDateValue,
					},
				];

	return (
		<LineChart
			xAxis={[
				{
					scaleType: "time",
					data: points.map((point) => point.date),
					label: "Date",
					height: 56,
					valueFormatter: formatDate,
				},
			]}
			yAxis={[
				{
					scaleType: "time",
					label: "Forecast completion date",
					valueFormatter: formatDateValue,
				},
			]}
			series={series}
			height={height}
			sx={targetData === null ? undefined : TARGET_STEP_SX}
			slotProps={{
				legend: {
					direction: "horizontal",
					position: { vertical: "top", horizontal: "end" },
				},
			}}
		>
			{targetData === null && (
				<ChartsReferenceLine
					y={history.deliveryDate}
					label="Delivery Target"
					lineStyle={{ strokeDasharray: TARGET_LINE_DASH }}
				/>
			)}
		</LineChart>
	);
};

interface RenderBodyArgs {
	hasDataForView: boolean;
	showWhen: boolean;
	history: DeliveryMetricsHistory;
	lineColor: string;
	changeColor: string;
	palette: Record<number, string>;
	height: number;
}

const renderBody = ({
	hasDataForView,
	showWhen,
	history,
	lineColor,
	changeColor,
	palette,
	height,
}: RenderBodyArgs): ReactElement => {
	if (!hasDataForView) {
		return <EmptyBody />;
	}

	if (showWhen) {
		return (
			<WhenView
				history={history}
				palette={palette}
				targetColor={lineColor}
				height={height}
			/>
		);
	}

	return (
		<LikelihoodView
			points={history.points}
			lineColor={lineColor}
			changeColor={changeColor}
			height={height}
		/>
	);
};

const DeliveryPredictabilityChart = ({
	history,
	title = "Delivery Predictability",
	height = 320,
}: DeliveryPredictabilityChartProps): ReactElement => {
	const theme = useTheme();
	const [view, setView] = useState<PredictabilityView>("likelihood");

	const points = history.points;
	const showWhen = view === "when";
	const hasDataForView = showWhen
		? points.some(hasWhenDistribution)
		: points.some(hasLikelihood);

	const onViewChange = (next: PredictabilityView | null): void => {
		if (next !== null) {
			setView(next);
		}
	};

	return (
		<Card
			data-testid="delivery-predictability-chart"
			sx={{ p: 2, borderRadius: 2, height: "100%" }}
		>
			<CardContent
				sx={{ height: "100%", display: "flex", flexDirection: "column" }}
			>
				<Typography variant="h6">{title}</Typography>
				<ToggleButtonGroup
					exclusive
					size="small"
					value={view}
					onChange={(_event, next) => onViewChange(next)}
					sx={{ mb: 1 }}
				>
					<ToggleButton value="likelihood">How Likely?</ToggleButton>
					<ToggleButton value="when">When?</ToggleButton>
				</ToggleButtonGroup>
				{renderBody({
					hasDataForView,
					showWhen,
					history,
					lineColor: theme.palette.text.secondary,
					changeColor: theme.palette.text.primary,
					palette: WHEN_PALETTE,
					height,
				})}
			</CardContent>
		</Card>
	);
};

export default DeliveryPredictabilityChart;
