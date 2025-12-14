import DeleteIcon from "@mui/icons-material/Delete";
import {
	Box,
	Button,
	IconButton,
	List,
	ListItem,
	ListItemText,
	Typography,
} from "@mui/material";
import type React from "react";
import { useCallback, useContext, useEffect, useState } from "react";
import DeleteConfirmationDialog from "../../../components/Common/DeleteConfirmationDialog/DeleteConfirmationDialog";
import { useErrorSnackbar } from "../../../components/Common/SnackbarErrorHandler/SnackbarErrorHandler";
import type { Delivery } from "../../../models/Delivery";
import type { Portfolio } from "../../../models/Portfolio/Portfolio";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface PortfolioDeliveryViewProps {
	project: Portfolio;
}

const PortfolioDeliveryView: React.FC<PortfolioDeliveryViewProps> = ({
	project,
}) => {
	const [deliveries, setDeliveries] = useState<Delivery[]>([]);
	const [isLoading, setIsLoading] = useState(false);
	const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
	const [deliveryToDelete, setDeliveryToDelete] = useState<Delivery | null>(
		null,
	);

	const { deliveryService } = useContext(ApiServiceContext);
	const { showError } = useErrorSnackbar();

	const fetchDeliveries = useCallback(async () => {
		setIsLoading(true);
		try {
			const portfolioDeliveries = await deliveryService.getByPortfolio(
				project.id,
			);
			setDeliveries(portfolioDeliveries);
		} catch (_error) {
			showError("Failed to fetch deliveries");
		} finally {
			setIsLoading(false);
		}
	}, [deliveryService, project.id, showError]);

	const handleAddDelivery = async () => {
		try {
			// Calculate date 4 weeks from now
			const fourWeeksFromNow = new Date();
			fourWeeksFromNow.setDate(fourWeeksFromNow.getDate() + 28);

			// Get all features (since we can't filter by state with current data structure)
			const featureIds = project.features?.map((feature) => feature.id) || [];

			await deliveryService.create(
				project.id,
				"Next Delivery",
				fourWeeksFromNow,
				featureIds,
			);

			// Refresh deliveries list
			await fetchDeliveries();
		} catch (_error) {
			showError("Failed to create delivery");
		}
	};

	const handleDeleteDelivery = (delivery: Delivery) => {
		setDeliveryToDelete(delivery);
		setDeleteDialogOpen(true);
	};

	const handleDeleteConfirmation = async (confirmed: boolean) => {
		if (confirmed && deliveryToDelete) {
			try {
				await deliveryService.delete(deliveryToDelete.id);
				// Refresh deliveries list
				await fetchDeliveries();
			} catch (_error) {
				showError("Failed to delete delivery");
			}
		}

		setDeleteDialogOpen(false);
		setDeliveryToDelete(null);
	};

	useEffect(() => {
		fetchDeliveries();
	}, [fetchDeliveries]);

	if (isLoading) {
		return <Typography>Loading deliveries...</Typography>;
	}

	return (
		<Box>
			<Box
				display="flex"
				justifyContent="space-between"
				alignItems="center"
				mb={2}
			>
				<Typography variant="h5">Deliveries</Typography>
				<Button variant="contained" onClick={handleAddDelivery}>
					Add Next Delivery
				</Button>
			</Box>

			{deliveries.length === 0 ? (
				<Typography variant="body1" color="text.secondary">
					No deliveries found. Click "Add Next Delivery" to create one.
				</Typography>
			) : (
				<List>
					{deliveries.map((delivery) => (
						<ListItem
							key={delivery.id}
							divider
							secondaryAction={
								<IconButton
									edge="end"
									aria-label="delete"
									onClick={() => handleDeleteDelivery(delivery)}
								>
									<DeleteIcon />
								</IconButton>
							}
						>
							<ListItemText
								primary={delivery.name}
								secondary={
									<>
										<Typography component="span" variant="body2">
											Date: {new Date(delivery.date).toLocaleDateString()}
										</Typography>
										<br />
										<Typography component="span" variant="body2">
											Features: {delivery.features?.length || 0}
										</Typography>
									</>
								}
							/>
						</ListItem>
					))}
				</List>
			)}
			{deliveryToDelete && (
				<DeleteConfirmationDialog
					open={deleteDialogOpen}
					itemName={deliveryToDelete.name}
					onClose={handleDeleteConfirmation}
				/>
			)}
		</Box>
	);
};

export default PortfolioDeliveryView;
