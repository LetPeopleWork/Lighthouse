import DeleteIcon from "@mui/icons-material/Delete";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import {
	Accordion,
	AccordionDetails,
	AccordionSummary,
	Box,
	Chip,
	IconButton,
	Paper,
	Typography,
} from "@mui/material";
import type React from "react";
import { ForecastLevel } from "../../../../../components/Common/Forecasts/ForecastLevel";
import ProgressIndicator from "../../../../../components/Common/ProgressIndicator/ProgressIndicator";
import type { ArchivedDelivery } from "../../../../../models/Delivery/ArchivedDelivery";
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

interface ArchivedDeliveriesSectionProps {
	archivedDeliveries: ArchivedDelivery[];
	canEdit: boolean;
	onDelete: (delivery: ArchivedDelivery) => void;
}

const ArchivedDeliveriesSection: React.FC<ArchivedDeliveriesSectionProps> = ({
	archivedDeliveries,
	canEdit,
	onDelete,
}) => {
	const { getTerm } = useTerminology();
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);
	const deliveriesTerm = getTerm(TERMINOLOGY_KEYS.DELIVERIES);
	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const workItemsTerm = getTerm(TERMINOLOGY_KEYS.WORK_ITEMS);

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
							deliveryTerm={deliveryTerm}
							featuresTerm={featuresTerm}
							workItemsTerm={workItemsTerm}
						/>
					))}
				</AccordionDetails>
			</Accordion>
		</Paper>
	);
};

interface ArchivedDeliveryRowProps {
	archived: ArchivedDelivery;
	canEdit: boolean;
	onDelete: (delivery: ArchivedDelivery) => void;
	deliveryTerm: string;
	featuresTerm: string;
	workItemsTerm: string;
}

/**
 * Everything here is read straight off the record written when the Delivery closed. Nothing on this
 * row is worked out from a Feature, because the Features have moved on since and the whole point of
 * the row is what the Delivery said on its last day.
 */
const ArchivedDeliveryRow: React.FC<ArchivedDeliveryRowProps> = ({
	archived,
	canEdit,
	onDelete,
	deliveryTerm,
	featuresTerm,
	workItemsTerm,
}) => {
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
		<Box
			sx={{
				display: "flex",
				alignItems: "center",
				gap: 4,
				py: 1.5,
				borderBottom: 1,
				borderColor: "divider",
				"&:last-of-type": { borderBottom: 0 },
			}}
		>
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
					<Typography variant="body2" color="text.secondary">
						{deliveryTerm} Date: {archived.getFormattedDate()}
					</Typography>
					<Typography variant="body2" color="text.secondary">
						Archived: {archived.getFormattedArchivedOn()}
					</Typography>
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
			{canEdit && (
				<IconButton
					size="small"
					onClick={() => onDelete(archived)}
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
			)}
		</Box>
	);
};

export default ArchivedDeliveriesSection;
