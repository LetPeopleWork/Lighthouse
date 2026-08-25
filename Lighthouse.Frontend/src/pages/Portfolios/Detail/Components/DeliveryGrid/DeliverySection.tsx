import ArchiveIcon from "@mui/icons-material/Archive";
import AutoModeIcon from "@mui/icons-material/AutoMode";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import LinkIcon from "@mui/icons-material/Link";
import LinkOffIcon from "@mui/icons-material/LinkOff";
import TouchAppIcon from "@mui/icons-material/TouchApp";
import {
	Accordion,
	AccordionDetails,
	AccordionSummary,
	Box,
	Button,
	Chip,
	Dialog,
	DialogActions,
	DialogContent,
	DialogContentText,
	DialogTitle,
	IconButton,
	Paper,
	Tab,
	Tabs,
	Tooltip,
	Typography,
} from "@mui/material";
import type { GridRowId, GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import {
	useCallback,
	useContext,
	useEffect,
	useId,
	useMemo,
	useState,
} from "react";
import type {
	DataGridColumn,
	DataGridExportTable,
} from "../../../../../components/Common/DataGrid/types";
import {
	createForecastsColumn,
	createStateColumn,
} from "../../../../../components/Common/FeatureListDataGrid/columns";
import FeatureListDataGrid from "../../../../../components/Common/FeatureListDataGrid/FeatureListDataGrid";
import FeatureProgressIndicator from "../../../../../components/Common/FeatureListDataGrid/FeatureProgressIndicator";
import FeatureName from "../../../../../components/Common/FeatureName/FeatureName";
import { FeatureLikelihoodChip } from "../../../../../components/Common/Forecasts/FeatureLikelihoodChip";
import { ForecastLevel } from "../../../../../components/Common/Forecasts/ForecastLevel";
import ProgressIndicator from "../../../../../components/Common/ProgressIndicator/ProgressIndicator";
import StyledLink from "../../../../../components/Common/StyledLink/StyledLink";
import WorkItemsDialog from "../../../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import type { Delivery } from "../../../../../models/Delivery";
import type { IDeliverySource } from "../../../../../models/Delivery/DeliverySource";
import type { IEntityReference } from "../../../../../models/EntityReference";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../../../models/WorkItem";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../../services/TerminologyContext";
import { getWorkItemName } from "../../../../../utils/featureName";
import {
	CANNOT_FORECAST_SHORT,
	cannotBeForecast,
	cannotForecastReason,
} from "../../../../../utils/forecast/cannotForecast";
import { formatLikelihood } from "../../../../../utils/forecast/formatLikelihood";
import { INSUFFICIENT_FORECAST_DATA_SHORT } from "../../../../../utils/forecast/insufficientForecastData";
import { isForecastDataInsufficient } from "../../../../../utils/forecast/isForecastDataInsufficient";
import { jointLikelihoodLabel } from "../../../../../utils/forecast/jointLikelihoodLabel";
import { PREMIUM_UPGRADE_TOOLTIP } from "../../../../../utils/premiumUpgradeTooltip";
import DeliveryMetricsTab, {
	MINIMUM_METRIC_SNAPSHOTS,
	metricsUnavailableReason,
	useLazyMetricsHistory,
} from "./DeliveryMetricsTab";
import DeliveryNotesPanel from "./DeliveryNotesPanel";
import { buildDeliveryExportTable } from "./deliveryExportTable";
import { isStoredAs } from "./deliverySelectionTabs";

// A Feature's own chance of landing, not the Delivery's — the Delivery's asks whether they ALL land,
// so it sits below any single row's, and the heading must not invite the two to be read as one number.
const MARGINAL_LIKELIHOOD_HEADER = "Likelihood";

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
	const { featureService } = useContext(ApiServiceContext);

	const [selectedFeature, setSelectedFeature] = useState<IFeature | null>(null);
	const [featureWorkItems, setFeatureWorkItems] = useState<IWorkItem[]>([]);
	const [isWorkItemsDialogOpen, setIsWorkItemsDialogOpen] = useState(false);
	const [isUnbindDialogOpen, setIsUnbindDialogOpen] = useState(false);
	const [isAboutToUnbind, setIsAboutToUnbind] = useState(false);

	const [activeTab, setActiveTab] = useState<"workItems" | "metrics" | "notes">(
		"workItems",
	);
	const {
		history: metricsHistory,
		isLoading: isLoadingMetrics,
		hasFailed: metricsFailed,
		load: loadMetricsHistory,
	} = useLazyMetricsHistory(delivery.id);

	const isMetricsTabDisabled =
		delivery.metricSnapshotCount < MINIMUM_METRIC_SNAPSHOTS;

	const handleTabChange = useCallback(
		(
			_event: React.SyntheticEvent,
			nextTab: "workItems" | "metrics" | "notes",
		) => {
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

	const handleShowFeatureDetails = useCallback(
		async (feature: IFeature) => {
			setSelectedFeature(feature);
			setFeatureWorkItems([]);
			setIsWorkItemsDialogOpen(true);

			const items = await featureService.getFeatureWorkItems(feature.id);
			setFeatureWorkItems(items);
		},
		[featureService],
	);

	const handleCloseWorkItemsDialog = () => {
		setIsWorkItemsDialogOpen(false);
		setSelectedFeature(null);
	};

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
		() => [
			{
				field: "name",
				headerName: `${featureTerm} Name`,
				hideable: false,
				minWidth: 120,
				flex: 1,
				renderCell: ({ row }) => (
					<FeatureName
						name={getWorkItemName(row.name, row.referenceId)}
						url={row.url ?? ""}
					/>
				),
			},
			{
				field: "owningTeam",
				headerName: "Team",
				minWidth: 100,
				flex: 0.5,
				renderCell: ({ row }) => {
					const teamsWithWork = teams.filter(
						(team) => row.getTotalWorkForTeam(team.id) > 0,
					);

					if (teamsWithWork.length === 0) {
						return (
							<Typography variant="body2" color="text.secondary">
								Unassigned
							</Typography>
						);
					}

					return (
						<Box sx={{ display: "flex", flexDirection: "column", gap: 0.5 }}>
							{teamsWithWork.map((team) => (
								<StyledLink key={team.id} to={`/teams/${team.id}`}>
									<Typography variant="body2">{team.name}</Typography>
								</StyledLink>
							))}
						</Box>
					);
				},
			},
			{
				field: "progress",
				headerName: "Progress",
				minWidth: 200,
				flex: 1,
				sortable: false,
				renderCell: ({ row }) => (
					<FeatureProgressIndicator
						feature={row}
						teams={teams}
						onShowDetails={() => handleShowFeatureDetails(row)}
					/>
				),
			},
			{
				...createForecastsColumn("Forecast"),
				minWidth: 100,
				flex: 0.5,
				width: undefined,
			},
			{
				field: "likelihood",
				headerName: MARGINAL_LIKELIHOOD_HEADER,
				minWidth: 110,
				flex: 0.3,
				sortable: false,
				renderCell: ({ row }) =>
					delivery.featureLikelihoods
						.filter((fl) => fl.featureId === row.id)
						.map((fl) => (
							<FeatureLikelihoodChip
								key={fl.featureId}
								featureLikelihood={fl}
								hasRemainingWork={row.getRemainingWorkForFeature() > 0}
							/>
						)),
			},
			createStateColumn(),
		],
		[featureTerm, delivery, teams, handleShowFeatureDetails],
	);

	// Everyone sees that this Delivery follows a source; only a reader who may edit it can act on
	// the marker to let go of that source, and only they are told so - offering the action to
	// someone who may only look is a promise the screen cannot keep.
	const canUnbind = isSourceBound && canEdit && onUnbind !== undefined;

	let SelectionModeIcon = TouchAppIcon;
	let selectionModeHint = `Manual: ${featuresTerm} are fixed`;
	if (isSourceBound) {
		SelectionModeIcon = LinkIcon;
		selectionModeHint = `Bound to ${sourceLabel}`;
	} else if (isRuleBased) {
		SelectionModeIcon = AutoModeIcon;
		selectionModeHint = `Rule-Based: ${featuresTerm} automatically update based on rules`;
	}
	if (canUnbind) {
		selectionModeHint = `${selectionModeHint} — click to stop following`;
	}

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
	let likelihoodLabel: string;
	if (deliveryCannotBeForecast || deliveryLikelihood === null) {
		likelihoodLabel = CANNOT_FORECAST_SHORT;
	} else if (hasInsufficientData) {
		likelihoodLabel = INSUFFICIENT_FORECAST_DATA_SHORT;
	} else {
		likelihoodLabel = jointLikelihoodLabel({
			term: featuresTerm,
			date: delivery.getFormattedDate(),
			value: formatLikelihood(deliveryLikelihood, {
				hasRemainingWork: delivery.remainingWork > 0,
				precision: "round",
			}),
		});
	}

	return (
		<Paper elevation={1} sx={{ mb: 2, overflow: "hidden" }}>
			<Box sx={{ position: "relative" }}>
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
							<Box
								sx={{
									position: "absolute",
									top: "50%",
									transform: "translateY(-50%)",
									right: 8,
									zIndex: 1,
									display: "flex",
									alignItems: "center",
									gap: 0.5,
								}}
							>
								<Tooltip
									title={
										canArchive
											? `Archive ${deliveryTerm}`
											: PREMIUM_UPGRADE_TOOLTIP
									}
								>
									{/* The button is disabled without a licence, and a disabled button fires no
									    events, so the tooltip needs an enabled element of its own to sit on. */}
									<span>
										<IconButton
											size="small"
											disabled={!canArchive}
											onClick={(e) => {
												e.stopPropagation();
												onArchive?.(delivery);
											}}
											aria-label="archive"
											sx={{
												bgcolor: "background.paper",
												"&:hover": {
													bgcolor: "primary.light",
												},
											}}
										>
											<ArchiveIcon />
										</IconButton>
									</span>
								</Tooltip>
								<IconButton
									size="small"
									onClick={(e) => {
										e.stopPropagation();
										onEdit(delivery);
									}}
									aria-label="edit"
									sx={{
										bgcolor: "background.paper",
										"&:hover": {
											bgcolor: "primary.light",
										},
									}}
								>
									<EditIcon />
								</IconButton>
								<IconButton
									size="small"
									onClick={(e) => {
										e.stopPropagation();
										onDelete(delivery);
									}}
									aria-label="delete"
									sx={{
										bgcolor: "background.paper",
										"&:hover": {
											bgcolor: "error.light",
										},
									}}
								>
									<DeleteIcon />
								</IconButton>
							</Box>
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
										<Tooltip title={selectionModeHint}>
											{canUnbind ? (
												<IconButton
													size="small"
													aria-label={selectionModeHint}
													onClick={(e) => {
														e.stopPropagation();
														setIsUnbindDialogOpen(true);
													}}
													onMouseEnter={() => setIsAboutToUnbind(true)}
													onMouseLeave={() => setIsAboutToUnbind(false)}
													onFocus={() => setIsAboutToUnbind(true)}
													onBlur={() => setIsAboutToUnbind(false)}
													sx={{ color: "text.secondary", p: 0 }}
												>
													{isAboutToUnbind ? (
														<LinkOffIcon fontSize="small" />
													) : (
														<SelectionModeIcon fontSize="small" />
													)}
												</IconButton>
											) : (
												<Box sx={{ display: "flex", alignItems: "center" }}>
													<SelectionModeIcon
														fontSize="small"
														sx={{ color: "text.secondary" }}
													/>
												</Box>
											)}
										</Tooltip>
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
												label={`${forecast.probability}%: ${formatDate(forecast.expectedDate)}`}
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
								featuresTerm={featuresTerm}
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
					open={isWorkItemsDialogOpen}
					onClose={handleCloseWorkItemsDialog}
				/>
			)}
		</Paper>
	);
};

interface UnbindConfirmationDialogProps {
	open: boolean;
	sourceLabel: string;
	deliveryName: string;
	featuresTerm: string;
	onConfirm: () => void;
	onCancel: () => void;
}

/**
 * Asked because the way back is not symmetrical: binding again means finding the same entry in the
 * work tracking system and picking it a second time, which nothing on this page will do for you.
 */
const UnbindConfirmationDialog: React.FC<UnbindConfirmationDialogProps> = ({
	open,
	sourceLabel,
	deliveryName,
	featuresTerm,
	onConfirm,
	onCancel,
}) => {
	const titleId = useId();
	const descriptionId = useId();
	// The dialog is still on screen while it fades out, so the button that started the unbind can be
	// pressed a second time. That second press sends the same version number the first one has just
	// spent, and the reader is told someone else changed the delivery moments after their own change
	// went through.
	const [asked, setAsked] = useState(false);

	useEffect(() => {
		if (open) {
			setAsked(false);
		}
	}, [open]);

	return (
		<Dialog
			open={open}
			onClose={onCancel}
			aria-labelledby={titleId}
			aria-describedby={descriptionId}
		>
			<DialogTitle id={titleId}>Stop following the {sourceLabel}?</DialogTitle>
			<DialogContent>
				<DialogContentText id={descriptionId}>
					{`"${deliveryName}" keeps the name, the date and the ${featuresTerm} it has right now, and from then on they are yours to edit. It stops taking them from the ${sourceLabel}.`}
				</DialogContentText>
			</DialogContent>
			<DialogActions>
				<Button onClick={onCancel} color="primary">
					Cancel
				</Button>
				<Button
					onClick={() => {
						setAsked(true);
						onConfirm();
					}}
					disabled={asked}
					color="primary"
					variant="contained"
					autoFocus
				>
					Stop following
				</Button>
			</DialogActions>
		</Dialog>
	);
};

interface WorkItemsTabProps {
	isLoadingFeatures: boolean;
	features: IFeature[];
	columns: DataGridColumn<IFeature & GridValidRowModel>[];
	deliveryId: number;
	featuresTerm: string;
	deliveryTerm: string;
	exportFileName: string;
	exportTable: (orderedRowIds: GridRowId[]) => DataGridExportTable;
}

const WorkItemsTab: React.FC<WorkItemsTabProps> = ({
	isLoadingFeatures,
	features,
	columns,
	deliveryId,
	featuresTerm,
	deliveryTerm,
	exportFileName,
	exportTable,
}) => {
	if (isLoadingFeatures) {
		return (
			<Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
				{`Loading ${featuresTerm}...`}
			</Typography>
		);
	}

	if (features.length === 0) {
		return (
			<Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
				No {featuresTerm} in this {deliveryTerm}.
			</Typography>
		);
	}

	return (
		<Box sx={{ mx: 2, mb: 2, mt: 2 }}>
			<FeatureListDataGrid
				features={features}
				columns={columns}
				storageKey={`delivery-features-${deliveryId}`}
				hideCompletedStorageKey={`lighthouse_hide_completed_features_delivery_${deliveryId}`}
				loading={false}
				emptyStateMessage={`No ${featuresTerm} found`}
				enableExport={true}
				exportFileName={exportFileName}
				exportTable={exportTable}
			/>
		</Box>
	);
};

export default DeliverySection;
