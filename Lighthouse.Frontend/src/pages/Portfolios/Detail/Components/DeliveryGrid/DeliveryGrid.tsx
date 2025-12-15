import DeleteIcon from "@mui/icons-material/Delete";
import { Chip, IconButton, useTheme } from "@mui/material";
import type React from "react";
import { useMemo } from "react";
import DataGridBase from "../../../../../components/Common/DataGrid/DataGridBase";
import { ForecastLevel } from "../../../../../components/Common/Forecasts/ForecastLevel";
import type { Delivery } from "../../../../../models/Delivery";
import { TERMINOLOGY_KEYS } from "../../../../../models/TerminologyKeys";
import { useTerminology } from "../../../../../services/TerminologyContext";
import { hexToRgba } from "../../../../../utils/theme/colors";

interface DeliveryGridProps {
	deliveries: Delivery[];
	isLoading: boolean;
	onDelete: (delivery: Delivery) => void;
}

export const DeliveryGrid: React.FC<DeliveryGridProps> = ({
	deliveries,
	isLoading,
	onDelete,
}) => {
	const theme = useTheme();
	const { getTerm } = useTerminology();
	const deliveryTerm = getTerm(TERMINOLOGY_KEYS.DELIVERY);

	// Filter out past deliveries and sort by date (soonest first)
	const filteredDeliveries = useMemo(() => {
		const now = new Date();
		return deliveries
			.filter((delivery) => new Date(delivery.date) > now)
			.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
	}, [deliveries]);

	// Grid columns configuration
	const columns = useMemo(
		() => [
			{
				field: "name",
				headerName: deliveryTerm,
				flex: 1,
				minWidth: 200,
			},
			{
				field: "date",
				headerName: "Date",
				width: 120,
				renderCell: ({ value }: { value: unknown }) =>
					new Date(value as string).toLocaleDateString(),
			},
			{
				field: "likelihood",
				headerName: "Likelihood",
				width: 140,
				renderCell: ({ row }: { row: unknown }) => {
					const delivery = row as Delivery;
					const forecastLevel = new ForecastLevel(
						delivery.likelihoodPercentage,
					);
					return (
						<Chip
							label={`${delivery.likelihoodPercentage.toFixed(0)}%`}
							size="small"
							sx={{
								borderColor: forecastLevel.color,
								backgroundColor: hexToRgba(
									forecastLevel.color,
									theme.opacity.high,
								),
								color: theme.palette.text.primary,
								fontWeight: 500,
							}}
							variant="filled"
						/>
					);
				},
			},
			{
				field: "features",
				headerName: "Features",
				width: 100,
				renderCell: ({ row }: { row: unknown }) =>
					`${(row as Delivery).getFeatureCount()}`,
			},
			{
				field: "actions",
				headerName: "Actions",
				width: 80,
				hideable: false,
				renderCell: ({ row }: { row: unknown }) => (
					<IconButton
						size="small"
						onClick={() => onDelete(row as Delivery)}
						aria-label="delete"
					>
						<DeleteIcon />
					</IconButton>
				),
			},
		],
		[theme, onDelete, deliveryTerm],
	);

	return (
		<DataGridBase
			rows={filteredDeliveries}
			columns={columns}
			storageKey="portfolio-deliveries"
			loading={isLoading}
			initialSortModel={[{ field: "date", sort: "asc" }]}
			emptyStateMessage="No upcoming deliveries"
		/>
	);
};

export default DeliveryGrid;
