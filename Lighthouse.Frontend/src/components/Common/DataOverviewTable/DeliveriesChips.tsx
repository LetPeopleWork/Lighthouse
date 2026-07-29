import { Box, Chip, Tooltip, Typography } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import { Link } from "react-router";
import type { Delivery } from "../../../models/Delivery";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import {
	CANNOT_FORECAST_SHORT,
	cannotBeForecast,
	cannotForecastReason,
} from "../../../utils/forecast/cannotForecast";
import { formatLikelihood } from "../../../utils/forecast/formatLikelihood";
import { isForecastDataInsufficient } from "../../../utils/forecast/isForecastDataInsufficient";
import { jointLikelihoodLabel } from "../../../utils/forecast/jointLikelihoodLabel";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import { INSUFFICIENT_FORECAST_DATA_SHORT } from "../Forecasts/InsufficientForecastDataIndicator";

export interface DeliveriesChipsProps {
	portfolioId: number;
}

export const DeliveriesChips: React.FC<DeliveriesChipsProps> = ({
	portfolioId,
}) => {
	const { deliveryService } = useContext(ApiServiceContext);
	const [deliveries, setDeliveries] = useState<Delivery[]>([]);
	const { getTerm } = useTerminology();

	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const deliveriesTerm = getTerm(TERMINOLOGY_KEYS.DELIVERIES);

	useEffect(() => {
		const fetchDeliveries = async () => {
			try {
				const fetchedDeliveries =
					await deliveryService.getByPortfolio(portfolioId);
				setDeliveries(fetchedDeliveries);
			} catch (error) {
				console.error("Error fetching deliveries:", error);
				setDeliveries([]);
			}
		};

		if (portfolioId) {
			fetchDeliveries();
		}
	}, [portfolioId, deliveryService]);

	if (deliveries.length === 0) {
		return (
			<Typography variant="body2" color="text.secondary">
				No {deliveriesTerm}
			</Typography>
		);
	}

	return (
		<Box sx={{ display: "flex", flexDirection: "column", gap: 0.5 }}>
			{deliveries.map((delivery) => {
				const forecastLevel = new ForecastLevel(delivery.likelihoodPercentage);
				const teamsWithoutForecast = delivery.teamsWithoutForecast ?? [];
				const isUnforecastable = cannotBeForecast({ teamsWithoutForecast });

				let forecastSummary: string;
				if (isUnforecastable || delivery.likelihoodPercentage === null) {
					forecastSummary = CANNOT_FORECAST_SHORT;
				} else if (
					isForecastDataInsufficient({
						hasRemainingWork: delivery.remainingWork > 0,
						hasSufficientData: delivery.hasSufficientData,
					})
				) {
					forecastSummary = INSUFFICIENT_FORECAST_DATA_SHORT;
				} else {
					// Same joint framing as the delivery header (ADR-113 D1, ruling R1). No date: this
					// surface renders none, and no count either - the segment before it already carries
					// the scope, and it has to stay there for the two states that name no term at all.
					forecastSummary = jointLikelihoodLabel({
						term: featuresTerm,
						value: formatLikelihood(delivery.likelihoodPercentage, {
							hasRemainingWork: delivery.remainingWork > 0,
							precision: "round",
						}),
					});
				}

				const chip = (
					<Chip
						label={`${delivery.name} | ${delivery.getFeatureCount()} ${featuresTerm} | ${forecastSummary}`}
						size="small"
						sx={{
							bgcolor: forecastLevel.color,
							color: "#fff",
							fontWeight: "bold",
							cursor: "pointer",
							"&:hover": {
								opacity: 0.8,
							},
						}}
					/>
				);

				return (
					<Link
						key={delivery.id}
						to={`/portfolios/${delivery.portfolioId}/deliveries`}
						style={{ textDecoration: "none" }}
					>
						{isUnforecastable ? (
							<Tooltip title={cannotForecastReason(teamsWithoutForecast)}>
								<span>{chip}</span>
							</Tooltip>
						) : (
							chip
						)}
					</Link>
				);
			})}
		</Box>
	);
};
