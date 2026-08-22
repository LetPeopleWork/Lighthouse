import { Box, Typography } from "@mui/material";
import type React from "react";
import { useCallback, useContext, useState } from "react";
import DeliveryBurnupChart from "../../../../../components/Common/Charts/DeliveryBurnupChart";
import DeliveryEpicSizeChart from "../../../../../components/Common/Charts/DeliveryEpicSizeChart";
import DeliveryFeverChart from "../../../../../components/Common/Charts/DeliveryFeverChart";
import DeliveryPredictabilityChart from "../../../../../components/Common/Charts/DeliveryPredictabilityChart";
import EnlargeableChart from "../../../../../components/Common/Charts/EnlargeableChart";
import type { DeliveryMetricsHistory } from "../../../../../models/Delivery/DeliveryMetricsHistory";
import { ApiServiceContext } from "../../../../../services/Api/ApiServiceContext";

export const MINIMUM_METRIC_SNAPSHOTS = 3;

export const METRICS_GRID_COLUMNS = { xs: "1fr", lg: "1fr 1fr" };

export function metricsUnavailableReason(snapshotCount: number): string {
	return `Metrics need at least ${MINIMUM_METRIC_SNAPSHOTS} daily records to chart trends (have ${snapshotCount}).`;
}

/**
 * The daily history, fetched the first time somebody asks for it and kept thereafter. A Delivery
 * nobody opens the Metrics tab on costs no request, and one opened repeatedly costs one.
 */
export function useLazyMetricsHistory(deliveryId: number) {
	const { deliveryService } = useContext(ApiServiceContext);
	const [history, setHistory] = useState<DeliveryMetricsHistory | null>(null);
	const [isLoading, setIsLoading] = useState(false);

	const load = useCallback(() => {
		if (history !== null || isLoading) {
			return;
		}

		setIsLoading(true);
		deliveryService
			.getMetricsHistory(deliveryId)
			.then(setHistory)
			.finally(() => setIsLoading(false));
	}, [deliveryService, deliveryId, history, isLoading]);

	return { history, isLoading, load };
}

interface DeliveryMetricsTabProps {
	isLoading: boolean;
	history: DeliveryMetricsHistory | null;
	featuresTerm: string;
}

const DeliveryMetricsTab: React.FC<DeliveryMetricsTabProps> = ({
	isLoading,
	history,
	featuresTerm,
}) => {
	if (isLoading || history === null) {
		return (
			<Typography variant="body2" color="text.secondary" sx={{ p: 2 }}>
				Loading metrics...
			</Typography>
		);
	}

	return (
		<Box
			data-testid="delivery-metrics-grid"
			sx={{
				mx: 2,
				mb: 2,
				mt: 2,
				display: "grid",
				gap: 2,
				gridTemplateColumns: METRICS_GRID_COLUMNS,
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
			<EnlargeableChart
				ariaLabel={`${featuresTerm} over Time`}
				render={(height) => (
					<DeliveryEpicSizeChart
						history={history}
						featuresTerm={featuresTerm}
						height={height}
					/>
				)}
			/>
			<EnlargeableChart
				ariaLabel="Delivery Progress"
				render={(height) => (
					<DeliveryFeverChart history={history} height={height} />
				)}
			/>
		</Box>
	);
};

export default DeliveryMetricsTab;
