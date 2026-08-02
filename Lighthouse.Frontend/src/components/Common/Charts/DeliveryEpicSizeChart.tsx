import { Card, CardContent, Typography } from "@mui/material";
import {
	BarPlot,
	ChartsContainer,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	LinePlot,
	MarkPlot,
} from "@mui/x-charts";
import { type ReactElement, type ReactNode, type SVGProps, useId } from "react";
import type {
	DeliveryMetricsHistory,
	DeliveryMetricsHistoryPoint,
} from "../../../models/Delivery/DeliveryMetricsHistory";
import {
	appColors,
	getColorMapForKeys,
	getContrastText,
} from "../../../utils/theme/colors";
import ChartLegend from "./ChartLegend";
import { useLegendFilter } from "./useLegendFilter";

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

const HATCH_TILE = 8;
const HATCH_STROKE_WIDTH = 3;
const HATCH_STROKE_OPACITY = 0.6;
// Terse on purpose: the sentence DISCUSS AC-3.3 suggested overran the tooltip row (review 2026-08-02).
const DEFAULT_SIZE_NOTE = "(estimated)";

interface EpicSeriesDescriptor {
	referenceId: string;
	name: string;
}

const sizeDataKey = (referenceId: string): string => `items:${referenceId}`;

// Null, not "": ChartsAxisTooltipContent drops a row whose formattedValue is null, which is how an
// epic that was not in the delivery that day stays out of the day's tooltip.
const formatSize = (
	value: number | null,
	usesDefaultSize: boolean,
): string | null => {
	if (value === null) {
		return null;
	}

	return usesDefaultSize ? `${value} ${DEFAULT_SIZE_NOTE}` : `${value}`;
};

const sizesOn = (point: DeliveryMetricsHistoryPoint): Map<string, number> => {
	const sizes = new Map<string, number>();
	for (const metric of point.featureBreakdown) {
		if (metric.totalItems !== null) {
			sizes.set(metric.referenceId, metric.totalItems);
		}
	}
	return sizes;
};

// AC-3.5: only a recorded flag makes an epic a guess. Every snapshot taken before slice 02 carries no
// flag at all, so reading absence as "estimated" would hatch the whole back-history.
const defaultSizedOn = (point: DeliveryMetricsHistoryPoint): Set<string> => {
	const estimated = new Set<string>();
	for (const metric of point.featureBreakdown) {
		if (metric.isUsingDefaultSize) {
			estimated.add(metric.referenceId);
		}
	}
	return estimated;
};

// The props MUI-X hands a bar slot (ADR-119). Declared here rather than imported: the acceptance test
// mocks the @mui/x-charts barrel, and a ninth named import from it would resolve to undefined.
type EpicBarProps = Omit<SVGProps<SVGRectElement>, "color"> & {
	seriesId: string | number;
	dataIndex: number;
	color?: string;
	ownerState?: { isFaded?: boolean; isHighlighted?: boolean };
	xOrigin?: number;
	yOrigin?: number;
	layout?: "vertical" | "horizontal";
	skipAnimation?: boolean;
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

interface EpicHatch {
	referenceId: string;
	id: string;
	color: string;
}

// The stroke is derived from the segment's own colour: the epic ramp runs green to teal and a fixed
// white or black hatch vanishes at one end of it.
const EpicHatchDefs = ({ hatches }: { hatches: EpicHatch[] }): ReactElement => (
	<defs>
		{hatches.map((hatch) => (
			<pattern
				key={hatch.referenceId}
				id={hatch.id}
				patternUnits="userSpaceOnUse"
				patternTransform="rotate(45)"
				width={HATCH_TILE}
				height={HATCH_TILE}
			>
				<rect width={HATCH_TILE} height={HATCH_TILE} fill={hatch.color} />
				<line
					x1={0}
					y1={0}
					x2={0}
					y2={HATCH_TILE}
					stroke={getContrastText(hatch.color)}
					strokeOpacity={HATCH_STROKE_OPACITY}
					strokeWidth={HATCH_STROKE_WIDTH}
				/>
			</pattern>
		))}
	</defs>
);

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
	const title = `${featuresTerm} over Time`;
	// Scopes this chart's pattern defs; the guillemets React 19 puts in a generated id are not safe
	// inside url(#...).
	const instanceId = useId().replace(/[^a-zA-Z0-9]/g, "");
	const { selected, isVisible, toggle, showAll } = useLegendFilter();

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
	// AC-4.5 / D8: only the stacked sizes are filtered — the count line below is a delivery-level fact.
	const visibleEpics = epics.filter((epic) => isVisible(epic.referenceId));
	const hasSizes = visibleEpics.length > 0;

	const estimatedByDay = history.points.map(defaultSizedOn);
	const usesDefaultSize = (referenceId: string, dataIndex: number): boolean =>
		estimatedByDay[dataIndex]?.has(referenceId) ?? false;

	// Ids are instance-scoped so two deliveries expanded on the same page cannot reuse each other's
	// pattern def (AC-3.6).
	const hatches: EpicHatch[] = epics.map((epic, index) => ({
		referenceId: epic.referenceId,
		id: `hatch-${instanceId}-${index}`,
		color: colorByReferenceId[epic.referenceId],
	}));
	const hatchIdByReferenceId: Record<string, string> = Object.fromEntries(
		hatches.map((hatch) => [hatch.referenceId, hatch.id]),
	);

	const renderEpicBar = (props: EpicBarProps): ReactElement => {
		const referenceId = `${props.seriesId}`;
		const hatch = usesDefaultSize(referenceId, props.dataIndex)
			? hatchIdByReferenceId[referenceId]
			: undefined;

		return (
			<rect
				x={props.x}
				y={props.y}
				width={props.width}
				height={props.height}
				className={props.className}
				style={props.style}
				opacity={props.ownerState?.isFaded ? 0.3 : 1}
				filter={
					props.ownerState?.isHighlighted ? "brightness(120%)" : undefined
				}
				fill={hatch ? `url(#${hatch})` : (props.fill ?? props.color)}
			/>
		);
	};

	// BarPlot routes the slot down to each bar. ChartsContainer's own `slots` knows only the material
	// slots and ignores a `bar` key, so it must NOT be configured there.
	const barSlots = { bar: renderEpicBar };

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

	const sizeSeries = visibleEpics.map((epic) => ({
		id: epic.referenceId,
		type: "bar" as const,
		dataKey: sizeDataKey(epic.referenceId),
		label: epic.name,
		yAxisId: ITEMS_AXIS_ID,
		stack: ITEMS_STACK_ID,
		color: colorByReferenceId[epic.referenceId],
		valueFormatter: (value: number | null, context: { dataIndex: number }) =>
			formatSize(value, usesDefaultSize(epic.referenceId, context.dataIndex)),
	}));

	const itemsAxis = hasSizes
		? [{ id: ITEMS_AXIS_ID, position: "left" as const }]
		: [];

	return (
		<ChartCard title={title}>
			<ChartsContainer
				dataset={dataset}
				xAxis={[{ scaleType: "band", dataKey: LABEL_DATA_KEY }]}
				yAxis={[
					...itemsAxis,
					// From zero always: an axis starting at the window's lowest count reads as a cliff.
					{ id: COUNT_AXIS_ID, position: "right", min: 0 },
				]}
				series={[
					...sizeSeries,
					{
						id: COUNT_SERIES_ID,
						type: "line",
						dataKey: COUNT_DATA_KEY,
						label: COUNT_AXIS_LABEL,
						yAxisId: COUNT_AXIS_ID,
						// Orange: the epic palette is a green-teal ramp, so the line needs a hue it never uses.
						color: appColors.status.warning,
					},
				]}
				height={height}
				margin={{ left: 60, right: 60, top: 20, bottom: 80 }}
			>
				<EpicHatchDefs hatches={hatches} />
				<BarPlot slots={barSlots} />
				<LinePlot />
				<MarkPlot />
				<ChartsXAxis />
				{hasSizes && (
					<ChartsYAxis axisId={ITEMS_AXIS_ID} label={ITEMS_AXIS_LABEL} />
				)}
				<ChartsYAxis axisId={COUNT_AXIS_ID} label={COUNT_AXIS_LABEL} />
				<ChartsTooltip />
			</ChartsContainer>
			{epics.length > 0 && (
				<ChartLegend
					items={epics.map((epic) => ({
						id: epic.referenceId,
						label: epic.name,
						color: colorByReferenceId[epic.referenceId],
					}))}
					selected={selected}
					onToggle={toggle}
					onShowAll={showAll}
				/>
			)}
		</ChartCard>
	);
};

export default DeliveryEpicSizeChart;
