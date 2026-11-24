import {
	Card,
	CardContent,
	FormControl,
	FormControlLabel,
	MenuItem,
	Select,
	Stack,
	Switch,
	type Theme,
	Typography,
	useTheme,
} from "@mui/material";
import {
	ChartsContainer,
	ChartsReferenceLine,
	ChartsTooltip,
	ChartsXAxis,
	ChartsYAxis,
	type ScatterMarkerProps,
	ScatterPlot,
} from "@mui/x-charts";
import {
	QueryClient,
	QueryClientContext,
	skipToken,
	useQuery,
} from "@tanstack/react-query";
import type React from "react";
import { useContext, useEffect, useMemo, useRef, useState } from "react";
import { useChartVisibility } from "../../../hooks/useChartVisibility";
import {
	combineWidgetStatuses,
	useReportWidgetStatus,
	type WidgetStatus,
} from "../../../hooks/useReportWidgetStatus";
import type { IBlackoutPeriod } from "../../../models/BlackoutPeriod";
import type { ICycleTimeOutliers } from "../../../models/Metrics/CycleTimeOutliers";
import type { INamedCycleTimeDefinition } from "../../../models/Metrics/NamedCycleTime";
import type { IPercentileValue } from "../../../models/PercentileValue";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../models/WorkItem";
import { UsageDataEventName } from "../../../services/Api/UsageDataService";
import { useTerminology } from "../../../services/TerminologyContext";
import { useUsageDataReporter } from "../../../services/UsageData/usageDataReporter";
import {
	dateValueFormatter,
	getDateOnlyTimestamp,
	integerValueFormatter,
} from "../../../utils/charts/chartAxisUtils";
import {
	linesAboveAxis,
	markerSpecs,
} from "../../../utils/charts/offScaleLines";
import { axisTop, findOutliers } from "../../../utils/charts/outliers";
import { outlierWording } from "../../../utils/charts/outlierWording";
import {
	getBubbleSize,
	getMarkerColor,
	renderMarkerButton,
	renderMarkerCircle,
} from "../../../utils/charts/scatterMarkerUtils";
import { getWorkItemName } from "../../../utils/featureName";
import { getColorMapForKeys } from "../../../utils/theme/colors";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import WorkItemsDialog from "../WorkItemsDialog/WorkItemsDialog";
import type { WorkItemsDialogOwnerKind } from "../WorkItemsDialog/workItemsDialogContexts";
import {
	type CycleTimeMeasure,
	DefaultCycleTimeMeasure,
	NamedCycleTimeMeasure,
	type PlottedWorkItem,
} from "./cycleTimeMeasure";
import LegendChip from "./LegendChip";
import OffScaleLineMarkers, { MARKER_ROW_HEIGHT } from "./OffScaleLineMarkers";
import { outlierHeaderChip } from "./outlierHeaderChip";
import {
	allLineValues,
	buildPlotLines,
	currentNamedPercentiles,
	type INamedAnswer,
	isOfVisibleType,
	reservedMarkerRows,
} from "./outlierPlot";
import PercentileLegend from "./PercentileLegend";
import TimeBlackoutOverlay from "./TimeBlackoutOverlay";
import { useFrameIsReady, useHeaderSlotContent } from "./widgetHeaderSlot";

interface IGroupedWorkItem {
	plottedDateTimestamp: number;
	cycleTime: number;
	items: PlottedWorkItem[];
	type: string;
	hasBlockedItems: boolean;
}

const groupWorkItems = (items: PlottedWorkItem[]): IGroupedWorkItem[] => {
	const groups: Record<string, IGroupedWorkItem> = {};

	for (const item of items) {
		const plottedDateTimestamp = getDateOnlyTimestamp(item.plottedDate);
		const key = `${plottedDateTimestamp}-${item.cycleTime}`;

		if (!groups[key]) {
			groups[key] = {
				plottedDateTimestamp,
				cycleTime: item.cycleTime,
				items: [],
				type: item.type || "Unknown",
				hasBlockedItems: false,
			};
		}

		groups[key].items.push(item);

		if (item.isBlocked) {
			groups[key].hasBlockedItems = true;
		}
	}

	return Object.values(groups).map((g) => ({
		...g,
		type: g.items?.[0]?.type || g.type || "Unknown",
	}));
};

const ScatterMarker = (
	props: ScatterMarkerProps,
	groupedDataPoints: IGroupedWorkItem[],
	theme: Theme,
	workItemsTerm: string,
	blockedTerm: string,
	colorMap: Record<string, string>,
	onShowItems: (items: PlottedWorkItem[]) => void,
) => {
	const dataIndex = props.dataIndex || 0;
	const group = groupedDataPoints[dataIndex];

	if (!group) return null;

	const bubbleSize = getBubbleSize(group.items.length);
	const providedColor = (props as unknown as { color?: string }).color;
	const bubbleColor = getMarkerColor(group, colorMap, theme, providedColor);

	const blockedSuffix = group.hasBlockedItems ? ` (${blockedTerm})` : "";
	const itemsText = group.items.length > 1 ? "s" : "";

	const handleOpenWorkItems = () => {
		if (group.items.length > 0) {
			onShowItems(group.items);
		}
	};

	return (
		<>
			{renderMarkerCircle({
				x: props.x,
				y: props.y,
				size: bubbleSize,
				color: bubbleColor,
				isHighlighted: props.isHighlighted,
				theme,
				title: `${group.items.length} item${itemsText} with cycle time ${group.cycleTime} days${blockedSuffix}`,
			})}
			{renderMarkerButton({
				x: props.x,
				y: props.y,
				size: bubbleSize,
				ariaLabel: `View ${group.items.length} ${workItemsTerm}${itemsText} with cycle time ${group.cycleTime} days${blockedSuffix}`,
				onClick: handleOpenWorkItems,
			})}
		</>
	);
};

/** Asks the backend for the Work Items that reached the definition's end state in the date range. */
type NamedDataFetcher = (definitionId: number) => Promise<IWorkItem[]>;

// A named measure plots its own Work Items, so offering one without a way to fetch them cannot compile.
type NamedMeasureProps =
	| {
			namedCycleTimeDefinitions?: never;
			onFetchNamedCycleTimeData?: never;
	  }
	| {
			namedCycleTimeDefinitions: INamedCycleTimeDefinition[];
			onFetchNamedCycleTimeData: NamedDataFetcher;
	  };

type CycleTimeScatterPlotChartProps = NamedMeasureProps & {
	percentileValues: IPercentileValue[];
	cycleTimeDataPoints: IWorkItem[];
	serviceLevelExpectation?: IPercentileValue | null;
	blackoutPeriods?: IBlackoutPeriod[];
	onFetchNamedCycleTimePercentiles?: (
		definitionId: number,
	) => Promise<IPercentileValue[]>;
	ownerKind: WorkItemsDialogOwnerKind;
	/** Asks the backend which closed Work Items of the date range are outliers, for the given Cycle Time. */
	onFetchCycleTimeOutliers?: (
		definitionId: number | null,
	) => Promise<ICycleTimeOutliers>;
	ownerId?: number;
	startDate?: Date;
	endDate?: Date;
};

type OutlierFetcher = NonNullable<
	CycleTimeScatterPlotChartProps["onFetchCycleTimeOutliers"]
>;

// Each fetcher a view hands over stands for one owner and date range, so a new fetcher is a new question.
const fetcherIds = new WeakMap<object, number>();
let lastFetcherId = 0;
const fetcherIdOf = (fetcher: object | undefined): number | null => {
	if (!fetcher) return null;
	let id = fetcherIds.get(fetcher);
	if (id === undefined) {
		lastFetcherId += 1;
		id = lastFetcherId;
		fetcherIds.set(fetcher, id);
	}
	return id;
};

interface OutlierHandling {
	readonly hideOutliers: boolean;
	readonly verdict: ICycleTimeOutliers | null;
}

// Without limits nobody judged the items, so a count would mislead: the reason takes the chip's place.
function outlierHeaderSlot(
	{ hideOutliers, verdict }: OutlierHandling,
	chip: Parameters<typeof outlierHeaderChip>[0],
): React.ReactNode {
	if (hideOutliers && verdict?.status === "NoLimits") {
		return (
			<Typography variant="body2" color="text.secondary">
				{chip.wording.noLimits}
			</Typography>
		);
	}
	return outlierHeaderChip(chip);
}

// A chart without a fetcher asks nothing, and may be drawn outside a query provider.
const withoutAProvider = new QueryClient();

interface OutlierVerdictState {
	/** The outlier ids and limit of the current question, or null while there is no answer for it. */
	readonly verdict: ICycleTimeOutliers | null;
	readonly status: "loading" | "error" | "ready";
}

interface OutlierQuestion {
	readonly ownerKind: WorkItemsDialogOwnerKind;
	readonly ownerId?: number;
	readonly startDate?: Date;
	readonly endDate?: Date;
	readonly definitionId: number | null;
	readonly fetcher?: OutlierFetcher;
}

function useOutlierVerdict({
	ownerKind,
	ownerId,
	startDate,
	endDate,
	definitionId,
	fetcher,
}: OutlierQuestion): OutlierVerdictState {
	const client = useContext(QueryClientContext) ?? withoutAProvider;
	const query = useQuery(
		{
			queryKey: [
				"cycleTimeOutliers",
				ownerKind,
				ownerId ?? null,
				startDate?.toISOString() ?? null,
				endDate?.toISOString() ?? null,
				fetcherIdOf(fetcher),
				definitionId ?? "default",
			],
			queryFn: fetcher ? () => fetcher(definitionId) : skipToken,
		},
		client,
	);

	if (!fetcher) return { verdict: null, status: "ready" };
	if (query.isError) return { verdict: null, status: "error" };
	if (query.data === undefined) return { verdict: null, status: "loading" };
	return { verdict: query.data, status: "ready" };
}

/** The Work Items one named Cycle Time measured, tagged with that definition. */
interface NamedPopulation {
	readonly definitionId: number;
	readonly items: IWorkItem[];
}

interface NamedPopulationState {
	/** The answer to show: the selected definition's, or while it loads the one shown before it. */
	readonly answer: NamedPopulation | null;
	readonly status: WidgetStatus;
}

/** The named measure the reader selected, with the fetcher that answers for its Work Items. */
interface NamedSelection {
	readonly definitionId: number;
	readonly fetcher: NamedDataFetcher;
}

interface NamedPopulationQuestion {
	readonly ownerKind: WorkItemsDialogOwnerKind;
	readonly ownerId?: number;
	readonly startDate?: Date;
	readonly endDate?: Date;
	readonly selection: NamedSelection | null;
}

// Keyed by owner, range and definition, so an answer for a measure the reader has left lands in its
// own entry and never on the chart.
function useNamedPopulation({
	ownerKind,
	ownerId,
	startDate,
	endDate,
	selection,
}: NamedPopulationQuestion): NamedPopulationState {
	const client = useContext(QueryClientContext) ?? withoutAProvider;
	const query = useQuery(
		{
			queryKey: [
				"namedCycleTimeData",
				ownerKind,
				ownerId ?? null,
				startDate?.toISOString() ?? null,
				endDate?.toISOString() ?? null,
				fetcherIdOf(selection?.fetcher),
				selection?.definitionId ?? null,
			],
			queryFn: selection
				? async (): Promise<NamedPopulation> => ({
						definitionId: selection.definitionId,
						items: await selection.fetcher(selection.definitionId),
					})
				: skipToken,
			placeholderData: (previous: NamedPopulation | undefined) => previous,
		},
		client,
	);

	const { data, isError, isPlaceholderData } = query;
	const isSelected = selection !== null;
	return useMemo((): NamedPopulationState => {
		if (!isSelected) return { answer: null, status: "ready" };
		if (isError) return { answer: null, status: "error" };
		if (data === undefined || isPlaceholderData) {
			return { answer: data ?? null, status: "loading" };
		}
		return { answer: data, status: "ready" };
	}, [isSelected, data, isError, isPlaceholderData]);
}

const DEFAULT_MEASURE = new DefaultCycleTimeMeasure();

interface ShownPopulation {
	readonly measure: CycleTimeMeasure;
	readonly items: readonly IWorkItem[];
}

/**
 * What the chart plots: the default Cycle Time's closed Work Items, or a named measure's own
 * population. While a named answer is on its way the previous dots stay, for the frame to dim; after
 * a failure none are shown as if they were the answer.
 */
function shownPopulation(
	defaultItems: readonly IWorkItem[],
	definitions: readonly INamedCycleTimeDefinition[],
	named: NamedPopulationState,
): ShownPopulation {
	if (named.status === "error") return { measure: DEFAULT_MEASURE, items: [] };
	const answer = named.answer;
	const definition =
		answer &&
		definitions.find((candidate) => candidate.id === answer.definitionId);
	if (!answer || !definition) {
		return { measure: DEFAULT_MEASURE, items: defaultItems };
	}
	return {
		measure: new NamedCycleTimeMeasure(definition),
		items: answer.items,
	};
}

const DEFAULT_SELECTION = "default";
const PLOT_TOP_MARGIN = 8;

const plotMargin = (markerRows: number) =>
	markerRows > 0
		? { top: PLOT_TOP_MARGIN + markerRows * MARKER_ROW_HEIGHT }
		: undefined;

const CycleTimeScatterPlotChart: React.FC<CycleTimeScatterPlotChartProps> = ({
	percentileValues,
	cycleTimeDataPoints,
	serviceLevelExpectation = null,
	blackoutPeriods = [],
	namedCycleTimeDefinitions = [],
	onFetchNamedCycleTimePercentiles,
	ownerKind,
	onFetchCycleTimeOutliers,
	onFetchNamedCycleTimeData,
	ownerId,
	startDate,
	endDate,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();

	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const serviceLevelExpectationTerm = getTerm(
		TERMINOLOGY_KEYS.SERVICE_LEVEL_EXPECTATION,
	);
	const sleTerm = getTerm(TERMINOLOGY_KEYS.SLE);
	const cycleTimeTerm = getTerm(TERMINOLOGY_KEYS.CYCLE_TIME);
	const blockedTerm = getTerm(TERMINOLOGY_KEYS.BLOCKED);

	const [selectedDefinitionId, setSelectedDefinitionId] = useState<
		number | null
	>(null);
	const [namedAnswer, setNamedAnswer] = useState<INamedAnswer | null>(null);
	const frameIsReady = useFrameIsReady();
	const reportUsage = useUsageDataReporter();

	useEffect(() => {
		if (selectedDefinitionId === null) {
			return;
		}
		const selected = namedCycleTimeDefinitions.find(
			(definition) => definition.id === selectedDefinitionId,
		);
		if (!selected || selected.isValid === false) {
			setSelectedDefinitionId(null);
		}
	}, [selectedDefinitionId, namedCycleTimeDefinitions]);

	useEffect(() => {
		if (selectedDefinitionId === null || !onFetchNamedCycleTimePercentiles) {
			setNamedAnswer(null);
			return;
		}

		let cancelled = false;
		onFetchNamedCycleTimePercentiles(selectedDefinitionId)
			.then((percentiles) => {
				if (!cancelled)
					setNamedAnswer({
						definitionId: selectedDefinitionId,
						fetcher: onFetchNamedCycleTimePercentiles,
						percentiles,
					});
			})
			.catch(() => {
				if (!cancelled) setNamedAnswer(null);
			});
		return () => {
			cancelled = true;
		};
	}, [selectedDefinitionId, onFetchNamedCycleTimePercentiles]);

	const namedSelection = useMemo(
		(): NamedSelection | null =>
			selectedDefinitionId !== null && onFetchNamedCycleTimeData
				? {
						definitionId: selectedDefinitionId,
						fetcher: onFetchNamedCycleTimeData,
					}
				: null,
		[selectedDefinitionId, onFetchNamedCycleTimeData],
	);

	const namedPopulation = useNamedPopulation({
		ownerKind,
		ownerId,
		startDate,
		endDate,
		selection: namedSelection,
	});

	const shown = useMemo(
		() =>
			shownPopulation(
				cycleTimeDataPoints,
				namedCycleTimeDefinitions,
				namedPopulation,
			),
		[cycleTimeDataPoints, namedCycleTimeDefinitions, namedPopulation],
	);

	const effectiveDataPoints = useMemo(
		() => shown.measure.plot(shown.items),
		[shown],
	);

	// An answer fetched for another definition, or by an older callback, is not the current one.
	const namedPercentiles = currentNamedPercentiles(
		namedAnswer,
		selectedDefinitionId,
		onFetchNamedCycleTimePercentiles,
	);

	const effectivePercentiles =
		shown.measure.definitionId === null ? percentileValues : namedPercentiles;

	const types = useMemo(() => {
		const typeSet = new Set<string>();
		for (const item of shown.items) {
			if (item.type) typeSet.add(item.type);
		}
		return Array.from(typeSet).sort((a, b) => a.localeCompare(b));
	}, [shown]);

	const colorMap = useMemo(() => getColorMapForKeys(types), [types]);

	const {
		visiblePercentiles,
		togglePercentileVisibility,
		sleVisible,
		toggleSleVisibility,
		visibleTypes,
		toggleTypeVisibility,
	} = useChartVisibility({
		percentiles: effectivePercentiles,
		types,
		canHideEveryType: true,
		sleInitiallyVisible: true,
	});

	const chipRef = useRef<HTMLDivElement>(null);
	const [dialogOpen, setDialogOpen] = useState<boolean>(false);
	const [dialogContent, setDialogContent] = useState<{
		title: string;
		items: IWorkItem[];
		scope?: {
			cycleTimeScope: number;
			namedCycleTimeDefinitions: INamedCycleTimeDefinition[];
		};
	}>({ title: "", items: [] });

	// Never stored: every time the chart opens, outliers are hidden again.
	const [hideOutliers, setHideOutliers] = useState<boolean>(true);

	const outlierVerdict = useOutlierVerdict({
		ownerKind,
		ownerId,
		startDate,
		endDate,
		definitionId: selectedDefinitionId,
		fetcher: onFetchCycleTimeOutliers,
	});
	useReportWidgetStatus(
		combineWidgetStatuses([outlierVerdict.status, namedPopulation.status]),
		true,
	);
	const verdictPending = hideOutliers && outlierVerdict.status === "loading";

	// While the verdict is on its way or has failed, no item is set apart. The limit and the axis come
	// from every item, so hiding a type never moves them.
	const analysis = useMemo(() => {
		const verdict = outlierVerdict.verdict;
		if (!hideOutliers || verdict === null) {
			return { cut: null, outliers: [], typical: [...effectiveDataPoints] };
		}
		return {
			cut: verdict.upperNaturalProcessLimit,
			...findOutliers(
				effectiveDataPoints,
				(item) => item.cycleTime,
				(item) => item.id,
				verdict.outlierWorkItemIds,
			),
		};
	}, [effectiveDataPoints, hideOutliers, outlierVerdict.verdict]);

	const fixedXAxisDomain = useMemo((): [number, number] | null => {
		if (effectiveDataPoints.length === 0) return null;
		const timestamps = effectiveDataPoints.map((item) =>
			getDateOnlyTimestamp(item.plottedDate),
		);
		return [Math.min(...timestamps), Math.max(...timestamps)];
	}, [effectiveDataPoints]);

	const yAxisMax = useMemo(
		() =>
			axisTop({
				typicalValues: analysis.typical.map((item) => item.cycleTime),
				lineValues: allLineValues(
					effectivePercentiles,
					serviceLevelExpectation,
				),
				cut: analysis.cut,
				hasOutliers: analysis.outliers.length > 0,
			}),
		[analysis, effectivePercentiles, serviceLevelExpectation],
	);

	const groupedDataPoints = useMemo(
		() =>
			groupWorkItems(analysis.typical).filter((group) =>
				group.items.some((item) => isOfVisibleType(item, visibleTypes)),
			),
		[analysis, visibleTypes],
	);

	const visibleOutliers = analysis.outliers.filter((item) =>
		isOfVisibleType(item, visibleTypes),
	);

	const plotLines = useMemo(
		() =>
			buildPlotLines({
				percentiles: effectivePercentiles,
				visiblePercentiles,
				sle: serviceLevelExpectation,
				sleVisible,
				sleTerm,
				sleColor: theme.palette.primary.main,
			}),
		[
			effectivePercentiles,
			visiblePercentiles,
			sleVisible,
			serviceLevelExpectation,
			sleTerm,
			theme.palette.primary.main,
		],
	);

	const offScaleLines = linesAboveAxis(plotLines, yAxisMax);
	const markerRows = reservedMarkerRows(
		effectivePercentiles,
		serviceLevelExpectation,
		yAxisMax,
		analysis.outliers.length > 0,
	);

	const handleShowItems = (items: PlottedWorkItem[]) => {
		const [first] = items;
		if (!first) return;
		setDialogContent({
			title: shown.measure.dotListTitle(
				{ workItems: workItemsTerm, cycleTime: cycleTimeTerm },
				first,
			),
			items,
			// The list's Cycle Time column reads the measure the dots were plotted with.
			scope:
				shown.measure.definitionId === null
					? undefined
					: {
							cycleTimeScope: shown.measure.definitionId,
							namedCycleTimeDefinitions,
						},
		});
		setDialogOpen(true);
	};

	const wording = useMemo(
		() =>
			outlierWording({
				workItems: workItemsTerm,
				sle: sleTerm,
				cycleTime: cycleTimeTerm,
			}),
		[workItemsTerm, sleTerm, cycleTimeTerm],
	);

	useHeaderSlotContent(
		outlierHeaderSlot(
			{ hideOutliers, verdict: outlierVerdict.verdict },
			{
				wording,
				outliers: visibleOutliers,
				offScaleLines,
				disabled: !frameIsReady,
				verdictPending,
				onOpen: () => {
					reportUsage({ name: UsageDataEventName.OutlierListOpened });
					setDialogContent({
						title: wording.listTitle(analysis.cut ?? 0),
						items: [...visibleOutliers],
					});
					setDialogOpen(true);
				},
				chipRef,
			},
		),
	);

	const outlierSwitch = (
		<FormControlLabel
			label={wording.hideOutliersLabel}
			control={
				<Switch
					checked={hideOutliers}
					onChange={(event) => {
						if (!event.target.checked) {
							reportUsage({
								name: UsageDataEventName.OutlierHandlingSwitchedOff,
							});
						}
						setHideOutliers(event.target.checked);
					}}
				/>
			}
		/>
	);

	const cycleTimeSelector =
		namedCycleTimeDefinitions.length > 0 ? (
			<FormControl size="small" sx={{ minWidth: 200 }}>
				<Select
					value={
						selectedDefinitionId === null
							? DEFAULT_SELECTION
							: String(selectedDefinitionId)
					}
					onChange={(event) => {
						const value = event.target.value;
						setSelectedDefinitionId(
							value === DEFAULT_SELECTION ? null : Number(value),
						);
					}}
					SelectDisplayProps={{ "aria-label": cycleTimeTerm }}
				>
					<MenuItem value={DEFAULT_SELECTION}>Default</MenuItem>
					{namedCycleTimeDefinitions.map((definition) => (
						<MenuItem
							key={definition.id}
							value={String(definition.id)}
							disabled={definition.isValid === false}
						>
							{definition.isValid === false
								? `${definition.name} (invalid — fix its states)`
								: definition.name}
						</MenuItem>
					))}
				</Select>
			</FormControl>
		) : null;

	if (cycleTimeDataPoints.length === 0) {
		return (
			<Typography variant="body2" color="text.secondary">
				No data available
			</Typography>
		);
	}

	return (
		<>
			<Card sx={{ p: 2, borderRadius: 2, height: "100%" }}>
				<CardContent
					sx={{ display: "flex", flexDirection: "column", height: "100%" }}
				>
					<Stack
						direction="row"
						spacing={2}
						sx={{
							alignItems: "center",
							mb: 1,
						}}
					>
						<Typography variant="h6">{cycleTimeTerm}</Typography>
						{cycleTimeSelector}
						{outlierSwitch}
					</Stack>

					{effectiveDataPoints.length > 0 ? (
						<>
							<Stack
								direction="row"
								spacing={1}
								sx={{
									mb: 2,
									flexWrap: "wrap",
									gap: 1,
									justifyContent: "space-between",
								}}
							>
								<PercentileLegend
									percentiles={effectivePercentiles}
									visiblePercentiles={visiblePercentiles}
									onTogglePercentile={togglePercentileVisibility}
									serviceLevelExpectation={serviceLevelExpectation}
									serviceLevelExpectationLabel={serviceLevelExpectationTerm}
									sleVisible={sleVisible}
									onToggleSle={toggleSleVisibility}
								/>

								{types.length > 0 && (
									<Stack
										direction="row"
										spacing={1}
										sx={{ flexWrap: "wrap", gap: 1 }}
									>
										{types.map((type) => (
											<LegendChip
												key={`legend-type-${type}`}
												label={type}
												color={colorMap[type]}
												visible={visibleTypes[type] !== false}
												onToggle={() => toggleTypeVisibility(type)}
											/>
										))}
									</Stack>
								)}
							</Stack>

							{groupedDataPoints.length > 0 ? (
								<ChartsContainer
									sx={{ flex: 1, minHeight: 0, height: "100%" }}
									margin={plotMargin(markerRows)}
									xAxis={[
										{
											id: "timeAxis",
											scaleType: "time",
											label: shown.measure.dateAxisLabel,
											height: 56,
											min: fixedXAxisDomain?.[0],
											max: fixedXAxisDomain?.[1],
											valueFormatter: dateValueFormatter,
										},
									]}
									yAxis={[
										{
											id: "cycleTimeAxis",
											scaleType: "linear",
											label: `${cycleTimeTerm} (days)`,
											min: 1,
											max: yAxisMax,
											valueFormatter: integerValueFormatter,
										},
									]}
									series={[
										{
											type: "scatter",
											data: groupedDataPoints.map((group, index) => ({
												x: group.plottedDateTimestamp,
												y: group.cycleTime,
												id: index,
												itemCount: group.items.length,
												color: getMarkerColor(group, colorMap, theme),
											})),
											xAxisId: "timeAxis",
											yAxisId: "cycleTimeAxis",
											color: theme.palette.primary.main,
											markerSize: 4,
											highlightScope: {
												highlight: "item",
												fade: "global",
											},
											valueFormatter: (item) => {
												if (item?.id === undefined) return "";

												const group = groupedDataPoints[item.id as number];
												if (!group) return "";

												const numberOfItems = group.items.length;

												if (numberOfItems === 1) {
													const workItem = group.items[0];
													return `${getWorkItemName(workItem.name, workItem.referenceId)} (Click for details)`;
												}

												return `${shown.measure.groupLabel(numberOfItems, workItemsTerm)} (Click for details)`;
											},
										},
									]}
								>
									{/* Reference lines for percentiles */}
									{effectivePercentiles.map((p) => {
										const forecastLevel = new ForecastLevel(p.percentile);
										return visiblePercentiles[p.percentile] &&
											p.value <= yAxisMax ? (
											<ChartsReferenceLine
												key={`percentile-${p.percentile}`}
												y={p.value}
												label={`${p.percentile}%`}
												labelAlign="end"
												lineStyle={{
													stroke: forecastLevel.color,
													strokeWidth: 1,
													strokeDasharray: "5 5",
												}}
											/>
										) : null;
									})}

									{/* SLE reference line */}
									{sleVisible &&
										serviceLevelExpectation &&
										serviceLevelExpectation.value <= yAxisMax && (
											<ChartsReferenceLine
												key="sle-reference-line"
												y={serviceLevelExpectation.value}
												label={`${sleTerm}: ${serviceLevelExpectation.percentile}% @ ${serviceLevelExpectation.value} days or less`}
												labelAlign="start"
												lineStyle={{
													stroke: theme.palette.primary.main,
													strokeWidth: 2,
													strokeDasharray: "3 3",
												}}
											/>
										)}

									{offScaleLines.length > 0 && (
										<OffScaleLineMarkers markers={markerSpecs(offScaleLines)} />
									)}
									<ChartsXAxis axisId="timeAxis" />
									<ChartsYAxis axisId="cycleTimeAxis" />
									<TimeBlackoutOverlay
										blackoutPeriods={blackoutPeriods}
										axisId="timeAxis"
									/>
									<ScatterPlot
										slots={{
											marker: (props) =>
												ScatterMarker(
													props,
													groupedDataPoints,
													theme,
													workItemsTerm,
													blockedTerm,
													colorMap,
													handleShowItems,
												),
										}}
									/>

									<ChartsTooltip
										trigger="item"
										sx={{
											zIndex: 1200,
											maxWidth: "400px",
											"& .MuiChartsTooltip-mark": {
												display: "none",
											},
											"& .MuiChartsTooltip-markContainer": {
												display: "none",
											},
											"& .MuiChartsTooltip-table": {
												maxWidth: "100%",
												wordBreak: "break-word",
											},
											"& .MuiChartsTooltip-valueCell": {
												whiteSpace: "pre-line",
												maxWidth: "300px",
												overflowWrap: "break-word",
											},
											"& .MuiPopper-root": {
												transition: "opacity 0.2s ease-in-out",
												transitionDelay: "150ms",
											},
										}}
									/>
								</ChartsContainer>
							) : (
								<Typography
									variant="body2"
									color="text.secondary"
									sx={{ flex: 1 }}
								>
									{visibleOutliers.length > 0
										? wording.allVisibleAreOutliers
										: wording.allTypesHidden}
								</Typography>
							)}
						</>
					) : (
						<Typography variant="body2" color="text.secondary" sx={{ flex: 1 }}>
							No data available
						</Typography>
					)}
				</CardContent>
			</Card>
			<WorkItemsDialog
				title={dialogContent.title}
				items={dialogContent.items}
				open={dialogOpen}
				onClose={() => {
					setDialogOpen(false);
					chipRef.current?.focus();
				}}
				context="closedItems"
				ownerKind={ownerKind}
				{...dialogContent.scope}
			/>
		</>
	);
};

export default CycleTimeScatterPlotChart;
