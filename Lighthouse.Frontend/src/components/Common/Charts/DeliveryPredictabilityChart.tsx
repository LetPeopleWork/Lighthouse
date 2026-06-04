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
	certainColor,
	confidentColor,
	realisticColor,
	riskyColor,
} from "../../../utils/theme/colors";

interface DeliveryPredictabilityChartProps {
	history: DeliveryMetricsHistory;
	title?: string;
}

type PredictabilityView = "likelihood" | "when";

const FORWARD_ONLY_EMPTY_STATE =
	"This chart builds forward from today — no snapshots recorded yet.";

const LIKELIHOOD_SERIES_ID = "likelihood";
const LIKELIHOOD_SERIES_LABEL = "Likelihood";

const RAG_THRESHOLDS = [50, 70, 85];
const RAG_BAND_COLORS = [
	riskyColor,
	realisticColor,
	confidentColor,
	certainColor,
];

const WHEN_PERCENTILES = [50, 70, 85, 95];
const DEFAULT_WHEN_PERCENTILE = 70;
const TARGET_LINE_DASH = "6 4";

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
}: {
	points: DeliveryMetricsHistoryPoint[];
	lineColor: string;
}): ReactElement => (
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
					thresholds: RAG_THRESHOLDS,
					colors: RAG_BAND_COLORS,
				},
			},
		]}
		series={[
			{
				id: LIKELIHOOD_SERIES_ID,
				label: LIKELIHOOD_SERIES_LABEL,
				data: points.map((point) => point.likelihoodPercentage),
				showMark: true,
				color: lineColor,
				valueFormatter: formatPercentage,
			},
		]}
		height={320}
		slotProps={{
			legend: {
				direction: "horizontal",
				position: { vertical: "top", horizontal: "end" },
			},
		}}
	/>
);

const WhenView = ({
	history,
	palette,
}: {
	history: DeliveryMetricsHistory;
	palette: Record<number, string>;
}): ReactElement => {
	const points = history.points;
	const whenSeries = buildWhenSeries(points, palette);

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
			series={whenSeries.map((entry) => ({
				id: entry.id,
				label: entry.label,
				data: entry.data,
				showMark: entry.emphasized,
				color: entry.color,
				valueFormatter: formatDateValue,
			}))}
			height={320}
			slotProps={{
				legend: {
					direction: "horizontal",
					position: { vertical: "top", horizontal: "end" },
				},
			}}
		>
			<ChartsReferenceLine
				y={history.deliveryDate}
				label="Delivery Target"
				lineStyle={{ strokeDasharray: TARGET_LINE_DASH }}
			/>
		</LineChart>
	);
};

interface RenderBodyArgs {
	hasDataForView: boolean;
	showWhen: boolean;
	history: DeliveryMetricsHistory;
	lineColor: string;
	palette: Record<number, string>;
}

const renderBody = ({
	hasDataForView,
	showWhen,
	history,
	lineColor,
	palette,
}: RenderBodyArgs): ReactElement => {
	if (!hasDataForView) {
		return <EmptyBody />;
	}

	if (showWhen) {
		return <WhenView history={history} palette={palette} />;
	}

	return <LikelihoodView points={history.points} lineColor={lineColor} />;
};

const DeliveryPredictabilityChart = ({
	history,
	title = "Delivery Predictability",
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
					palette: WHEN_PALETTE,
				})}
			</CardContent>
		</Card>
	);
};

export default DeliveryPredictabilityChart;
