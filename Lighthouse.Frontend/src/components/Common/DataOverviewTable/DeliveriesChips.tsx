import { Box, Chip, Typography } from "@mui/material";
import type React from "react";
import { useContext, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import type { Delivery } from "../../../models/Delivery";
import { TERMINOLOGY_KEYS } from "../../../models/TerminologyKeys";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";
import { useTerminology } from "../../../services/TerminologyContext";
import { ForecastLevel } from "../Forecasts/ForecastLevel";

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
			} catch (_error) {
				// Silently fail - show empty state
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

				return (
					<Link
						key={delivery.id}
						to={`/portfolios/${delivery.portfolioId}/deliveries`}
						style={{ textDecoration: "none" }}
					>
						<Chip
							label={`${delivery.name} | ${delivery.getFeatureCount()} ${featuresTerm} | Likelihood: ${Math.round(delivery.likelihoodPercentage)}%`}
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
					</Link>
				);
			})}
		</Box>
	);
};
