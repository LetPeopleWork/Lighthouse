import { Chip, Tooltip } from "@mui/material";
import { useQuery } from "@tanstack/react-query";
import type React from "react";
import { useContext } from "react";
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
import { INSUFFICIENT_FORECAST_DATA_SHORT } from "../../../utils/forecast/insufficientForecastData";
import { isForecastDataInsufficient } from "../../../utils/forecast/isForecastDataInsufficient";
import { jointLikelihoodLabel } from "../../../utils/forecast/jointLikelihoodLabel";
import { ForecastLevel } from "../Forecasts/ForecastLevel";
import { OverviewCellChips, overviewCellStatusOf } from "./OverviewCellChips";

export interface DeliveriesChipsProps {
	portfolioId: number;
}

const deliveriesPathOf = (portfolioId: number): string =>
	`/portfolios/${portfolioId}/deliveries`;

const forecastSummaryOf = (
	delivery: Delivery,
	featuresTerm: string,
	isUnforecastable: boolean,
): string => {
	if (isUnforecastable || delivery.likelihoodPercentage === null) {
		return CANNOT_FORECAST_SHORT;
	}
	const hasRemainingWork = delivery.remainingWork > 0;
	if (
		isForecastDataInsufficient({
			hasRemainingWork,
			hasSufficientData: delivery.hasSufficientData,
		})
	) {
		return INSUFFICIENT_FORECAST_DATA_SHORT;
	}
	// The same joint framing as the Delivery's own header. No date, since this surface shows none, and no
	// count, because the segment before already carries the scope.
	return jointLikelihoodLabel({
		term: featuresTerm,
		value: formatLikelihood(delivery.likelihoodPercentage, {
			hasRemainingWork,
			precision: "round",
		}),
	});
};

const DeliveryChip: React.FC<{
	readonly delivery: Delivery;
	readonly featuresTerm: string;
}> = ({ delivery, featuresTerm }) => {
	const forecastLevel = new ForecastLevel(delivery.likelihoodPercentage);
	const teamsWithoutForecast = delivery.teamsWithoutForecast ?? [];
	const isUnforecastable = cannotBeForecast({ teamsWithoutForecast });
	const forecastSummary = forecastSummaryOf(
		delivery,
		featuresTerm,
		isUnforecastable,
	);

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
			to={deliveriesPathOf(delivery.portfolioId)}
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
};

export const DeliveriesChips: React.FC<DeliveriesChipsProps> = ({
	portfolioId,
}) => {
	const { deliveryService } = useContext(ApiServiceContext);
	const { getTerm } = useTerminology();

	const featuresTerm = getTerm(TERMINOLOGY_KEYS.FEATURES);
	const deliveriesTerm = getTerm(TERMINOLOGY_KEYS.DELIVERIES);

	const deliveries = useQuery({
		queryKey: ["deliveries", "portfolio", portfolioId],
		queryFn: () => deliveryService.getByPortfolio(portfolioId),
		select: (byPortfolio) => byPortfolio.active,
		enabled: portfolioId > 0,
		// The Deliveries tab changes them without passing through this cache, so every visit reads them again.
		staleTime: 0,
	});

	return (
		<OverviewCellChips
			status={overviewCellStatusOf(deliveries)}
			items={deliveries.data}
			renderItem={(delivery) => (
				<DeliveryChip
					key={delivery.id}
					delivery={delivery}
					featuresTerm={featuresTerm}
				/>
			)}
			moreTo={deliveriesPathOf(portfolioId)}
			emptyText={`No ${deliveriesTerm}`}
			failureText={`Couldn't load ${deliveriesTerm}`}
		/>
	);
};
