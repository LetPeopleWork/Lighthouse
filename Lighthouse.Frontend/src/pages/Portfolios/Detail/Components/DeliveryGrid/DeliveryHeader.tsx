import AddIcon from "@mui/icons-material/Add";
import { Box, Button, Chip, Typography } from "@mui/material";
import type React from "react";
import { useMemo } from "react";
import { ForecastLevel } from "../../../../../components/Common/Forecasts/ForecastLevel";
import ProgressIndicator from "../../../../../components/Common/ProgressIndicator/ProgressIndicator";
import type { Delivery } from "../../../../../models/Delivery";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../../services/TerminologyContext";

interface DeliveryHeaderProps {
	onAddDelivery: () => void;
	deliveries: Delivery[];
}

export const DeliveryHeader: React.FC<DeliveryHeaderProps> = ({
	onAddDelivery,
	deliveries,
}) => {
	const { getTerm } = useTerminology();
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);
	const deliveriesTerm = getTerm(TERMINOLOGY_KEYS.DELIVERIES);

	// Calculate aggregate statistics
	const aggregateStats = useMemo(() => {
		if (deliveries.length === 0) {
			return {
				totalWork: 0,
				remainingWork: 0,
				minLikelihood: 0,
				avgLikelihood: 0,
			};
		}

		const totalWork = deliveries.reduce(
			(sum, delivery) => sum + delivery.totalWork,
			0,
		);
		const remainingWork = deliveries.reduce(
			(sum, delivery) => sum + delivery.remainingWork,
			0,
		);
		const minLikelihood = Math.min(
			...deliveries.map((d) => d.likelihoodPercentage),
		);
		const avgLikelihood =
			deliveries.reduce((sum, d) => sum + d.likelihoodPercentage, 0) /
			deliveries.length;

		return {
			totalWork,
			remainingWork,
			minLikelihood,
			avgLikelihood,
		};
	}, [deliveries]);

	const minForecastLevel = new ForecastLevel(aggregateStats.minLikelihood);
	const avgForecastLevel = new ForecastLevel(aggregateStats.avgLikelihood);

	return (
		<Box>
			<Box
				display="flex"
				justifyContent="space-between"
				alignItems="flex-start"
				mb={2}
			>
				<Box>
					<Typography variant="h5" sx={{ mb: 1 }}>
						{deliveriesTerm}
					</Typography>
					{deliveries.length > 0 && (
						<Box sx={{ display: "flex", gap: 1, flexWrap: "wrap" }}>
							<Chip
								label={`Min: ${Math.round(aggregateStats.minLikelihood)}%`}
								size="small"
								sx={{
									bgcolor: minForecastLevel.color,
									color: "#fff",
									fontWeight: "bold",
								}}
							/>
							<Chip
								label={`Avg: ${Math.round(aggregateStats.avgLikelihood)}%`}
								size="small"
								sx={{
									bgcolor: avgForecastLevel.color,
									color: "#fff",
									fontWeight: "bold",
								}}
							/>
							<Chip
								label={`${deliveries.length} ${deliveries.length === 1 ? deliveryTerm : deliveriesTerm}`}
								size="small"
								variant="outlined"
							/>
						</Box>
					)}
				</Box>
				<Button
					variant="outlined"
					startIcon={<AddIcon />}
					onClick={onAddDelivery}
				>
					Add {deliveryTerm}
				</Button>
			</Box>
			{deliveries.length > 0 && aggregateStats.totalWork > 0 && (
				<Box sx={{ mb: 2 }}>
					<ProgressIndicator
						title="Overall Portfolio Delivery Progress"
						progressableItem={{
							remainingWork: aggregateStats.remainingWork,
							totalWork: aggregateStats.totalWork,
						}}
						showDetails={true}
					/>
				</Box>
			)}
		</Box>
	);
};

export default DeliveryHeader;
