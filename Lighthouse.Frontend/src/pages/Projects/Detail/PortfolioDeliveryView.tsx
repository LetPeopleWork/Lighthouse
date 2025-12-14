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
import type { Delivery } from "../../../models/Delivery";
import type { Portfolio } from "../../../models/Project/Portfolio";
import { ApiServiceContext } from "../../../services/Api/ApiServiceContext";

interface PortfolioDeliveryViewProps {
	project: Portfolio;
}

const PortfolioDeliveryView: React.FC<PortfolioDeliveryViewProps> = ({
	project,
}) => {
	const [deliveries, setDeliveries] = useState<Delivery[]>([]);
	const [isLoading, setIsLoading] = useState(false);

	const { deliveryService } = useContext(ApiServiceContext);

	const fetchDeliveries = useCallback(async () => {
		setIsLoading(true);
		try {
			const portfolioDeliveries = await deliveryService.getByPortfolio(
				project.id,
			);
			setDeliveries(portfolioDeliveries);
		} catch (error) {
			console.error("Failed to fetch deliveries:", error);
		} finally {
			setIsLoading(false);
		}
	}, [deliveryService, project.id]);

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
		} catch (error) {
			console.error("Failed to create delivery:", error);
		}
	};

	const handleDeleteDelivery = async (deliveryId: number) => {
		try {
			await deliveryService.delete(deliveryId);
			// Refresh deliveries list
			await fetchDeliveries();
		} catch (error) {
			console.error("Failed to delete delivery:", error);
		}
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
									onClick={() => handleDeleteDelivery(delivery.id)}
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
		</Box>
	);
};

export default PortfolioDeliveryView;
