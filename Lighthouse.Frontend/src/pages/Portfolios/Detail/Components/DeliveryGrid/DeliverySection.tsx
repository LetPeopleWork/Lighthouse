import AutoModeIcon from "@mui/icons-material/AutoMode";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import TouchAppIcon from "@mui/icons-material/TouchApp";
import {
	Accordion,
	AccordionDetails,
	AccordionSummary,
	Box,
	Chip,
	IconButton,
	Paper,
	Tab,
	Tabs,
	Tooltip,
	Typography,
} from "@mui/material";
import type { GridValidRowModel } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useContext, useMemo, useState } from "react";
import DeliveryBurnupChart from "../../../../../components/Common/Charts/DeliveryBurnupChart";
import DeliveryFeverChart from "../../../../../components/Common/Charts/DeliveryFeverChart";
import DeliveryPredictabilityChart from "../../../../../components/Common/Charts/DeliveryPredictabilityChart";
import EnlargeableChart from "../../../../../components/Common/Charts/EnlargeableChart";
import type { DataGridColumn } from "../../../../../components/Common/DataGrid/types";
import {
	createForecastsColumn,
	createStateColumn,
} from "../../../../../components/Common/FeatureListDataGrid/columns";
import FeatureListDataGrid from "../../../../../components/Common/FeatureListDataGrid/FeatureListDataGrid";
import FeatureProgressIndicator from "../../../../../components/Common/FeatureListDataGrid/FeatureProgressIndicator";
import FeatureName from "../../../../../components/Common/FeatureName/FeatureName";
import { ForecastLevel } from "../../../../../components/Common/Forecasts/ForecastLevel";
import { INSUFFICIENT_FORECAST_DATA_SHORT } from "../../../../../components/Common/Forecasts/InsufficientForecastDataIndicator";
import ProgressIndicator from "../../../../../components/Common/ProgressIndicator/ProgressIndicator";
import StyledLink from "../../../../../components/Common/StyledLink/StyledLink";
import WorkItemsDialog from "../../../../../components/Common/WorkItemsDialog/WorkItemsDialog";
import type { Delivery } from "../../../../../models/Delivery";
import type { DeliveryMetricsHistory } from "../../../../../models/Delivery/DeliveryMetricsHistory";
import type { IEntityReference } from "../../../../../models/EntityReference";
import type { IFeature } from "../../../../../models/Feature";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import type { IWorkItem } from "../../../../../models/WorkItem";
import { DeliverySelectionMode } from "../../../../../models/WorkItemRules";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../../../services/TerminologyContext";
import { getWorkItemName } from "../../../../../utils/featureName";
import { formatLikelihood } from "../../../../../utils/forecast/formatLikelihood";
import { isForecastDataInsufficient } from "../../../../../utils/forecast/isForecastDataInsufficient";

interface DeliverySectionProps {
	delivery: Delivery;
	features: IFeature[];
	isExpanded: boolean;
	isLoadingFeatures: boolean;
	onToggleExpanded: (deliveryId: number) => void;
	onDelete: (delivery: Delivery) => void;
	onEdit: (delivery: Delivery) => void;
	teams: IEntityReference[];
	canEdit?: boolean;
}

const DeliverySection: React.FC<DeliverySectionProps> = ({
	delivery,
	features,
	isExpanded,
	isLoadingFeatures,
	onToggleExpanded,
	onDelete,
	onEdit,
	teams,
	canEdit = true,
}) => {
	const { featureService, deliveryService } = useContext(ApiServiceContext);

	const [selectedFeature, setSelectedFeature] = useState<IFeature | null>(null);
	const [featureWorkItems, setFeatureWorkItems] = useState<IWorkItem[]>([]);
	const [isWorkItemsDialogOpen, setIsWorkItemsDialogOpen] = useState(false);

	const [activeTab, setActiveTab] = useState<"workItems" | "metrics">(
		"workItems",
	);
	const [metricsHistory, setMetricsHistory] =
		useState<DeliveryMetricsHistory | null>(null);
	const [isLoadingMetrics, setIsLoadingMetrics] = useState(false);

	const handleTabChange = useCallback(
		(_event: React.SyntheticEvent, nextTab: "workItems" | "metrics") => {
			setActiveTab(nextTab);
			if (
				nextTab !== "metrics" ||
				metricsHistory !== null ||
				isLoadingMetrics
			) {
				return;
			}
			setIsLoadingMetrics(true);
			deliveryService
				.getMetricsHistory(delivery.id)
				.then(setMetricsHistory)
				.finally(() => setIsLoadingMetrics(false));
		},
		[deliveryService, delivery.id, metricsHistory, isLoadingMetrics],
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

	const isRuleBased =
		delivery.selectionMode === DeliverySelectionMode.RuleBased ||
		(delivery.selectionMode as unknown as string) === "RuleBased";

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
				headerName: "Likelihood",
				minWidth: 100,
				flex: 0.3,
				sortable: false,
				renderCell: ({ row }) =>
					delivery.featureLikelihoods
						.filter((fl) => fl.featureId === row.id)
						.map((fl) => (
							<Chip
								key={fl.featureId}
								label={
									isForecastDataInsufficient({
										hasRemainingWork: row.getRemainingWorkForFeature() > 0,
										hasSufficientData: fl.hasSufficientData,
									})
										? INSUFFICIENT_FORECAST_DATA_SHORT
										: formatLikelihood(fl.likelihoodPercentage, {
												hasRemainingWork: row.getRemainingWorkForFeature() > 0,
												precision: "round",
											})
								}
								size="small"
								sx={{
									bgcolor: new ForecastLevel(fl.likelihoodPercentage).color,
									color: "#fff",
									fontWeight: "bold",
								}}
							/>
						)),
			},
			createStateColumn(),
		],
		[featureTerm, delivery, teams, handleShowFeatureDetails],
	);

	const forecastLevel = new ForecastLevel(delivery.likelihoodPercentage);

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
							pr: 8,
						}}
					>
						{canEdit && (
							<IconButton
								size="small"
								onClick={(e) => {
									e.stopPropagation();
									onEdit(delivery);
								}}
								aria-label="edit"
								sx={{
									position: "absolute",
									top: "50%",
									transform: "translateY(-50%)",
									right: 48,
									zIndex: 1,
									bgcolor: "background.paper",
									"&:hover": {
										bgcolor: "primary.light",
									},
								}}
							>
								<EditIcon />
							</IconButton>
						)}
						{canEdit && (
							<IconButton
								size="small"
								onClick={(e) => {
									e.stopPropagation();
									onDelete(delivery);
								}}
								aria-label="delete"
								sx={{
									position: "absolute",
									top: "50%",
									transform: "translateY(-50%)",
									right: 8,
									zIndex: 1,
									bgcolor: "background.paper",
									"&:hover": {
										bgcolor: "error.light",
									},
								}}
							>
								<DeleteIcon />
							</IconButton>
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
										<Tooltip
											title={
												isRuleBased
													? "Rule-Based: Features automatically update based on rules"
													: "Manual: Features are fixed"
											}
										>
											<Box sx={{ display: "flex", alignItems: "center" }}>
												{isRuleBased ? (
													<AutoModeIcon
														fontSize="small"
														sx={{ color: "text.secondary" }}
													/>
												) : (
													<TouchAppIcon
														fontSize="small"
														sx={{ color: "text.secondary" }}
													/>
												)}
											</Box>
										</Tooltip>
										<Typography variant="body2" color="text.secondary">
											Delivery Date: {delivery.getFormattedDate()}
										</Typography>
										<Chip
											label={
												isForecastDataInsufficient({
													hasRemainingWork: delivery.remainingWork > 0,
													hasSufficientData: delivery.hasSufficientData,
												})
													? INSUFFICIENT_FORECAST_DATA_SHORT
													: `Likelihood: ${formatLikelihood(
															delivery.likelihoodPercentage,
															{
																hasRemainingWork: delivery.remainingWork > 0,
																precision: "round",
															},
														)}`
											}
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
							<Tab label="Metrics" value="metrics" />
						</Tabs>
						{activeTab === "workItems" && (
							<WorkItemsTab
								isLoadingFeatures={isLoadingFeatures}
								features={features}
								columns={columns}
								deliveryId={delivery.id}
								featuresTerm={featuresTerm}
								deliveryTerm={deliveryTerm}
							/>
						)}
						{activeTab === "metrics" && (
							<MetricsTab
								isLoading={isLoadingMetrics}
								history={metricsHistory}
							/>
						)}
					</AccordionDetails>
				</Accordion>
			</Box>
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

interface WorkItemsTabProps {
	isLoadingFeatures: boolean;
	features: IFeature[];
	columns: DataGridColumn<IFeature & GridValidRowModel>[];
	deliveryId: number;
	featuresTerm: string;
	deliveryTerm: string;
}

const WorkItemsTab: React.FC<WorkItemsTabProps> = ({
	isLoadingFeatures,
	features,
	columns,
	deliveryId,
	featuresTerm,
	deliveryTerm,
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
			/>
		</Box>
	);
};

interface MetricsTabProps {
	isLoading: boolean;
	history: DeliveryMetricsHistory | null;
}

const MetricsTab: React.FC<MetricsTabProps> = ({ isLoading, history }) => {
	if (isLoading || history === null) {
		return (
			<Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
				Loading metrics...
			</Typography>
		);
	}

	return (
		<Box
			sx={{
				mx: 2,
				mb: 2,
				mt: 2,
				display: "grid",
				gap: 2,
				gridTemplateColumns: { xs: "1fr", lg: "1fr 1fr" },
			}}
		>
			<EnlargeableChart
				ariaLabel="Delivery Burnup"
				render={(height) => (
					<DeliveryBurnupChart history={history} height={height} />
				)}
			/>
			<EnlargeableChart
				ariaLabel="Delivery Predictability"
				render={(height) => (
					<DeliveryPredictabilityChart history={history} height={height} />
				)}
			/>
			<Box sx={{ gridColumn: { lg: "1 / -1" } }}>
				<EnlargeableChart
					ariaLabel="Delivery Progress"
					render={(height) => (
						<DeliveryFeverChart history={history} height={height} />
					)}
				/>
			</Box>
		</Box>
	);
};

export default DeliverySection;
