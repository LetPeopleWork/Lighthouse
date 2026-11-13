import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import {
	Accordion,
	AccordionDetails,
	AccordionSummary,
	Box,
	Chip,
	Paper,
	Tab,
	Tabs,
	Tooltip,
	Typography,
} from "@mui/material";
import type { GridRowId, GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useMemo, useState } from "react";
import type {
	DataGridColumn,
	DataGridExportTable,
} from "../../../../../components/Common/DataGrid/types";
import { useFeatureChildItems } from "../../../../../components/Common/FeatureListDataGrid/useFeatureChildItems";
import { ForecastLevel } from "../../../../../components/Common/Forecasts/ForecastLevel";
import { percentileDateText } from "../../../../../components/Common/Forecasts/forecastPercentileText";
import ProgressIndicator from "../../../../../components/Common/ProgressIndicator/ProgressIndicator";
import WorkItemsDialog from "../../../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import type { Delivery } from "../../../../../models/Delivery";
import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";
import type { IEntityReference } from "../../../../../models/EntityReference";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { useTerminology } from "../../../../../services/TerminologyContext";
import { getWorkItemName } from "../../../../../utils/featureName";
import {
	cannotBeForecast,
	cannotForecastReason,
} from "../../../../../utils/forecast/cannotForecast";
import { isForecastDataInsufficient } from "../../../../../utils/forecast/isForecastDataInsufficient";
import DeliveryMetricsTab, {
	MINIMUM_METRIC_SNAPSHOTS,
	metricsUnavailableReason,
	useLazyMetricsHistory,
} from "./DeliveryMetricsTab";
import DeliveryNotesPanel from "./DeliveryNotesPanel";
import { buildDeliveryExportTable } from "./deliveryExportTable";
import {
	DeliveryRowActions,
	featureColumns,
	SelectionModeMarker,
} from "./deliveryFeatureRow";
import {
	howTheFeaturesWereChosen,
	WhatIsWrongWithTheSource,
	whatTheHeaderChipSays,
} from "./deliveryHeaderCopy";
import { isStoredAs } from "./deliverySelectionTabs";
import DeliveryTimelineTab from "./timeline/DeliveryTimelineTab";
import UnbindConfirmationDialog from "./UnbindConfirmationDialog";
import WorkItemsTab from "./WorkItemsTab";

type DeliveryDetailTab = "workItems" | "metrics" | "timeline" | "notes";

const OVERDUE_LABEL = "Overdue";

interface DeliverySectionProps {
	delivery: Delivery;
	features: IFeature[];
	isExpanded: boolean;
	isLoadingFeatures: boolean;
	onToggleExpanded: (deliveryId: number) => void;
	onDelete: (delivery: Delivery) => void;
	onEdit: (delivery: Delivery) => void;
	onArchive?: (delivery: Delivery) => void;
	teams: IEntityReference[];
	canEdit?: boolean;
	canArchive?: boolean;
	/** Everything this Portfolio's connection offers, so a stored source key can be named. */
	deliverySources?: IDeliverySource[];
	onUnbind?: (delivery: Delivery) => void;
}

const DeliverySection: React.FC<DeliverySectionProps> = ({
	delivery,
	features,
	isExpanded,
	isLoadingFeatures,
	onToggleExpanded,
	onDelete,
	onEdit,
	onArchive,
	teams,
	canEdit = true,
	canArchive = false,
	deliverySources = [],
	onUnbind,
}) => {
	const [isUnbindDialogOpen, setIsUnbindDialogOpen] = useState(false);

	const [activeTab, setActiveTab] = useState<DeliveryDetailTab>("workItems");
	const {
		history: metricsHistory,
		isLoading: isLoadingMetrics,
		hasFailed: metricsFailed,
		load: loadMetricsHistory,
	} = useLazyMetricsHistory(delivery.id);

	const isMetricsTabDisabled =
		delivery.metricSnapshotCount < MINIMUM_METRIC_SNAPSHOTS;

	// Parsed once rather than inline in the timeline's props: a fresh Date on every render is a
	// fresh identity, and the chart's axis range is memoised against it.
	const targetDate = useMemo(
		() => (delivery.date ? new Date(delivery.date) : undefined),
		[delivery.date],
	);

	const handleTabChange = useCallback(
		(_event: React.SyntheticEvent, nextTab: DeliveryDetailTab) => {
			if (nextTab === "metrics" && isMetricsTabDisabled) {
				return;
			}
			setActiveTab(nextTab);
			if (nextTab === "metrics") {
				loadMetricsHistory();
			}
		},
		[isMetricsTabDisabled, loadMetricsHistory],
	);

	const {
		selectedFeature,
		items: featureWorkItems,
		status: featureWorkItemsStatus,
		show: handleShowFeatureDetails,
		close: handleCloseWorkItemsDialog,
	} = useFeatureChildItems();

	const { getTerm } = useTerminology();
	const featureTerm = getTerm(TERMINOLOGY_KEYS.FEATURE);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);
	const portfolioTerm = getTerm(TERMINOLOGY_KEYS.PORTFOLIO);
	const teamTerm = getTerm(TERMINOLOGY_KEYS.TEAM);

	// The grid hands back the ids in the order the reader is looking at them, so the file is sorted
	// and filtered the way the screen is.
	const exportTable = useCallback(
		(orderedRowIds: GridRowId[]): DataGridExportTable => {
			const featureById = new Map(
				features.map((feature) => [feature.id, feature]),
			);
			const orderedFeatures = orderedRowIds
				.map((rowId) => featureById.get(Number(rowId)))
				.filter((feature): feature is IFeature => feature !== undefined);

			return buildDeliveryExportTable(delivery, orderedFeatures, teams, {
				deliveryTerm,
				workItemsTerm,
				featureTerm,
				portfolioTerm,
				teamTerm,
			});
		},
		[
			delivery,
			features,
			teams,
			deliveryTerm,
			workItemsTerm,
			featureTerm,
			portfolioTerm,
			teamTerm,
		],
	);

	const isRuleBased = isStoredAs(delivery, DeliverySelectionMode.RuleBased);
	const isSourceBound = isStoredAs(delivery, DeliverySelectionMode.SourceBound);

	// The key is what the row falls back to when the connection no longer offers that source: harder
	// to read than "Jira Release", but it still says which one, which a blank would not.
	const sourceLabel =
		deliverySources.find((source) => source.key === delivery.sourceKey)
			?.displayName ??
		delivery.sourceKey ??
		"";

	const formatDate = (date: Date): string => {
		return new Date(date).toLocaleDateString("en-US", {
			month: "short",
			day: "numeric",
			year: "numeric",
		});
	};

	const columns: DataGridColumn<IFeature & GridValidRowModel>[] = useMemo(
		() =>
			featureColumns({
				featureTerm,
				delivery,
				teams,
				onShowDetails: handleShowFeatureDetails,
			}),
		[featureTerm, delivery, teams, handleShowFeatureDetails],
	);

	// Everyone sees that this Delivery follows a source; only a reader who may edit it can act on
	// the marker to let go of that source, and only they are told so - offering the action to
	// someone who may only look is a promise the screen cannot keep.
	const canUnbind = isSourceBound && canEdit && onUnbind !== undefined;

	const { Icon: SelectionModeIcon, hint: selectionModeHint } =
		howTheFeaturesWereChosen({
			isSourceBound,
			isRuleBased,
			canUnbind,
			sourceLabel,
			featuresTerm,
		});

	// A bound Delivery takes whatever date its source now holds, past ones included, so a target that
	// has been and gone is an ordinary state rather than something only a stale hand-entry could reach.
	// Whether it has is the backend's answer, because "today" is the instance's day and this browser
	// may well be on the other side of midnight from it.
	const isOverdue = delivery.isOverdue;

	const forecastLevel = new ForecastLevel(delivery.likelihoodPercentage);

	const teamsWithoutForecast = delivery.teamsWithoutForecast ?? [];
	const deliveryLikelihood = delivery.likelihoodPercentage;
	const deliveryCannotBeForecast =
		cannotBeForecast({ teamsWithoutForecast }) || deliveryLikelihood === null;

	const hasInsufficientData = isForecastDataInsufficient({
		hasRemainingWork: delivery.remainingWork > 0,
		hasSufficientData: delivery.hasSufficientData,
	});
	const likelihoodLabel = whatTheHeaderChipSays({
		delivery,
		featuresTerm,
		cannotBeForecast: deliveryCannotBeForecast,
		hasInsufficientData,
	});

	return (
		<Paper elevation={1} sx={{ mb: 2, overflow: "hidden" }}>
			<Box sx={{ position: "relative" }}>
				{/* Above the row rather than inside it: the values below are real values that were true
				    when they were last read, so what has to arrive first is that nothing is maintaining
				    them now. Outside the Accordion so reading it never collapses the Delivery. */}
				<WhatIsWrongWithTheSource
					delivery={delivery}
					isSourceBound={isSourceBound}
					sourceLabel={sourceLabel}
					deliveryTerm={deliveryTerm}
					onUnbind={canUnbind ? () => setIsUnbindDialogOpen(true) : undefined}
				/>
				<Accordion
					expanded={isExpanded}
					onChange={() => onToggleExpanded(delivery.id)}
					sx={{ overflow: "hidden" }}
					slotProps={{
						transition: {
							unmountOnExit: false,
						},
					}}
				>
					<AccordionSummary
						expandIcon={<ExpandMoreIcon />}
						sx={{
							minHeight: 80,
							position: "relative",
							"&.Mui-expanded": {
								minHeight: 80,
							},
							"& .MuiAccordionSummary-content": {
								alignItems: "center",
								margin: "12px 0",
								"&.Mui-expanded": {
									margin: "12px 0",
								},
							},
							pr: 15,
						}}
					>
						{canEdit && (
							<DeliveryRowActions
								delivery={delivery}
								deliveryTerm={deliveryTerm}
								canArchive={canArchive}
								onArchive={onArchive}
								onEdit={onEdit}
								onDelete={onDelete}
							/>
						)}
						<Box
							sx={{
								display: "flex",
								flexDirection: "column",
								width: "100%",
								gap: 1,
							}}
						>
							<Box
								sx={{
									display: "flex",
									alignItems: "center",
									justifyContent: "space-between",
									width: "100%",
									gap: 4,
								}}
							>
								<Box
									sx={{
										display: "flex",
										flexDirection: "column",
										gap: 1,
										flex: 1,
									}}
								>
									<Box
										sx={{
											display: "flex",
											alignItems: "center",
											gap: 2,
											flexShrink: 0,
										}}
									>
										<Typography variant="h6" component="h3">
											{delivery.name}
										</Typography>
										{/* A whole link reads as a badge, not as something to press. Breaking it the moment
										    the pointer or the keyboard arrives shows what pressing will do, without a second
										    glyph sitting beside every name for the rest of the time. */}
										<SelectionModeMarker
											hint={selectionModeHint}
											Icon={SelectionModeIcon}
											canUnbind={canUnbind}
											onAskToUnbind={() => setIsUnbindDialogOpen(true)}
										/>
										<Typography variant="body2" color="text.secondary">
											{deliveryTerm} Date: {delivery.getFormattedDate()}
										</Typography>
										{/* The chip carries both halves of the signal: red for whoever reads colour
										    fastest, and the word for whoever cannot see the difference. Colouring the
										    date as well was redundant emphasis on the one part of this that no test
										    in this project can see - MUI renders both theme colours to an identical
										    class under jsdom, so it could break and stay broken. */}
										{isOverdue && (
											<Chip
												label={OVERDUE_LABEL}
												title="The target date has passed."
												size="small"
												color="error"
												variant="outlined"
											/>
										)}
										<Chip
											title={
												deliveryCannotBeForecast
													? cannotForecastReason(teamsWithoutForecast)
													: undefined
											}
											label={likelihoodLabel}
											size="small"
											sx={{
												bgcolor: forecastLevel.color,
												color: "#fff",
												fontWeight: "bold",
											}}
										/>
									</Box>
									<Box
										sx={{
											display: "flex",
											alignItems: "center",
											gap: 1,
											flexWrap: "wrap",
										}}
									>
										<Typography variant="body2" color="text.secondary">
											Forecast:
										</Typography>
										{delivery.completionDates.map((forecast) => (
											<Chip
												key={forecast.probability}
												label={`${forecast.probability}%: ${percentileDateText(forecast, formatDate)}`}
												size="small"
												variant="outlined"
											/>
										))}
									</Box>
								</Box>
								<Box
									sx={{
										display: "flex",
										alignItems: "center",
										justifyContent: "center",
										minWidth: 200,
										flex: 1,
									}}
								>
									<ProgressIndicator
										title={`${delivery.getFeatureCount()} ${delivery.getFeatureCount() === 1 ? featureTerm : featuresTerm} (${delivery.totalWork} ${workItemsTerm})`}
										progressableItem={{
											remainingWork: delivery.remainingWork,
											totalWork: delivery.totalWork,
										}}
										showDetails={true}
									/>
								</Box>
							</Box>
						</Box>
					</AccordionSummary>
					<AccordionDetails sx={{ p: 0 }}>
						<Tabs
							value={activeTab}
							onChange={handleTabChange}
							aria-label="delivery view tabs"
							sx={{ px: 2, borderBottom: 1, borderColor: "divider" }}
						>
							<Tab label={workItemsTerm} value="workItems" />
							<Tab
								value="metrics"
								disabled={isMetricsTabDisabled}
								label={
									<Tooltip
										title={
											isMetricsTabDisabled
												? metricsUnavailableReason(delivery.metricSnapshotCount)
												: ""
										}
									>
										<span style={{ pointerEvents: "auto" }}>Metrics</span>
									</Tooltip>
								}
							/>
							<Tab label="Timeline" value="timeline" />
							{/* Always enabled: unlike Metrics, a note needs no accumulated history. */}
							<Tab label="Notes" value="notes" />
						</Tabs>
						{activeTab === "workItems" && (
							<WorkItemsTab
								isLoadingFeatures={isLoadingFeatures}
								features={features}
								columns={columns}
								deliveryId={delivery.id}
								featuresTerm={featuresTerm}
								deliveryTerm={deliveryTerm}
								exportFileName={delivery.name}
								exportTable={exportTable}
							/>
						)}
						{activeTab === "metrics" && (
							<DeliveryMetricsTab
								isLoading={isLoadingMetrics}
								history={metricsHistory}
								hasFailed={metricsFailed}
							/>
						)}
						{activeTab === "timeline" && (
							<DeliveryTimelineTab
								features={features}
								targetDate={targetDate}
								featuresTerm={featuresTerm}
								teams={teams}
							/>
						)}
						{activeTab === "notes" && (
							<DeliveryNotesPanel deliveryId={delivery.id} canWrite={canEdit} />
						)}
					</AccordionDetails>
				</Accordion>
			</Box>
			<UnbindConfirmationDialog
				open={isUnbindDialogOpen}
				sourceLabel={sourceLabel}
				deliveryName={delivery.name}
				featuresTerm={featuresTerm}
				onCancel={() => setIsUnbindDialogOpen(false)}
				onConfirm={() => {
					setIsUnbindDialogOpen(false);
					onUnbind?.(delivery);
				}}
			/>
			{selectedFeature && (
				<WorkItemsDialog
					title={`${getWorkItemName(selectedFeature.name, selectedFeature.referenceId)} ${workItemsTerm}`}
					items={featureWorkItems}
					open
					context="featureChildren"
					ownerKind="portfolio"
					status={featureWorkItemsStatus}
					onClose={handleCloseWorkItemsDialog}
				/>
			)}
		</Paper>
	);
};

export default DeliverySection;
