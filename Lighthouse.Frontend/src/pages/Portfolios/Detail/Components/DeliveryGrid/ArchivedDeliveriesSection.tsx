import AutoModeIcon from "@mui/icons-material/AutoMode";
import DeleteIcon from "@mui/icons-material/Delete";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import TouchAppIcon from "@mui/icons-material/TouchApp";
import UnarchiveIcon from "@mui/icons-material/Unarchive";
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
import type { GridRowId } from "@mui/x-data-grid";
import type React from "react";
import { useCallback, useMemo, useState } from "react";
import type { DataGridExportTable } from "../../../../../components/Common/DataGrid/types";
import { ForecastLevel } from "../../../../../components/Common/Forecasts/ForecastLevel";
import ProgressIndicator from "../../../../../components/Common/ProgressIndicator/ProgressIndicator";
import type { ArchivedDelivery } from "../../../../../models/Delivery/ArchivedDelivery";
import type { FeatureMetric } from "../../../../../models/Delivery/DeliveryMetricsHistory";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../../services/TerminologyContext";
import {
	CANNOT_FORECAST_SHORT,
	cannotBeForecast,
	cannotForecastReason,
} from "../../../../../utils/forecast/cannotForecast";
import { formatLikelihood } from "../../../../../utils/forecast/formatLikelihood";
import { INSUFFICIENT_FORECAST_DATA_SHORT } from "../../../../../utils/forecast/insufficientForecastData";
import { isForecastDataInsufficient } from "../../../../../utils/forecast/isForecastDataInsufficient";
import { jointLikelihoodLabel } from "../../../../../utils/forecast/jointLikelihoodLabel";
import ArchivedFeatureGrid from "./ArchivedFeatureGrid";
import DeliveryMetricsTab, {
	MINIMUM_METRIC_SNAPSHOTS,
	metricsUnavailableReason,
	useLazyMetricsHistory,
} from "./DeliveryMetricsTab";
import DeliveryNotesPanel from "./DeliveryNotesPanel";
import { buildArchivedDeliveryExportTable } from "./deliveryExportTable";

interface ArchivedDeliveriesSectionProps {
	archivedDeliveries: ArchivedDelivery[];
	canEdit: boolean;
	onDelete: (delivery: ArchivedDelivery) => void;
	onUnarchive: (delivery: ArchivedDelivery) => void;
}

const ArchivedDeliveriesSection: React.FC<ArchivedDeliveriesSectionProps> = ({
	archivedDeliveries,
	canEdit,
	onDelete,
	onUnarchive,
}) => {
	const { getTerm } = useTerminology();
	const terms = useMemo(
		() => ({
			deliveryTerm: getTerm(TERMINOLOGY_KEYS.DELIVERY),
			featureTerm: getTerm(TERMINOLOGY_KEYS.FEATURE),
			featuresTerm: getTerm(TERMINOLOGY_KEYS.FEATURES),
			workItemsTerm: getTerm(TERMINOLOGY_KEYS.WORK_ITEMS),
			portfolioTerm: getTerm(TERMINOLOGY_KEYS.PORTFOLIO),
			teamTerm: getTerm(TERMINOLOGY_KEYS.TEAM),
		}),
		[getTerm],
	);
	const deliveriesTerm = getTerm(TERMINOLOGY_KEYS.DELIVERIES);

	if (archivedDeliveries.length === 0) {
		return null;
	}

	return (
		<Paper elevation={1} sx={{ mt: 2, overflow: "hidden" }}>
			{/* Nothing is rendered until the section is opened: a Portfolio can accumulate retired
			    Deliveries indefinitely, and none of them is what the reader came for. */}
			<Accordion
				sx={{ overflow: "hidden" }}
				slotProps={{ transition: { unmountOnExit: true } }}
			>
				<AccordionSummary expandIcon={<ExpandMoreIcon />}>
					<Typography variant="h6" component="h3">
						Archived {deliveriesTerm} ({archivedDeliveries.length})
					</Typography>
				</AccordionSummary>
				<AccordionDetails>
					{archivedDeliveries.map((archived) => (
						<ArchivedDeliveryRow
							key={archived.id}
							archived={archived}
							canEdit={canEdit}
							onDelete={onDelete}
							onUnarchive={onUnarchive}
							terms={terms}
						/>
					))}
				</AccordionDetails>
			</Accordion>
		</Paper>
	);
};

interface ArchivedRowTerms {
	deliveryTerm: string;
	featureTerm: string;
	featuresTerm: string;
	workItemsTerm: string;
	portfolioTerm: string;
	teamTerm: string;
}

interface ArchivedDeliveryRowProps {
	archived: ArchivedDelivery;
	canEdit: boolean;
	onDelete: (delivery: ArchivedDelivery) => void;
	onUnarchive: (delivery: ArchivedDelivery) => void;
	terms: ArchivedRowTerms;
}

const selectionSummary = (
	archived: ArchivedDelivery,
	featuresTerm: string,
): string => {
	if (!archived.isRuleBased) {
		return `Manual: ${featuresTerm} were fixed`;
	}

	if (archived.rules.length === 0) {
		return `Rule-Based: ${featuresTerm} were chosen by a rule`;
	}

	const conditions = archived.rules
		.map((rule) => `${rule.fieldKey} ${rule.operator} ${rule.value}`)
		.join(archived.mode === "or" ? " or " : " and ");

	return `Rule-Based: ${featuresTerm} were chosen by ${conditions}`;
};

/**
 * Everything here is read straight off the record written when the Delivery closed. Nothing on this
 * row is worked out from a Feature, because the Features have moved on since and the whole point of
 * the row is what the Delivery said on its last day.
 */
const ArchivedDeliveryRow: React.FC<ArchivedDeliveryRowProps> = ({
	archived,
	canEdit,
	onDelete,
	onUnarchive,
	terms,
}) => {
	const { deliveryTerm, featureTerm, featuresTerm, workItemsTerm } = terms;
	const [activeTab, setActiveTab] = useState<"workItems" | "metrics" | "notes">(
		"workItems",
	);
	const {
		history: metricsHistory,
		isLoading: isLoadingMetrics,
		hasFailed: metricsFailed,
		load: loadMetricsHistory,
	} = useLazyMetricsHistory(archived.id);

	const isMetricsTabDisabled =
		archived.metricSnapshotCount < MINIMUM_METRIC_SNAPSHOTS;

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

	// The grid hands back the references in the order the reader is looking at them, so the file is
	// sorted and filtered the way the screen is — the same arrangement a live Delivery exports by.
	const exportTable = useCallback(
		(orderedRowIds: GridRowId[]): DataGridExportTable => {
			const rowByReference = new Map(
				archived.featureBreakdown.map((row) => [row.referenceId, row]),
			);
			const ordered = orderedRowIds
				.map((rowId) => rowByReference.get(String(rowId)))
				.filter((row): row is FeatureMetric => row !== undefined);

			return buildArchivedDeliveryExportTable(archived, ordered, terms);
		},
		[archived, terms],
	);

	const teamsWithoutForecast = archived.teamsWithoutForecast;
	const forecastLevel = new ForecastLevel(archived.likelihoodPercentage);
	const deliveryCannotBeForecast =
		cannotBeForecast({ teamsWithoutForecast }) ||
		archived.likelihoodPercentage === null;
	const hasInsufficientData = isForecastDataInsufficient({
		hasRemainingWork: archived.remainingWork > 0,
		hasSufficientData: archived.hasSufficientData,
	});

	let likelihoodLabel: string;
	if (deliveryCannotBeForecast || archived.likelihoodPercentage === null) {
		likelihoodLabel = CANNOT_FORECAST_SHORT;
	} else if (hasInsufficientData) {
		likelihoodLabel = INSUFFICIENT_FORECAST_DATA_SHORT;
	} else {
		likelihoodLabel = jointLikelihoodLabel({
			term: featuresTerm,
			date: archived.getFormattedDate(),
			value: formatLikelihood(archived.likelihoodPercentage, {
				hasRemainingWork: archived.remainingWork > 0,
				precision: "round",
			}),
		});
	}

	return (
		<Accordion
			sx={{ overflow: "hidden" }}
			slotProps={{ transition: { unmountOnExit: true } }}
		>
			<AccordionSummary
				expandIcon={<ExpandMoreIcon />}
				sx={{
					"& .MuiAccordionSummary-content": { alignItems: "center", gap: 4 },
					pr: 12,
				}}
			>
				{canEdit && (
					<Box
						sx={{
							position: "absolute",
							top: "50%",
							transform: "translateY(-50%)",
							right: 48,
							zIndex: 1,
							display: "flex",
							alignItems: "center",
							gap: 0.5,
						}}
					>
						{/* Deliberately ungated: archiving is a premium capability, but a lapsed licence
						    must never leave somebody unable to bring a Delivery back. */}
						<Tooltip title={`Bring this ${deliveryTerm} back`}>
							<IconButton
								size="small"
								onClick={(event) => {
									event.stopPropagation();
									onUnarchive(archived);
								}}
								aria-label="unarchive"
								sx={{
									bgcolor: "background.paper",
									"&:hover": { bgcolor: "primary.light" },
								}}
							>
								<UnarchiveIcon />
							</IconButton>
						</Tooltip>
						<IconButton
							size="small"
							onClick={(event) => {
								event.stopPropagation();
								onDelete(archived);
							}}
							aria-label="delete"
							sx={{
								bgcolor: "background.paper",
								"&:hover": { bgcolor: "error.light" },
							}}
						>
							<DeleteIcon />
						</IconButton>
					</Box>
				)}
				<Box sx={{ display: "flex", flexDirection: "column", gap: 1, flex: 1 }}>
					<Box
						sx={{
							display: "flex",
							alignItems: "center",
							gap: 2,
							flexWrap: "wrap",
						}}
					>
						<Typography variant="subtitle1" component="h4">
							{archived.name}
						</Typography>
						<Tooltip title={selectionSummary(archived, featuresTerm)}>
							<Box sx={{ display: "flex", alignItems: "center" }}>
								{archived.isRuleBased ? (
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
							{deliveryTerm} Date: {archived.getFormattedDate()}
						</Typography>
						<Chip
							data-testid="archived-marker"
							label={`Archived: ${archived.getFormattedArchivedOn()}`}
							size="small"
							variant="outlined"
						/>
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
				</Box>
				<Box
					sx={{
						display: "flex",
						alignItems: "center",
						minWidth: 200,
						flex: 1,
					}}
				>
					<ProgressIndicator
						title={`${archived.totalWork} ${workItemsTerm}`}
						progressableItem={{
							remainingWork: archived.remainingWork,
							totalWork: archived.totalWork,
						}}
						showDetails={true}
					/>
				</Box>
			</AccordionSummary>
			<AccordionDetails sx={{ p: 0 }}>
				<Tabs
					value={activeTab}
					onChange={handleTabChange}
					aria-label={`archived ${deliveryTerm} view tabs`}
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
										? metricsUnavailableReason(archived.metricSnapshotCount)
										: ""
								}
							>
								<span style={{ pointerEvents: "auto" }}>Metrics</span>
							</Tooltip>
						}
					/>
					<Tab label="Notes" value="notes" />
				</Tabs>
				{activeTab === "workItems" && (
					<ArchivedFeatureGrid
						rows={archived.featureBreakdown}
						deliveryId={archived.id}
						featureTerm={featureTerm}
						featuresTerm={featuresTerm}
						exportFileName={archived.name}
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
					<DeliveryNotesPanel
						deliveryId={archived.id}
						canWrite={canEdit}
						isReadOnly={true}
					/>
				)}
			</AccordionDetails>
		</Accordion>
	);
};

export default ArchivedDeliveriesSection;
